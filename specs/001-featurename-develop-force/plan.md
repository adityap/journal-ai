# Implementation Plan: Journal AI — core platform

**Branch**: `001-featurename-develop-force` | **Spec**: [spec.md](./spec.md)

> This file previously held the unfilled spec-kit template. The detailed
> architecture and roadmap live in
> [`.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md`](../../.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md);
> the data model is in
> [`data-model.md`](../../.specify/specs/001-journal-ai/data-model.md).
> Current build/test status is in [docs/PROJECT_STATUS.md](../../docs/PROJECT_STATUS.md).

## Technical context (as built)

- **Language/Runtime**: C# / .NET 8 (ASP.NET Core) backend; React 18 + TypeScript + Vite frontend
- **Primary dependencies**: EF Core 8, Hangfire (in-memory), AWSSDK.S3, SixLabors.ImageSharp, BCrypt.Net, JWT bearer
- **Storage**: SQLite (dev) / PostgreSQL (prod); S3-compatible object storage (MinIO dev / AWS S3 prod) for media
- **Testing**: xUnit (backend, 124 tests); frontend type-check + ESLint (no unit-test runner configured)
- **Project type**: web (separate backend API + SPA frontend under `src/`)
- **Auth**: JWT (HS256), `Jwt:Key` config; `sub`/`email` claims mapped to `ClaimTypes.NameIdentifier`/`Email`

## Structure (actual)

```text
src/
├── backend/      # ASP.NET Core API: Controllers/, Services/, Models/, Data/, Tests/
└── frontend/     # React + Vite: src/components/, src/services/, src/contexts/, src/utils/
```

## Constitution check (current state, not aspirational)

- [x] Auth: passwords hashed with bcrypt (≥12 rounds); JWT with expiry
- [x] Per-user data isolation enforced in controllers
- [x] Audit logging on entry creation
- [~] Test coverage: backend well-covered (124 tests); frontend has no unit-test runner
- [ ] Rate limiting — **not implemented** (planned)
- [ ] Encryption at rest — **not implemented** (planned)
- [ ] Broader audit logging (update/delete) — partial

See [docs/PROJECT_STATUS.md](../../docs/PROJECT_STATUS.md) for the authoritative
implemented-vs-planned breakdown.
