# SignalCut — Production Runbook & Operations Guide

This runbook guides engineers through local development, test execution, database migrations, and production Docker deployment.

---

## 1. Quickstart: Local Development

### Prerequisites:
- .NET 8 or 9/10 SDK
- Python 3.11+ with FFmpeg installed
- Node.js 20+ & npm

### Starting the Services:

#### 1. Backend API:
```powershell
cd backend
dotnet restore
dotnet run --project src/SignalCut.API
```
- API will listen on `http://localhost:5000` (or `https://localhost:5001`).
- Swagger OpenAPI Explorer: `http://localhost:5000/swagger`.
- Seed Admin Account: `demo@signalcut.app` / `DemoPassword123!`.

#### 2. AI Worker:
```powershell
cd ai-worker
pip install -r requirements.txt
python -m uvicorn app.main:app --port 8000 --reload
```
- Listens on `http://localhost:8000`.
- Health Check: `http://localhost:8000/health`.

#### 3. React Frontend:
```powershell
cd frontend
npm install
npm run dev
```
- Web UI will listen on `http://localhost:5173`.

---

## 2. Running Automated Tests

### Backend Unit & Critical Integration Tests:
```powershell
dotnet test backend/tests/SignalCut.Tests/SignalCut.Tests.csproj
```
Verifies all 10 critical security, credit reservation, refund, tenant isolation, and SSRF tests.

### AI Worker Tests:
```powershell
cd ai-worker
pytest tests/
```

### Frontend Build & Typecheck:
```powershell
cd frontend
npm run build
```

---

## 3. Production Deployment with Docker Compose

Ensure `.env` has been configured from `.env.example`:
```bash
docker-compose up --build -d
```

### Container Endpoints:
- Frontend: `http://localhost:3000`
- Backend API: `http://localhost:5000`
- AI Worker: `http://localhost:8000`
- Postgres: `localhost:5432`
- Redis: `localhost:6379`

---

## 4. Health Checks & Diagnostics

- **Backend Health**: `GET http://localhost:5000/health`
- **AI Worker & FFmpeg Health**: `GET http://localhost:8000/health`
- **Failed Jobs Inspector**: Viewable via `/admin` dashboard or `GET /api/v1/admin/failed-jobs`.
