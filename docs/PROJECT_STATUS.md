# Project Status

**Last updated:** 2026-06-08
**Branch:** `001-featurename-develop-force`

This is the single source of truth for current build/test health and known
issues. Older status docs live in [`archive/`](archive/) and are not maintained.

## Build & test health

| Area | Status | How to check |
|------|--------|--------------|
| Backend build | ✅ 0 errors | `dotnet build src/backend/JournalAI.csproj` |
| Backend tests | ✅ 136 / 136 passing | `dotnet test src/backend/JournalAI.Tests.csproj` |
| Frontend type-check | ✅ 0 errors | `npx tsc --noEmit` (in `src/frontend`) |
| Frontend build | ✅ passing | `npm run build` (in `src/frontend`) |

## What works

- **Auth** — register / login / logout, JWT (HS256), bcrypt password hashing.
- **Entries** — create / list / view / edit / delete, same-day immutability,
  timezone-aware read-only boundary, pagination, tag/category filtering.
- **Sentiment** — client-side keyword analyzer; the real computed score, label,
  and model now persist end-to-end (regression-tested). Note: this was broken
  until recently — the client hardcoded a test value before it was fixed.
- **Media** — presigned S3/MinIO upload, metadata, async thumbnail generation
  (Hangfire), association with entries.
- **Visualizations** — list, mindmap, and sentiment-trend views.
- **Confidentiality / unlock** — private entries are gated at read time: list,
  single-entry read, and the similarity graph return them locked (content
  redacted) until the user unlocks. `POST /api/v1/unlock` verifies the account
  password and issues an account-wide session (1-hour TTL, revoked on logout);
  only a SHA-256 hash of the token is stored, and the client resends the raw
  token via the `X-Unlock-Token` header. Export is intentionally exempt (it's the
  owner's own full-data download). See FR-C2/FR-E9 in the spec.
- **Export / import** — synchronous JSON/Markdown export of all your entries, and
  JSON import (`POST /api/v1/imports`). Import is defensive: ownership is forced
  to the caller (file-supplied ids ignored), foreign/unknown `categoryId`s are
  dropped, unknown confidentiality defaults to `private`, and duplicate entries
  (same title+body+createdAt hash) are skipped so re-importing is idempotent.

## Known issues / follow-ups

- **ImageSharp moderate CVE** — `SixLabors.ImageSharp` is on `3.1.10` (latest
  3.x; all high-severity advisories cleared). One moderate advisory
  (GHSA-rxmq-m78w-7wmc) remains and is unpatched in all of 3.x. The fix is in
  4.x, which is a major version with breaking API changes and a license-tier
  consideration (Six Labors Split License) — deferred as a deliberate decision.
- The `tasks.md` in `specs/001-featurename-develop-force/` marks everything
  complete; treat it as a historical task log, not a guarantee of current
  behavior. (`spec.md` and `plan.md` in that folder are filled in and kept
  accurate; the canonical deep-dive spec lives under
  [`.specify/specs/001-journal-ai/`](../.specify/specs/001-journal-ai/).)

## Conventions worth knowing

- JWT config is keyed under `Jwt:Key` (issuer `journalai`, audience
  `journalai-users`) in both `appsettings.json` and `Program.cs`. Tokens map the
  `sub`/`email` claims to `ClaimTypes.NameIdentifier` / `ClaimTypes.Email`;
  controllers read those mapped claim types.
- Entry CRUD on the frontend lives in `src/frontend/src/services/entriesClient.ts`
  (single source of truth); `apiClient.ts` only owns the shared axios instance.
- Local dev uses SQLite (`journalai-dev.db`); production targets PostgreSQL. The
  `*.db` / `*.db-shm` / `*.db-wal` files are gitignored.
