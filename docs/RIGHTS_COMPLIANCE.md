# SignalCut — Rights, Authorization & Legal Compliance

SignalCut is intentionally built **NOT** as a copyright scraper, but as an authorized content discovery and repurposing platform.

---

## 1. The Core Legal Principle

> **Never assume that publicly accessible content on the web is automatically available for commercial reuse.**

Public videos, podcasts, and talks are protected by copyright. SignalCut enforces clear boundaries between:
1. **Discovery & Indexing**: Searching, cataloging, timestamping, quoting for analysis, and summarizing (where permitted).
2. **Media Transformation & Generation**: Downloading raw audio/video streams, re-encoding, modifying, watermarking, branding, and republishing.

---

## 2. Source Rights States

Every discovered item is tagged with a distinct `RightsStatus`:

| Rights Status | Description | Allowed in Generation Pipeline? |
|---|---|:---:|
| `UNKNOWN` | Newly discovered item without verified ownership | ❌ No |
| `DISCOVERY_ONLY` | Public content found on web/YouTube/podcasts | ❌ No |
| `BLOCKED` | Explicitly blocked, DMCA flagged, or prohibited | ❌ No |
| `USER_OWNED` | User is the original creator / copyright owner | ✅ Yes |
| `USER_AUTHORIZED` | User holds a signed client authorization or release | ✅ Yes |
| `LICENSED` | Media acquired via commercial licensing platform | ✅ Yes |
| `PUBLIC_DOMAIN` | Media released under Creative Commons CC0 or expired copyright | ✅ Yes |

---

## 3. Affirmative Confirmation Gate

Before any source can have clips rendered or media downloaded, the user must undergo the **Rights Verification Gate**:

1. User selects claimed status (`USER_OWNED`, `USER_AUTHORIZED`, `LICENSED`, `PUBLIC_DOMAIN`).
2. Optional license proof document URL / file is attached.
3. User must affirmatively check and submit the statutory statement:
   ```
   "I confirm that I own or have permission to use this content."
   ```
4. An immutable `AuditLog` row is written containing:
   - `UserId`
   - `SourceId`
   - `Timestamp`
   - `ClaimedStatus`
   - `IpAddress`
   - `ConfirmationStatement`

---

## 4. No Automated "Fair Use" Claims

The platform specifically **prohibits** automatic claims of "fair use." Fair use is a legal defense evaluated on case-by-case statutory factors by courts, not a programmatic toggle. Users are required to attest to explicit ownership or contractual authorization.
