# Journal AI — Personal Journaling Platform

A privacy-first journaling application with text and media entries, client-side
sentiment analysis, and entry visualizations.

> **Current build/test status and known issues:** see [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md). Historical status/implementation docs are archived under [docs/archive/](docs/archive/).
>
> ⚠️ **Scope note:** This README distinguishes what is **implemented today** from
> what is **planned**. Earlier versions marked planned features as done — that has
> been corrected. The detailed product vision lives in the spec under
> [`.specify/specs/001-journal-ai/`](.specify/specs/001-journal-ai/).

## Features

### ✅ Implemented

- Create journal entries with text and attached media (images/video via S3/MinIO)
- Organize entries with tags; optional category reference per entry
- Public / Private confidentiality levels
- Same-day edit/delete enforcement (entries become immutable after their creation day, in the user's timezone)
- Client-side sentiment analysis (keyword-based; no entry text is sent to a third party for analysis)
- Sentiment trend chart and a category/tag mind-map view (SVG-based)
- JWT authentication (register / login / logout), bcrypt password hashing (≥12 rounds)
- `no_training_use` flag defaulted on every entry
- Audit-log row written on entry creation
- Async thumbnail generation for uploaded media (Hangfire)

### 🚧 Planned / not yet implemented

These are designed in the spec (and some have database tables reserved) but have
**no working endpoint yet**:

- Full-text search across entries
- Export (JSON / Markdown / ZIP) and bulk import — `export_jobs` table exists, no job/endpoint
- Account-based unlock sessions for private entries (1-hour TTL) — `unlock_sessions` table exists, no endpoint
- GDPR data export & account deletion
- Category management API (categories can't yet be created via the API; the mind-map groups by raw category id)
- TF-IDF / similarity-based mind-map graph (current mind-map is simple category/tag grouping)
- Rate limiting, encryption at rest, and an observability stack (OpenTelemetry / Prometheus / Grafana / Serilog)

## Getting Started

### Local dev (recommended)

In **Development** the API uses **SQLite** and **in-memory Hangfire** — no
Postgres, Redis, or MinIO required for auth, entries, and sentiment. (MinIO is
only needed to exercise media *upload*.) The schema is created and seeded
automatically on first run; no manual migration step is needed in dev.

**Prerequisites:** .NET 8 SDK, Node.js 18+.

**Backend** → http://localhost:5000
```bash
cd src/backend
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5000 \
  dotnet run --no-launch-profile --project JournalAI.csproj
```
> The folder contains two project files, so `--project JournalAI.csproj` is required.

**Frontend** → http://localhost:5173
```bash
cd src/frontend
npm install        # first time only
npm run dev
```

A seed user is created on first run: `dev@example.com` (see `Data/SeedData.cs`).
You can also register a new account from the UI.

See [.claude/skills/run/SKILL.md](.claude/skills/run/SKILL.md) for the full
launch + smoke-test recipe.

### Containerized

`docker-compose.yml` defines `postgres`, `minio`, `redis`, `api`, and `frontend`
services for a production-style setup (Postgres-backed). This path is not the
primary dev flow and is less exercised than the local SQLite flow above.

```bash
docker compose up
# Frontend: http://localhost:5173   API: http://localhost:5000   MinIO console: http://localhost:9001
```

## Architecture

```
┌─────────────────────────────────────────────┐
│  React + Vite frontend (TypeScript)          │
│  • client-side sentiment analyzer            │
│  • SVG list / mind-map / sentiment views     │
└───────────────────┬──────────────────────────┘
                    │ HTTP / JSON (JWT bearer)
┌───────────────────▼──────────────────────────┐
│  ASP.NET Core 8 API                           │
│  • JWT auth, entry CRUD + immutability rules  │
│  • media upload via presigned S3 URLs         │
│  • Hangfire jobs (in-memory) for thumbnails   │
└───────────┬───────────────────┬───────────────┘
            │                   │
      ┌─────▼─────┐       ┌─────▼─────┐
      │  SQLite   │       │  S3/MinIO │
      │ (dev) /   │       │  (media)  │
      │ Postgres  │       └───────────┘
      │  (prod)   │
      └───────────┘
```

## Tech Stack

- **Backend**: ASP.NET Core 8 + Entity Framework Core 8
- **Database**: SQLite (dev) / PostgreSQL (prod)
- **Storage**: S3-compatible — MinIO (dev) / AWS S3 (prod), via AWSSDK.S3
- **Frontend**: React 18 + Vite + TypeScript
- **Sentiment**: custom client-side keyword analyzer (`src/frontend/src/utils/sentimentAnalyzer.ts`)
- **Visualizations**: hand-rolled SVG (no external chart/graph library)
- **Async jobs**: Hangfire (in-memory storage)
- **Auth**: JWT bearer tokens (HS256), BCrypt password hashing
- **Image processing**: SixLabors.ImageSharp (thumbnails)
- **CI**: GitHub Actions (`.github/workflows/ci.yml`)

## API

Swagger UI (development only): http://localhost:5000/swagger

### Implemented endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/v1/auth/register` | Create account (returns JWT) |
| POST | `/api/v1/auth/login` | Sign in (returns JWT) |
| POST | `/api/v1/auth/logout` | Clear session |
| GET | `/api/v1/auth/me` | Current user |
| POST | `/api/v1/auth/validate` | Validate a token |
| POST | `/api/v1/entries` | Create entry |
| GET | `/api/v1/entries` | List entries (paginated, filter by date/category/tag) |
| GET | `/api/v1/entries/{id}` | Read entry |
| PATCH | `/api/v1/entries/{id}` | Edit entry (same day only) |
| DELETE | `/api/v1/entries/{id}` | Delete entry (same day only) |
| POST | `/api/v1/media/initiate` | Get presigned upload URL |
| POST | `/api/v1/media/complete` | Finalize upload |
| GET | `/api/v1/media` | List media (paginated) |
| GET | `/api/v1/media/{id}` | Media metadata |
| DELETE | `/api/v1/media/{id}` | Delete media |
| POST | `/api/v1/media/{mediaId}/associate-entry/{entryId}` | Link media to entry |
| GET | `/health` | Health check |

> The `/unlock`, `/exports`, `/import`, and `/mindmap` endpoints described in the
> original spec are **not implemented yet** (see Planned, above).

The detailed OpenAPI spec lives at
[`.specify/specs/001-journal-ai/openapi.yaml`](.specify/specs/001-journal-ai/openapi.yaml)
and describes the full intended surface, not just what's built today.

## Database Schema

Core tables (created via EF Core; `EnsureCreated` in dev):

- **users** — authentication & settings
- **entries** — journal entries with sentiment fields, confidentiality, immutability
- **categories** — entry categories (optional reference per entry; no management API yet)
- **media** — images/videos with S3 references and thumbnails
- **audit_logs** — mutation log (currently written on entry creation)
- **export_jobs** — reserved for the planned async export feature (unused)
- **unlock_sessions** — reserved for the planned private-entry unlock feature (unused)

See [`.specify/specs/001-journal-ai/data-model.md`](.specify/specs/001-journal-ai/data-model.md) for the full intended schema.

## Configuration

See `.env.example` for available options. Backend configuration keys
(`appsettings.json`, overridable via `Jwt__Key`-style environment variables):

```
Jwt:Key            # signing key (≥32 bytes recommended; shorter keys are SHA-256 derived)
Jwt:Issuer         # default "journalai"
Jwt:Audience       # default "journalai-users"
Jwt:ExpiryMinutes  # default 1440 (24h)
ConnectionStrings:DefaultConnection   # Postgres (prod)
ConnectionStrings:SqliteDev           # SQLite (dev, defaults to journalai-dev.db)
S3:Endpoint / S3:AccessKey / S3:SecretKey
```

## Security (current state)

Implemented:
- BCrypt password hashing (≥12 rounds)
- JWT bearer auth with expiry
- CORS restricted to the configured frontend origin
- Parameterized queries via EF Core (no string-built SQL)

Planned (claimed previously but **not yet implemented**): enforced TLS, rate
limiting, encryption at rest. Don't treat those as present.

## Privacy

- **No training use**: every entry is created with `no_training_use = true`
- **Client-side sentiment**: sentiment is computed in the browser; raw entry text is not sent to a third party for analysis
- **Audit logging**: entry creation is logged (broader mutation logging is planned)
- GDPR export/deletion endpoints are **planned**, not yet built

See [`.specify/memory/constitution.md`](.specify/memory/constitution.md) for privacy principles.

## Testing

```bash
# Backend (94 tests)
cd src/backend
dotnet test JournalAI.Tests.csproj

# Frontend — type-check, build, and lint (no unit-test runner configured)
cd src/frontend
npm run build      # tsc + vite build
npm run lint       # eslint
```

## Specification & Planning

The detailed spec and roadmap (the source of the intended-feature list above):
- [specs.md](.specify/specs/001-journal-ai/specs.md) — MVP features, API, UX flows
- [data-model.md](.specify/specs/001-journal-ai/data-model.md) — schema, indexes
- [TECHNICAL_PLAN_V2.md](.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md) — architecture, sprint roadmap
- [constitution.md](.specify/memory/constitution.md) — engineering principles
- [CLARIFICATION_SESSION_2026_02_13.md](.specify/specs/001-journal-ai/CLARIFICATION_SESSION_2026_02_13.md) — design decisions

(Note: `specs/001-featurename-develop-force/spec.md` is an unfilled template; the
real specification is the `.specify/specs/001-journal-ai/` set above.)

## Development

See [CONTRIBUTING.md](./CONTRIBUTING.md) for setup, commit conventions, PR
process, and code-review guidelines.

## Troubleshooting

**API health check:** `GET http://localhost:5000/health` → `{"status":"ok"}`

**Frontend can't reach API:** check the browser console for CORS errors; the API
allows the frontend origin via the `AllowFrontend` CORS policy.

**Stale dev database:** the dev schema is built with `EnsureCreated()`, which does
**not** upgrade an existing `journalai-dev.db` after a model change. If you hit
missing-column errors, move the file aside (`mv journalai-dev.db journalai-dev.db.bak`)
and restart the API to rebuild it.

## License

TBD.

---

**Status:** see [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) for current build/test health.
