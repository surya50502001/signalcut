# SignalCut — AI-Powered Content Discovery & Repurposing Platform

> **Turn any topic into a content pipeline.**  
> Discover the conversations, videos, and ideas that matter — then turn authorized content into publish-ready clips.

---

## What is SignalCut?

SignalCut is **NOT** just an AI video clipper. The fundamental differentiator is:

> **The user searches for a topic, rather than starting with a video URL.**

When a user searches for *"What are CEOs saying about AI agents?"* or *"Latest discussions about .NET 10"*, SignalCut:
1. **Discovers** relevant conversations, podcasts, interviews, conferences, and connected workspace media.
2. **Tags** every source with strict **Rights & Permission States** (`UNKNOWN`, `DISCOVERY_ONLY`, `USER_OWNED`, `LICENSED`, `PUBLIC_DOMAIN`, `BLOCKED`).
3. **Analyzes** transcript sections using semantic density & moment scoring across configurable objectives (*Educational*, *Contrarian*, *Newsworthy*, *Emotional*, *Technical*).
4. **Enforces** mandatory affirmative user rights verification before entering the generation pipeline.
5. **Transforms** selected moments into 9:16 vertical short-form publishable media with smart crop, animated captions, brand kit styling, and progress bars.
6. **Publishes** directly to YouTube Shorts, TikTok, Instagram Reels, and LinkedIn.
7. **Tracks** billing with an immutable, thread-safe credit ledger (reservation, deduction, refund pattern) preventing negative balances.

---

## Repository Structure

```
├── backend/                  # ASP.NET Core Web API (.NET 8/10 LTS)
│   ├── SignalCut.sln
│   ├── src/
│   │   ├── SignalCut.Domain/          # Clean Architecture Domain (Entities, Enums)
│   │   ├── SignalCut.Application/     # Use Cases, CQRS, DTOs, FluentValidation, Interfaces
│   │   ├── SignalCut.Infrastructure/  # EF Core, PostgreSQL, Search/LLM/Storage/Payment Providers
│   │   └── SignalCut.API/             # REST Controllers, JWT Auth, Swagger, Exception Middleware
│   └── tests/
│       └── SignalCut.Tests/           # 18 Critical unit & integration tests
├── ai-worker/                # Python 3.12 / FastAPI microservice
│   ├── app/main.py           # Transcription, Semantic Analysis, FFmpeg 9:16 Render Engine
│   ├── requirements.txt
│   ├── Dockerfile
│   └── tests/test_worker.py  # Pytest test suite
├── frontend/                 # React 18 + TypeScript + Vite + Tailwind CSS
│   ├── src/
│   │   ├── api/client.ts     # Axios client with auth interceptors
│   │   ├── context/          # Auth & credit state management
│   │   ├── components/       # Navbar, Sidebar, RightsModal, PaymentModal
│   │   └── pages/            # Landing, Search, Discoveries, Moments, Editor, Clips, Publishing, Credits, Analytics, Admin
│   ├── package.json
│   ├── Dockerfile
│   └── nginx.conf
├── docs/                     # Production Documentation
│   ├── ARCHITECTURE.md       # Clean Architecture & Flow Diagrams
│   ├── DATABASE_ERD.md       # PostgreSQL Schema & Mermaid ER Diagram
│   ├── RIGHTS_COMPLIANCE.md  # Intellectual Property & Affirmative Gate
│   ├── COST_CONTROL.md       # Pay-As-You-Go Credit Ledger & Reservation Mechanics
│   ├── SECURITY_AND_SSRF.md  # SSRF Protection & AES Token Encryption
│   └── RUNBOOK.md            # Deployment, Health Checks & Operations
├── docker-compose.yml        # Orchestration for PostgreSQL, Redis, API, AI Worker, Frontend
└── .env.example              # Documented environment variables template
```

---

## 10 Critical Automated Test Gates

All 10 critical security, authorization, and financial tests specified in production requirements pass:
1. **User cannot process without sufficient credits** (`InsufficientCreditsException` thrown, balance preserved).
2. **Failed job refunds reserved credits** (reservation released, refund transaction recorded in ledger).
3. **Retry cannot double-charge** (retries re-use or safely renew reservation without duplicating credit deductions).
4. **Payment webhook cannot double-credit** (event idempotency keys prevent duplicate additions on webhook replay).
5. **User cannot access another organization** (complete tenant isolation enforced across all repositories and controllers).
6. **Unauthorized media cannot enter generation pipeline** (`DISCOVERY_ONLY` content strictly blocked until affirmative rights confirmation is logged).
7. **Worker failure does not leave credits permanently locked** (timeouts and cancellations release reserved balances).
8. **Publishing token is never exposed** (encrypted OAuth credentials omitted from client-facing DTOs).
9. **Malicious URLs cannot trigger SSRF** (DNS resolution blocks loopback, RFC 1918, RFC 4193, and cloud metadata `169.254.169.254`).
10. **Concurrent jobs cannot overspend credits** (thread-safe wallet reservations guarantee balance never goes negative).

---

## Quickstart

### 1. Run Automated Test Suites
```powershell
# Backend Tests (18 tests passing)
dotnet test backend/tests/SignalCut.Tests/SignalCut.Tests.csproj

# AI Worker Tests (Pytest passing)
cd ai-worker
pytest tests/

# Frontend Build & Typecheck (Vite build passing)
cd ../frontend
npm run build
```

### 2. Launch Local Environment
```powershell
# Terminal 1: Backend API (http://localhost:5000)
cd backend
dotnet run --project src/SignalCut.API

# Terminal 2: AI Worker (http://localhost:8000)
cd ai-worker
python -m uvicorn app.main:app --port 8000 --reload

# Terminal 3: Frontend (http://localhost:5173)
cd frontend
npm run dev
```

### 3. Containerized Deployment
```bash
docker-compose up --build -d
```
