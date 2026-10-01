using SignalCut.Domain.Common;
using SignalCut.Domain.Enums;

namespace SignalCut.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual CreditWallet? CreditWallet { get; set; }
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<PublishingAccount> PublishingAccounts { get; set; } = new List<PublishingAccount>();
}

public class User : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.USER;
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasUsedFreeTrial { get; set; } = false;

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<Search> Searches { get; set; } = new List<Search>();
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
}

public class Project : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation
    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<Search> Searches { get; set; } = new List<Search>();
    public virtual ICollection<Source> Sources { get; set; } = new List<Source>();
    public virtual ICollection<Clip> Clips { get; set; } = new List<Clip>();
}
