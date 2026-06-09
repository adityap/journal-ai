---
name: run
description: Launch and drive the Journal AI app (.NET API + React/Vite frontend) locally to verify changes work in the real app. Use when asked to run, start, or smoke-test the application.
---

# Running Journal AI

Two services: a .NET 8 API and a React/Vite frontend. In **Development** the API
uses **SQLite** (`journalai-dev.db`) and **in-memory Hangfire** — no Postgres,
Redis, or MinIO needed for auth/entries/sentiment. (MinIO is only required for
media *upload*; everything else runs standalone.)

## 1. Backend API → http://localhost:5000

```bash
cd src/backend
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5000 \
  dotnet run --no-launch-profile --project JournalAI.csproj
```

Run it in the background. Wait ~20s for build + startup; it's ready when the log
prints `Now listening on: http://localhost:5000`.

**Gotchas (these will bite you):**
- `--project JournalAI.csproj` is **required** — the folder also contains
  `JournalAI.Tests.csproj`, so a bare `dotnet run` fails with "more than one
  project file."
- Set `ASPNETCORE_URLS` explicitly — there is no `launchSettings.json`
  (it's gitignored), and the frontend expects the API on port **5000**.
- Schema is created via `EnsureCreated()` (not migrations) on first run, and a
  seed user/entry is inserted. `EnsureCreated()` will **not** upgrade an existing
  DB — if `journalai-dev.db` predates a schema change you'll get column errors.
  Fix: move it aside so a fresh one is built — `mv journalai-dev.db journalai-dev.db.bak`.

## 2. Frontend → http://localhost:5173

```bash
cd src/frontend
npm run dev          # node_modules is usually already present; npm install if not
```

Background it; ready in ~1s (`VITE ready ... Local: http://localhost:5173/`).

## 3. Drive it (don't just launch it)

**Use PowerShell `Invoke-RestMethod`, not `curl`** — `curl` is blocked by the
sandbox/permissions in this environment; PowerShell is allowed (run Bash with
the sandbox disabled for these localhost calls).

```bash
powershell.exe -NoProfile -Command '
$base="http://localhost:5000/api/v1"
(Invoke-RestMethod "http://localhost:5000/health")          # -> status: ok
$body=@{email="demo@example.com";password="Demo123!@#"} | ConvertTo-Json
try { Invoke-RestMethod "$base/auth/register" -Method Post -ContentType "application/json" -Body $body } catch {}
$login=Invoke-RestMethod "$base/auth/login" -Method Post -ContentType "application/json" -Body $body
$h=@{ Authorization = "Bearer $($login.accessToken)" }
$entry=@{ title="Great Day"; bodyText="I love this, it is amazing!"; confidentiality="public"; sentimentScore=0.75; sentimentLabel="Very Positive"; sentimentModel="simple-keyword-analysis" } | ConvertTo-Json
$created=Invoke-RestMethod "$base/entries" -Method Post -ContentType "application/json" -Headers $h -Body $entry
$list=Invoke-RestMethod "$base/entries" -Method Get -Headers $h
$list.items | ForEach-Object { "{0} | score={1} | {2}" -f $_.title,$_.sentimentScore,$_.sentimentLabel }
'
```

A healthy run: `/health` returns `ok`, login returns a ~300-char `accessToken`,
and the created entry's `sentimentScore` is echoed **and** read back unchanged
from the DB (it should match what you sent — a constant value like `0.99`
regardless of input means the client-sentiment regression has returned).

For the **UI**, open http://localhost:5173/ in a browser and log in with
`demo@example.com` / `Demo123!@#`. (No headless browser is available in this
Windows shell, so UI driving is manual.)

## Endpoints

- Auth: `POST /api/v1/auth/{register,login,logout}`, `GET /api/v1/auth/me`
- Entries: `GET|POST /api/v1/entries`, `GET|PATCH|DELETE /api/v1/entries/{id}`
- Health: `GET /health`  ·  Swagger UI: http://localhost:5000/swagger (dev only)
