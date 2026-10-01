# SignalCut — Database Schema & ER Diagram

The database schema is designed for PostgreSQL with UUID primary keys, UTC timestamps, soft-deletion auditing, multi-tenant isolation via `OrganizationId`, and an immutable ledger pattern for billing.

```mermaid
erDiagram
    ORGANIZATIONS ||--o{ USERS : "has members"
    ORGANIZATIONS ||--o| CREDIT_WALLETS : "owns"
    ORGANIZATIONS ||--o{ PROJECTS : "contains"
    ORGANIZATIONS ||--o{ SEARCHES : "performs"
    ORGANIZATIONS ||--o{ SOURCES : "discovers"
    ORGANIZATIONS ||--o{ CLIPS : "renders"
    ORGANIZATIONS ||--o{ JOBS : "executes"
    ORGANIZATIONS ||--o{ PAYMENTS : "purchases"
    ORGANIZATIONS ||--o{ PUBLISHING_ACCOUNTS : "connects"
    ORGANIZATIONS ||--o{ USAGE_RECORDS : "records"
    ORGANIZATIONS ||--o{ AUDIT_LOGS : "logs"

    CREDIT_WALLETS ||--o{ CREDIT_TRANSACTIONS : "ledger entries"

    SOURCES ||--o| TRANSCRIPTS : "transcribed to"
    TRANSCRIPTS ||--o{ TRANSCRIPT_CHUNKS : "chunked into"
    SOURCES ||--o{ MOMENTS : "extracts"
    MOMENTS ||--o{ CLIPS : "customized into"
    CLIPS ||--o{ PUBLISHING_JOBS : "dispatched to"
    PUBLISHING_ACCOUNTS ||--o{ PUBLISHING_JOBS : "published through"

    USERS {
        uuid Id PK
        uuid OrganizationId FK
        string Email
        string PasswordHash
        string FullName
        string Role
        boolean IsEmailVerified
        boolean HasUsedFreeTrial
        datetime CreatedAt
    }

    CREDIT_WALLETS {
        uuid Id PK
        uuid OrganizationId FK
        decimal Balance
        decimal ReservedBalance
        decimal LifetimeEarned
        decimal LifetimeSpent
        string Currency
    }

    CREDIT_TRANSACTIONS {
        uuid Id PK
        uuid WalletId FK
        string Type
        decimal Amount
        decimal BalanceAfter
        decimal ReservedBalanceAfter
        uuid ReferenceJobId
        string IdempotencyKey
        datetime CreatedAt
    }

    SOURCES {
        uuid Id PK
        uuid OrganizationId FK
        string ExternalSourceId
        string Provider
        string Title
        string Url
        double DurationSeconds
        string RightsStatus
        string AuthorizationStatus
        uuid RightsConfirmedByUserId
        datetime RightsConfirmationTimestamp
        string RightsConfirmationStatement
    }

    MOMENTS {
        uuid Id PK
        uuid OrganizationId FK
        uuid SourceId FK
        double StartTime
        double EndTime
        double ClipScore
        double HookStrength
        double InformationDensity
        string Objective
        string SuggestedHook
        string SuggestedTitle
    }

    CLIPS {
        uuid Id PK
        uuid OrganizationId FK
        uuid MomentId FK
        double StartTime
        double EndTime
        string AspectRatio
        string CaptionStyle
        boolean HasWatermark
        string RenderedVideoUrl
        string RenderStatus
    }

    JOBS {
        uuid Id PK
        uuid OrganizationId FK
        uuid UserId FK
        string Type
        string Status
        int ProgressPercentage
        int RetryCount
        decimal ReservedCredits
        string IdempotencyKey
    }
```

---

## Key Invariants & Safeguards

1. **Credit Wallet Derivability**:
   `Wallet.AvailableBalance = Wallet.Balance - Wallet.ReservedBalance`.
   Every balance mutation must be accompanied by an immutable `CreditTransaction` row.
2. **Negative Balance Prevention**:
   The balance constraint ensures that `Balance >= 0` and `AvailableBalance >= 0` at all times.
3. **Tenant Boundary**:
   Every tenant-scoped query filters on `OrganizationId`. Cross-tenant querying is prevented at both the database and middleware layers.
4. **Idempotency Uniqueness**:
   `Payment.IdempotencyKey` and `CreditTransaction.IdempotencyKey` prevent duplicate credits upon webhook retries.
