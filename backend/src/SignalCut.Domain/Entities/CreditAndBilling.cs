using SignalCut.Domain.Common;
using SignalCut.Domain.Enums;

namespace SignalCut.Domain.Entities;

public class CreditWallet : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public decimal Balance { get; set; } = 0;
    public decimal ReservedBalance { get; set; } = 0;
    public decimal AvailableBalance => Balance - ReservedBalance;

    public decimal LifetimeEarned { get; set; } = 0;
    public decimal LifetimeSpent { get; set; } = 0;
    public string Currency { get; set; } = "INR";

    // Concurrency Token for optimistic concurrency check
    public byte[]? RowVersion { get; set; }

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<CreditTransaction> Transactions { get; set; } = new List<CreditTransaction>();
}

public class CreditTransaction : BaseEntity
{
    public Guid WalletId { get; set; }
    public CreditTransactionType Type { get; set; }
    public decimal Amount { get; set; } // positive or negative depending on transaction
    public decimal BalanceAfter { get; set; }
    public decimal ReservedBalanceAfter { get; set; }

    public Guid? ReferenceJobId { get; set; }
    public Guid? ReferencePaymentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }

    // Navigation
    public virtual CreditWallet Wallet { get; set; } = null!;
}

public class Payment : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal CreditsPurchased { get; set; }

    public PaymentProviderType Provider { get; set; } = PaymentProviderType.STRIPE;
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

    public string? ProviderOrderId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public string? ProviderSignature { get; set; }
    public string? IdempotencyKey { get; set; }

    public string? MetadataJson { get; set; }

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
