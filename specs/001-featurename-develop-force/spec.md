# Feature Specification: Journal AI — core platform

**Feature Branch**: `001-featurename-develop-force`
**Status**: In development (core delivered; see status link below)

> This is the working feature spec: the summary, user stories, and **functional /
> non-functional requirements** below are the contract for the feature, with
> implemented-vs-planned status marked inline. Deeper rationale, algorithms, UX
> flows, and example payloads live in the canonical reference docs:
>
> - [specs.md](../../.specify/specs/001-journal-ai/specs.md) — full MVP→v1 spec, API, UX flows, clarifications
> - [data-model.md](../../.specify/specs/001-journal-ai/data-model.md) — schema and indexes
> - [TECHNICAL_PLAN_V2.md](../../.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md) — architecture and roadmap
>
> For **live build/test status**, see [docs/PROJECT_STATUS.md](../../docs/PROJECT_STATUS.md)
> and the [README](../../README.md). For the task log, see [tasks.md](./tasks.md).

## Summary

A privacy-first personal journaling platform: authenticated users create text and
media entries, organize them with categories and tags, and review them through
list, sentiment-trend, category mind-map, and text-similarity ("Related") views.
Entries become immutable at the end of their creation day (user's timezone).
Sentiment is computed client-side.

## User stories

### US1 — Capture an entry (P1)
**As** a journaler, **I want** to create an entry with a title, body, optional
category/tags, and confidentiality level, **so that** my thoughts are saved.
**Acceptance:** entry persists with a client-computed sentiment score; it appears
in the timeline; it becomes read-only after the end of its creation day.

### US2 — Authenticate (P1)
**As** a user, **I want** to register and log in, **so that** my journal is
private to me. **Acceptance:** JWT-based session; passwords hashed with bcrypt;
protected endpoints reject unauthenticated requests.

### US3 — Find and revisit entries (P2)
**As** a user, **I want** to search by text and filter by date/category/tag,
**so that** I can find past entries. **Acceptance:** `?q=` matches title/body
case-insensitively, combinable with filters.

### US4 — Organize with categories (P2)
**As** a user, **I want** to create categories and assign them to entries, **so
that** related entries group together. **Acceptance:** per-user categories with
unique names; entries display the category name, not an id.

### US5 — See relationships and trends (P2)
**As** a user, **I want** mind-map, sentiment-trend, and text-similarity views,
**so that** I can reflect on patterns. **Acceptance:** the Related view links
entries with similar text (TF-IDF / cosine similarity).

### US6 — Own my data (P2)
**As** a user, **I want** to attach media, export my journal, and import it back,
**so that** I control my data. **Acceptance:** media upload via presigned URLs;
export to JSON or Markdown; JSON import that assigns ownership to the caller and
skips duplicates (re-importing the same export is idempotent).

### US7 — Keep entries private (P2)
**As** a user, **I want** to mark entries private and have them hidden until I
re-enter my password, **so that** someone glancing at my open session can't read
them. **Acceptance:** private entries appear as locked placeholders in the
timeline and detail views; entering the account password unlocks them for the
session (1 hour, or until logout).

## Functional requirements

Status legend: ✅ implemented · ◑ partial · 🚧 planned (not yet built). This list
is the contract for the feature; the deeper rationale, algorithms, and example
payloads live in [`.specify/specs/001-journal-ai/specs.md`](../../.specify/specs/001-journal-ai/specs.md).

### Authentication & accounts
- **FR-A1** ✅ Users register and log in with email + password; passwords are hashed with bcrypt (≥12 rounds).
- **FR-A2** ✅ Sessions are JWT (HS256); `sub`/`email` claims map to `ClaimTypes.NameIdentifier`/`Email`. All entry, category, media, export, and import endpoints reject unauthenticated requests.
- **FR-A3** ✅ Every user/entry carries a `no_training_use` flag (defaulted on) recording that content must not be used to train models.
- **FR-A4** 🚧 GDPR-style permanent account deletion (purging entries + media) is not yet implemented.

### Entries — capture
- **FR-E1** ✅ A user creates an entry with optional title and body text, a `type` (`text`/`photo`/`video`/`mixed`), exactly one optional category, and zero or more flat tags.
- **FR-E2** ✅ Confidentiality (`public`/`private`) is **required** and validated on create and on import; an invalid/missing value on import defaults to the safer `private`.
- **FR-E3** ✅ On create, the server computes `read_only_after` = end of the creation calendar day (23:59:59) in the user's timezone, stored in UTC, and derives `immutable`.
- **FR-E4** ✅ A client-computed sentiment score (−1..1), label, and model version may be supplied and are persisted with the entry.

### Entries — read, edit, delete
- **FR-E5** ✅ List entries with pagination, filterable by date range, category, and tag, and searchable by `?q=` (case-insensitive substring over title/body), scoped to the caller.
- **FR-E6** ✅ Get a single entry by id; a non-owner receives 403, a missing entry 404.
- **FR-E7** ✅ Edit (PATCH) and delete are permitted only while `now ≤ read_only_after`; otherwise the server returns 403.
- **FR-E8** ◑ An append-only audit-log row is written on create, update, delete, and import. Unlock is an account-level event recorded via structured logging (it isn't tied to a single entry, so it doesn't fit the entry-scoped audit table).
- **FR-E9** ✅ Private entries are gated at read time: list, single-entry read, and the similarity graph return private entries **locked** (`locked:true`, content fields redacted) unless the caller presents a valid unlock token. Export is intentionally exempt (see Out of scope).

### Confidentiality & unlock
- **FR-C1** ✅ Each entry stores a confidentiality level and protection method (`account_password`/`none`).
- **FR-C2** ✅ `POST /api/v1/unlock` verifies the account password and issues an account-wide session (1-hour TTL, revoked on logout). Only a SHA-256 hash of the opaque token is stored; the client resends the raw token via the `X-Unlock-Token` header. The timeline and entry-detail views show locked placeholders with an unlock prompt. (Per-entry passwords are deferred to v2.)

### Categories & tags
- **FR-T1** ✅ Per-user category CRUD with names unique per user; entries reference at most one category.
- **FR-T2** ✅ Reads resolve and display the category name (not a raw id); deleting a category clears entry references rather than orphaning ids.

### Media
- **FR-M1** ✅ Media upload uses presigned S3/MinIO URLs; metadata is persisted and media can be associated with an entry.
- **FR-M2** ✅ Thumbnails are generated asynchronously via a Hangfire background job.

### Visualizations
- **FR-V1** ✅ Sentiment-trend chart over the user's entries (SVG).
- **FR-V2** ✅ Category/tag mind-map view (SVG).
- **FR-V3** ✅ Text-similarity "Related" graph via `GET /api/v1/entries/graph` (TF-IDF + cosine similarity, server-computed).
- **FR-V4** 🚧 Force-directed layout and PNG/SVG graph export are not implemented (the Related graph uses a deterministic circular layout).

### Export & import
- **FR-X1** ✅ Synchronous export of all the caller's entries as JSON or Markdown (`GET /api/v1/exports?format=`).
- **FR-X2** ✅ JSON import (`POST /api/v1/imports`) accepts the export shape; ownership is forced to the caller (file-supplied ids ignored), foreign/unknown `categoryId` is dropped, and duplicates (hash of title+body+createdAt) are skipped so re-importing is idempotent. Batch capped at 5000 entries.
- **FR-X3** 🚧 ZIP export with media, async/background export jobs (the reserved `export_jobs` table), and ZIP import are not yet implemented.

## Non-functional requirements

- **NFR-1 (latency, target)** GET list P95 < 300 ms; single-entry read P95 < 150 ms on typical infra. Indexes on `(user_id, created_at)` and `(user_id, category_id)` support this.
- **NFR-2 (privacy)** ✅ Sentiment is computed client-side; no entry text is sent to a third party for analysis. Raw entry text is not logged.
- **NFR-3 (data isolation)** ✅ Every data-access path filters by the authenticated `user_id`.
- **NFR-4 (storage)** Media lives in S3-compatible object storage; local dev uses SQLite, prod targets PostgreSQL.
- **NFR-5 (security, planned)** 🚧 Encryption at rest, API rate limiting, and automated `no_training_use` enforcement audits are specified but not yet implemented.
- **NFR-6 (observability, planned)** 🚧 OpenTelemetry/metrics/alerting are not yet wired up.

## Key entities

- **User** — id, email, password_hash, timezone, `no_training_use`, timestamps.
- **Entry** — id, user_id, title?, body_text?, type, confidentiality, confidentiality_method, category_id?, tags[], sentiment (score/label/model), created_at, updated_at?, read_only_after, immutable, metadata, source (`ui`/`import`/`api`), original_hash.
- **Category** — id, user_id, name (unique per user), color, parent_id?, timestamps.
- **AuditLog** — id, entry_id, user_id, action (`create`/`update`/`delete`/`import`/`unlock`), actor_ip, timestamp.
- **UnlockSession** — account-wide unlock session: id, user_id, entry_id (nullable; null = account-wide), session_token (SHA-256 hash of the opaque token), expires_at, created_at.
- **ExportJob** — reserved for async export (FR-X3); unused.

## Out of scope / planned

The 🚧-marked requirements above — GDPR account deletion (FR-A4), ZIP/media +
async export and ZIP import (FR-X3), force-directed/exportable graphs (FR-V4),
and the encryption-at-rest / rate-limiting / observability NFRs — are specified
but **not yet implemented**. See [docs/PROJECT_STATUS.md](../../docs/PROJECT_STATUS.md)
for current build status.

**Export and the unlock gate:** `GET /api/v1/exports` deliberately includes the
caller's private entries in full, even without an active unlock session — it is
the explicit "download all of my own data" action and is already owner-authenticated.
The spec's "confirm (and possibly re-enter password) before including private
entries in an export" refinement is a planned enhancement, tracked here so the
gap is explicit rather than silent.
