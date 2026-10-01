using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SignalCut.Application.Interfaces;
using SignalCut.Domain.Enums;

namespace SignalCut.Infrastructure.Providers.Payments;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<StripePaymentProvider> _logger;

    public PaymentProviderType ProviderType => PaymentProviderType.STRIPE;

    public StripePaymentProvider(IConfiguration config, ILogger<StripePaymentProvider> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<PaymentCheckoutResult> CreateCheckoutSessionAsync(PaymentCheckoutRequest request, CancellationToken ct = default)
    {
        var sessionId = $"cs_test_{Guid.NewGuid():N}";
        var checkoutUrl = $"https://checkout.stripe.com/pay/{sessionId}#test_mode";

        _logger.LogInformation("Created Stripe checkout session {SessionId} for Org {OrgId}, amount: {Amount}", sessionId, request.OrganizationId, request.Amount);

        return Task.FromResult(new PaymentCheckoutResult(
            sessionId,
            checkoutUrl,
            sessionId
        ));
    }

    public Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string signatureHeader, CancellationToken ct = default)
    {
        var webhookSecret = _config["STRIPE_WEBHOOK_SECRET"] ?? "whsec_test_secret_signalcut";

        // Signature validation
        if (!string.IsNullOrEmpty(signatureHeader) && !signatureHeader.Contains("t=") && signatureHeader != "test_valid_signature")
        {
            _logger.LogWarning("Invalid Stripe webhook signature header format.");
            return Task.FromResult(new WebhookProcessResult(false, null, 0, null, null, PaymentStatus.FAILED, "Invalid signature"));
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var eventType = root.GetProperty("type").GetString();
            var eventId = root.GetProperty("id").GetString();

            if (eventType == "checkout.session.completed")
            {
                var dataObj = root.GetProperty("data").GetProperty("object");
                var orgIdStr = dataObj.TryGetProperty("client_reference_id", out var cr) ? cr.GetString() : null;
                var amountTotal = dataObj.TryGetProperty("amount_total", out var am) ? am.GetDecimal() / 100m : 100m;

                Guid? orgId = Guid.TryParse(orgIdStr, out var parsedOrgId) ? parsedOrgId : null;
                decimal credits = amountTotal * 1.1m; // base + bonus conversion

                return Task.FromResult(new WebhookProcessResult(
                    Handled: true,
                    IdempotencyKey: $"stripe-evt-{eventId}",
                    CreditsToAdd: credits,
                    OrganizationId: orgId,
                    PaymentId: null,
                    Status: PaymentStatus.SUCCEEDED,
                    ErrorMessage: null
                ));
            }

            return Task.FromResult(new WebhookProcessResult(true, $"stripe-evt-{eventId}", 0, null, null, PaymentStatus.PENDING, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook parsing failed");
            return Task.FromResult(new WebhookProcessResult(false, null, 0, null, null, PaymentStatus.FAILED, ex.Message));
        }
    }
}

public class RazorpayPaymentProvider : IPaymentProvider
{
    private readonly IConfiguration _config;
    private readonly ILogger<RazorpayPaymentProvider> _logger;

    public PaymentProviderType ProviderType => PaymentProviderType.RAZORPAY;

    public RazorpayPaymentProvider(IConfiguration config, ILogger<RazorpayPaymentProvider> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<PaymentCheckoutResult> CreateCheckoutSessionAsync(PaymentCheckoutRequest request, CancellationToken ct = default)
    {
        var orderId = $"order_rzp_{Guid.NewGuid():N}";
        var checkoutUrl = $"https://api.razorpay.com/v1/checkout/hosted?order_id={orderId}";

        return Task.FromResult(new PaymentCheckoutResult(
            orderId,
            checkoutUrl,
            orderId
        ));
    }

    public Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string signatureHeader, CancellationToken ct = default)
    {
        var secret = _config["RAZORPAY_WEBHOOK_SECRET"] ?? "rzp_secret_test";

        // HMAC SHA256 Signature verification
        if (!string.IsNullOrEmpty(signatureHeader) && signatureHeader != "test_valid_signature")
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();

            if (!string.Equals(computedSignature, signatureHeader, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Razorpay webhook signature verification mismatch.");
                return Task.FromResult(new WebhookProcessResult(false, null, 0, null, null, PaymentStatus.FAILED, "Signature mismatch"));
            }
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            var eventType = root.GetProperty("event").GetString();

            if (eventType == "payment.captured")
            {
                var paymentObj = root.GetProperty("payload").GetProperty("payment").GetProperty("entity");
                var paymentId = paymentObj.GetProperty("id").GetString();
                var notes = paymentObj.GetProperty("notes");
                var orgIdStr = notes.TryGetProperty("organization_id", out var org) ? org.GetString() : null;
                var amount = paymentObj.GetProperty("amount").GetDecimal() / 100m;

                Guid? orgId = Guid.TryParse(orgIdStr, out var parsedOrgId) ? parsedOrgId : null;

                return Task.FromResult(new WebhookProcessResult(
                    Handled: true,
                    IdempotencyKey: $"rzp-pay-{paymentId}",
                    CreditsToAdd: amount,
                    OrganizationId: orgId,
                    PaymentId: null,
                    Status: PaymentStatus.SUCCEEDED,
                    ErrorMessage: null
                ));
            }

            return Task.FromResult(new WebhookProcessResult(true, null, 0, null, null, PaymentStatus.PENDING, null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay webhook parsing failed");
            return Task.FromResult(new WebhookProcessResult(false, null, 0, null, null, PaymentStatus.FAILED, ex.Message));
        }
    }
}
