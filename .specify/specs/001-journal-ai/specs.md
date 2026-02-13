# Journal AI — Specification (MVP → v1)

## Clarifications

### Session 2026-02-13

- Q: Should entries have single or multiple categories? → A: **Single category per entry (1:1 relationship)** with flat tag support
- Q: Where should sentiment analysis run? → A: **Client-side only (sentiment.js library)**
- Q: What is the exact boundary for same-day edit/delete window? → A: **End of calendar day (23:59:59) in user's timezone**
- Q: How should users unlock private entries? → A: **Account password only (no per-entry passwords for MVP)**
- Q: When does export become asynchronous? → A: **User choice (let user pick sync vs async download option)**

---

## Purpose & Constraints

- Provide a personal journaling platform supporting text, photos, and videos.
- Persist entries with date/time, allow sorting, categorization, retrieval, export.
- Provide sentiment analysis for entries.
- Provide mind-maps at category / journal / note level.
- Privacy guarantee: journal content MUST NOT be used to train AI models. (See "Privacy & ML" below.)
- Edit/delete allowed only on the same calendar day; entries become read-only afterwards.
- Confidentiality levels: Public, Private. Private entries are hidden from timeline unless the user supplies a password (or unlock token) at view time.
- When accepting journal submissions (e.g., import/API), the confidentiality level must be provided and validated.

---

## MVP Feature List (priority)

1. Create journal entries: text, images, video (store metadata + content pointer).
2. Read entries: list, get by id, filter (date range, category, type), search by text.
- Edit/Delete policy: editable/deletable until 23:59:59 on created_at date in user's timezone; otherwise read-only.
4. Categories & Tags: each entry has ONE primary category (optional); multiple flat tags supported.
5. Timeline view: shows chronological entries, hides Private unless unlocked.
6. Confidentiality unlock: prompt for account password; ephemeral unlock (session-based, 1-hour timeout).
7. Export: export selected entries or full journal as JSON, Markdown, or ZIP (media included).
8. Sentiment analysis: per-entry sentiment score + overall trend visual (client-side only using sentiment.js; no server-side processing in MVP).
9. Mind-map: generate a node graph at category/journal/note level; export PNG/SVG.
10. Onboarding tips: suggest journaling habits and examples.
11. Import API: accept bulk entries with confidentiality metadata.
12. Audit/logging: records of edits/deletes (authorized user only).

---

## Non-functional requirements

- Availability: 99.9% for user-facing APIs.
- Latency: GET list P95 < 300ms; read single entry P95 < 150ms (on typical backend infra).
- Storage: media stored in blob store (S3-compatible) with encryption at rest.
- Data retention & deletion: Users can permanently delete entries; deletion must remove media from storage.
- Security: authenticated users only; optional per-entry password for "Private" view gating.
- Privacy: explicit prohibition: data will not be used to train AI models; include logs and consent banner. Store this as a policy in DB + enforce in datapath.

---

## UX / Flow Summaries

1. Create Entry
   - UI: "New Entry" → choose type (Text/Photo/Video) → input content, choose category, add tags, set confidentiality (Public/Private), add timestamp override (optional).
   - Backend: POST /api/v1/entries
   - Validation: confidentiality must be present; if Private, optional password set (or rely on user's account password unlocking mechanism).

2. Timeline
   - UI: chronological feed; Private entries display as locked placeholders (title/date) unless session unlocked.
   - Unlock: user taps a locked item → prompt account password → if correct, unlock all private entries for session (1-hour timeout).

3. Edit / Delete
   - UI: edit/delete actions visible only if entry.created_at is today (calendar day in user's timezone).
   - Backend: PATCH /api/v1/entries/{id} allowed only before timezone-day end; DELETE similar.
   - Auditing: record who edited/deleted and the original content (append-only audit log) if retention policy allows.

4. Mind-map
   - UI: "Create Mindmap" → choose scope (Category / Journal / Single Entry) → run generation → interactive node graph with expand/collapse, drag, link creation.
   - Backend: server returns graph JSON (nodes/edges) computed from metadata, content keywords, tags, semantic similarity.
   - Export: PNG/SVG/JSON.

5. Export
   - UI: select entries or date range → offer choice: "Download now" (sync) or "Email when ready" (async) → export as .zip containing markdown files and media.
   - Backend: Sync download for immediate requests; async job for large exports or if user selects async option; notify when ready.

6. Import
   - API / UI import: accept JSON/ZIP with confidentiality metadata; validate and store.

---

## Data Model (core)

- User
  - id, email, password_hash, settings (timezone, export preferences), created_at

- Entry
  - id (uuid)
  - user_id
  - title (optional)
  - body_text (string, optional)
  - media: array of { id, type (image|video), url, mime, size, thumbnail_url }
  - category_id (uuid, optional; max 1 category per entry)
  - tags: [string] (flat; no hierarchy)
  - type: enum('text','photo','video','mixed')
  - confidentiality: enum('public','private')
  - confidentiality_protection: { method: 'account_password' | 'none' } // MVP: account password only; per-entry passwords deferred to v2
  - sentiment: { score: float (-1..1), label: 'positive'|'neutral'|'negative', model_version: string, computed_at }
  - created_at (datetime with timezone)
  - updated_at
  - read_only_after: datetime // computed: end of calendar day in user timezone
  - immutable: boolean // derived (true if now > read_only_after)
  - metadata: free JSON (language, mentions, location, etc.)
  - source: enum('ui','import','api')
  - original_hash // for dedupe if needed

- Category
  - id, user_id, name, color, parent_id (nullable), created_at, updated_at

- AuditLog
  - id, entry_id, user_id, action('create','update','delete','unlock'), actor_ip, timestamp, diff (optional encrypted)

- ExportJob
  - id, user_id, scope, format, status, download_url, created_at, completed_at

Notes:
- If using relational DB (Postgres), store media in blob store and media meta in Media table.
- For performance, index on (user_id, created_at), (user_id, category_id), and full-text index on body_text.

---

## API Surface (REST JSON examples)

- POST /api/v1/entries
  - body: { title, body_text, category_id, tags, confidentiality, confidentiality_protection, created_at?, media: [upload ids], source }
  - response: 201 { id, read_only_after, immutable }

- GET /api/v1/entries?start=&end=&category=&tag=&type=&page=&per_page=
  - returns list with confidentiality status; if private and not unlocked, returns {id, date, locked: true, title_hint}

- GET /api/v1/entries/{id}?unlock_token=...
  - if private requires unlock: either session unlocked or include unlock token/password
  - response: full entry

- PATCH /api/v1/entries/{id}
  - allowed only if immutable == false; otherwise 403

- DELETE /api/v1/entries/{id}
  - allowed only if immutable == false; will enqueue media deletion and log audit

- POST /api/v1/unlock
  - body: { entry_id, password } or { user_password } // returns session token to view private entries for session

- POST /api/v1/categories
  - manage categories

- POST /api/v1/exports
  - body: { scope: { entries:[ids] } | { date_range }, format: 'json'|'md'|'zip' }
  - returns job id

- GET /api/v1/mindmap
  - params: scope (category|entry|user), id, options (similarity_threshold)
  - returns graph JSON

- POST /api/v1/import
  - body: { entries: [entry objects with confidentiality] }
  - validation: confidentiality required per entry

Security: all sensitive data (entry_password hashes, audit) encrypted at rest and in transit.

---

## Sentiment Analysis (privacy-preserving)

Options:
- On-device (client computes sentiment using local model or local JS library): best privacy (no data leaves user device).
- Server-side ephemeral compute: server computes sentiment but does not persist raw content beyond compute; only stores aggregated sentiment result and model version; raw content is not logged or retained for training. Implement strict pipeline:
  - Sentiment job receives text, computes score, returns score.
  - Ensure logs scrub raw text; logging stores only entry id and model_version.
  - Add policy checkbox and stored "no_training_consent" flag for the account.
Recommendation: start with an on-device sentiment library (e.g., a lightweight JS sentiment model or rule-based (vader-like) for offline web/mobile). If server-side required, implement strict non-retention and internal policy + automated audits, and store the "no_training" flag.

Model versioning:
- Store sentiment.model_version on the Entry for traceability.
- If models change, allow backfilling or re-computing with user permission.

---

## Mind-map Generation (algorithm)

- Input: scope (entries in category or tags or single entry)
- Steps:
  1. Extract semantic tokens: keywords via TF-IDF + named entities, plus tags and categories.
  2. Compute pairwise similarity (cosine) on vector embeddings (local sentence-transformer or approximate textual similarity).
  3. Create nodes = entries + category nodes; edges when similarity > threshold or explicit tag/category link.
  4. Add optional clustering (community detection) for layout.
- Implementation options:
  - Client-side: use lightweight similarity (TF-IDF + cosine) for small scopes.
  - Server-side: use embeddings service (if allowed) but ONLY with user's explicit opt-in and non-retention.
- Provide UI to let users merge/split nodes and persist user-created edges.

---

## Confidentiality & Unlocking policy

- Confidentiality levels are required when creating or importing entries.
- Two unlocking modes:
  1. Account-based unlock: user unlocks Private entries for the session by entering account password; server issues short-lived unlock session token.
  2. Entry-password: entries can set their own password (hash stored); user (or import source) must supply password to view.
- Timeline hides Private entries entirely unless unlocked; locked placeholder should show date, category, and optional hint (user-supplied).
- When accepting entries via API, require confidentiality field and, if private and entry-password used, the entry password hash must be stored (never plaintext). Validate strength if provided.

---

## Edit/Delete rules

- Determine user's timezone from settings at creation.
- read_only_after = end-of-day (23:59:59) in user's timezone on created_at date.
- Allow edits/deletes only if now <= read_only_after.
- For compliance and audit, retain an append-only AuditLog entry for edits/deletes (store diffs encrypted).
- UI should show a small badge "editable until <local time>" on entries within the same day.

---

## Import / Acceptance contract

- When accepting bulk or API imports, each entry must include:
  - required: user_id, created_at, confidentiality
  - optional: content fields
- Server validates confidentiality present and rejects imports lacking confidentiality for entries.

---

## Export contract

- Exports return either direct download or async job with signed URL.
- Export content includes metadata: created_at, category, tags, confidentiality; when exporting Private entries, require user to confirm (and possibly re-enter password) to include in export.

---

## Testing & QA

- Unit tests: all API endpoints, business logic (edit window enforcement), confidentiality gating.
- Integration tests: export job, media upload/download, import validation, mindmap generation.
- E2E tests: create/read/unlock/edit/export flows.
- Accessibility tests: timeline view, unlock prompt, mobile responsiveness.
- Performance tests: listing queries with large numbers (10k entries), mindmap generation with large scope (1000 nodes — implement server-side limit).
- Flaky test policy: quarantine unstable tests with ticket.

---

## Security & Compliance

- Authentication: OAuth2/JWT or session cookie; enforce strong password policies.
- Encryption: TLS in transit; media & sensitive fields encrypted at rest.
- Secrets: Use managed secrets for storage keys; never store plaintext passwords.
- Data deletion: when user deletes entries, remove from DB and enqueue media deletion; confirm deletion with user.
- No training clause: add a data-policy record for each user indicating "no_training_use: true/false". Enforce that any model training pipeline checks these flags and excludes data. Add automated audits to verify no training uses user data.
- Logging: do not log raw entry text. If logging required for debugging, use scrubbing and store only entry ids.

---

## Telemetry & Observability

- Events: entry.create, entry.update, entry.delete, entry.unlock, export.created, mindmap.created
- Metrics: API latency, export job duration, entry counts per user, P95 retrieval times, error rates.
- Alerts: export job failures rate > 1% per hour, unlock failure spike, auth failures.

---

## Performance & Scaling

- DB: use partitioning or sharding per user if scale grows.
- Query optimization: index user_id+created_at; support cursor-based pagination.
- Media: serve via CDN, pre-generate thumbnails.
- Mindmap generation: limit scope for on-demand generation; use async jobs for large graphs.
- Export: async job with worker queue.

---

## UX Guidance & Journaling Suggestions

- Default prompts: 'What happened today?', 'What are you grateful for?', 'One highlight', 'A challenge and one action'.
- Habit nudges: optional daily reminders, streak counter, weekly summary (most common sentiment).
- Onboarding: show example entries and categories; let users import sample categories.
- Differentiating types: display media thumbnails, type badges, and category colors.

---

## Acceptance Criteria (for MVP)

- Create, retrieve, list entries with confidentiality metadata.
- Private entries are hidden in timeline unless unlocked.
- Edit/Delete permitted same-day only.
- Export selected entries (JSON/MD) including media.
- Basic sentiment analysis computed and visible per entry.
- Mindmap generation for small scopes (<=100 nodes).
- Policy flag ensuring data not used for model training.
- Tests covering critical flows + CI checks.

---

## Implementation Roadmap (sprints)

- Sprint 0 (setup, 1 week): repo, CI basic checks, DB schema, auth, media storage integration.
- Sprint 1 (core entries, 2 weeks): create/read/list, categories, confidentiality metadata, same-day edit/delete enforcement.
- Sprint 2 (media & export, 2 weeks): media uploads, thumbnails, export job, import validation.
- Sprint 3 (sentiment & privacy, 2 weeks): integrate on-device sentiment or server ephemeral compute, implement "no_training" flags and audits.
- Sprint 4 (mindmap & UX polish, 2 weeks): mindmap generation, UI polish, accessibility audit.
- Sprint 5 (scale & monitoring, 2 weeks): performance tests, dashboards, and optimizations.

---

## Example JSON payloads

Create entry (POST /api/v1/entries)

```
{
  "title": "Morning thoughts",
  "body_text": "Felt productive today. Wrote some ideas about a side project.",
  "category_id": "c_123",
  "tags": ["productivity","ideas"],
  "type":"text",
  "media": [],
  "confidentiality":"private",
  "confidentiality_protection": { "method":"entry_password", "password_hash":"<salted-hash>" },
  "created_at": "2025-11-19T09:12:00-05:00"
}
```

Unlock entry (POST /api/v1/unlock)
```
{ "entry_id":"e_123", "password":"hunter2" }
```

Mindmap request (GET /api/v1/mindmap?scope=category&id=c_123&threshold=0.6)

---

## QA checklist for PRs affecting these features

- Lint and type checks pass.
- Unit tests for business logic.
- Integration tests for import/export pipelines.
- Accessibility checks for UI changes.
- Security review for confidentiality storage and audit logging.
- Performance smoke test for listing and mindmap generation.

---

*Spec created: 2025-11-19*
