# Journal AI — Quickstart (dev)

This guide helps you run the Journal AI stack locally for development (Windows / PowerShell). It assumes Docker and .NET SDK are installed.

Prerequisites
- Git
- .NET SDK 8.x (or latest LTS)
- Node.js (16+ recommended) and pnpm/npm
- Docker Desktop running

1) Clone repository

```powershell
git clone <your-repo-url> journal-ai
cd journal-ai
```

2) Start dev services with Docker Compose

A simple `docker-compose.yml` (provided in repo) should include Postgres, MinIO, and Redis (optional).

```powershell
# from repo root
docker compose up -d postgres minio
```

3) Backend setup

```powershell
cd src/backend
# set DB connection string in appsettings.Development.json or env var
# Apply EF core migrations
dotnet tool restore
dotnet ef database update
# Run API
dotnet run
```

Alternatively run the API container:

```powershell
docker compose up --build api
```

4) Frontend setup

```powershell
cd src/frontend
pnpm install
pnpm run dev
# or with npm
npm install
npm run dev
```

5) Create a test account & try flows
- Use the frontend UI to create a user and log in (or use API to create with seeded data).
- Create entries with Public and Private confidentiality to verify timeline locking/unlocking.

6) Running tests

```powershell
# Backend unit tests
cd src/backend/tests
dotnet test

# Frontend tests (if using Playwright/Cypress)
cd src/frontend
pnpm test
```

7) Useful commands

```powershell
# View logs
docker compose logs -f api

# Stop services
docker compose down
```

Notes
- For local media, MinIO is used and credentials are configured via env vars.
- Use the `no_training_use` flag in the seeded user to test privacy enforcement in pipelines.

*Quickstart created: 2025-11-19*
