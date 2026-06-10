# Feature Specification: Journal AI — core platform

**Feature Branch**: `001-featurename-develop-force`
**Status**: In development (core delivered; see status link below)

> This file previously held the unfilled spec-kit template. The **canonical,
> detailed specification** for Journal AI lives under
> [`.specify/specs/001-journal-ai/`](../../.specify/specs/001-journal-ai/):
>
> - [specs.md](../../.specify/specs/001-journal-ai/specs.md) — full MVP→v1 spec, API, UX flows, clarifications
> - [data-model.md](../../.specify/specs/001-journal-ai/data-model.md) — schema and indexes
> - [TECHNICAL_PLAN_V2.md](../../.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md) — architecture and roadmap
>
> For **current build/test status and the implemented-vs-planned breakdown**, see
> [docs/PROJECT_STATUS.md](../../docs/PROJECT_STATUS.md) and the
> [README](../../README.md). For the task log, see [tasks.md](./tasks.md).

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
**As** a user, **I want** to attach media and export my journal, **so that** I
control my data. **Acceptance:** media upload via presigned URLs; export to JSON
or Markdown.

## Out of scope / planned

Account-based private-entry unlock sessions, GDPR account deletion, ZIP/media
export, bulk import, and ranked full-text search are specified in the canonical
spec but **not yet implemented** — see [docs/PROJECT_STATUS.md](../../docs/PROJECT_STATUS.md).
