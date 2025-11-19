# [PROJECT_NAME] Constitution
<!-- Example: Spec Constitution, TaskFlow Constitution, etc. -->

## Core Principles

### [PRINCIPLE_1_NAME]
<!-- Example: I. Library-First -->
[PRINCIPLE_1_DESCRIPTION]
<!-- Example: Every feature starts as a standalone library; Libraries must be self-contained, independently testable, documented; Clear purpose required - no organizational-only libraries -->

### [PRINCIPLE_2_NAME]
<!-- Example: II. CLI Interface -->
[PRINCIPLE_2_DESCRIPTION]
<!-- Example: Every library exposes functionality via CLI; Text in/out protocol: stdin/args → stdout, errors → stderr; Support JSON + human-readable formats -->

### [PRINCIPLE_3_NAME]
<!-- Example: III. Test-First (NON-NEGOTIABLE) -->
[PRINCIPLE_3_DESCRIPTION]
<!-- Example: TDD mandatory: Tests written → User approved → Tests fail → Then implement; Red-Green-Refactor cycle strictly enforced -->

### [PRINCIPLE_4_NAME]
<!-- Example: IV. Integration Testing -->
[PRINCIPLE_4_DESCRIPTION]
<!-- Example: Focus areas requiring integration tests: New library contract tests, Contract changes, Inter-service communication, Shared schemas -->

### [PRINCIPLE_5_NAME]
<!-- Example: V. Observability, VI. Versioning & Breaking Changes, VII. Simplicity -->
[PRINCIPLE_5_DESCRIPTION]
<!-- Example: Text I/O ensures debuggability; Structured logging required; Or: MAJOR.MINOR.BUILD format; Or: Start simple, YAGNI principles -->

## [SECTION_2_NAME]
<!-- Example: Additional Constraints, Security Requirements, Performance Standards, etc. -->

[SECTION_2_CONTENT]
<!-- Example: Technology stack requirements, compliance standards, deployment policies, etc. -->

## [SECTION_3_NAME]
<!-- Example: Development Workflow, Review Process, Quality Gates, etc. -->

[SECTION_3_CONTENT]
<!-- Example: Code review requirements, testing gates, deployment approval process, etc. -->

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

[GOVERNANCE_RULES]
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
