# Journal AI — Constitution v1.1.0

**Ratification Date**: 2025-11-19  
**Last Amended**: 2026-02-13  
**Version**: 1.1.0 (MINOR: Added security/privacy principles, technical standards, API requirements)

---

## Sync Impact Report (v1.0.0 → v1.1.0)

**Changes**:
- Added PRINCIPLE_1: Privacy-First Data Handling
- Added PRINCIPLE_2: User-Data Protection & No-Training Guarantee
- Added PRINCIPLE_3: Test-First Development (TDD Mandatory)
- Added PRINCIPLE_4: API Contract Excellence
- Added PRINCIPLE_5: Security & Encryption Standards
- Added SECTION_2: Security, Privacy & Compliance Requirements
- Added SECTION_3: Technical & API Standards
- Completed governance rules with Journal-AI specifics

**Templates Updated**:
- `.specify/templates/spec-template.md` — ✅ Add "Privacy Declaration" & "Error Handling" sections
- `.specify/templates/plan-template.md` — ✅ Add "Privacy Compliance Checklist" & "Test Coverage Gate"
- `.specify/templates/tasks-template.md` — ✅ Add task tags: `privacy-critical`, `test-gate`, `security-review`
- `src/backend/JournalAI.csproj` — ⚠️ Pending: Add test coverage target
- GitHub Actions CI — ⚠️ Pending: Add privacy audit + coverage enforcement

**Deferred Decisions**:
- None (all critical principles defined for MVP)

---

## Core Principles

### I. Privacy-First Data Handling

**MUST**: All user journal data is confidential and belongs solely to the user. The application MUST NOT use user data for any purpose beyond the user's explicit request.

**How it works**:
- Every user account has a `no_training_use` flag set to `true` by default and immutable without formal consent.
- All data pipeline code must check this flag before any processing beyond immediate request handling.
- No data is shared, sold, or used for model training without explicit signed user consent.
- Upon user deletion, all related data and media are permanently removed from all storage systems (DB + blob store) within 24 hours.

**Rationale**: Journal entries contain deeply personal, sensitive information. Trust is foundational to the product.

---

### II. User-Data Protection & No-Training Guarantee

**MUST**: Implement enforcement, auditing, and transparency for the no-training guarantee.

**Requirements**:
- Sentiment analysis MUST run on-device (client-side JavaScript library) for MVP. Server-side compute is NOT permitted without re-architecting for strict ephemeral processing + encryption + automated audits.
- Mindmap generation uses only local TF-IDF; no embeddings-as-a-service unless explicitly approved by user.
- Audit logging: every data access event must be recorded and user-reviewable. Logs are retained for 90 days then deleted unless required by law.
- Automated weekly scan: validate that NO data with `no_training_use=true` appears in any ML pipelines, inference models, or external services. If breach detected, trigger alert + immediate user notification.

**Rationale**: Privacy is a differentiator and competitive advantage; legal/reputational risk on violations is existential.

---

### III. Test-First Development (TDD Mandatory)

**MUST**: All non-trivial code changes follow TDD discipline.

**Process**:
1. Write failing unit test(s) that capture the acceptance criteria.
2. Get test and acceptance criteria reviewed/approved by another developer or QA.
3. Implement code to pass tests.
4. Code review includes verification that tests are meaningful and tests pass.

**Coverage targets**:
- Project-wide baseline: ≥ 80% unit test coverage (measured on merged main).
- Core modules (Entries, Auth, Export/Import): ≥ 90% coverage.
- Sentiment, Mindmap services: ≥ 85% coverage.
- Coverage shortfalls must be justified in PR with a follow-up ticket.

**Test categories**:
- Unit tests (pure logic, mocked I/O): <1 sec per test; <10 min total.
- Integration tests (DB, blob store, real dependencies): <500 ms per test; run separately from unit tests.
- E2E tests (browser automation): <5 sec per test; run on merge, not per-commit.

**Rationale**: Privacy-critical features and confidentiality logic require bulletproof tests. TDD reduces rework and security bugs.

---

### IV. API Contract Excellence

**MUST**: Every API endpoint is a contract that must not break users.

**Requirements**:
- All endpoints MUST have documented request/response schemas in OpenAPI 3.0 format with examples.
- All error codes (400, 403, 404, 409, 500) MUST be specified per endpoint with error schema: `{ code: string, message: string, details?: object }`.
- Endpoint behavior MUST NOT change without a MAJOR version bump or feature flag.
- Deprecated endpoints MUST include a `Deprecation` header and migration guidance in the response.
- Rate-limiting MUST be enforced: 100 requests/min per authenticated user, 10 requests/sec per IP, 30-sec exponential backoff on limit exceeded.
- All media uploads MUST be validated: max 100 MB/file, supported MIME types (image/jpeg, image/png, image/webp, video/mp4, video/webm), image max 4000×4000, video max 1 hour.

**Error Response Format**:
```json
{
  "code": "ENTRY_IMMUTABLE",
  "message": "This entry is read-only after the same calendar day.",
  "details": {
    "entry_id": "e_123",
    "read_only_after": "2025-11-19T23:59:59-05:00",
    "current_time": "2025-11-20T08:00:00-05:00"
  }
}
```

**Rationale**: Users depend on the API for their most private data; breaking changes cause data loss risk and trust erosion.

---

### V. Security & Encryption Standards

**MUST**: Protect user data in transit and at rest with industry-standard encryption.

**Requirements**:
- All API traffic MUST use TLS 1.2+ (HTTPS only).
- Media stored in blob store MUST be encrypted at rest (AES-256).
- Entry passwords and account password hashes MUST use bcrypt with salt cost ≥ 12.
- Audit log diffs (if stored) MUST be encrypted with a backend master key, NOT user password.
- Session tokens MUST expire after 1 hour of inactivity or 24 hours max lifetime.
- Temporary unlock tokens (for private entries) MUST expire after 30 minutes or on logout, whichever is sooner.
- Secrets (DB passwords, API keys, blob store credentials) MUST be stored in a managed secrets store, never in code or `.env` files in repo.
- All dependencies MUST be scanned weekly for known vulnerabilities (CVSS ≥ 5.0 triggers alert + remediation ticket).

**Immutable Field Trigger**: 
- The `immutable` field on entries MUST be set to `true` automatically at the end-of-day boundary (23:59:59 in user's timezone) when created_at date changes. Implementation: daily cron job at 00:30 UTC that scans entries with `immutable=false` and `read_only_after < now()` and sets `immutable=true`.

**Rationale**: Encryption and lifecycle management are foundational to privacy and regulatory compliance (GDPR, CCPA).

---

## Security, Privacy & Compliance Requirements

### Data Handling

**Confidentiality Enforcement**:
- Entries MUST include `confidentiality` field (public|private); rejection if omitted on create or import.
- Private entries MUST be hidden from timeline unless user supplies password or account unlock token.
- Unlock session MUST be scoped to the user and the browser session; logged in audit.

**Deletion & Retention**:
- User-initiated deletion: immediate DB removal + async blob store deletion (within 1 hour).
- Data retention: user data retained indefinitely unless user deletes account.
- Account deletion: all related entries, media, categories, audit logs, and export jobs deleted within 24 hours; confirmation email sent.
- Audit logs: retained for 90 days, then purged automatically.

**Password Policy**:
- Account passwords: minimum 12 characters, at least 3 of 4 character classes (upper, lower, digit, special).
- Entry passwords: user-supplied or generated by application; minimum 8 characters recommended.
- Password resets: single-use token valid for 30 minutes, sent via email, logs include IP and timestamp.

### Compliance & Auditing

**Audit Logging**:
- Actions logged: entry create/update/delete, category changes, unlock attempts, export/import, account settings changes.
- Audit format: JSON { user_id, action, timestamp, actor_ip, resource_id, details }.
- All logs written to append-only DB table; no modification or deletion except admin override with ticket.

**Privacy Audit Automation**:
- Weekly scan (Sunday 02:00 UTC): verify no user data in external services or ML pipelines.
- Slack alert to #security if any violations detected.
- On violation: pause account, notify user, investigate and document remediation.

**Consent & Transparency**:
- Privacy policy and data usage summary visible on signup and in account settings.
- User can request data export (GDPR right) within 48 hours; includes all entries, metadata, audit logs.
- User can revoke consent to on-device sentiment feature; disables sentiment computation.

---

## Technical & API Standards

### Error Handling & Response Codes

**Standard HTTP codes & meanings**:
- `200 OK`: Successful read or update.
- `201 Created`: Resource successfully created (POST).
- `204 No Content`: Successful delete (DELETE).
- `400 Bad Request`: Validation error; error response includes field-level details.
- `401 Unauthorized`: Missing or invalid JWT/session token.
- `403 Forbidden`: Authenticated but lacks permission (e.g., edit/delete outside same-day window).
- `404 Not Found`: Resource doesn't exist or user lacks permission to view.
- `409 Conflict`: Concurrency conflict (e.g., simultaneous edits) or duplicate key.
- `429 Too Many Requests`: Rate limit exceeded; include `Retry-After` header with seconds.
- `500 Internal Server Error`: Unexpected backend error; include request ID for debugging.

**Per-endpoint error documentation** (OpenAPI):
- Entries POST: 400 (missing confidentiality), 403 (unauthorized), 500
- Entries PATCH: 400 (validation), 403 (not editable), 404 (not found), 409 (conflict)
- Entries DELETE: 403 (not deletable), 404 (not found)
- Unlock POST: 400 (missing entry_id or password), 403 (incorrect password after 3 attempts → lockout 15 min), 404

### Media Upload Specifications

**Constraints**:
- Max file size: 100 MB per upload.
- Supported types:
  - Images: image/jpeg, image/png, image/webp (max 4000×4000 px).
  - Videos: video/mp4, video/webm (max 1 hour, H.264 codec).
- Validation: MIME type verified both by extension and magic bytes on server; rejected if mismatch.
- Failure handling: 400 Bad Request with specific error (oversized, unsupported type, etc.).

**Thumbnail Generation**:
- Automatic thumbnail for all image uploads: max 300×300 px, JPEG quality 80%.
- Stored separately in blob store with predictable key: `thumbnails/{entry_id}/{media_id}.jpg`.
- Video thumbnails: extracted at 0s (or 5s if placeholder) as JPEG.
- Regeneration: thumbnails regenerated if media re-uploaded to same entry; old thumbnails cached for 30 days.

### Export & Import Specifications

**Export Async Threshold**:
- Synchronous export (immediate download): ≤ 50 entries OR ≤ 10 MB total.
- Asynchronous export (background job): > 50 entries OR > 10 MB; user notified via email when ready (max 15 min SLA).
- Export timeout: 15 minutes; if exceeded, job marked failed and user notified.

**Export Format Details**:
- JSON: flat array of entry objects with metadata; media URLs as signed S3 URLs (24-hour expiry).
- Markdown: one file per entry (filename: `{created_at_YYYYMMDDHHmmss}_{entry_id}.md`); media embedded as relative links in ZIP.
- ZIP: contains Markdown files + media folder; includes manifest.json with metadata.

**Import Schema** (required/optional fields):
```json
{
  "entries": [
    {
      "user_id": "uuid (required)",
      "title": "string (optional)",
      "body_text": "string (optional, but title or body required)",
      "type": "text|photo|video|mixed (required)",
      "confidentiality": "public|private (required)",
      "confidentiality_method": "account_password|entry_password|none (optional)",
      "category_id": "uuid (optional)",
      "tags": ["string"] (optional),
      "created_at": "ISO 8601 timestamp (required)",
      "media": [{"url": "string", "mime": "string"}] (optional)
    }
  ]
}
```
- Validation: rejects if `confidentiality` missing, `body_text` and media both empty, or `created_at` in future (> 5 minutes).

### Sentiment Analysis & Model Versioning

**MVP Approach** (locked): On-device sentiment using `sentiment.js` library (lightweight, client-side).
- Model version: hardcoded to `sentiment.js/v1.0` on client.
- Computed scores: stored on entry with model_version metadata.
- No server-side processing of raw text for sentiment.

**Future (v2+, requires separate RFC)**:
- Server-side ephemeral compute only with strict enforcement: input → compute → store result, delete input.
- Requires dedicated RFC with privacy architect review + user opt-in consent model.

### Mindmap Specifications

**Generation Scope**:
- Supports: single entry, category, user-wide journal.
- Similarity metric: cosine distance on TF-IDF vectors (0.0–1.0 scale).
- Threshold parameter: user-adjustable 0.1–0.95 (default 0.6).
- Max nodes per generation: 100 nodes (soft limit). If scope > 100 entries, return 400 with suggestion to filter or use async job.

**Async Threshold**: On-demand generation < 100 nodes. > 100 nodes: enqueue async job (SLA: complete within 5 min).

**Output Format**: Node graph JSON (cytoscape.js compatible) or PNG/SVG via headless browser export.

---

## Governance — Decision Guidance & Workflow

**Decision hierarchy**:
1. User impact: prioritize user-facing reliability and experience.
2. Security & correctness: safety/compliance come next.
3. Operational cost & maintainability.
4. Performance optimizations justified by telemetry/benchmarks.

**When to write an RFC**:
- Any change that:
  - Adds a major dependency or infra change.
  - Changes public API contracts.
  - Alters performance or scale assumptions.
  - Affects privacy or data handling.
- RFC should include: problem statement, alternatives, design, impact analysis (performance, cost, security), rollback plan, migration steps, and owners.

**PR Review & Merge Checklist** (enforce via PR template and CI):
- Lint/format checks passed.
- Type checks/static analysis passed.
- Unit tests present/updated; coverage met or explicitly justified with ticket.
- Integration/E2E updated if flows affected.
- Performance impact evaluated (bundle diff, microbench, or telemetry link).
- Accessibility checklist completed for UI changes.
- Documentation updated (README, API docs, design notes).
- Security considerations noted (dependency audit, encryption, data handling).
- Privacy impact assessment if data-handling code affected.
- Linked issue or RFC for non-trivial changes.

**Enforcement & Tooling**:
- CI gates: format, lint, type, unit tests MUST pass to merge. Integration/E2E/perf in separate lanes.
- Automated checks: coverage check (affected modules), bundle-size diff, a11y scan for UI, dependency security scan, privacy audit (weekly).

**Exception workflow**:
- Temporary exceptions allowed with:
  - An issue explaining justification and remediation timeline.
  - Explicit approver(s) listed in PR.
  - Expiration date (default: 2 weeks).
- Permanent exceptions require an RFC and majority approval.

**Quality gates & triage**:
- Build/type-check: PASS/FAIL.
- Lint/format: PASS/FAIL.
- Unit tests + coverage: PASS/FAIL.
- Integration/E2E (if applicable): PASS/FAIL.
- Smoke test (manual/local): PASS/FAIL.

**Operational checklist for production changes**:
- Add monitoring alerts and dashboards for new failure modes.
- Add runbook entries for newly introduced failure scenarios.
- For DB/schema changes: backward-compatible migrations with phased rollout.
- For privacy/data changes: security review + audit trail confirmation.

**Proactive defaults**:
- Add or update at least one unit test when modifying behavior.
- Add short "why" comments for non-obvious code.
- Attach a11y scan results/screenshots to UI PRs.
- For privacy changes, attach privacy impact assessment.
- For perf-sensitive changes, attach simple benchmark or telemetry snapshot.

**Enforcement metrics & reporting**:
- Weekly CI health: passing rate, average build time.
- Coverage trends: project and module-level coverage.
- Performance regressions: flagged PRs and summary per release.
- Accessibility regressions: violations introduced per release.
- Privacy audit results: scans run, violations detected, remediation status.

## Final Notes

These principles are the operating standard for the repository. Amendments require:
1. RFC documenting rationale, impact, and migration plan.
2. Review by tech lead, security lead, and at least one team member.
3. Update to this constitution with version bump and amendment log.

---

**Version**: 1.1.0 | **Ratified**: 2025-11-19 | **Last Amended**: 2026-02-13
## Journal-AI Constitution — Engineering Principles

Purpose
- Provide concise, actionable engineering principles to guide code quality, testing, user-experience consistency, performance, and governance across the project.

Scope
- Applies to source code, CI/CD pipelines, documentation, design assets, and production monitoring for this repository.

### Contract (inputs / outputs / success criteria)
- Inputs: PR changes (code, tests, docs, assets), architecture proposals, infra changes.
- Outputs: Merged code that meets quality gates (lint, types, tests), documented decisions, and monitored deployments.
- Success criteria:
	- PRs merge only when quality gates pass.
	- No critical regressions in behavior, accessibility, or performance.
	- Public APIs and shared components are documented and tested.

## 1 — Code Quality

- Readability & Simplicity
	- Prefer clear intent over cleverness. Use expressive names and small, focused functions.
	- Target < 300–400 LOC per module; files > 400 LOC require maintainer signoff and a short rationale.

- Consistency & Style
	- Enforce a single formatter/linter configuration in CI. PRs must pass formatting and linting with zero warnings.

- Types & Static Analysis
	- Use static typing where available. Treat type errors as build failures in CI.
	- Run static analyzers and security linters on every PR.

- Modularity & Explicit Boundaries
	- Prefer explicit interfaces and dependency injection. Isolate side-effects and make them visible.
	- Document inputs, outputs, error modes, and performance characteristics for public APIs.

- Documentation & Maintenance
	- Public modules must include short usage examples and expected failure modes.
	- Link any TODOs in code to an issue; no orphaned TODOs in merged code.

Edge cases
- Platform-specific behavior (Windows vs Unix paths).
- Backwards compatibility for public APIs.

## 2 — Testing Standards

- Coverage & Test Types
	- Baseline: >= 80% unit test coverage project-wide; critical/core modules >= 90%.
	- Use unit, integration, and end-to-end (E2E) tests as appropriate.

- Fast Feedback
	- Unit tests + lint + type checks should run in under 10 minutes on CI.
	- Long-running integration/E2E tests run in separate CI stages; unit failures block merges.

- Determinism & Flake Management
	- Tests must be deterministic. Quarantine flaky tests with a linked ticket; fix within one sprint.
	- Use time-mocking, seeded RNGs, and fixtures to reduce nondeterminism.

- Test Design
	- Use pure unit tests for logic, fixtures for integration, and recorded fixtures for external services.
	- Add contract tests for stable public interfaces.

- Performance & Regression Tests
	- Add performance tests for critical flows. CI should detect regressions beyond agreed thresholds (default: >5% P95 increase).

Examples
- New public function → unit tests + doc example.
- API change → contract test + integration test.

## 3 — User Experience Consistency

- Shared Design System
	- Use a single component library and design token set for spacing, color, and typography. Reuse components for common UI.

- Accessibility (a11y)
	- Aim for WCAG 2.1 AA for public UI. Enforce automated a11y checks (axe, Lighthouse) in CI and manual audits for major screens.

- UX Patterns & Internationalization
	- Document canonical patterns (auth, forms, errors, notifications) and reuse them.
	- Externalize all user-facing strings; review for i18n readiness before merge.

- Error Handling
	- Surface actionable, consistent error messages and recovery paths. Document API error codes and client expectations.

Edge cases
- Mobile vs desktop responsive behavior.
- Low-bandwidth / high-latency environments.

## 4 — Performance Requirements

- Budgets & Targets
	- Frontend: aim for <= 150 KB gzipped initial payload per major page; monitor bundle growth per PR.
	- API latency: P95 for core read ops < 300 ms. Background jobs may have relaxed SLAs.
	- Define memory/CPU budgets and enforce in deployment manifests.

- Observability
	- Instrument critical paths for latency, errors, and throughput. Add distributed tracing for cross-service transactions.

- Regression Controls
	- Run performance tests for critical flows; CI should block large regressions (>5% P95) or require approval with mitigation.

- Efficiency
	- Use caching, pagination, streaming, and incremental rendering where appropriate. Measure before/after for optimizations.

## Governance — Decision Guidance & Workflow

Decision hierarchy
1. User impact: prioritize user-facing reliability and experience.
2. Security & correctness: safety/compliance come next.
3. Operational cost & maintainability.
4. Performance optimizations justified by telemetry/benchmarks.

When to write an RFC
- Any change that:
	- Adds a major dependency or infra change.
	- Changes public API contracts.
	- Alters performance or scale assumptions.
- RFC should include: problem statement, alternatives, design, impact analysis (performance, cost, security), rollback plan, migration steps, and owners.

PR Review & Merge Checklist (enforce via PR template and CI)
- Lint/format checks passed.
- Type checks/static analysis passed.
- Unit tests present/updated; coverage met or explicitly justified.
- Integration/E2E updated if flows affected.
- Performance impact evaluated (bundle diff, microbench, or telemetry link).
- Accessibility checklist completed for UI changes.
- Documentation updated (README, API docs, design notes).
- Security considerations noted for dependency or surface changes.
- Linked issue or RFC for non-trivial changes.

Enforcement & Tooling
- CI gates: format, lint, type, and unit tests must pass to merge. Additional checks (integration, E2E, perf) run in separate lanes.
- Automated checks: coverage check (affected modules), bundle-size diff, a11y scan for UI PRs, dependency security scan.

Exception workflow
- Temporary exceptions allowed with:
	- An issue explaining the justification and remediation timeline.
	- Explicit approver(s) listed in the PR.
	- An expiration date (default: 2 weeks).
- Permanent exceptions require an RFC and majority approval.

Decision examples & guidance
- New dependency vs in-house: prefer well-maintained dependencies that reduce complexity; require RFC for major additions.
- Quick fix vs refactor: quick fixes allowed when timeboxed and documented; include a follow-up refactor ticket.
- Complex performance optimizations: require benchmarks and tests; prefer feature flags for risky changes.

Quality gates & triage
- Build/type-check: PASS/FAIL.
- Lint/format: PASS/FAIL.
- Unit tests + coverage: PASS/FAIL.
- Integration/E2E (if applicable): PASS/FAIL.
- Smoke test (manual/local): PASS/FAIL.

Operational checklist for production changes
- Add monitoring alerts and dashboards for new failure modes.
- Add runbook entries for newly introduced failure scenarios.
- For DB/schema changes: backward-compatible migrations with phased rollout.

Proactive defaults (small extras)
- Add or update at least one unit test when modifying behavior.
- Add short "why" comments for non-obvious code.
- Attach a11y scan results/screenshots to UI PRs.
- For perf-sensitive changes, attach a simple benchmark or telemetry snapshot.

Enforcement metrics & reporting
- Weekly CI health: passing rate, average build time.
- Coverage trends: project and module-level coverage.
- Performance regressions: flagged PRs and summary per release.
- Accessibility regressions: violations introduced per release.

## Final notes
- These principles are the operating standard for the repository. Amendments should be introduced via RFC and recorded in this document.

**Version**: 1.0.0 | **Ratified**: 2025-11-19 | **Last Amended**: 2025-11-19

``` 
