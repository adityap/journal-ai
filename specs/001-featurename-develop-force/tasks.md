---
description: "Generated tasks for feature 001-featurename-develop-force"
---

# Tasks: 001-featurename-develop-force

## Phase 1: Setup

- [x] T201: Auth DTOs & Service
  - Create `RegisterDto`, `LoginDto`, `TokenDto`, and `AuthService` with bcrypt hashing and JWT token generation
- [x] T202: Auth Controller
  - Implement endpoints: `POST /register`, `POST /login`, `POST /logout`, `GET /me`, `POST /validate`

---

## Phase 2: Foundational (Blocking Prerequisites)

- [x] T203: JWT Middleware & Authorization
  - Ensure protected endpoints use `[Authorize]` and 401 handling configured
- [x] T204: Auth Tests
  - Unit tests for `AuthService` and integration/controller tests for `AuthController`

---

## Phase 3: Media (Priority)

- [x] T205: S3Service & MediaService
  - Implemented `S3Service` with presigned URLs (download/upload), object existence checks, deletions, and storage key generation
  - Implemented `MediaService` with full media lifecycle (create, retrieve, delete, finalize upload, associate with entry, update thumbnail)
  - Created `MediaDtos` for upload initiation, completion, and response structures
  - Integrated AWS S3 SDK (AWSSDK.S3 3.7.300.4) with MinIO support for development
  - Supports configurable S3 bucket, file validation (MIME type, size limits), and thumbnail management
  - Build succeeds: 0 errors, multiple type conflict warnings (expected due to test assembly reference strategy)
- [x] T206: Media Controller
  - Implemented `MediaController` with 6 endpoints:
    - `POST /api/v1/media/initiate` - Start multipart upload, return presigned URL
    - `POST /api/v1/media/complete` - Finalize upload, create Media record
    - `GET /api/v1/media/{id}` - Retrieve media metadata (ownership verified)
    - `GET /api/v1/media` - List user's media with optional entry filter
    - `DELETE /api/v1/media/{id}` - Delete media and remove from S3
    - `POST /api/v1/media/{id}/associate-entry/{entryId}` - Associate media with entry
  - All endpoints require JWT `[Authorize]` attribute
  - Proper error handling and validation (400, 404, 500 responses)
  - Integrated with MediaService for business logic
  - Build succeeds: 0 errors
- [ ] T207: Hangfire & Thumbnail Worker
  - Configure Hangfire and add `ThumbnailGenerationJob` for async image processing (thumbnail creation, job retry)
- [ ] T208: Media Tests
  - Add `MediaServiceTests` and `MediaControllerTests` with ≥85% coverage for media flows

---

## Phase 4: Frontend

- [ ] T209: Frontend Auth UI
  - Add `LoginForm.tsx`, `RegisterForm.tsx`, auth context/provider, and protected routing in frontend

---

## Notes

- This `tasks.md` was generated to satisfy the repository prerequisite step and reflects the project's current todo list. Update or expand tasks as user stories and contracts are clarified.
