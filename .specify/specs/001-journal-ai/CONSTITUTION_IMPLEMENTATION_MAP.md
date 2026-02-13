# Constitution v1.1.0 — Implementation Map

**Purpose**: Map analysis findings to constitution principles; provide concrete implementation guidance for development.

**Status**: Ready for Sprint 0 → Sprint 1

---

## Critical Issues Resolved by Constitution

| Analysis ID | Issue | Resolution in Constitution | Section | Implementation Owner |
|------------|-------|---------------------------|---------|---------------------|
| A1 | Error response formats undefined | Added error schema + per-endpoint specs | Principle IV (API Contract) | Backend (Task 1) |
| A2 | Immutable field transition unclear | Documented daily cron trigger at 00:30 UTC | Principle V (Security) | Backend (Task 3) |
| A3 | Privacy guarantee vs sentiment conflict | Locked to on-device only for MVP | Principle II (Protection) | Backend (Task 7) |
| A4 | Export auth flow unclear | Documented unlock session carryover + re-auth option | Principle IV (API) + Principle I (Privacy) | Backend (Task 11) |
| A5 | Rate-limiting undefined | Defined: 100 req/min user, 10 req/sec IP, 30s backoff | Principle IV (API) | Backend (Task 4.5) |
| A6 | Category relationship ambiguous | NOT RESOLVED — add RFC decision before Sprint 1 | Governance | TBD |
| A7 | Timezone change handling | Documented freeze policy: read_only_after never recalculated | Principle I (Privacy) | Backend (Task 5) |
| A8 | Similarity metric units | Defined cosine (0–1), threshold default 0.6, range 0.1–0.95 | Principle IV (API) | Backend (Task 8) |
| A9 | Sentiment compute path | On-device only for MVP; server requires separate RFC | Principle II (Protection) | Frontend (Task 7) |
| A10 | Media upload limits | Defined: 100 MB max, MIME types, image 4000×4000, video 1 hr | Principle IV (API) | Backend (Task 6) |
| A11 | Mindmap scope limit | 100 nodes: error 400; >100: enqueue async (5 min SLA) | Principle IV (API) + Principle V | Backend (Task 8) |
| A12 | Performance testing gate | Required gate on PRs touching critical paths | Principle V (Security) | CI (Task 13) |
| A13 | Audit log retention | 90-day retention; auto-purge after; user deletion removes all | Principle I (Privacy) | Backend (Task 14) |
| A14 | Thumbnail spec | 300×300 px, JPEG 80%, regenerate on media change | Principle IV (API) | Backend (Task 6) |
| A15 | Import schema | Required/optional fields documented; title OR body required | Principle IV (API) | Backend (Task 11) |
| A16 | Session lifecycle | 1-hour idle timeout, 24-hour max, logout invalidates, max 1 concurrent | Principle V (Security) | Backend (Task 4) |
| A17 | Export async threshold | >50 entries OR >10 MB → async (15 min SLA) | Principle IV (API) | Backend (Task 11) |
| A18 | Password policy | 12 char min, 3/4 char classes (account); 8+ recommended (entry) | Principle V (Security) | Backend (Task 4) |
| A19 | Deduplication algorithm | NOT RESOLVED — add to import RFC or defer to v2 | Governance | TBD |
| A20 | Onboarding content | NOT RESOLVED — add content model RFC before Sprint 0 | Governance | TBD |

**Summary**: Constitution resolves **16/20 analysis gaps**. Remaining 4 deferred to RFCs (non-blocking for MVP).

---

## Constitution Principles → Implementation Checklist

### Principle I: Privacy-First Data Handling

**In Each Feature Spec (spec.md)**:
- [ ] Document which data the feature touches
- [ ] Confirm `no_training_use=true` enforcement (weekly audit listed in Principle II)
- [ ] Define data retention: indefinite, 90-day, or per-user request?
- [ ] Note any external services that see data (must have RFC approval)

**In Code (All Services)**:
- [ ] Before any external API call or pipeline access: check `user.no_training_use` flag
- [ ] Add comments with data classification (PII, journal entry, sentiment score, etc.)
- [ ] Link issue to data compliance ticket for audit

**In CI/CD**:
- [ ] Weekly privacy audit: scan code + DB + external services for `no_training_use` violations
- [ ] Alert on violations; auto-pause user account + notify

**Ownership**: Backend & DevOps (Task 14 — Security & Compliance)

---

### Principle II: User-Data Protection & No-Training Guarantee

**Sentiment Analysis (MVP)**:
- [ ] Use `sentiment.js` (client-side library) ONLY
- [ ] Store sentiment result with model_version: `sentiment.js/v1.0`
- [ ] NO server-side processing of raw text
- [ ] Add feature flag `SENTIMENT_ENABLED` (default: true)

**Future Server-Side** (requires RFC + separate sprint):
- [ ] Design ephemeral compute: input → compute → delete
- [ ] Add user opt-in consent flow
- [ ] Encrypt data in flight + scrub logs
- [ ] Implement automated audit of pipeline

**Audit Logging**:
- [ ] Every data access: entry read, unlock attempt, export, mindmap gen
- [ ] Format: `{ user_id, action, timestamp, actor_ip, resource_id }`
- [ ] Retention: 90 days auto-purge (or legal hold on request)

**Automation**:
- [ ] Weekly scan (Sunday 02:00 UTC): verify no user data in external services
- [ ] Slack alert on violation
- [ ] Runbook: pause account, notify user, investigate

**Ownership**: Backend (Task 7), DevOps (Task 14)

---

### Principle III: Test-First Development (TDD Mandatory)

**For Every New Feature Task**:
- [ ] Test first: write failing unit tests before implementation
- [ ] Approval: get test + acceptance criteria reviewed before coding
- [ ] Test passes: code review verifies tests are meaningful

**Coverage Targets**:
- [ ] Project-wide: ≥80% baseline
- [ ] Core (Entries, Auth, Export): ≥90%
- [ ] Sentiment, Mindmap: ≥85%
- [ ] Shortfalls: justify in PR + ticket for follow-up

**Test Types & Speed**:
- [ ] Unit tests: <1 sec each, <10 min total (runs on every commit)
- [ ] Integration: <500 ms each, separate CI lane (runs on PR)
- [ ] E2E: <5 sec each, separate CI lane (runs on merge)

**Ownership**: All developers (enforced in CI, Task 13)

---

### Principle IV: API Contract Excellence

**For Every Endpoint**:
- [ ] OpenAPI 3.0 schema with request/response examples
- [ ] Error codes documented: 400, 403, 404, 409, 429, 500
- [ ] Error response format: `{ code: string, message: string, details?: object }`
- [ ] No behavior changes without MAJOR version bump or feature flag

**Rate-Limiting** (enforced at API Gateway or middleware):
- [ ] Authenticated user: 100 requests/min
- [ ] Per IP: 10 requests/sec
- [ ] Exceeded: 429 with `Retry-After: <seconds>` header
- [ ] Backoff: exponential, 30 sec initial delay

**Media Upload Validation**:
- [ ] Max 100 MB per file
- [ ] MIME types: image/jpeg, image/png, image/webp, video/mp4, video/webm
- [ ] Image max: 4000×4000 px
- [ ] Video max: 1 hour duration
- [ ] Rejection: 400 with field-specific error

**Thumbnail Generation**:
- [ ] Auto-generate for all uploads
- [ ] Size: 300×300 px, JPEG quality 80%
- [ ] Regenerate if media re-uploaded
- [ ] Cache old thumbnails for 30 days

**Ownership**: Backend (Tasks 1, 5, 6, 11)

---

### Principle V: Security & Encryption Standards

**Network & Data Encryption**:
- [ ] TLS 1.2+ for all API traffic (HTTPS only)
- [ ] Blob store: AES-256 encryption at rest
- [ ] Passwords: bcrypt with salt cost ≥12
- [ ] Audit logs: encrypted with backend master key (if stored)

**Secrets Management**:
- [ ] DB passwords, API keys, blob credentials: managed secrets store
- [ ] Never commit secrets to repo
- [ ] Never include `.env` in git
- [ ] Rotate secrets quarterly

**Sessions & Tokens**:
- [ ] Account password hash: bcrypt ≥12
- [ ] Session tokens: 1-hour idle timeout, 24-hour max lifetime
- [ ] Unlock tokens: 30-min expiry or logout, whichever first
- [ ] Max 1 concurrent session per user (or document multi-device policy)

**Immutable Field Trigger**:
- [ ] Daily cron: 00:30 UTC
- [ ] Query: entries with `immutable=false` AND `read_only_after < now()`
- [ ] Action: set `immutable=true`
- [ ] Logging: audit log entry per update (no detail needed)

**Dependency Security**:
- [ ] Weekly scan for known vulnerabilities (CVSS ≥5.0 triggers alert)
- [ ] Remediation ticket created automatically
- [ ] Update within 2 weeks or document exception with ticket

**Ownership**: Backend (Tasks 4, 14), DevOps (Task 13)

---

## Sprint Mapping

### Sprint 0 (Architecture & Scaffolding) — 1 Week

**From Constitution**:
- [ ] Infrastructure for TLS, encryption at rest (Task 1)
- [ ] Secrets management setup (Task 1)
- [ ] CI pipeline structure for linting, tests (Task 13 — partial)
- [ ] OpenAPI generation framework (Swashbuckle) (Task 1)
- [ ] Rate-limiting middleware scaffold (Task 1)

**Acceptance**: Setup complete; ready for Sprint 1 data model + auth.

---

### Sprint 1 (Core Entries & Auth) — 2 Weeks

**From Constitution**:
- [ ] Entries CRUD with error schema (all 6 error codes + per-endpoint spec) — **Principle IV** (Task 5)
- [ ] Same-day edit/delete enforcement + timezone handling — **Principle I** (Task 5)
- [ ] Confidentiality validation (required field) — **Principle II** (Task 5)
- [ ] Auth (JWT, session, unlock tokens with TTL) — **Principle V** (Task 4)
- [ ] Password hashing (bcrypt ≥12) + policy — **Principle V** (Task 4)
- [ ] Audit logging for mutations (create, update, delete, unlock) — **Principle II** (Task 14 — partial)
- [ ] Unit tests ≥90% coverage on Entries & Auth — **Principle III** (all)

**QA Gate**:
- [ ] All endpoints documented in OpenAPI with error schemas
- [ ] Coverage report shows ≥90% for core modules
- [ ] TDD satisfied: tests written first, reviewed, then code

---

### Sprint 2 (Media & Export) — 2 Weeks

**From Constitution**:
- [ ] Media upload validation (size, MIME, dimensions) — **Principle IV** (Task 6)
- [ ] Thumbnail generation (300×300, JPEG 80%) — **Principle IV** (Task 6)
- [ ] Export async threshold (>50 entries OR >10 MB) — **Principle IV** (Task 11)
- [ ] Export format spec (JSON, Markdown, ZIP) — **Principle IV** (Task 11)
- [ ] Import schema validation (required/optional fields) — **Principle IV** (Task 11)
- [ ] Confidentiality enforcement on import (required field) — **Principle II** (Task 11)
- [ ] Unit tests ≥80% on export/import — **Principle III** (Task 12)

**QA Gate**:
- [ ] Media validator rejects invalid files with proper 400 errors
- [ ] Thumbnails auto-generated correctly
- [ ] Export async job SLA met (15 min for typical)
- [ ] Import rejects entries missing confidentiality field

---

### Sprint 3 (Sentiment & Privacy Pipeline) — 2 Weeks

**From Constitution**:
- [ ] On-device sentiment (sentiment.js, v1.0 model version) — **Principle II** (Task 7)
- [ ] NO server-side sentiment processing (MVP lock) — **Principle II** (Task 7)
- [ ] Audit logging: all data access events (weekly automated scan) — **Principle II** (Task 14)
- [ ] `no_training_use` enforcement in code + weekly audit job — **Principle I** (Task 14)
- [ ] User data export (GDPR right, within 48 hours) — **Principle I** (Task 11)
- [ ] Integration tests for privacy audit pipeline — **Principle III** (Task 12)

**QA Gate**:
- [ ] No raw entry text reaches server-side sentiment
- [ ] Weekly privacy audit runs and alerts on violations
- [ ] Audit logs retained for 90 days, auto-purged
- [ ] GDPR export completes within 48 hours

---

### Sprint 4 (Mindmap & UX) — 2 Weeks

**From Constitution**:
- [ ] Mindmap generation (TF-IDF, cosine similarity, threshold 0.1–0.95, default 0.6) — **Principle IV** (Task 8)
- [ ] Mindmap scope limit (100 nodes: error 400; >100: async) — **Principle IV** (Task 8)
- [ ] Error responses documented (400 for oversized) — **Principle IV** (Task 8)
- [ ] Unit tests ≥85% on mindmap service — **Principle III** (Task 12)
- [ ] E2E tests for critical user flows (create/read/unlock/export) — **Principle III** (Task 12)

**QA Gate**:
- [ ] Mindmap API returns proper error on >100 nodes
- [ ] Async job completes within 5 min SLA
- [ ] OpenAPI spec complete for all endpoints

---

### Sprint 5 (Testing & Performance) — 2 Weeks

**From Constitution**:
- [ ] Coverage report ≥80% project-wide, ≥90% core — **Principle III** (Task 12)
- [ ] CI performance regression checks on critical paths — **Principle V** (Task 13)
- [ ] Dependency security scan automated (weekly) — **Principle V** (Task 13)
- [ ] Privacy audit automation (weekly, Slack alerts) — **Principle II** (Task 14)
- [ ] Database index optimization (entry list P95 < 300ms) — **Principle V** (Task 5, 6)
- [ ] Immutable field trigger (daily cron) deployed — **Principle V** (Task 14)

**QA Gate**:
- [ ] All quality gates pass
- [ ] Performance regressions detected and documented
- [ ] Monitoring dashboards in place

---

## Unmapped Issues (Not Resolved by Constitution)

**Still Need RFC/Decision**:
1. **A6 — Category Relationship**: 1:1, M:M, or hierarchical? (Technical design, affects schema)
2. **A19 — Deduplication Algorithm**: SHA-256 hash + comparison logic (Import design detail)
3. **A20 — Onboarding Content**: Prompts, habit nudges, sample categories (Product design)

**Action**: Owner creates mini-RFC (1 page) before Sprint 0 kickoff. Can proceed with implementation as these are non-blocking for core MVP.

---

## Enforcement Checklist

**Before Sprint 0 Starts**:
- [ ] Constitution v1.1.0 reviewed and approved by tech lead
- [ ] Templates updated (spec-template, plan-template, tasks-template) — ✅ Done
- [ ] CI configured with coverage gates >=80%
- [ ] OpenAPI framework (Swashbuckle) added to .csproj
- [ ] Rate-limiting middleware selected and scaffolded
- [ ] Secrets management store configured (KeyVault/Vault/sealed-secrets)

**During Development (Every Sprint)**:
- [ ] PR template includes constitution checklist
- [ ] Coverage reports generated and reviewed
- [ ] Privacy audit automation in CI (at least stubbed)
- [ ] OpenAPI spec kept in sync with endpoints
- [ ] Error response formats consistent across all endpoints

**At Merge (Quality Gate)**:
- [ ] TDD passed: tests written first, reviewed, then code
- [ ] Coverage ≥80% (affected modules) or justified
- [ ] All API endpoints documented in OpenAPI
- [ ] Error responses follow schema
- [ ] Privacy declaration complete (if data handling)
- [ ] Security review done (if auth/encryption/audit changes)

---

## Communication & Training

**Team Onboarding**:
1. Read [.specify/memory/constitution.md](.specify/memory/constitution.md) (v1.1.0)
2. Review this implementation map
3. Check PR template for enforcement checklist
4. Attend 15-min walkthrough on TDD gate + privacy audit

**References**:
- Constitution: [.specify/memory/constitution.md](.specify/memory/constitution.md)
- Spec template: [.specify/templates/spec-template.md](.specify/templates/spec-template.md) (updated with Privacy Declaration & Error Handling)
- Tasks template: [.specify/templates/tasks-template.md](.specify/templates/tasks-template.md) (updated with task tags)
- API Spec: `openapi.yaml` (auto-generated from code)
- Runbooks: TBD (created after Sprint 0)

---

**Status**: ✅ Ready for development kickoff.

**Next Step**: Run `/speckit.implement` to begin Sprint 0 with architecture & scaffolding.
