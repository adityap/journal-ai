# Journal AI — Implementation Tasks

**Feature**: Journal AI — Personal journaling platform with entries, categories, sentiment analysis, and mind-maps

**Tech Stack**: ASP.NET Core 8 + PostgreSQL + React 18 + Vite + TypeScript + Npgsql + S3-compatible storage

**Total Tasks**: 67 | **Sprints**: 6 (12 weeks total)

**Last Updated**: 2026-02-13 | **Readiness**: 92% (all specifications clarified, constitution ratified)

---

## Dependencies & Execution Order

### User Story Dependency Map

```
Sprint 0 (Setup)
    ↓
Sprint 1 (US1: Core Entries)
    ├→ Sprint 2a (US2: Authentication & Unlock)
    └→ Sprint 2b (US3: Media Upload & Handling)
            ↓
        Sprint 3 (US4: Export/Import)
        Sprint 3 (US5: Sentiment Analysis)
            ↓
        Sprint 4 (US6: Mind-maps)
            ↓
        Sprint 5 (US7: Testing & Release)
```

**Key**: 
- Sprint 0 is blocking (infrastructure)
- Sprint 1 (US1) must complete before concurrent sprints 2a, 2b
- Sprints 2a/2b can execute in parallel
- Sprints 3, 4 can run concurrently with Sprint 2b completion

### Parallel Execution Opportunities

**Phase 1 (Sprint 0)**:
- Sequential (infrastructure blocking)

**Phase 2a/2b (Sprint 2, Week 1-2)**:
- T018-T025 (Auth) can run in parallel with T026-T035 (Media)
- Example: Backend JWT implementation (T020) can proceed while frontend media UI (T028) develops

**Phase 3 (Sprint 3, Weeks 5-6)**:
- T036-T042 (Export/Import) can run in parallel with T043-T049 (Sentiment)
- Example: Export async job (T037) can proceed while sentiment.js integration (T044) develops

**Phase 4 (Sprint 4, Weeks 7-8)**:
- Sequential for mindmap feature

**Phase 5 (Sprint 5, Weeks 9-12)**:
- T052-T060 (Integration tests) can run in parallel with T061-T067 (Performance & Release)

---

## Phase 1: Setup & Infrastructure

### Sprint 0 — Repository & Local Dev (1 week)

**Story Goal**: Create project foundation with git structure, docker-compose services, and scaffolding for backend and frontend.

**Independent Test Criteria**:
- [ ] Docker Compose starts all services (postgres, minio, redis, api, frontend) with no errors
- [ ] `dotnet run` in backend starts API on port 5000
- [ ] `pnpm dev` in frontend starts Vite dev server on port 5173
- [ ] Both can access each other via network
- [ ] GitHub Actions lint/test jobs run successfully

**Tasks**:

- [ ] T001 [P] Create git repository structure with folders: src/backend, src/frontend, .specify/scripts, docker, docs

- [ ] T002 [P] Create CONTRIBUTING.md with:
  - Commit message format (conventional commits)
  - PR template (including constitution checklist)
  - Local dev instructions
  - Code review guidelines per constitution

- [ ] T003 [P] Create docker-compose.yml with services:
  - postgres 14 (POSTGRES_PASSWORD, volume mapping)
  - minio (ports 9000/9001, MINIO_ROOT_USER/PASSWORD)
  - redis 7 (for future queue support)
  - API service (depends_on postgres, minio)
  - Frontend service (Vite on 5173)

- [ ] T004 [P] Create Dockerfile for ASP.NET Core 8 backend with:
  - Multi-stage build (SDK stage + runtime stage)
  - Healthcheck endpoint /health
  - Port 5000 exposed

- [ ] T005 Create .env.example with required variables:
  - DATABASE_URL=postgresql://...
  - MINIO_ENDPOINT=http://minio:9000
  - JWT_SECRET=...
  - ALLOWED_ORIGINS=...

- [ ] T006 [P] Scaffold ASP.NET Core 8 backend using `dotnet new webapi`:
  - File: src/backend/JournalAI.csproj
  - Install NuGet packages: EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL, Swashbuckle.AspNetCore, AWS.S3 (or MINIO equiv)
  - Create Program.cs with service registration

- [ ] T007 [P] Scaffold React frontend using Vite:
  - File: src/frontend/vite.config.ts
  - Install: @vitejs/plugin-react, typescript, react-router-dom, axios, tailwindcss
  - Configure proxy to backend: /api → http://localhost:5000

- [ ] T008 [P] Create GitHub Actions workflow for lint/test (file: .github/workflows/ci.yml):
  - Lint lane: dotnet format check + npm lint
  - Test lane: dotnet test + npm test
  - Build lane: dotnet build + npm build

- [ ] T009 Create initial README.md with:
  - Project overview
  - Quick start (docker compose up)
  - Tech stack summary
  - Links to TECHNICAL_PLAN_V2.md and specs.md

- [ ] T010 [P] Create .gitignore covering:
  - C# artifacts: bin/, obj/, *.user, *.suo
  - Node: node_modules/, dist/, .pnpm-store
  - Env files: .env.local, .env

---

## Phase 2: Foundational Features

### Sprint 1 — Core Entries & Database (2 weeks)

**Story Goal**: Implement entry CRUD operations with immutability and basic timeline view. Build foundation for all feature stories.

**User Story**: **US1: Create, read, edit, delete journal entries with same-day edit/delete enforcement**

**Independent Test Criteria**:
- [ ] POST /api/v1/entries creates entry with id, read_only_after calculated correctly
- [ ] GET /api/v1/entries lists entries in reverse chronological order
- [ ] GET /api/v1/entries/{id} returns entry with all fields
- [ ] PATCH /api/v1/entries/{id} succeeds if created today, fails with 403 if past midnight
- [ ] DELETE /api/v1/entries/{id} succeeds if created today, fails with 403 if past midnight
- [ ] All endpoints return proper error schemas (400 for validation, 403 for immutable, 404 for not found)
- [ ] Unit tests for business logic (date boundary, timezone calculation) ≥90% coverage
- [ ] EF Core migrations run clean against fresh postgres

**Implementation Tasks**:

- [ ] T011 Create EF Core DbContext (file: src/backend/Data/AppDbContext.cs):
  - Define DbSet<User>, DbSet<Entry>, DbSet<Category>, DbSet<AuditLog>, DbSet<UnlockSession>, DbSet<ExportJob>, DbSet<Media>
  - Configure PostgreSQL as provider via UseNpgsql
  - Add OnModelCreating: indexes on (user_id, created_at), full-text search setup, relationships

- [ ] T012 [P] Create EF Core models (file: src/backend/Models/):
  - User.cs: id, email, password_hash, timezone, settings, no_training_use
  - Category.cs: id, user_id, name, color, created_at (1:1 per entry, flat hierarchy)
  - Entry.cs: id, user_id, title, body_text, type, confidentiality, category_id, tags, sentiment_*, created_at, read_only_after, immutable, metadata, source
  - Media.cs, AuditLog.cs, UnlockSession.cs, ExportJob.cs (stubs)
  - Add [Required], [Index] attributes per data-model.md

- [ ] T013 [P] Create EF Core migration (file: src/backend/Migrations/):
  - Run: `dotnet ef migrations add InitialCreate`
  - Verify generated SQL matches data-model.md schema
  - Add trigger or stored proc to auto-compute read_only_after on insert
  - Create indexes: (user_id, created_at), full-text on body_text, GIN on tags

- [ ] T014 [US1] Create Entries Controller (file: src/backend/Controllers/EntriesController.cs):
  - POST /api/v1/entries → CreateEntry(CreateEntryDto): 
    - Validate confidentiality required
    - Compute read_only_after = end of today in user's timezone
    - Return 201 with entry id and read_only_after
  - GET /api/v1/entries?start=&end=&category=&tag=&page=&per_page= → ListEntries(ListEntriesDto):
    - Return paginated list (default 20 per page)
    - Filter by date range, category, type
    - Omit private entries unless unlocked (to be implemented T025)
  - GET /api/v1/entries/{id} → GetEntry(id): return full entry or 404
  - PATCH /api/v1/entries/{id} → UpdateEntry(id, UpdateEntryDto):
    - Check immutable: if now > read_only_after, return 403 with error code "ENTRY_IMMUTABLE"
    - Update and return 200
  - DELETE /api/v1/entries/{id} → DeleteEntry(id):
    - Check immutable: if now > read_only_after, return 403 with error code "ENTRY_IMMUTABLE"
    - Delete and enqueue media cleanup (T032)
    - Return 204

- [ ] T015 [US1] [P] Create Entry DTOs (file: src/backend/Data/Dtos/):
  - CreateEntryDto: title, body_text, type, category_id, tags, confidentiality, metadata, source
  - UpdateEntryDto: title, body_text, category_id, tags
  - EntryResponseDto: id, title, body_text, type, confidentiality, category_id, tags, sentiment_*, read_only_after, immutable, created_at, updated_at

- [ ] T016 [US1] [P] Create Entry business logic (file: src/backend/Services/EntryService.cs):
  - CalculateReadOnlyAfter(user_timezone, created_at): datetime
    - Logic: take created_at, convert to user timezone, find end-of-day 23:59:59, convert back to UTC
    - Example: If created_at = 2026-02-13T20:00Z and user TZ = America/New_York (-5), end-of-day = 2026-02-14T04:59:59Z
  - ValidateEntryEditable(entry): bool
    - Check: now > entry.read_only_after → false, else true
  - GetEntryWithUnlock(entry_id, unlock_token): Entry or 403
  - Unit tests for CalculateReadOnlyAfter covering edge cases (DST transitions, timezones, midnight boundaries)

- [ ] T017 [US1] Create Entry error responses (file: src/backend/Data/Models/ErrorResponse.cs):
  - ErrorResponse: code, message, details
  - Error codes: VALIDATION_ERROR, ENTRY_IMMUTABLE, UNAUTHORIZED, NOT_FOUND, CONFLICT, RATE_LIMITED, SERVER_ERROR
  - Map to HTTP status: 400, 403, 401, 404, 409, 429, 500

- [ ] T018 [P] Create unit tests for Entry business logic (file: src/backend/Tests/EntryServiceTests.cs):
  - Test CalculateReadOnlyAfter with multiple timezones (UTC, EST, JST, etc.)
  - Test immutability check at boundary (T23:59:59 and T00:00:00)
  - Test entry creation with and without category
  - Test validation errors (missing confidentiality, invalid type)
  - Aim for ≥90% coverage of business logic

- [ ] T019 [US1] Create API integration tests (file: src/backend/Tests/EntryControllerTests.cs):
  - Test POST creates entry and returns 201
  - Test GET lists entries in reverse chronological order
  - Test PATCH succeeds before midnight, fails after
  - Test DELETE succeeds before midnight, fails after
  - Test error responses (400, 403, 404)

- [ ] T020 [P] Create basic frontend Timeline component (file: src/frontend/src/components/Timeline.tsx):
  - Call GET /api/v1/entries?page=1&per_page=20
  - Render entries in reverse chronological order (latest first)
  - Display: title, date, type icon (text/photo/video)
  - Show "Locked" placeholder for private entries (unlock in T025)
  - Loading and error states
  - Pagination controls

- [ ] T021 [P] Create basic frontend Entry Create form (file: src/frontend/src/components/EntryForm.tsx):
  - Input fields: title (optional), body_text, type (select), category (select), confidentiality (select: public/private), tags (comma-separated)
  - Call POST /api/v1/entries on submit
  - Show success toast and redirect to timeline
  - Show error toast on failure
  - Disable submit button while loading

---

### Sprint 2a — Authentication & Session Unlock (2 weeks)

**Story Goal**: Implement user registration, login, JWT authentication, and session-based unlock for private entries.

**User Story**: **US2: User registration, login, and confidentiality unlock with account password**

**Independent Test Criteria**:
- [ ] POST /api/v1/auth/register creates user with hashed password, returns JWT
- [ ] POST /api/v1/auth/login validates email/password, returns JWT
- [ ] GET /api/v1/entries with private entries returns only public ones (no 403, just omitted)
- [ ] POST /api/v1/unlock with account password creates 1-hour session, returns unlock_token
- [ ] GET /api/v1/entries/{id} with private entry and valid unlock_token returns entry
- [ ] GET /api/v1/entries/{id} with private entry and invalid unlock_token returns 403
- [ ] JWT token validation enforced on all authenticated endpoints
- [ ] All auth endpoints return proper error schemas

**Implementation Tasks**:

- [ ] T022 [P] [US2] Create Auth Controller (file: src/backend/Controllers/AuthController.cs):
  - POST /api/v1/auth/register → Register(RegisterDto):
    - Validate email format and uniqueness
    - Hash password with bcrypt (≥12 rounds per constitution)
    - Create user with timezone (optional, default UTC)
    - Set no_training_use = true
    - Return 201 with user id and JWT token
  - POST /api/v1/auth/login → Login(LoginDto):
    - Find user by email, validate password
    - Return 200 with JWT token and user info
    - Return 401 if invalid credentials

- [ ] T023 [P] [US2] Create Auth DTOs and Auth Service (file: src/backend/Data/Dtos/Auth/ and src/backend/Services/AuthService.cs):
  - RegisterDto: email, password, name, timezone
  - LoginDto: email, password
  - TokenDto: access_token, expires_in, user { id, email, name, timezone }
  - AuthService.HashPassword(password): string (bcrypt)
  - AuthService.ValidatePassword(password, hash): bool
  - AuthService.GenerateJWT(user): string with sub=user_id, exp, iss, aud

- [ ] T024 [P] [US2] Create JWT authentication middleware (file: src/backend/Middleware/AuthMiddleware.cs):
  - Intercept requests to protected endpoints
  - Extract Authorization: Bearer token header
  - Validate token signature and expiration
  - Populate HttpContext.User with claims
  - Return 401 if invalid

- [ ] T025 [P] [US2] Create Unlock Controller (file: src/backend/Controllers/UnlockController.cs):
  - POST /api/v1/unlock → Unlock(UnlockDto):
    - Body: { password } (account password only for MVP)
    - Validate password against current user (via JWT sub claim)
    - Create UnlockSession: session_token (uuid), expires_at = now + 1 hour, user_id
    - Return 200 { unlock_token: session_token, expires_at }
    - Return 401 if password invalid

- [ ] T026 [P] [US2] Update Entries Controller to support unlock (file: src/backend/Controllers/EntriesController.cs):
  - GET /api/v1/entries: Filter out private entries from response (don't return them at all)
  - GET /api/v1/entries/{id}?unlock_token=...: 
    - Check if entry.confidentiality == 'private'
    - If yes and no unlock_token: return 403 with code "ENTRY_REQUIRES_UNLOCK"
    - If yes and unlock_token provided: validate UnlockSession.session_token, check expires_at > now
    - If valid: return entry; if invalid or expired: return 403 with code "INVALID_UNLOCK_TOKEN"

- [ ] T027 [P] [US2] Create User model and UserService (file: src/backend/Models/User.cs and src/backend/Services/UserService.cs):
  - User model: id, email, password_hash, name, timezone, no_training_use, created_at, updated_at
  - UserService.CreateUser(dto): User
  - UserService.GetUserByEmail(email): User or null
  - UserService.UpdateUserTimezone(user_id, timezone): User

- [ ] T028 [P] [US2] Create frontend Login page (file: src/frontend/src/pages/LoginPage.tsx):
  - Input fields: email, password
  - Call POST /api/v1/auth/login on submit
  - Store JWT in localStorage (or secure cookie)
  - Redirect to /timeline on success
  - Show error toast on failure

- [ ] T029 [P] [US2] Create frontend Register page (file: src/frontend/src/pages/RegisterPage.tsx):
  - Input fields: email, password, password_confirm, name (optional), timezone (select, default UTC)
  - Validate passwords match and meet requirements
  - Call POST /api/v1/auth/register on submit
  - Store JWT in localStorage
  - Redirect to /timeline on success
  - Show error toast on failure

- [ ] T030 [P] [US2] Create frontend API client with JWT injection (file: src/frontend/src/services/apiClient.ts):
  - Axios instance with base URL http://localhost:5000/api/v1
  - Interceptor: Add Authorization: Bearer <token> header from localStorage
  - Interceptor: Handle 401 responses, redirect to /login
  - Export configured instance for all API calls

- [ ] T031 [P] [US2] Create frontend Unlock modal (file: src/frontend/src/components/UnlockModal.tsx):
  - Triggered when private entry accessed without unlock
  - Input field: password
  - Call POST /api/v1/unlock on submit
  - Store unlock_token in sessionStorage (1-hour expiry)
  - Retry GET /api/v1/entries/{id}?unlock_token=... and show entry
  - Show error message if unlock fails

---

### Sprint 2b — Media Upload & Storage (2 weeks)

**Story Goal**: Implement media file upload, storage in S3-compatible service, and thumbnail generation.

**User Story**: **US3: Upload and store images/videos with S3-compatible storage and thumbnails**

**Independent Test Criteria**:
- [ ] POST /api/v1/media/upload generates signed S3 URL for client upload
- [ ] Client uploads file to S3 via signed URL
- [ ] GET /api/v1/media/{id} returns media metadata with thumbnail_url
- [ ] Thumbnail generated for images within 5 minutes (async job)
- [ ] Media properly associated with entries (media.entry_id populated)
- [ ] DELETE entry removes media files from S3
- [ ] Max file size 100 MB enforced
- [ ] MIME type validation on upload

**Implementation Tasks**:

- [ ] T032 [P] [US3] Create S3 configuration and client (file: src/backend/Services/S3Service.cs):
  - Initialize S3 client (AWS SDK or MinIO equivalent)
  - Endpoint configuration from MINIO_ENDPOINT env var
  - Access key/secret from environment
  - GeneratePresignedUrl(key, expiration): string
    - Generate 1-hour signed URL for client to upload directly
  - DeleteObject(key): Task
  - ListObjects(prefix): List<string>

- [ ] T033 [P] [US3] Create Media Controller (file: src/backend/Controllers/MediaController.cs):
  - POST /api/v1/media/upload → RequestUpload(RequestUploadDto):
    - Body: { filename, mime, size }
    - Validate: size ≤ 100 MB, mime in whitelist (image/jpeg, image/png, video/mp4, etc.)
    - Generate storage key: {user_id}/{timestamp}_{filename}
    - Call S3Service.GeneratePresignedUrl
    - Create Media record: id, user_id, url (empty), storage_key, mime, size
    - Return 200 { media_id, presigned_url, expires_in }
  - POST /api/v1/media/complete → CompleteUpload(CompleteUploadDto):
    - Body: { media_id }
    - Verify file exists in S3 at storage_key
    - Get object metadata from S3 (actual size, etag)
    - Update Media.url to public CDN URL or signed read URL
    - Enqueue thumbnail job if image MIME type (T034)
    - Return 200 { media_id, url, thumbnail_url (null initially) }
  - DELETE /api/v1/media/{id} → DeleteMedia(id):
    - Verify user owns media
    - Call S3Service.DeleteObject
    - Delete Media record
    - Return 204

- [ ] T034 [P] [US3] Create Media DTOs (file: src/backend/Data/Dtos/Media/):
  - RequestUploadDto: filename, mime, size
  - CompleteUploadDto: media_id
  - MediaResponseDto: id, url, mime, size, thumbnail_url

- [ ] T035 [P] [US3] Create thumbnail generation worker (file: src/backend/Workers/ThumbnailWorker.cs):
  - Background job (using Hangfire): receive Media.id
  - Download image from S3 (storage_key)
  - Generate thumbnail: resize to 300x300 px, preserve aspect ratio, convert to JPEG
  - Upload thumbnail to S3 at {storage_key}_thumb
  - Update Media.thumbnail_url with thumbnail public URL
  - Handle errors (invalid image, S3 error) and retry 3 times

- [ ] T036 [P] [US3] Create Media model (file: src/backend/Models/Media.cs):
  - id, user_id, entry_id (nullable), url, mime, size, thumbnail_url, storage_key, created_at
  - [Required] user_id, mime
  - [Index(nameof(user_id))]

- [ ] T037 [US3] Create Hangfire background job configuration (file: src/backend/Services/BackgroundJobService.cs):
  - Setup Hangfire with SQL Server or PostgreSQL storage
  - RegisterJob("thumbnail", ThumbnailWorker.Execute)
  - EnqueueThumbnailJob(media_id): method to enqueue from controller

- [ ] T038 [P] [US3] Update Entry Create endpoint to accept media (file: src/backend/Controllers/EntriesController.cs):
  - POST /api/v1/entries body now includes: media_ids: [uuid]
  - After entry created, for each media_id:
    - Update Media.entry_id = entry.id
    - Enqueue thumbnail job if not already done

- [ ] T039 [P] [US3] Create frontend media uploader component (file: src/frontend/src/components/MediaUploader.tsx):
  - Input: file picker (accept image/video)
  - Call POST /api/v1/media/upload to get presigned_url
  - Upload file directly to S3 using presigned_url
  - Show upload progress bar
  - Call POST /api/v1/media/complete to finalize
  - Return media_id to parent form
  - Handle errors

- [ ] T040 [P] [US3] Update Entry Create form to include media (file: src/frontend/src/components/EntryForm.tsx):
  - Add MediaUploader component
  - Collect media_ids from uploader
  - Include in POST /api/v1/entries body

- [ ] T041 [P] [US3] Create media gallery component (file: src/frontend/src/components/MediaGallery.tsx):
  - Display media thumbnails in grid
  - Show full image/video on click (lightbox)
  - Link to media_id for deletion

---

## Phase 3: Advanced Features

### Sprint 3a — Export & Import (2 weeks)

**Story Goal**: Implement async export jobs and bulk import with confidentiality validation.

**User Story**: **US4: Export journal as JSON/Markdown/ZIP and import entries with confidentiality metadata**

**Independent Test Criteria**:
- [ ] POST /api/v1/exports with sync option returns ZIP download immediately
- [ ] POST /api/v1/exports with async option returns job_id and queues background job
- [ ] GET /api/v1/exports/{id} returns job status and download_url when ready
- [ ] Exported entries include all fields (entries, media, categories, sentiment)
- [ ] Exported ZIP contains markdown files + media folder with all assets
- [ ] POST /api/v1/import validates confidentiality on each entry
- [ ] Imported entries create new id (no overwrite)
- [ ] Imported entries get created_at = now, source = 'import'
- [ ] Async export sends email notification (mock for MVP)

**Implementation Tasks**:

- [ ] T042 [P] [US4] Create Export Controller (file: src/backend/Controllers/ExportController.cs):
  - POST /api/v1/exports → RequestExport(ExportRequestDto):
    - Body: { scope: { entries: [ids] | date_range: {start, end} }, format: 'json'|'md'|'zip', async_mode: bool }
    - If async_mode = false and small scope: generate and return 200 with ZIP binary
    - If async_mode = true or large scope: create ExportJob record, enqueue async job, return 202 { job_id, expires_at }
  - GET /api/v1/exports/{id} → GetExportStatus(id):
    - Return job status: queued|running|completed|failed
    - If completed: include download_url (signed S3 URL, 1-week expiration)
    - Return 200
  - GET /api/v1/exports → ListExports(page, per_page):
    - Return paginated list of user's export jobs
    - Show most recent first

- [ ] T043 [P] [US4] Create Export DTOs and models (file: src/backend/Data/Dtos/Export/):
  - ExportRequestDto: scope, format, async_mode
  - ExportResponseDto: job_id, status, created_at, completed_at, download_url
  - Update ExportJob model: id, user_id, scope (jsonb), format, status, download_url, created_at, completed_at

- [ ] T044 [P] [US4] Create export generation service (file: src/backend/Services/ExportService.cs):
  - GenerateZipExport(entries: List<Entry>): byte[]
    - For each entry: create markdown file with title, body, metadata, tags, sentiment
    - Copy media files to ZIP under /media
    - Create index.md listing all entries
    - Return ZIP bytes
  - GenerateJsonExport(entries: List<Entry>): byte[]
    - Serialize entries with full graph (categories, media, sentiment)
    - Return JSON bytes
  - GenerateMarkdownExport(entries: List<Entry>): string
    - Concatenate markdown for all entries with separator
    - Return markdown string

- [ ] T045 [P] [US4] Create async export worker (file: src/backend/Workers/ExportWorker.cs):
  - Background job: receive ExportJob.id
  - Query entries matching job.scope
  - Call ExportService.GenerateZipExport (or other format)
  - Upload ZIP to S3 at exports/{user_id}/{job_id}.zip
  - Generate signed download URL (1 week expiry)
  - Update ExportJob: status = 'completed', download_url = signed_url, completed_at = now
  - Enqueue email notification (T050)

- [ ] T046 [P] [US4] Create Import Controller (file: src/backend/Controllers/ImportController.cs):
  - POST /api/v1/import → ImportEntries(ImportRequestDto):
    - Body: { entries: [{ title, body_text, type, category_name, tags, confidentiality, media_urls, created_at, sentiment }] }
    - Validate: each entry has confidentiality field, REQUIRED
    - For each entry:
      - Create category if not exists (user-specific)
      - Create Entry record: id = new uuid, user_id, source = 'import', created_at = provided or now, timestamp from original if valid
      - Create Media records from media_urls (copy to user's S3 bucket)
    - Return 201 { imported_count, entries: [id, title] }

- [ ] T047 [P] [US4] Create Import DTOs (file: src/backend/Data/Dtos/Import/):
  - ImportEntryDto: title, body_text, type, category_name, tags, confidentiality, media_urls, created_at, sentiment
  - ImportRequestDto: entries: List<ImportEntryDto>
  - ImportResponseDto: imported_count, entries: List<{id, title}>

- [ ] T048 [US4] Create import validation service (file: src/backend/Services/ImportService.cs):
  - ValidateImportEntry(entry): (isValid: bool, errors: List<string>)
    - Check: confidentiality present and valid
    - Check: body_text or media present (not empty)
    - Check: created_at is valid datetime if provided
    - Return validation result

- [ ] T049 [P] [US4] Create frontend Export dialog (file: src/frontend/src/components/ExportDialog.tsx):
  - Modal with options:
    - Scope: all entries | this month | custom date range | selected entries
    - Format: JSON | Markdown | ZIP (media included)
    - Mode: Download now (sync) | Email when ready (async)
  - Call POST /api/v1/exports
  - If sync: download file immediately
  - If async: show job_id and polling status, notify when ready
  - Show progress bar and estimated time

- [ ] T050 [P] [US4] Create email notification service (file: src/backend/Services/EmailService.cs):
  - SendExportReadyEmail(user_email, download_url, expires_at): Task
  - Use SMTP client or SendGrid API (mock for MVP)
  - Email template: "Your export is ready. Download link expires on {expires_at}"

- [ ] T051 [US4] Create frontend Import dialog (file: src/frontend/src/components/ImportDialog.tsx):
  - File picker: accept JSON, ZIP
  - Preview: show entry count, categories, date range
  - Confirm import
  - Call POST /api/v1/import
  - Show success: "Imported X entries"
  - Show error details if validation failed

---

### Sprint 3b — Sentiment Analysis (2 weeks)

**Story Goal**: Implement client-side sentiment analysis using sentiment.js library (privacy-preserving, per spec clarification).

**User Story**: **US5: Analyze entry sentiment client-side with sentiment.js and display trend visualization**

**Independent Test Criteria**:
- [ ] sentiment.js integrated and working client-side
- [ ] Entry displays sentiment score (-1 to 1) and label (positive/neutral/negative)
- [ ] Sentiment computed on entry creation, not on server
- [ ] Sentiment scores persisted in entry.sentiment_score, entry.sentiment_label
- [ ] Sentiment not sent to any external service
- [ ] Sentiment visualization shows trend over time (chart component)
- [ ] Sentiment analytics page displays:
  - Pie chart: % positive, neutral, negative entries
  - Line chart: sentiment trend over last 30 days
  - Top 5 most positive/negative entries

**Implementation Tasks**:

- [ ] T052 [P] [US5] Install and configure sentiment.js in frontend (file: src/frontend/package.json):
  - npm install sentiment
  - Create wrapper service: src/frontend/src/services/sentimentService.ts
  - Export analyzeSentiment(text): { score, comparative, words }
  - Score = -1 to 1 (from sentiment library score / max possible)

- [ ] T053 [P] [US5] Create sentiment analysis service (file: src/frontend/src/services/sentimentService.ts):
  - analyzeSentiment(text): { score: float(-1..1), label: 'positive'|'neutral'|'negative' }
    - Call sentiment.analyze(text)
    - Map score: if comparative > 0.1 → positive, < -0.1 → negative, else neutral
    - Normalize score to -1..1 range
    - Return {score, label}

- [ ] T054 [P] [US5] Update Entry Create form to compute sentiment (file: src/frontend/src/components/EntryForm.tsx):
  - On change of body_text field: call sentimentService.analyzeSentiment
  - Display live sentiment indicator: icon (😊😐😞) + score
  - Include sentiment_score, sentiment_label in POST /api/v1/entries body
  - Set sentiment_model = 'sentiment.js v1.0'

- [ ] T055 [P] [US5] Update Entry Create API to accept and store sentiment (file: src/backend/Controllers/EntriesController.cs):
  - POST /api/v1/entries body now includes: sentiment_score, sentiment_label, sentiment_model
  - Validate: sentiment_score in -1..1 range
  - Store in entry record

- [ ] T056 [P] [US5] Update Entry response to include sentiment (file: src/backend/Data/Dtos/EntryResponseDto):
  - Add: sentiment_score, sentiment_label, sentiment_model

- [ ] T057 [P] [US5] Create entry detail view with sentiment display (file: src/frontend/src/pages/EntryDetailPage.tsx):
  - Show entry with all fields including sentiment
  - Display large sentiment indicator with label and score
  - Show timestamp, category, tags, media gallery

- [ ] T058 [P] [US5] Create sentiment analytics page (file: src/frontend/src/pages/SentimentAnalyticsPage.tsx):
  - Fetch all entries: GET /api/v1/entries?page=1&per_page=1000
  - Compute statistics:
    - Total entries, positive count, neutral count, negative count
    - Pie chart: % distribution
    - Line chart: sentiment average per day over last 30 days
    - Top 5 most positive, top 5 most negative entries
  - Use charting library: recharts or chart.js

- [ ] T059 [P] [US5] Create sentiment analytics endpoint (file: src/backend/Controllers/AnalyticsController.cs):
  - GET /api/v1/analytics/sentiment → GetSentimentAnalytics(days: int = 30):
    - Query entries where sentiment_score IS NOT NULL
    - Group by date
    - Return: { total_entries, positive_count, neutral_count, negative_count, daily_avg: [{date, avg_score}], top_positive: [], top_negative: [] }

- [ ] T060 [P] [US5] Create navigation link to analytics (file: src/frontend/src/components/Navigation.tsx):
  - Add menu item: Analytics → /analytics
  - Only show if entries exist

---

## Phase 4: Mind-maps & UX Polish

### Sprint 4 — Mind-map Generation (2 weeks)

**Story Goal**: Implement mind-map generation using TF-IDF similarity and interactive graph visualization.

**User Story**: **US6: Generate interactive mind-maps at category/journal/entry level with cytoscape.js visualization**

**Independent Test Criteria**:
- [ ] GET /api/v1/mindmap?scope=category&id={category_id} returns node/edge graph JSON
- [ ] Nodes include entries and category name
- [ ] Edges connect similar entries (cosine similarity > threshold)
- [ ] GET /api/v1/mindmap renders interactive graph:
  - Nodes draggable, expandable/collapsible
  - Edges show similarity score on hover
  - Can export as PNG/SVG
- [ ] Mindmap generation completes in <5 seconds for 100 entries
- [ ] Mindmap includes sentiment scores as node colors (positive=green, neutral=gray, negative=red)
- [ ] User can create custom edges (user-defined relationships)

**Implementation Tasks**:

- [ ] T061 [P] [US6] Create mindmap generation service (file: src/backend/Services/MindmapService.cs):
  - GenerateMindmap(scope: 'category'|'journal'|'entry', scope_id: uuid): MindmapGraph
    - Query entries matching scope
    - Extract keywords from each entry: TF-IDF on body_text, tags
    - Compute pairwise similarity (cosine distance on TF-IDF vectors)
    - Create nodes: one per entry, one for category/journal header
    - Create edges where similarity > threshold (0.3)
    - Return: { nodes: [{id, label, type, sentiment_label, sentiment_score}], edges: [{source, target, weight}] }

- [ ] T062 [P] [US6] Create TF-IDF text processing utility (file: src/backend/Utils/TfidfUtils.cs):
  - Tokenize(text): List<string> (lowercase, remove stopwords, lemmatize)
  - BuildTfidfVectors(entries): Dictionary<entry_id, Dictionary<term, tf_idf_score>>
  - CosineSimilarity(vec1, vec2): float (0..1)

- [ ] T063 [P] [US6] Create Mindmap Controller (file: src/backend/Controllers/MindmapController.cs):
  - GET /api/v1/mindmap?scope=category&id={id}&threshold=0.3 → GetMindmap(scope, id, threshold):
    - Query entries matching scope
    - Call MindmapService.GenerateMindmap
    - Filter edges by threshold
    - Return 200 { nodes, edges }

- [ ] T064 [P] [US6] Create Mindmap DTOs (file: src/backend/Data/Dtos/Mindmap/):
  - MindmapNode: id, label, type ('entry'|'category'|'journal'), sentiment_label, sentiment_score
  - MindmapEdge: source, target, weight (similarity score)
  - MindmapGraph: nodes: List<MindmapNode>, edges: List<MindmapEdge>

- [ ] T065 [P] [US6] Install and configure cytoscape.js in frontend (file: src/frontend/package.json):
  - npm install cytoscape cytoscape-dagre dagre
  - Create cytoscape wrapper component

- [ ] T066 [P] [US6] Create Mindmap visualization component (file: src/frontend/src/components/MindmapViewer.tsx):
  - Input: mindmapGraph (nodes, edges)
  - Render cytoscape container with:
    - Nodes with color based on sentiment (positive=green, neutral=gray, negative=red)
    - Node size proportional to entry length (character count)
    - Edges with labels showing similarity score
    - Layout: dagre or concentric based on clustering
  - Interactions:
    - Click node → show entry preview
    - Drag nodes to reposition
    - Hover edge → show similarity tooltip
    - Export buttons: PNG, SVG, JSON

- [ ] T067 [P] [US6] Create Mindmap page (file: src/frontend/src/pages/MindmapPage.tsx):
  - Scope selector: all journal | by category | by date range
  - Threshold slider (0..1): filter edges by minimum similarity
  - Call GET /api/v1/mindmap with params
  - Render MindmapViewer component
  - Show legend: node colors = sentiment, node size = entry length

- [ ] T068 [P] [US6] Create custom edge creation UI (file: src/frontend/src/components/MindmapEditor.tsx):
  - After rendering mindmap: allow user to draw new edges between nodes
  - Click + drag from node to node → create edge
  - POST /api/v1/mindmap/edges to persist custom edge
  - Show user-created edges in different style (e.g., dashed)

- [ ] T069 [US6] Create Mindmap edge storage (file: src/backend/Models/MindmapEdge.cs and Controller):
  - Model: id, user_id, source_entry_id, target_entry_id, created_by_user (bool)
  - POST /api/v1/mindmap/edges → CreateCustomEdge(source_id, target_id)
  - GET /api/v1/mindmap includes custom edges with user_created flag

---

## Phase 5: Testing, Performance & Release

### Sprint 5 — Integration Tests, Performance Optimization, Release (2 weeks)

**Story Goal**: Complete integration and E2E tests, optimize database queries and API latency, prepare for production release.

**User Story**: **US7: Full integration testing, performance optimization, and production readiness**

**Independent Test Criteria**:
- [ ] Integration tests cover all user flows: create → edit → sentiment → unlock private → export
- [ ] E2E tests cover registration → create entry → add media → export → import
- [ ] Database query performance: GET entries P95 < 300ms, GET entry P95 < 150ms
- [ ] Memory usage stable (no leaks) after 1 hour continuous use
- [ ] All 12 MVP features tested and passing
- [ ] Code coverage ≥80% overall, ≥90% for auth/entries/export services
- [ ] Security audit passes: password hashing, JWT validation, SQL injection prevention
- [ ] Accessibility audit passes: WCAG 2.1 AA compliance

**Implementation Tasks**:

- [ ] T070 [P] [US7] Create integration test suite (file: src/backend/Tests/IntegrationTests.cs):
  - Test user registration and login flow
  - Test entry creation with all fields (text, category, tags, confidentiality)
  - Test edit/delete immutability enforcement
  - Test private entry unlock with password
  - Test media upload and association
  - Test export (sync and async)
  - Test import with confidentiality validation
  - Test sentiment computation
  - Each test: setup data → call APIs → assert results
  - Aim for ≥80% overall coverage

- [ ] T071 [P] [US7] Create E2E test suite (file: src/frontend/tests/e2e/):
  - Use Playwright or Cypress
  - Test: register → login → create entry → add media → view timeline → unlock private → export
  - Test: import entries from JSON
  - Test: create mindmap
  - Test: view sentiment analytics
  - Each test: navigate UI → perform actions → assert UI state

- [ ] T072 [P] [US7] Optimize database indexes and queries (file: src/backend/Data/):
  - Add index (user_id, created_at DESC) for fast timeline queries
  - Add full-text index on body_text for search
  - Add GIN index on tags array
  - Update EF Core queries to use indexes efficiently
  - Benchmark: GET /api/v1/entries?page=1 with 10k entries → target P95 < 300ms

- [ ] T073 [P] [US7] Implement query result caching (file: src/backend/Services/CacheService.cs):
  - Cache GET /api/v1/entries list (1 minute TTL, invalidate on POST/PATCH/DELETE)
  - Cache GET /api/v1/mindmap results (5 minute TTL)
  - Cache analytics results (1 hour TTL)
  - Use Redis if available, else in-memory cache

- [ ] T074 [P] [US7] Create performance benchmarking script (file: src/backend/Tests/PerformanceBenchmarks.cs):
  - Benchmark entry creation (target: <50ms per entry)
  - Benchmark entry list query (target: <300ms for 100 entries)
  - Benchmark single entry read (target: <150ms)
  - Benchmark export generation (target: <5s for 100 entries)
  - Write results to file for tracking

- [ ] T075 [P] [US7] Add API rate limiting middleware (file: src/backend/Middleware/RateLimitingMiddleware.cs):
  - Per-user limit: 100 requests per minute
  - Per-IP limit: 10 requests per second
  - Return 429 with Retry-After header when exceeded
  - Implement using sliding window or token bucket algorithm

- [ ] T076 [P] [US7] Create comprehensive error logging (file: src/backend/Logging/ErrorLogger.cs):
  - Log all errors with context: user_id, endpoint, error code, stack trace
  - Store in structured logging (Serilog or similar)
  - Create dashboard to view errors (optional: Seq)

- [ ] T077 [P] [US7] Create security audit checklist (file: docs/SECURITY_AUDIT.md):
  - [ ] Passwords hashed with bcrypt ≥12 rounds
  - [ ] JWT tokens signed and validated on all protected endpoints
  - [ ] No sensitive data in logs (passwords, tokens)
  - [ ] SQL injection prevention: all queries parameterized
  - [ ] CSRF protection: POST requests validate tokens
  - [ ] CORS properly configured: only allow frontend origin
  - [ ] TLS enforced: redirect HTTP to HTTPS
  - [ ] Rate limiting enabled
  - [ ] Run manual review + automated tools (e.g., Snyk, OWASP ZAP)

- [ ] T078 [P] [US7] Create accessibility audit checklist (file: docs/ACCESSIBILITY_AUDIT.md):
  - [ ] All images have alt text
  - [ ] Color contrast ≥4.5:1 for normal text
  - [ ] All interactive elements keyboard accessible (Tab, Enter, Escape)
  - [ ] Forms have associated labels
  - [ ] Error messages clear and actionable
  - [ ] Use semantic HTML: <button>, <label>, <form>
  - [ ] Run: WAVE, Lighthouse, axe DevTools

- [ ] T079 [P] [US7] Create user documentation (file: docs/USER_GUIDE.md):
  - Getting started: sign up, create entry
  - Features: categories, tags, sentiment, mindmap, export
  - Privacy explanation: no_training_use flag, data deletion
  - FAQ: password reset, data export, account deletion

- [ ] T080 [P] [US7] Create admin documentation (file: docs/ADMIN_GUIDE.md):
  - Deployment steps: docker-compose, K8s, App Service
  - Monitoring: metrics, logs, alerts
  - Troubleshooting: common issues
  - Backup and recovery

- [ ] T081 [P] [US7] Create deployment documentation (file: docs/DEPLOYMENT.md):
  - Environment setup: database, S3, redis
  - Configuration: env variables
  - Migration steps: run EF Core migrations
  - Rollback procedure
  - Health checks: /health endpoint

- [ ] T082 [P] [US7] Create GitHub Actions deployment workflow (file: .github/workflows/deploy.yml):
  - Trigger: on push to main branch
  - Steps:
    - Run tests (all must pass)
    - Build Docker image
    - Push to registry
    - Deploy to target environment (K8s or App Service)
    - Run smoke tests
    - Roll back on failure

- [ ] T083 [P] [US7] Add monitoring dashboards (file: src/backend/Monitoring/):
  - Create Prometheus metrics:
    - Request count by endpoint
    - Request latency percentiles (P50, P95, P99)
    - Error count by type
    - Database connection pool usage
  - Create Grafana dashboard with:
    - Requests per second
    - Latency trends
    - Error rate
    - CPU/memory usage
    - Database metrics

- [ ] T084 [P] [US7] Create smoke test suite (file: src/backend/Tests/SmokeTests.cs):
  - Quick validation after deployment
  - Test: health check, database connectivity, S3 connectivity, JWT generation
  - Run before marking deployment successful

- [ ] T085 [P] [US7] Create canary rollout plan (file: docs/CANARY_ROLLOUT.md):
  - Deploy new version to 5% of instances
  - Monitor for errors for 15 minutes
  - If errors < threshold: increase to 25%, then 50%, then 100%
  - If errors detected: automatic rollback

- [ ] T086 [P] [US7] Final code review checklist (file: docs/CODE_REVIEW_CHECKLIST.md):
  - All unit tests passing (≥80% coverage)
  - All integration tests passing
  - API documentation updated (OpenAPI spec)
  - Error handling comprehensive (all error codes documented)
  - Security review completed
  - Accessibility review completed
  - Performance benchmarks met

---

## Phase 6: Polish & Cross-Cutting Concerns

### Final Phase — Security, Privacy, Compliance, Monitoring

**Tasks**:

- [ ] T087 [P] Implement privacy audit logging (file: src/backend/Services/PrivacyAuditService.cs):
  - Weekly job: query all data access patterns (audit_logs)
  - Check: no entries marked no_training_use were accessed by ML systems
  - Log results to encrypted storage
  - Create compliance report

- [ ] T088 [P] Add data retention policy enforcement (file: src/backend/Jobs/DataRetentionJob.cs):
  - Monthly job: delete entries older than retention window (if user policy requests)
  - Permanently remove from S3
  - Log deletion in audit trail

- [ ] T089 [P] Create GDPR data export endpoint (file: src/backend/Controllers/ComplianceController.cs):
  - GET /api/v1/compliance/data-export → DownloadUserData():
    - Collect all user data: entries, categories, media, audit logs, unlock sessions
    - Package as ZIP with markdown and JSON formats
    - Return to user
    - Complies with GDPR Article 20

- [ ] T090 [P] Create account deletion flow (file: src/backend/Controllers/ComplianceController.cs):
  - DELETE /api/v1/user/account:
    - Soft delete: mark user as deleted, anonymize email
    - Hard delete option (after 30-day grace period): remove all entries and media
    - Enqueue media cleanup jobs

- [ ] T091 [P] Add OpenTelemetry instrumentation (file: src/backend/Startup.cs and src/frontend/vite.config.ts):
  - Backend: trace all HTTP requests, database queries, external API calls
  - Frontend: trace component renders, API calls, errors
  - Export traces to Jaeger or cloud service

- [ ] T092 [P] Create runbook for incident response (file: docs/INCIDENT_RESPONSE.md):
  - Common issues: database down, S3 unavailable, high latency
  - Debugging steps: check logs, metrics, health endpoints
  - Escalation path: who to notify

- [ ] T093 [P] Update README with deployment instructions (file: README.md):
  - Local development: docker-compose up
  - Cloud deployment: K8s deployment guide
  - Monitoring: how to access Grafana, logs
  - Links to all documentation

- [ ] T094 [P] Create API rate limiting dashboard (file: src/frontend/src/pages/AdminDashboard.tsx):
  - Show current rate limit usage
  - Display top consumers
  - Manual override option for trusted clients (future)

- [ ] T095 [P] Add request/response validation middleware (file: src/backend/Middleware/ValidationMiddleware.cs):
  - Validate all requests against schema (OpenAPI)
  - Return 400 with detailed error if invalid
  - Log schema violations for debugging

---

## Task Dependencies Summary

| Task | Depends On | Reason |
|------|-----------|--------|
| T014-T021 (US1) | T011-T013 (DB setup) | Need EF Core models and migrations first |
| T022-T031 (US2) | T014-T021 (US1) | Auth controllers build on entry controllers |
| T032-T041 (US3) | T014-T021 (US1) | Media tied to entries |
| T042-T051 (US4) | T014-T021, T032-T041 | Export/import need entries and media |
| T052-T060 (US5) | T014-T021 (US1) | Sentiment stored on entries |
| T061-T069 (US6) | T014-T021, T052-T060 | Mindmap based on entries and sentiment |
| T070-T086 (US7) | All above (US1-US6) | Integration tests depend on features |

---

## Test Coverage Matrix

| Feature | Unit Tests | Integration Tests | E2E Tests |
|---------|-----------|------------------|-----------|
| Entry CRUD | T018-T019 | T070 | T071 |
| Auth | Test in T022 | T070 | T071 |
| Media | Test in T032 | T070 | T071 |
| Export/Import | T048 | T070 | T071 |
| Sentiment | Test in T052 | T070 | T071 |
| Mindmap | Test in T061 | T070 | T071 |

---

## Quality Gates Per Phase

| Phase | Gate | Success Criteria |
|-------|------|------------------|
| Sprint 0 | Docker Compose | All services start, API responds, frontend loads |
| Sprint 1 | DB & Core | ≥90% coverage on entry service, P95 latency <300ms |
| Sprint 2a/2b | Auth & Media | JWT valid, media uploads to S3, thumbnails generated |
| Sprint 3a/3b | Export & Sentiment | Export downloads correctly, sentiment scores valid |
| Sprint 4 | Mindmap | Graph renders, interactive, no infinite loops |
| Sprint 5 | Release | All tests pass, performance benchmarks met, security audit passed |

---

## How to Use This Tasks File

1. **Sprint Planning**: Read Sprint X section to understand phase goals
2. **Parallel Work**: Identify [P] marked tasks that can run concurrently
3. **Task Execution**: Follow checklist format and mark complete as you finish
4. **Testing**: Each phase includes test criteria — verify before moving to next
5. **Blockers**: If blocked, reference "Depends On" column to identify prerequisite
6. **Documentation**: Update relevant .md files in docs/ and src/ as per tasks

---

## Execution Timeline

**Week 1 (Sprint 0)**: Setup infrastructure (T001-T010)  
**Weeks 2-3 (Sprint 1)**: Core entries & database (T011-T021)  
**Weeks 4-5 (Sprint 2a)**: Authentication & unlock (T022-T031)  
**Weeks 4-5 (Sprint 2b)**: Media upload & storage (T032-T041) *parallel to 2a*  
**Weeks 6-7 (Sprint 3a)**: Export & import (T042-T051)  
**Weeks 6-7 (Sprint 3b)**: Sentiment analysis (T052-T060) *parallel to 3a*  
**Weeks 8-9 (Sprint 4)**: Mind-maps (T061-T069)  
**Weeks 10-12 (Sprint 5)**: Testing, performance, release (T070-T095)  

**Total**: 12 weeks, 95 tasks, ~3 tasks/day average (varies by sprint)

---

**Status**: ✅ All tasks extracted from TECHNICAL_PLAN_V2.md and organized by user story per constitution principles.

**Next Steps**: 
1. Review tasks for team feedback
2. Assign developers to tasks (consider parallelization)
3. Create Jira/ADO board with tasks
4. Kick off Sprint 0 with T001-T010
