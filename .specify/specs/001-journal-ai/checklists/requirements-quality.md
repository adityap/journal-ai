# Journal AI — Requirements Quality Checklist

**Purpose**: Unit tests for Journal AI specification quality, validating completeness, clarity, consistency, measurability, and coverage of all requirement dimensions.

**Created**: 2025-02-13  
**Focus**: Full specification review (MVP → v1)  
**Audience**: Author (self-review)  
**Depth**: Standard + all quality dimensions

---

## Requirement Completeness

- [ ] CHK001 - Are all CREATE operations fully specified with required/optional fields and validation rules? [Completeness, Spec §API Surface]
- [ ] CHK002 - Are all READ operations defined for single, list, and filtered retrieval? [Completeness, Spec §API Surface]
- [ ] CHK003 - Are all UPDATE/DELETE business rules documented (same-day edit/delete enforcement, timezone handling)? [Completeness, Spec §Edit/Delete rules]
- [ ] CHK004 - Are all error cases and error response formats specified for each endpoint? [Gap, Spec §API Surface]
- [ ] CHK005 - Is the confidentiality unlock flow fully specified (account-based and entry-password modes)? [Completeness, Spec §Confidentiality & Unlocking policy]
- [ ] CHK006 - Are all authentication and authorization requirements documented for API endpoints? [Completeness, Spec §Security & Compliance]
- [ ] CHK007 - Is the media upload/download flow fully specified (signed URLs, blob storage integration)? [Gap, Spec §API Surface]
- [ ] CHK008 - Are export job states and status transitions fully documented? [Gap, Data Model §export_jobs]
- [ ] CHK009 - Is the import validation contract fully specified (which fields are required, format validation)? [Completeness, Spec §Import / Acceptance contract]
- [ ] CHK010 - Are all UI screens/flows documented with user actions and expected outcomes? [Gap, Spec §UX / Flow Summaries]
- [ ] CHK011 - Is the onboarding flow (first-time user, sample data, habit setup) fully specified? [Gap, Spec §UX Guidance & Journaling Suggestions]
- [ ] CHK012 - Are all data retention and deletion requirements specified? [Completeness, Spec §Data retention & deletion]
- [ ] CHK013 - Is the sentiment analysis computation model and fallback behavior fully defined? [Gap, Spec §Sentiment Analysis]
- [ ] CHK014 - Are all mindmap generation edge cases documented (empty scope, single entry, large scopes)? [Gap, Spec §Mind-map Generation]
- [ ] CHK015 - Is the mobile/responsive behavior fully specified for all UI components? [Gap, Spec §UX / Flow Summaries]

---

## Requirement Clarity

- [ ] CHK016 - Is "same-day edit/delete" quantified in terms of user timezone and clock boundaries? [Clarity, Spec §Edit/Delete rules]
- [ ] CHK017 - Does "Private entry" define what is hidden vs. shown (title, date, category, hint)? [Clarity, Spec §Confidentiality & Unlocking policy]
- [ ] CHK018 - Is the term "immutable" defined with clear state transitions and when it's set? [Ambiguity, Data Model §entries]
- [ ] CHK019 - Is "read-only_after" computation fully specified (exact time in user's timezone)? [Clarity, Spec §Edit/Delete rules]
- [ ] CHK020 - Are entry "types" (text/photo/video/mixed) clearly defined with inclusion/exclusion criteria? [Clarity, Spec §MVP Feature List]
- [ ] CHK021 - Is "sentiment score" range and label mapping clearly defined? [Clarity, Data Model §entries]
- [ ] CHK022 - Are the exact success/failure conditions for unlock attempts specified? [Ambiguity, Spec §Confidentiality & Unlocking policy]
- [ ] CHK023 - Is the "no_training_use" flag behavior documented in all data pipelines? [Gap, Spec §Security & Privacy]
- [ ] CHK024 - Are export format specifications defined (JSON structure, Markdown conventions, ZIP contents)? [Ambiguity, Spec §Export contract]
- [ ] CHK025 - Is the mindmap "threshold" parameter quantified with units and acceptable range? [Ambiguity, Spec §Mind-map Generation]
- [ ] CHK026 - Is "similarity" defined explicitly (cosine distance, embedding method, similarity units)? [Ambiguity, Spec §Mind-map Generation]
- [ ] CHK027 - Is the storage encryption "at rest" method specified (AES-256, key management)? [Gap, Spec §Security & Compliance]
- [ ] CHK028 - Are password strength requirements specified for entry passwords and account passwords? [Gap, Spec §Confidentiality & Unlocking policy]
- [ ] CHK029 - Is "session-based unlock" lifetime and behavior defined (session duration, logout behavior)? [Ambiguity, Spec §Confidentiality & Unlocking policy]
- [ ] CHK030 - Is the "tagged entries with category" relationship defined (can an entry have both category and tags? required/optional?)? [Clarity, Data Model §entries]

---

## Requirement Consistency

- [ ] CHK031 - Are confidentiality requirements consistent across create, import, and export operations? [Consistency, Spec §Confidentiality & Unlocking policy, Import / Acceptance contract, Export contract]
- [ ] CHK032 - Are timestamp handling rules consistent across all operations (timezone, UTC storage, local display)? [Consistency, Spec §Data Model §entries]
- [ ] CHK033 - Do sentinel analysis settings align between on-device and server options in terms of data retention? [Consistency, Spec §Sentiment Analysis]
- [ ] CHK034 - Are audit logging requirements consistent for all mutation operations (create, update, delete, unlock)? [Consistency, Spec §Auditing, Testing & QA]
- [ ] CHK035 - Is the user timezone handling consistent between entry creation, read-only window, and UI display? [Consistency, Spec §Edit/Delete rules]
- [ ] CHK036 - Are media encryption requirements consistent for all media types (image, video, blob store)? [Consistency, Spec §Security & Compliance, Data Model]
- [ ] CHK037 - Does the "source" field (ui/import/api) have consistent validation and allowed values? [Consistency, Data Model §entries]
- [ ] CHK038 - Are rate-limiting rules mentioned in non-functional requirements but missing from API endpoint specs? [Conflict, Spec §Non-functional requirements vs. §API Surface]
- [ ] CHK039 - Is the availibility SLA (99.9%) consistent with expected behavior during data deletion or export operations? [Consistency, Spec §Non-functional requirements]
- [ ] CHK040 - Are mindmap generation scopes (category/journal/single-entry) consistently defined across docs? [Consistency, Spec §Mind-map Generation §UX / Flow Summaries]

---

## Acceptance Criteria Quality

- [ ] CHK041 - Can "99.9% availability" be objectively measured and verified? [Measurability, Spec §Non-functional requirements]
- [ ] CHK042 - Is the P95 latency target for GET list (300ms) measurable under specified load conditions? [Measurability, Spec §Non-functional requirements]
- [ ] CHK043 - Is the P95 latency for single entry retrieval (150ms) defined for a specific backend infra and load profile? [Ambiguity, Spec §Non-functional requirements]
- [ ] CHK044 - Can "sentiment score accuracy" be objectively measured? [Gap, Spec §Sentiment Analysis]
- [ ] CHK045 - Is mindmap generation quality measurable (what constitutes "correct" graph layout)? [Gap, Spec §Mind-map Generation]
- [ ] CHK046 - Are export job SLOs specified (completion time for typical exports)? [Gap, Spec §Export contract]
- [ ] CHK047 - Can "successful unlock" be objectively verified with clear success/failure codes? [Measurability, Spec §Confidentiality & Unlocking policy]
- [ ] CHK048 - Are media thumbnail generation success criteria defined (size, format, quality)? [Gap, Spec §Media]
- [ ] CHK049 - Is the minimum password strength measurable (length, character classes, entropy)? [Gap, Spec §Confidentiality & Unlocking policy]
- [ ] CHK050 - Can "large scope" for mindmap be objectively defined (>=100 nodes threshold reason + upper limit)? [Clarity, Spec §Performance & Scaling]

---

## Scenario Coverage

- [ ] CHK051 - Are requirements specified for creating entries with all type combinations (text only, photo only, video only, mixed)? [Coverage, Spec §MVP Feature List]
- [ ] CHK052 - Are requirements specified for retrieving entries on exact boundary times (midnight transitions, timezone edges)? [Coverage, Edge Case]
- [ ] CHK053 - Are requirements specified for editing/deleting entries after the boundary window (should be rejected)? [Coverage, Spec §Edit/Delete rules]
- [ ] CHK054 - Are requirements specified for unlocking entries with incorrect password (maximum attempts, lockout)? [Coverage, Exception Flow, Gap]
- [ ] CHK055 - Are requirements specified for import with missing or invalid confidentiality field? [Coverage, Spec §Import / Acceptance contract]
- [ ] CHK056 - Are requirements specified for export when user lacks permission to view private entries (password required)? [Coverage, Spec §Export contract]
- [ ] CHK057 - Are requirements specified for mindmap generation when scope has zero entries? [Coverage, Edge Case, Gap]
- [ ] CHK058 - Are requirements specified for mindmap generation when all entries are identical (no similarity variation)? [Coverage, Edge Case, Gap]
- [ ] CHK059 - Are requirements specified for media upload when file size exceeds limits? [Coverage, Exception Flow, Gap]
- [ ] CHK060 - Are requirements specified for concurrent edit attempts on the same entry? [Coverage, Exception Flow, Gap]
- [ ] CHK061 - Are requirements specified for user deletion and cascading data removal? [Coverage, Exception Flow, Gap]
- [ ] CHK062 - Are requirements specified for sentiment analysis on entries with empty or minimal text? [Coverage, Edge Case, Gap]
- [ ] CHK063 - Are requirements specified for recovery from partial export job failures (resume, retry)? [Coverage, Recovery Flow, Gap]
- [ ] CHK064 - Are requirements specified for handling media storage failures (S3 downtime, network errors)? [Coverage, Exception Flow, Gap]
- [ ] CHK065 - Are requirements specified for handling timestamp clock skew (entry created_at in future)? [Coverage, Edge Case, Gap]

---

## Edge Case Coverage

- [ ] CHK066 - What happens when an entry is edited on the boundary minute (is the read_only_after timestamp before or after edit)? [Edge Case]
- [ ] CHK067 - What happens when a user's timezone changes after entries are created (should read_only_after be recalculated)? [Edge Case, Gap]
- [ ] CHK068 - Is behavior specified for entries with no body_text and no media (empty entries)? [Edge Case, Gap]
- [ ] CHK069 - Is behavior specified for very large entry text (storage limit, search performance)? [Edge Case, Gap]
- [ ] CHK070 - Is behavior specified for categories with circular parent_id references? [Edge Case, Gap]
- [ ] CHK071 - Is behavior specified for entries assigned to deleted categories? [Edge Case, Gap]
- [ ] CHK072 - Is behavior specified for unlock requests after session timeout? [Edge Case, Gap]
- [ ] CHK073 - Is behavior specified for export jobs that exceed resource limits (memory, time, file size)? [Edge Case, Gap]
- [ ] CHK074 - Is behavior specified for duplicate entry submissions (same content, created_at, user_id)? [Edge Case, Deduplication]
- [ ] CHK075 - Is behavior specified when media URL becomes inaccessible (CDN/blob store down)? [Edge Case, Gap]
- [ ] CHK076 - Is behavior specified for sentiment analysis on non-English text or unsupported languages? [Edge Case, Gap]
- [ ] CHK077 - Is behavior specified for extremely long tags (storage limits, indexing performance)? [Edge Case, Gap]
- [ ] CHK078 - Is behavior specified for URL/filename collision in blob storage? [Edge Case, Gap]
- [ ] CHK079 - Is behavior specified for audit log growth management (archival, retention)? [Edge Case, Gap]
- [ ] CHK080 - Is behavior specified for recovery from partial media deletion (entry orphaned from media)? [Edge Case, Recovery Flow, Gap]

---

## Non-Functional Requirements Specification

- [ ] CHK081 - Is availability SLA (99.9%) defined with exclusions (maintenance windows, catastrophic failures)? [NFR, Clarity, Spec §Non-functional requirements]
- [ ] CHK082 - Are latency targets defined for all critical paths (create, list, unlock, export initiation)? [NFR, Gap, Spec §Non-functional requirements]
- [ ] CHK083 - Is storage encryption algorithm specified (AES-256, key derivation, IV management)? [NFR, Gap, Spec §Non-functional requirements]
- [ ] CHK084 - Is rate-limiting specified with thresholds (requests/sec, burst allowance, backoff)? [NFR, Gap, Spec §Non-functional requirements]
- [ ] CHK085 - Is backup and recovery RTO/RPO specified? [NFR, Gap]
- [ ] CHK086 - Is security audit frequency and scope specified? [NFR, Gap]
- [ ] CHK087 - Are accessibility requirements (WCAG level) specified for all UI? [NFR, Gap, Spec §Testing & QA]
- [ ] CHK088 - Are mobile device support requirements specified (iOS/Android, browsers)? [NFR, Gap]
- [ ] CHK089 - Is maximum concurrent user load specified for capacity planning? [NFR, Gap]
- [ ] CHK090 - Is observability/logging specified (events, metrics, trace sampling rates)? [NFR, Clarity, Spec §Telemetry & Observability]

---

## Dependencies & Assumptions

- [ ] CHK091 - Are external dependencies documented (PostgreSQL version, AWS/GCP services, sentiment libraries)? [Dependency, Gap]
- [ ] CHK092 - Is the assumption that "user's timezone will not change" or "will be recomputed" documented? [Assumption, Gap]
- [ ] CHK093 - Is the assumption that "entry passwords are user-supplied and verifiable" documented? [Assumption, Spec §Confidentiality & Unlocking policy]
- [ ] CHK094 - Is the assumption that "sentiment libraries are privacy-preserving" documented with evidence? [Assumption, Gap, Spec §Sentiment Analysis]
- [ ] CHK095 - Is the dependency on S3-compatible storage validated (MinIO, AWS S3, GCP, Azure compatibility)? [Dependency, Spec §Tech choices]
- [ ] CHK096 - Are dependencies on external ML models or embeddings services identified? [Dependency, Gap, Spec §Mind-map Generation]
- [ ] CHK097 - Is the assumption that "user passwords are strong" validated or enforced by policy? [Assumption, Gap]
- [ ] CHK098 - Is the dependency on JWT or session management library specified? [Dependency, Spec §Tech choices]
- [ ] CHK099 - Are dependencies on frontend libraries (React, Chakra UI) and versions specified? [Dependency, Spec §Tech choices]
- [ ] CHK100 - Is the assumption that "media is always retrievable from blob store" documented or backed by recovery mechanism? [Assumption, Gap]

---

## Ambiguities & Conflicts

- [ ] CHK101 - Conflict: Spec says 99.9% availability but no SLA for export/import jobs — should these affect overall SLA? [Conflict, Spec §Non-functional requirements vs. §Implementation Roadmap]
- [ ] CHK102 - Ambiguity: "Sentiment model versioning" — should old entries be re-computed if model updates? [Ambiguity, Spec §Sentiment Analysis]
- [ ] CHK103 - Ambiguity: Does "Private entry" in export hide content or include it encrypted? [Ambiguity, Spec §Export contract]
- [ ] CHK104 - Conflict: Data Model shows "tags: text[]" but MVP Feature List doesn't guarantee tag persistence or filtering. [Conflict, Data Model vs. Spec §MVP Feature List]
- [ ] CHK105 - Ambiguity: How are "related episodes" / "similar entries" computed in mindmap? (spec says "TF-IDF + similarity" but thresholds undefined). [Ambiguity, Spec §Mind-map Generation]
- [ ] CHK106 - Conflict: Plan says "start with on-device sentiment" but Spec doesn't specify which on-device library or model. [Conflict, Plan vs. Spec §Sentiment Analysis]
- [ ] CHK107 - Ambiguity: "Audit log encryption" — are diffs encrypted with user's password or backend key? [Ambiguity, Spec §Security & Compliance]
- [ ] CHK108 - Conflict: Spec guarantees "Privacy guarantee: no training use" but Plan mentions "optional server-side compute" which may require data exposure. [Conflict, Spec §Purpose & Constraints vs. §Sentiment Analysis]
- [ ] CHK109 - Ambiguity: "Immutable" field — is this computed automatically by trigger or job? [Ambiguity, Data Model §entries]
- [ ] CHK110 - Ambiguity: Are categories hierarchical (parent_id) or flat? Is depth limited? [Ambiguity, Data Model §categories]

---

## Traceability & Governance

- [ ] CHK111 - Are all requirements traceable to spec sections (each item should reference a section)? [Traceability]
- [ ] CHK112 - Is a requirement numbering scheme defined (e.g., REQ-001, REQ-002) for future reference? [Traceability, Gap]
- [ ] CHK113 - Are acceptance criteria defined for each major feature (MVP list items 1-12)? [Traceability, Spec §Acceptance Criteria]
- [ ] CHK114 - Is test coverage defined for each acceptance criterion? [Traceability, Gap]
- [ ] CHK115 - Are sprints and deliverables aligned with requirements (no orphan requirements)? [Traceability, Spec §Implementation Roadmap]

---

## Implementation Readiness

- [ ] CHK116 - Are all API endpoints fully specified with request/response examples? [Implementation, Spec §API Surface §Example JSON payloads]
- [ ] CHK117 - Are all data model fields fully specified with type, constraints, and indexes? [Implementation, Data Model]
- [ ] CHK118 - Are error response codes and formats specified for all failure scenarios? [Implementation, Gap]
- [ ] CHK119 - Is field validation (required/optional, length, format, constraints) specified? [Implementation, Gap]
- [ ] CHK120 - Are all third-party integrations documented with APIs and versions? [Implementation, Gap]

---

**Total Checkpoints**: 120

**Note**: Items marked `[Gap]` indicate missing requirements that should be addressed before implementation. Items marked `[Ambiguity]` indicate unclear language requiring clarification. Items marked `[Conflict]` indicate contradictions requiring resolution.
