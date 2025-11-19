# Implementation Audit — Journal AI

Purpose
- Provide a practical, sequenced audit of the implementation plan and supporting implementation documents.
- Map the step-by-step core implementation tasks to the exact places in the spec files where details or decisions can be found.
- Identify missing details and gaps that should be clarified before we scaffold or implement.

Files referenced
- `.specify/specs/001-journal-ai/specs.md` (feature-level requirements)
- `.specify/specs/001-journal-ai/plan.md` (technical plan & sprint breakdown)
- `.specify/specs/001-journal-ai/data-model.md` (detailed schema)
- `.specify/specs/001-journal-ai/quickstart.md` (dev run steps)
- `.specify/specs/001-journal-ai/research.md` (research notes — now includes Aspire section)

High-level observation
- The plan and specs provide a solid high-level roadmap and domain model. However, the current artifacts intentionally leave many engineering choices open (worker queue, exact auth flow, OpenAPI artifact, CI YAML, concrete Docker Compose, and example env vars). For safe, sequential implementation we should lock a small number of these decisions early (Auth approach, Worker pattern, Blob store details, CI smoke tests) so downstream work is unambiguous.

Recommended sequence of implementation steps (minimal, practical, and reference-linked)

1) Repo & developer environment (plan.md — "Sprint 0")
   - References: `plan.md` (Sprint 0), `quickstart.md` (dev steps)
   - Actions: Create repo skeleton, add codeowners, add CONTRIBUTING.md, add Docker Compose (postgres, minio, redis optional). Create `src/backend` and `src/frontend` folders.
   - Deliverables: `docker-compose.yml` in repo root, `.devcontainer` (optional), GitHub Actions skeleton.
   - Gaps: `docker-compose.yml` is not yet provided; quickstart references a compose file but doesn't include an example. Add a `docker-compose.example.yml` to repository.

2) Lock runtime and dependencies (plan.md, research.md)
   - References: `plan.md` (Tech choices), `research.md` (Aspire research added)
   - Actions: Decide exact .NET version (recommend .NET 8), EF Core version, Npgsql version, and React toolchain (Vite + TS version). Run the Aspire research tasks (IDs 18–22) to confirm compatibility and note any constraints.
   - Deliverables: `global.json` (optional) with .NET SDK version, `Directory.Packages.props` or package manifest with version pins.
   - Gaps: Aspire compatibility and exact package names/versions.

3) Data model & EF Core migrations (data-model.md)
   - References: `data-model.md` (tables + notes)
   - Actions: Implement EF Core models mirroring `data-model.md`, add initial migration and sample seed data for a test user (with `no_training_use` toggled), create DB indexes as suggested.
   - Deliverables: EF Core project + migrations, `scripts/seed_dev.sql` or EF seed code.
   - Gaps: Exact naming conventions for migrations and which indexes should be created in first pass vs later.

4) Authentication & account unlock design (specs.md, plan.md, research.md)
   - References: `specs.md` (Confidentiality & Unlocking policy), `plan.md` (Auth), `research.md` (.Aspire-auth research task)
   - Actions: Choose JWT vs cookie; implement user model, password hashing (e.g., Argon2/BCrypt), and entry unlock session logic (unlock_sessions table exists in `data-model.md`). Decide whether unlock uses account password (recommended default) or per-entry password.
   - Deliverables: Auth module, user management endpoints, `POST /api/v1/unlock` implementation, unit tests for unlock flow.
   - Gaps: Concrete API contract for unlock (token format, expiry) and UI flow details (quickstart mentions prompt but not session semantics). Add OpenAPI schema for unlock.

5) Entries API + same-day edit/delete enforcement (specs.md, data-model.md, plan.md)
   - References: `specs.md` (MVP features), `data-model.md` (read_only_after logic)
   - Actions: Implement POST/GET/PATCH/DELETE endpoints with business rules: `read_only_after` computed on create using user's timezone, server-side enforcement (403 for edits after window), audit log writes on changes.
   - Deliverables: Endpoints with unit and integration tests (DB-backed). Smoke test in GitHub Actions.
   - Gaps: Edge cases for timezone handling; need tests across DST boundaries and explicit timezone settings.

6) Media uploads & blob store integration (plan.md, quickstart.md, data-model.md)
   - References: `plan.md` (Media), `quickstart.md` (MinIO in dev), `data-model.md` (media table)
   - Actions: Implement signed upload URL flow, media metadata persistence, thumbnail generation worker (background job). Integrate MinIO in dev compose and S3 in prod notes.
   - Deliverables: Upload API, background worker, thumbnails stored and CDN guidance.
   - Gaps: Thumbnail generation tech (FFmpeg for video?), file size limits, content moderation policy (if any).

7) Export/Import jobs (specs.md, data-model.md, plan.md)
   - References: `specs.md` (Export), `data-model.md` (export_jobs), `plan.md` (Sprint 3)
   - Actions: Implement async job queue, export job generator (zip/markdown + media), and import validation (require confidentiality). Add job status endpoints and signed download URLs.
   - Deliverables: Export job API, import endpoint with validation, unit/integration tests.
   - Gaps: Queue implementation choice (Hangfire vs background worker vs external queue). Also retention policy for generated export zips.

8) Sentiment analysis (research.md, specs.md, plan.md)
   - References: `research.md` (Sentiment options), `specs.md` (privacy-preserving sentiment), `plan.md` (Sprint 3)
   - Actions: Implement client-side sentiment library for MVP. If a server-side option is needed, build ephemeral compute that never persists raw text and add automated audits and logs that show no raw text retention.
   - Deliverables: Sentiment compute hook, UI display, model_version stored in entry.
   - Gaps: Specific library choices for on-device (JS) or server-side library and associated size/accuracy trade-offs.

9) Mindmap service (specs.md, research.md)
   - References: `specs.md` (Mind-map), `research.md` (mindmap algorithms)
   - Actions: Implement TF-IDF similarity and graph creation for small scopes; expose `GET /api/v1/mindmap` returning graph JSON; client-side UI to render with cytoscape.
   - Deliverables: Mindmap API, client renderer, export PNG/SVG option (async for >100 nodes).
   - Gaps: Embeddings/semantic approach for future; decide if embeddings are opt-in only.

10) Frontend scaffold & UI components (plan.md, quickstart.md)
   - References: `plan.md` (Frontend), `quickstart.md` (dev steps)
   - Actions: Scaffold Vite + React + TypeScript, add routing, authentication, timeline, editor, unlock modal, mindmap page. Build component library and design tokens.
   - Deliverables: Working dev frontend with storybook (optional), accessibility checks.
   - Gaps: Design tokens and component mapping are not yet defined — pick Chakra UI or Tailwind with headless components.

11) Observability, testing, and performance validation (plan.md, specs.md)
   - References: `plan.md` (observability), `specs.md` and `data-model.md` (indexes and performance notes)
   - Actions: Instrument key paths, create dashboards, add unit/integration/E2E tests; run performance tests for list endpoints and mindmap generation.
   - Deliverables: OpenTelemetry instrumentation, Prometheus/Grafana dashboards (or cloud equivalents), CI lanes for tests, performance regression tests.
   - Gaps: Performance testing harness setup (k6, locust?) and budget thresholds for CI gating.

12) Security, compliance & release (plan.md, research.md)
   - References: `plan.md` (CI/CD), `research.md` (privacy), `specs.md` (no_training_use)
   - Actions: Implement KMS integration for encryption at rest, secrets management, automated audits to verify `no_training_use` exclusion, finalize runbooks and canary rollout.
   - Deliverables: Production config, runbooks, release notes.
   - Gaps: Vendor selection for secrets/KMS and automation for audit pipelines.

Cross-cutting gaps & recommended clarifications
- Docker Compose example: Add `docker-compose.example.yml` referenced by `quickstart.md`.
- OpenAPI spec: `openapi.yaml` missing — needed to generate clients and validate contracts. (Add to Sprint 0 / 1.)
- CI YAML: no example GitHub Actions; create minimal pipelines early to catch regressions.
- Worker/queue choice: pick either Hangfire, RabbitMQ, or background worker with persistent queue; document choice and implications.
- Auth/unlock contract: add OpenAPI schema and an example flow (unlock session token format, expiry, revocation) in `specs.md`.
- Media processing specifics: thumbnail sizes, video preview, storage lifecycle, and content limits.
- Testing harness for performance: choose k6 or similar and add minimal scripts.
- Timezone handling: add explicit unit test cases in `data-model.md`/`specs.md` for DST and timezone edge cases.

Suggested immediate next actions (short term)
1. Add `docker-compose.example.yml` to repo with `postgres`, `minio`, and `redis` (optional). (Sprint 0)
2. Run Aspire research tasks (IDs 18–22) to lock runtime and packages. (Precedes scaffold.)
3. Create `openapi.yaml` skeleton for the main endpoints (entries, unlock, export, mindmap). (Sprint 0 / 1)
4. Add GitHub Actions skeleton for lint/tests and a migration smoke test against ephemeral Postgres. (Sprint 0)
5. Decide on the worker queue technology (short doc) and add to `research.md`.

How this audit maps to spec files (quick reference)
- Where to read feature descriptions: `specs.md` (MVP features, confidentiality, export/import, mindmap)
- Where to read technical plan & sprints: `plan.md` (tech choices, sprints)
- Where to read schema & indexes: `data-model.md` (tables + implementation notes)
- Where to read dev run steps: `quickstart.md` (Docker + dev commands)
- Where to read research trade-offs & Aspire questions: `research.md` (sentiment, embeddings, mindmap, Aspire section)

Conclusion
- The repository contains solid planning and data modeling. For a safe and friction-free implementation we should lock runtime dependency versions (Aspire compatibility), add minimal infra artifacts (docker-compose example, OpenAPI skeleton), and choose the worker queue and auth unlock details. Once those are locked we can proceed with the sequenced list above.

*Audit created: 2025-11-19*
