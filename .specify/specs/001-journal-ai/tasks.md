# Journal AI — Project Tasks

This file lists actionable tasks organized by sprint and user story. Each task includes an ID, concise title, short description, and current status.

**Last Updated**: 2026-02-13  
**Sprint 0 Status**: ✅ COMPLETE (Infrastructure foundation ready)  
**Next Phase**: Sprint 1 (Core Entries & Database)

---

## Sprint 0 — Infrastructure & Setup ✅ (COMPLETE)

**Status**: COMPLETE — All foundation infrastructure in place

### Completed Tasks

- [X] T001 - Git repo structure with CONTRIBUTING.md
  Description: Repository organized with clear folder structure, comprehensive CONTRIBUTING.md with commit conventions, PR process, constitution checklist.
  Status: completed
  
- [X] T002 - Docker Compose services (Postgres, MinIO, Redis, API, Frontend)
  Description: Full development environment with all services configured, volumes, health checks, networking.
  Status: completed

- [X] T003 - Backend Dockerfile (multi-stage ASP.NET Core 8)
  Description: Optimized production-ready Dockerfile with security hardening, health checks, non-root user.
  Status: completed

- [X] T004 - Frontend Dockerfile (Node 18 + Vite dev)
  Description: Development Dockerfile for React/Vite frontend with pnpm package manager.
  Status: completed

- [X] T005 - .env.example configuration file
  Description: Comprehensive environment variable template covering all services, secrets, feature flags, rate limiting, logging.
  Status: completed

- [X] T006 - GitHub Actions CI workflow (lint, test, build)
  Description: Complete CI pipeline with backend lint/test, frontend lint/test, Docker image builds, artifact uploads.
  Status: completed

- [X] T007 - Updated README.md with quick start & architecture
  Description: Comprehensive README with quick start, architecture diagram, tech stack, implementation timeline, links to specs.
  Status: completed

- [X] T008 - Enhanced .gitignore with all exclusion patterns
  Description: Complete .gitignore covering C#, Node, Docker, IDEs, logs, build outputs, secrets.
  Status: completed

---

## Sprint 1 — Core Entries & Database (IN PROGRESS)

**Goal**: Implement entry CRUD with immutability enforcement and foundation for all features.  
**Duration**: Weeks 2-3 (10 business days)  
**User Story**: Create, read, edit, delete journal entries with same-day edit/delete enforcement

### Pending Tasks

- [ ] T101 - Design architecture & API contracts
  Description: Define high-level architecture (ASP.NET Core + PostgreSQL + React), data models, OpenAPI, and component boundaries. Create RFC if major infra decisions needed.
  Status: not-started

- [ ] T102 - Scaffold backend (ASP.NET Core)

- ID: 3
  Title: Database schema & migrations
  Description: Implement schema for users, entries, categories, media, audit logs, export jobs; add EF Core migrations and seed data for dev.
  Status: not-started

- ID: 4
  Title: Authentication & Authorization
  Description: Implement account auth (JWT or cookie), password hashing, session unlock mechanism for Private timelines, and role checks for APIs.
  Status: not-started

- ID: 5
  Title: Entries API (CRUD) & business rules
  Description: Implement endpoints for creating, listing, reading, editing, deleting entries with same-day edit/delete enforcement and confidentiality handling.
  Status: not-started

- ID: 6
  Title: Media storage integration
  Description: Integrate S3-compatible blob store for media, signed upload URLs, thumbnail generation, and CDN configuration guidance.
  Status: not-started

- ID: 7
  Title: Sentiment analysis & privacy pipeline
  Description: Decide on on-device vs server ephemeral compute; implement a privacy-preserving sentiment pipeline and model versioning; include 'no_training_use' enforcement.
  Status: not-started

- ID: 8
  Title: Mindmap service
  Description: Implement mindmap generation service (TF-IDF / local similarity for MVP) and graph JSON API; include export PNG/SVG option (async for large graphs).
  Status: not-started

- ID: 9
  Title: Scaffold frontend (React)
  Description: Create React app scaffold (Vite or CRA), routing, auth flow, and shared component library + design tokens.
  Status: not-started

- ID: 10
  Title: UI components & pages
  Description: Timeline, Entry editor, Unlock modal, Mindmap UI, Export/Import pages, Settings (timezone, confidentiality defaults).
  Status: not-started

- ID: 11
  Title: Export / Import jobs
  Description: Async export/import jobs, job tracking, signed download URLs, and import validation hooks enforcing confidentiality metadata.
  Status: not-started

- ID: 12
  Title: Testing: unit, integration, E2E
  Description: Add unit tests for business logic, integration tests (DB + blob), and E2E tests (Playwright/Cypress) for critical flows.
  Status: not-started

- ID: 13
  Title: CI / CD pipelines
  Description: Add GitHub Actions pipelines for linting, tests, build, migrations, and optional deployment; include performance test lane for PRs touching critical paths.
  Status: not-started

- ID: 14
  Title: Security & compliance checks
  Description: Implement encryption at rest in config, secrets management guidance, logging scrubbing, and automated audit jobs to verify 'no_training_use'.
  Status: not-started

- ID: 15
  Title: Documentation & CONTRIBUTING/PR templates
  Description: Add CONTRIBUTING.md, API OpenAPI spec, PR template with checklist (lint, tests, a11y, perf, security).
  Status: not-started

- ID: 16
  Title: Final QA, performance tuning & release
  Description: Run final QA, fix regressions, tune DB indexes and queries, prepare release notes and runbooks, and perform canary rollout.
  Status: not-started

- ID: 17
  Title: Create spec docs
  Description: Produce Plan, Quickstart, Research notes, and detailed data model files under .specify/specs/001-journal-ai.
  Status: completed

- ID: 18
  Title: .NET Aspire — verify latest stable release and runtime compatibility
  Description: Find Aspire's latest stable NuGet/package release, supported .NET runtime(s), and any compatibility notes with .NET 8/ASP.NET Core 8.
  Status: not-started

- ID: 19
  Title: .NET Aspire — integration patterns with EF Core & Npgsql
  Description: Research Aspire helper libraries or recommended patterns for EF Core integration, migrations, and Postgres connection tuning.
  Status: not-started

- ID: 20
  Title: .NET Aspire — authentication, unlock/session patterns
  Description: Research recommended authentication approaches when using Aspire (JWT/session), and best practices for per-entry unlock sessions.
  Status: not-started

- ID: 21
  Title: .NET Aspire — container images, deployment and production hardening
  Description: Find recommended Docker base images, container runtime settings, Kestrel tuning and production hardening guidance for Aspire apps.
  Status: not-started

- ID: 22
  Title: .NET Aspire — observability and telemetry integration
  Description: Research integration guidance or known issues for Aspire with OpenTelemetry, Application Insights, Datadog, and APMs.
  Status: not-started

- ID: 23
  Title: Audit implementation plan & map sequence
  Description: Create an implementation audit mapping step-by-step sequence of work, referencing spec docs and identifying gaps.
  Status: completed

- ID: 24
  Title: Create infra artifacts (docker-compose, openapi, CI)
  Description: Add docker-compose.example.yml, openapi.yaml skeleton, and a minimal GitHub Actions CI workflow.
  Status: completed

---

If you want this exported as a JSON or uploaded to your issue tracker (GitHub Issues), I can create that next.
