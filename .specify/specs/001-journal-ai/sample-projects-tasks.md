# Sample Projects — Tasks (generated)

Note: each project contains a variable number of tasks between 5 and 15. Tasks are distributed across three states: `not-started`, `in-progress`, and `completed`. Each project contains at least one task in each state.

---

## Project: Journal AI (core)
Task count: 7

1. [in-progress] Design Entry data model and DB schema
2. [completed] Create initial project README and spec
3. [not-started] Implement media upload (blob store integration)
4. [completed] Add authentication scaffold (JWT)
5. [in-progress] Implement create/read/list API for entries
6. [not-started] Implement same-day edit/delete enforcement logic
7. [completed] Add basic CI: lint and unit test runner

Distribution: not-started: 2, in-progress: 2, completed: 3

---

## Project: Mobile Client (React Native)
Task count: 12

1. [not-started] Setup project scaffold and CI for mobile build
2. [in-progress] Implement entry editor UI (text + media picker)
3. [completed] Design onboarding screens and example prompts
4. [in-progress] Implement timeline view with locked placeholders for Private entries
5. [not-started] Integrate local sentiment analysis library (on-device)
6. [completed] Implement offline caching for entries
7. [not-started] Add i18n support and string externalization
8. [completed] Implement image/video thumbnail generation on upload
9. [in-progress] Add unlock flow UI (password prompt + session unlock)
10. [not-started] Add accessibility improvements (labels, focus order)
11. [completed] Add analytics hooks (events: entry.create, unlock)
12. [not-started] Export/Share feature for selected entries (markdown/zip)

Distribution: not-started: 5, in-progress: 3, completed: 4

---

## Project: Analytics & Privacy
Task count: 9

1. [completed] Define "no_training_use" user flag and DB field
2. [in-progress] Implement audit checks to ensure data excluded from ML pipelines
3. [not-started] Create automated audits to verify non-retention of raw entry text for sentiment jobs
4. [completed] Build telemetry dashboard skeleton (latency, errors)
5. [not-started] Implement nightly job to scrub logs and verify no PII persisted
6. [in-progress] Implement performance tests for listing endpoints (10k entries)
7. [not-started] Add enforcement policy for sentiment compute non-retention (server-side)
8. [completed] Create runbook for export job failures and monitoring alerts
9. [not-started] Add tests for import validation requiring confidentiality field

Distribution: not-started: 5, in-progress: 2, completed: 2

---

*Generated: 2025-11-19*
