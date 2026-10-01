using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SignalCut.Application.DTOs;
using SignalCut.Application.Interfaces;
using SignalCut.Application.Services;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;
using SignalCut.Infrastructure.Persistence;
using SignalCut.Infrastructure.Providers.Payments;
using Xunit;

namespace SignalCut.Tests;

public class PaymentWebhookIdempotencyTests
{
    private SignalCutDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SignalCutDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SignalCutDbContext(options);
    }

    [Fact]
    public async Task CriticalTest4_PaymentWebhookCannotDoubleCreditOnRetry()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var creditService = new CreditWalletService(context, NullLogger<CreditWalletService>.Instance);
        var orgId = Guid.NewGuid();

        // Initial wallet starts with 50 trial credits
        var initialWallet = await creditService.GetWalletAsync(orgId);
        initialWallet.Balance.Should().Be(50m);

        var paymentProvider = new StripePaymentProvider(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), NullLogger<StripePaymentProvider>.Instance);
        var paymentService = new PaymentService(context, new[] { paymentProvider }, creditService, NullLogger<PaymentService>.Instance);

        var webhookPayload = $@"{{
            ""id"": ""evt_charge_123456789"",
            ""type"": ""checkout.session.completed"",
            ""data"": {{
                ""object"": {{
                    ""client_reference_id"": ""{orgId}"",
                    ""amount_total"": 50000
                }}
            }}
        }}";

        // Act 1: Process initial webhook
        var result1 = await paymentService.HandleWebhookAsync("STRIPE", webhookPayload, "test_valid_signature");
        result1.Should().BeTrue();

        var walletAfterFirst = await creditService.GetWalletAsync(orgId);
        // 50000 cents = 500 currency * 1.1 = 550 credits + 50 initial = 600 credits
        walletAfterFirst.Balance.Should().Be(600m);

        // Act 2: Webhook replay / retry from provider with same event ID
        var result2 = await paymentService.HandleWebhookAsync("STRIPE", webhookPayload, "test_valid_signature");
        result2.Should().BeTrue();

        // Assert: Balance MUST remain 600, NOT incremented to 1150
        var walletAfterSecond = await creditService.GetWalletAsync(orgId);
        walletAfterSecond.Balance.Should().Be(600m);

        // Ledger must contain exactly 1 purchase transaction for this event
        var transactions = await creditService.GetTransactionsAsync(orgId);
        transactions.Count(t => t.IdempotencyKey == "stripe-evt-evt_charge_123456789").Should().Be(1);
    }
}
