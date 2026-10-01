using Microsoft.EntityFrameworkCore;
using SignalCut.Domain.Entities;
using SignalCut.Domain.Enums;
using SignalCut.Infrastructure.Security;

namespace SignalCut.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(SignalCutDbContext context, IPasswordHasher hasher)
    {
        if (await context.Organizations.AnyAsync())
        {
            return;
        }

        var demoOrg = new Organization
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "SignalCut Creator Studio",
            Slug = "signalcut-demo",
            IsActive = true
        };

        var demoUser = new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            OrganizationId = demoOrg.Id,
            Email = "demo@signalcut.app",
            FullName = "Alex Rivera",
            PasswordHash = hasher.HashPassword("DemoPassword123!"),
            Role = UserRole.ADMIN,
            IsEmailVerified = true,
            HasUsedFreeTrial = false
        };

        var demoWallet = new CreditWallet
        {
            Id = Guid.NewGuid(),
            OrganizationId = demoOrg.Id,
            Balance = 250m,
            ReservedBalance = 0m,
            LifetimeEarned = 250m,
            Currency = "INR"
        };

        var initialGrantTx = new CreditTransaction
        {
            WalletId = demoWallet.Id,
            Type = CreditTransactionType.BONUS,
            Amount = 250m,
            BalanceAfter = 250m,
            ReservedBalanceAfter = 0m,
            Description = "Seed initial demonstration credits",
            IdempotencyKey = "seed-initial-demo-grant"
        };

        // Seed sample connected social account
        var demoAccount = new PublishingAccount
        {
            OrganizationId = demoOrg.Id,
            Platform = PublishingPlatform.YOUTUBE,
            AccountName = "SignalCut Official",
            AccountIdentifier = "@signalcut_official",
            EncryptedAccessToken = "enc_demo_token_youtube",
            IsConnected = true,
            TokenExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        context.Organizations.Add(demoOrg);
        context.Users.Add(demoUser);
        context.CreditWallets.Add(demoWallet);
        context.CreditTransactions.Add(initialGrantTx);
        context.PublishingAccounts.Add(demoAccount);

        await context.SaveChangesAsync();
    }
}
