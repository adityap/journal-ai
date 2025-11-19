# Journal AI — Technical Plan

This plan assumes ASP.NET Core for the backend, PostgreSQL as the database, and React for the frontend (user requested stack). The plan outlines concrete tech choices, dev environment, API and data modeling approach, CI, and a 6-sprint delivery plan.

## Tech choices

- Backend: ASP.NET Core 8 (or latest LTS available). Use Web API templates.
- ORM: EF Core with Npgsql provider.
- Authentication: JWT for API (bearer token) and session cookie for web UI if needed.
- Storage
  - Primary DB: PostgreSQL (managed or self-hosted).
  - Media: S3-compatible blob store (MinIO for dev; AWS S3/GCP/Blob in prod).
- Frontend: React (Vite recommended) + TypeScript. Component library: headless components + design tokens; consider Chakra UI or similar for accessibility-first components.
- Sentiment: prefer on-device JS sentiment library for privacy (MVP). Optionally provide server ephemeral compute with strict non-retention.
- Mindmap: run TF-IDF / lightweight similarity for MVP (client or server depending on scope). Use cytoscape.js or vis.js for interactive graph UI.
- Containerization: Docker for local/dev (docker-compose); production deploy via container registry + orchestration (K8s or managed App Service).
- Observability: OpenTelemetry + Prometheus/Grafana (or cloud-managed equivalents).

## High-level architecture

- API Gateway (optional) → ASP.NET Core API service(s) → PostgreSQL
- Worker(s) for async jobs (export, mindmap image generation, thumbnailing) — can be .NET worker service using background queues (RabbitMQ/SQS) or Hangfire.
- Blob store for media with CDN in front for serving large assets.
- Frontend (React) talking to API over HTTPS.

## API contracts

- Use OpenAPI (Swashbuckle) to generate and validate API contracts. Start with endpoints in `specs.md` (entries, unlock, categories, exports, mindmap, import).
- Enforce request/response schemas with DTOs and model validation attributes.

## Dev environment

- Docker Compose services: postgres, minio (or localstack), redis (optional for queues), and the API and frontend during dev.
- Local dev steps (high-level):
  - Start docker-compose
  - `dotnet run` in API project (or `docker compose up api`)
  - `pnpm install` / `npm install` in frontend and `pnpm run dev`

## Security & Privacy

- Ensure TLS in all environments; store secrets in environment variables or secret store.
- Store entry-level confidentiality metadata and implement unlock token/session logic.
- Implement `no_training_use` flag on user and enforce exclusion in any data pipelines. Add automated audits to check ML pipelines do not use user data without consent.

## CI / CD

- GitHub Actions with lanes for:
  - Lint + typecheck (frontend and backend)
  - Unit tests
  - Build and publish artifacts
  - Migrations check + optional smoke tests against ephemeral DB
  - Integration/E2E on merge branches
  - Performance regression check for critical paths (optional lane triggered by PR label)

## Sprint breakdown (6 sprints — 2 weeks each suggested)

Sprint 0 — Setup & scaffolding (1 week)
- Create repo structure, add codeowners, CONTRIBUTING.md
- Scaffold ASP.NET Core API and React frontend
- Add Docker Compose with Postgres + MinIO
- Add basic CI pipelines for lint/tests

Sprint 1 — Core entries & DB (2 weeks)
- Implement data model and EF Core migrations
- Implement entries create/list/read endpoints
- Add same-day edit/delete business rule in backend
- Add unit tests for business logic

Sprint 2 — Media & Auth (2 weeks)
- Integrate S3-compatible uploads (signed URL flow)
- Thumbnail generation worker
- Implement authentication (JWT + account unlock flows)
- Frontend entry editor basic UX

Sprint 3 — Export/Import & Sentiment (2 weeks)
- Implement async export job and signed download
- Implement import validation (confidentiality required)
- Add on-device sentiment (frontend) + server ephemeral option design

Sprint 4 — Mindmap & UX polish (2 weeks)
- Implement mindmap service (MVP TF-IDF similarity)
- Mindmap UI with interactive graph
- Accessibility audit and UX polish

Sprint 5 — Testing, performance, release (2 weeks)
- Add integration & E2E tests
- Run performance tests; optimize DB indexes and queries
- Finalize monitoring, runbooks, canary rollout

## Deliverables

- OpenAPI spec (`openapi.yaml`) generated via Swashbuckle.
- EF Core migrations and sample seed data.
- Docker-compose dev environment.
- React frontend scaffold and component library.
- CI pipelines for lint/tests/build.

## Acceptance criteria

- All core endpoints implemented with tests.
- Private/confidentiality flows enforce hide/unlock behavior.
- Media stored in blob store and included in exports.
- Sentiment computed and surfaced (MVP on-device).
- Mindmap generation returns graph JSON with usable layout.


*Plan created: 2025-11-19*
