# SignalCut — Security & SSRF Protection Architecture

SignalCut implements defense-in-depth protection against Server-Side Request Forgery (SSRF), secret leakage, and tenant isolation breaches.

---

## 1. SSRF Protection on User-Provided Media URLs

When media URLs are provided for ingestion or audio extraction, the `MediaSecurityValidator` executes the following checks before opening network connections:

1. **Protocol Restriction**: Only `http://` and `https://` schemes are accepted. Protocols like `file://`, `ftp://`, `gopher://`, or `dict://` are rejected immediately.
2. **DNS Resolution & IP Inspection**: The hostname is resolved via DNS to inspect all bound IP addresses.
3. **Blocklist Enforcement**:
   - **Loopback**: `127.0.0.0/8` and `::1`.
   - **Private RFC 1918**: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`.
   - **Link Local**: `169.254.0.0/16` and `fe80::/10`.
   - **Cloud Metadata Endpoints**: `169.254.169.254` (AWS, GCP, Azure metadata services).
   - **IPv6 Unique Local**: `fc00::/7`.
4. **Size and Timeout Caps**: Maximum download payload capped at 500 MB; connection timeouts enforced.

---

## 2. OAuth Social Token Encryption

OAuth access and refresh tokens for YouTube, TikTok, Instagram, and LinkedIn are **never stored in plaintext** and **never returned in client DTOs**:
- Encrypted using **AES-256-GCM** using `TOKEN_ENCRYPTION_KEY`.
- Stored as ciphertext in `PublishingAccount.EncryptedAccessToken`.
- `PublishingAccountDto` explicitly omits all token properties.

---

## 3. Webhook Signature Verification

Both Stripe and Razorpay webhook endpoints require cryptographic verification:
- **Stripe**: Evaluates `Stripe-Signature` timestamp and HMAC-SHA256 signature against `STRIPE_WEBHOOK_SECRET`.
- **Razorpay**: Validates HMAC-SHA256 hash using `RAZORPAY_WEBHOOK_SECRET`.
- **Idempotency Safeguard**: Webhook event IDs are stored in `CreditTransaction.IdempotencyKey`. Duplicate delivery attempts by payment providers are acknowledged without adding duplicate balance.
