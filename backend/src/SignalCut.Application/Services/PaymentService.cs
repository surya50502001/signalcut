using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Common;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;

namespace SignalCut.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;
    private readonly IEnumerable<IPaymentProvider> _paymentProviders;
    private readonly ICreditWalletService _creditWalletService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IApplicationDbContext context,
        IEnumerable<IPaymentProvider> paymentProviders,
        ICreditWalletService creditWalletService,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _paymentProviders = paymentProviders;
        _creditWalletService = creditWalletService;
        _logger = logger;
    }

    public List<CreditPackageDto> GetAvailablePackages()
    {
        return new List<CreditPackageDto>
        {
            new("pkg_starter", "Starter Pack", 100m, "INR", 100m, 0m, "Perfect for exploring topics and generating your first clips.", false),
            new("pkg_creator", "Creator Pro", 500m, "INR", 500m, 50m, "Most popular. Includes 50 bonus credits + 1080p vertical rendering.", true),
            new("pkg_agency", "Agency Scale", 1000m, "INR", 1000m, 200m, "Maximum value. 200 bonus credits + multi-platform publishing.", false)
        };
    }

    public async Task<PaymentCheckoutResult> InitiateCheckoutAsync(Guid organizationId, Guid userId, string packageId, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        var package = GetAvailablePackages().FirstOrDefault(p => p.Id == packageId);
        if (package == null)
        {
            throw new NotFoundException("CreditPackage", packageId);
        }

        var provider = _paymentProviders.FirstOrDefault(p => p.ProviderType == PaymentProviderType.STRIPE)
                       ?? _paymentProviders.FirstOrDefault();

        if (provider == null)
        {
            throw new Exception("No active payment provider registered.");
        }

        var totalCredits = package.Credits + package.BonusCredits;

        var payment = new Payment
        {
            OrganizationId = organizationId,
            UserId = userId,
            Amount = package.Price,
            Currency = package.Currency,
            CreditsPurchased = totalCredits,
            Provider = provider.ProviderType,
            Status = PaymentStatus.PENDING,
            IdempotencyKey = $"checkout-{organizationId}-{Guid.NewGuid()}",
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(new { packageId, package.Name })
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(ct);

        var checkoutReq = new PaymentCheckoutRequest(
            organizationId,
            userId,
            package.Price,
            package.Currency,
            totalCredits,
            successUrl,
            cancelUrl
        );

        var checkoutRes = await provider.CreateCheckoutSessionAsync(checkoutReq, ct);

        payment.ProviderOrderId = checkoutRes.ProviderOrderId;
        await _context.SaveChangesAsync(ct);

        return checkoutRes;
    }

    public async Task<bool> HandleWebhookAsync(string providerName, string payload, string signatureHeader, CancellationToken ct = default)
    {
        var provider = _paymentProviders.FirstOrDefault(p =>
            p.ProviderType.ToString().Equals(providerName, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            _logger.LogError("Payment provider {Provider} not found for webhook", providerName);
            return false;
        }

        // 1. Verify webhook signature and extract parsed transaction
        var result = await provider.ProcessWebhookAsync(payload, signatureHeader, ct);
        if (!result.Handled)
        {
            _logger.LogWarning("Webhook was not handled or failed verification. Error: {Err}", result.ErrorMessage);
            return false;
        }

        // 2. CRITICAL: Idempotency check to prevent double crediting
        if (!string.IsNullOrEmpty(result.IdempotencyKey))
        {
            var alreadyProcessed = await _context.CreditTransactions
                .AnyAsync(t => t.IdempotencyKey == result.IdempotencyKey, ct);

            if (alreadyProcessed)
            {
                _logger.LogInformation("Webhook idempotency key {Key} already processed. Acknowledged without double crediting.", result.IdempotencyKey);
                return true;
            }
        }

        if (result.OrganizationId.HasValue && result.CreditsToAdd > 0)
        {
            // Update payment entity if present
            if (result.PaymentId.HasValue)
            {
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Id == result.PaymentId.Value, ct);
                if (payment != null)
                {
                    payment.Status = result.Status;
                    payment.UpdatedAt = DateTime.UtcNow;
                }
            }

            // Record transaction and add credits into ledger
            await _creditWalletService.AddCreditsAsync(
                result.OrganizationId.Value,
                result.CreditsToAdd,
                CreditTransactionType.PURCHASE,
                result.PaymentId,
                $"Payment purchase of {result.CreditsToAdd} credits via {provider.ProviderType}",
                result.IdempotencyKey,
                ct
            );

            _logger.LogInformation("Successfully credited {Credits} credits to Org {OrgId} from verified webhook",
                result.CreditsToAdd, result.OrganizationId.Value);
        }

        return true;
    }
}
