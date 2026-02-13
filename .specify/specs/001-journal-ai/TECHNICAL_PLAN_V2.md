# Journal AI — Technical Implementation Plan

**Created**: 2026-02-13  
**Version**: 2.0 (Updated with clarifications & constitution)  
**Tech Stack**: ASP.NET Core 8 + PostgreSQL + React 18 + TypeScript  
**Status**: Ready for Sprint 0

---

## Executive Summary

**Goal**: Deliver Journal AI MVP in 6 sprints (12 weeks) with privacy-first architecture.

**Key Decisions**:
- ✅ Backend: ASP.NET Core 8 + EF Core
- ✅ Database: PostgreSQL + S3-compatible blob store (MinIO dev, AWS S3 prod)
- ✅ Frontend: React 18 + Vite + TypeScript
- ✅ Sentiment: Client-side only (sentiment.js library)
- ✅ Auth: JWT + session-based unlock (1-hour timeout)
- ✅ Data Model: Single category per entry (1:1) + flat tags
- ✅ Export: User choice (sync/async download)

**Architecture**: API Gateway → ASP.NET Core API → PostgreSQL + Blob Store + Background Workers

---

## Part 1: Technical Context

### Tech Stack Details

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Backend** | ASP.NET Core | 8 LTS | REST API, business logic, auth |
| **ORM** | EF Core + Npgsql | Latest | PostgreSQL data access |
| **Database** | PostgreSQL | 14+ | Relational data + JSONB metadata |
| **Blob Storage** | S3-compatible (MinIO/AWS S3) | Latest | Media (images, videos) |
| **Frontend** | React | 18+ | SPA, component-based UI |
| **Frontend Build** | Vite | 5+ | Fast HMR, optimized bundling |
| **Frontend Lang** | TypeScript | 5+ | Type safety, IDE support |
| **Sentiment** | sentiment.js | 5.0+ | Client-side NLP (no server) |
| **Mindmap UI** | cytoscape.js or vis.js | Latest | Interactive graph visualization |
| **Auth** | JWT + Sessions | — | Stateless API + session state |
| **Containerization** | Docker + Docker Compose | Latest | Local dev + production consistency |
| **Container Orchestration** | Kubernetes (optional) or App Service | Latest | Production deployment |
| **Observability** | OpenTelemetry + Prometheus/Grafana | Latest | Metrics, tracing, logging |
| **API Spec** | OpenAPI 3.0 + Swashbuckle | — | Contract-first design |
| **CI/CD** | GitHub Actions | — | Automated testing, build, deploy |

### Dependencies & Versions

**Backend (.NET 8)**:
```
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0+" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0+" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4+" />
  <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="6.35+" />
  <PackageReference Include="AWSSDK.S3" Version="3.7+" />
  <PackageReference Include="Hangfire" Version="1.8+" />
  <PackageReference Include="OpenTelemetry" Version="1.8+" />
</ItemGroup>
```

**Frontend (Node 18+)**:
```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "typescript": "^5.3.0",
    "axios": "^1.6.0",
    "sentiment": "^5.0.0",
    "cytoscape": "^3.28.0",
    "chakra-ui": "^2.8.0"
  },
  "devDependencies": {
    "vite": "^5.0.0",
    "@vitejs/plugin-react": "^4.2.0",
    "@testing-library/react": "^14.0.0",
    "vitest": "^1.0.0"
  }
}
```

### External Services

- **Email**: SMTP or SendGrid (for export notifications, password resets)
- **Secrets Management**: Azure KeyVault / HashiCorp Vault / AWS Secrets Manager
- **Monitoring**: Datadog / Application Insights / Prometheus + Grafana
- **CDN** (optional): Cloudflare / CloudFront (for media serving)

---

## Part 2: Architecture

### High-Level Diagram

```
┌─────────────────────────────────────────────────────┐
│                   Frontend (React)                  │
│  - Timeline view, Entry editor, Unlock modal       │
│  - Client-side sentiment (sentiment.js)            │
│  - Mindmap UI (cytoscape.js)                       │
│  - Export/Import UI                                │
└──────────────────────┬──────────────────────────────┘
                       │ HTTPS + JWT Bearer Token
                       ↓
┌─────────────────────────────────────────────────────┐
│        API Gateway (optional ALB/APIM)              │
│  - TLS termination                                  │
│  - Rate limiting (100 req/min per user)            │
│  - Authentication validation                       │
└──────────────────────┬──────────────────────────────┘
                       │ Internal HTTP
                       ↓
┌─────────────────────────────────────────────────────┐
│    ASP.NET Core 8 API (Kestrel Web Server)         │
│  ├─ POST /api/v1/entries (create entry)           │
│  ├─ GET /api/v1/entries (list entries)            │
│  ├─ GET /api/v1/entries/{id} (read entry)         │
│  ├─ PATCH /api/v1/entries/{id} (edit entry)       │
│  ├─ DELETE /api/v1/entries/{id} (delete entry)    │
│  ├─ POST /api/v1/unlock (unlock session)          │
│  ├─ POST /api/v1/exports (create export job)      │
│  ├─ GET /api/v1/exports/{id} (export status)      │
│  ├─ POST /api/v1/imports (bulk import)            │
│  ├─ GET /api/v1/mindmap (generate graph)          │
│  └─ POST /api/v1/categories (manage categories)   │
└──────────┬──────────────┬────────────────┬─────────┘
           │              │                │
    ┌──────▼──────┐  ┌───▼────────┐  ┌───▼─────────┐
    │ PostgreSQL  │  │   MinIO    │  │ Background  │
    │ (EF Core)   │  │  / AWS S3  │  │  Worker     │
    │ Database    │  │  Blob Store│  │ (Hangfire)  │
    │             │  │  Media     │  │ - Export    │
    │ - Users     │  │  - Images  │  │ - Thumbs    │
    │ - Entries   │  │  - Videos  │  │ - Mindmap   │
    │ - Audit Logs│  │  - Thumbs  │  │   gen       │
    └─────────────┘  └────────────┘  └────────────┘
```

### Data Flow — Create Entry Example

```
Frontend (React)
  ↓ POST /api/v1/entries
     { title, body_text, category_id, tags, confidentiality, media_ids, created_at }
  ↓
ASP.NET Core API
  ├─ Validate: confidentiality present, title+body or media required
  ├─ Validate: category_id exists (if provided)
  ├─ Compute: read_only_after = 23:59:59 on created_at date (user timezone)
  ├─ Encrypt: media URLs if confidentiality='private'
  ├─ Save: EF Core → PostgreSQL
  ├─ Audit: Log action to audit_logs table
  ├─ Return: { id, read_only_after, immutable=false, created_at }
  ↓
Frontend
  ├─ Display: "Entry created"
  ├─ Compute (client): sentiment.js on body_text
  ├─ Store: sentiment score + timestamp locally
  ├─ Display: sentiment badge on entry
  ↓
Background: None (entry creation is synchronous)
```

### Database Schema — Core Tables

```sql
-- Users table
CREATE TABLE users (
  id UUID PRIMARY KEY,
  email VARCHAR(320) UNIQUE NOT NULL,
  password_hash VARCHAR NOT NULL,
  timezone VARCHAR(64) NOT NULL DEFAULT 'UTC',
  settings JSONB,
  no_training_use BOOLEAN NOT NULL DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ
);

-- Categories table (flat, no hierarchy)
CREATE TABLE categories (
  id UUID PRIMARY KEY,
  user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  name VARCHAR NOT NULL,
  color VARCHAR(7),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ
);

-- Entries table
CREATE TABLE entries (
  id UUID PRIMARY KEY,
  user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  title VARCHAR,
  body_text TEXT,
  type VARCHAR(16) NOT NULL DEFAULT 'text', -- text|photo|video|mixed
  confidentiality VARCHAR(16) NOT NULL DEFAULT 'public', -- public|private
  confidentiality_method VARCHAR(32), -- account_password|none (MVP)
  category_id UUID REFERENCES categories(id) ON DELETE SET NULL,
  tags TEXT[] DEFAULT ARRAY[]::TEXT[],
  sentiment_score NUMERIC(5,4), -- -1..1
  sentiment_label VARCHAR(16), -- positive|neutral|negative
  sentiment_model VARCHAR DEFAULT 'sentiment.js/v1.0',
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ,
  read_only_after TIMESTAMPTZ NOT NULL,
  immutable BOOLEAN NOT NULL DEFAULT false,
  metadata JSONB,
  source VARCHAR(16) DEFAULT 'ui', -- ui|import|api
  PRIMARY KEY (id),
  UNIQUE (user_id, id),
  INDEX (user_id, created_at),
  INDEX (user_id, category_id),
  FULLTEXT INDEX (body_text)
);

-- Media table
CREATE TABLE media (
  id UUID PRIMARY KEY,
  user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  entry_id UUID REFERENCES entries(id) ON DELETE SET NULL,
  url TEXT NOT NULL,
  mime VARCHAR(128),
  size BIGINT,
  thumbnail_url TEXT,
  storage_key VARCHAR NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Audit logs table
CREATE TABLE audit_logs (
  id UUID PRIMARY KEY,
  entry_id UUID REFERENCES entries(id),
  user_id UUID REFERENCES users(id),
  action VARCHAR(32) NOT NULL, -- create|update|delete|unlock
  actor_ip VARCHAR(45),
  timestamp TIMESTAMPTZ NOT NULL DEFAULT now(),
  diff BYTEA -- encrypted diff
);

-- Export jobs table
CREATE TABLE export_jobs (
  id UUID PRIMARY KEY,
  user_id UUID NOT NULL REFERENCES users(id),
  scope JSONB NOT NULL,
  format VARCHAR(16) NOT NULL, -- json|md|zip
  mode VARCHAR(16) NOT NULL DEFAULT 'sync', -- sync|async
  status VARCHAR(16) NOT NULL DEFAULT 'queued', -- queued|running|completed|failed
  download_url TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  completed_at TIMESTAMPTZ
);

-- Unlock sessions table
CREATE TABLE unlock_sessions (
  id UUID PRIMARY KEY,
  user_id UUID NOT NULL REFERENCES users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  expires_at TIMESTAMPTZ NOT NULL, -- 1 hour from now
  invalidated_at TIMESTAMPTZ
);
```

### Key Indexes

```sql
-- Performance indexes
CREATE INDEX idx_entries_user_created ON entries(user_id, created_at DESC);
CREATE INDEX idx_entries_user_category ON entries(user_id, category_id);
CREATE INDEX idx_entries_immutable ON entries(immutable, read_only_after);
CREATE INDEX idx_audit_user_action ON audit_logs(user_id, action);
CREATE INDEX idx_exports_user_status ON export_jobs(user_id, status);

-- Full-text search (PostgreSQL)
CREATE INDEX idx_entries_body_search ON entries USING gin(to_tsvector('english', body_text));
```

---

## Part 3: Sprint Breakdown

### Sprint 0 — Setup & Scaffolding (1 Week)

**Goal**: Infrastructure ready; development environment working; no features shipped.

**Tasks**:
1. Create git repo structure + CONTRIBUTING.md + CODE_OF_CONDUCT.md
2. Scaffold ASP.NET Core 8 Web API project
   - Add Swashbuckle (OpenAPI generation)
   - Add EF Core + Npgsql
   - Configure appsettings.json, secrets management
   - Add health check endpoints
3. Scaffold React + Vite + TypeScript frontend
   - Add Chakra UI component library
   - Add React Router
   - Add Axios HTTP client
4. Create docker-compose.yml
   - PostgreSQL service (dev)
   - MinIO service (dev blob store)
   - Redis (optional, for queues)
   - Backend API service
   - Frontend dev service
5. Set up GitHub Actions CI
   - Lint (C#, TypeScript)
   - Type check (C#, TypeScript)
   - Unit test lane
   - Build lane
6. Create .editorconfig, prettier, eslint configs
7. Set up secrets management strategy (environment variables)
8. Create PR template with constitution checklist

**Deliverables**:
- ✅ Git repo with structure
- ✅ docker-compose.yml running locally
- ✅ `dotnet run` starts API on localhost:5000
- ✅ `npm run dev` starts frontend on localhost:5173
- ✅ GitHub Actions CI triggered on PR
- ✅ Basic health check: GET /health → 200 OK

**QA Gate**:
- [ ] Docker compose starts without errors
- [ ] Both frontend and backend start locally
- [ ] CI pipeline runs and reports status
- [ ] PR template in place with checklist

---

### Sprint 1 — Core Entries & Auth (2 Weeks)

**Goal**: Create entries, list/read, same-day edit/delete, basic auth.

**Tasks**:

**Database & Models**:
1. EF Core migrations: users, categories, entries, media tables (see schema above)
2. Create EF Core DbContext + models
3. Add seed data for development (5 users, 20 entries, 5 categories)
4. Add indexes for performance

**Authentication**:
5. Implement password hashing (bcrypt ≥12 salt cost)
6. Create User registration endpoint: POST /api/v1/auth/register
   - Validate email, password strength (12 chars, 3/4 char classes)
   - Hash password, store in DB
   - Return JWT token
7. Create User login endpoint: POST /api/v1/auth/login
   - Validate credentials
   - Return JWT token (expires 24 hours)
8. Add JWT middleware: validate bearer token on all protected endpoints
9. Create session unlock endpoint: POST /api/v1/unlock
   - Accept account password
   - Create unlock_session record
   - Return session token (expires 1 hour)

**Entries API**:
10. Implement: POST /api/v1/entries (create entry)
    - Validate: confidentiality present (required)
    - Validate: title+body OR media present
    - Compute: read_only_after = 23:59:59 on created_at date (user timezone)
    - Store: all fields + metadata
    - Audit: log creation
    - Return: 201 { id, read_only_after, immutable=false }
    - Error: 400 (validation), 403 (unauthorized)

11. Implement: GET /api/v1/entries (list entries)
    - Query params: category_id, tag, type, date_range, page, per_page
    - Return: paginated list with confidentiality status
    - Private entries: only { id, date, locked: true, title_hint } if not unlocked
    - Error: 403 (unauthorized)

12. Implement: GET /api/v1/entries/{id} (read single entry)
    - If confidentiality='private' and not unlocked: 403
    - Return: full entry + media URLs
    - Error: 403, 404

13. Implement: PATCH /api/v1/entries/{id} (edit entry)
    - Validate: immutable=false OR now <= read_only_after
    - Update: title, body_text, category_id, tags, confidentiality
    - Audit: log edit with diff
    - Return: 200 updated entry
    - Error: 403 (immutable), 404

14. Implement: DELETE /api/v1/entries/{id} (delete entry)
    - Validate: immutable=false OR now <= read_only_after
    - Delete: entry from DB, mark media for deletion
    - Audit: log deletion
    - Return: 204 No Content
    - Error: 403 (immutable), 404

**Categories API**:
15. Implement: POST /api/v1/categories (create category)
    - Validate: name, optional color
    - Store: user_id, name, color
    - Return: 201 { id, name, color }

16. Implement: GET /api/v1/categories (list user's categories)
    - Return: array of { id, name, color, entry_count }

**Testing**:
17. Unit tests (≥90% coverage for Auth + Entries modules)
    - Test: password hashing, JWT validation
    - Test: entry creation + validation rules
    - Test: same-day edit/delete enforcement
    - Test: read_only_after computation
    - Test: immutable transition
18. Integration tests
    - Test: create entry → read → edit → delete flow
    - Test: unlock session → read private entries
    - Test: confidentiality enforcement

**Frontend**:
19. Create basic layout: header (user profile, logout), sidebar (navigation)
20. Create Entry form component: title, body, category dropdown, tags, confidentiality toggle
21. Create Entry list component: timeline view, filters
22. Create Auth pages: login, register
23. Add Axios interceptor for JWT token + error handling

**Deliverables**:
- ✅ User registration + login working
- ✅ Create, read, list, edit, delete entries working
- ✅ Same-day edit/delete enforcement tested
- ✅ Confidentiality validation working
- ✅ Unit tests ≥90% coverage
- ✅ API documented in OpenAPI
- ✅ Frontend auth flow complete

**QA Gate**:
- [ ] Coverage report ≥90% for core modules
- [ ] All error codes 400/403/404/500 documented
- [ ] Edit/delete boundary tested at 23:59:59 cutoff
- [ ] Unlock session timeout tested (1 hour)
- [ ] OpenAPI spec complete

---

### Sprint 2 — Media & Export (2 Weeks)

**Goal**: Media uploads, thumbnails, export (sync + async).

**Tasks**:

**Media Upload**:
1. Implement: POST /api/v1/media/upload (signed URL request)
   - Validate: file size ≤100 MB, MIME type allowed
   - Generate: signed S3 URL (valid 15 min)
   - Return: { upload_url, media_id }
   - Error: 400 (validation)

2. Implement: POST /api/v1/media/complete (mark upload complete)
   - After client uploads to S3
   - Store: Media table record with storage_key
   - Trigger: thumbnail generation (background job)
   - Return: { id, url, thumbnail_url }

3. Thumbnail generation worker (Hangfire background job)
   - Input: media_id, storage_key, MIME type
   - Download: media from S3
   - Generate: 300×300 JPEG (quality 80%)
   - Upload: to S3 with thumbnail_ prefix
   - Store: thumbnail_url in Media table

**Export**:
4. Implement: POST /api/v1/exports (create export job)
   - Query: entries to export (entry_ids or date_range)
   - Query: format (json|md|zip), mode (sync|async)
   - If mode=sync & size <10MB: return immediate download
   - If mode=async or size >10MB: enqueue job, return job_id
   - Return: { job_id, download_url (if ready) }

5. Implement: GET /api/v1/exports/{id} (check export status)
   - Return: { id, status, download_url (if completed), expires_at }

6. Export job worker (Hangfire background job)
   - Input: export_id, entry_ids, format
   - Fetch: entries + media
   - Generate: JSON/Markdown/ZIP
   - Store: to S3 with signed URL (24-hour expiry)
   - Update: export_jobs table with download_url, status='completed'
   - Notify: send email to user with download link
   - Error: mark status='failed', notify user

7. Import validation
   - Implement: POST /api/v1/imports (import entries)
   - Validate: confidentiality present for each entry
   - Validate: title+body or media present
   - Validate: created_at not in future (>5 min)
   - Store: all entries with source='import'
   - Audit: log bulk import
   - Return: 201 { imported_count, failed_count, errors }

**Testing**:
8. Unit tests for media validation (size, MIME, dimensions)
9. Integration tests for export job (create → queue → complete)
10. E2E test: upload media → export → download

**Frontend**:
11. Create media upload component (drag-drop, file input)
12. Create export UI: select entries → choose format + mode → download/email
13. Create import UI: upload file → preview → import

**Deliverables**:
- ✅ Media upload working with S3-compatible storage
- ✅ Thumbnail generation working (300×300)
- ✅ Export (both sync + async) working
- ✅ Import validation working
- ✅ Export job email notification working
- ✅ Tests covering export/import flows

**QA Gate**:
- [ ] Media upload validates size/MIME correctly
- [ ] Thumbnails generated and accessible
- [ ] Export job SLA: <15 min for typical
- [ ] Import rejects entries missing confidentiality
- [ ] Email notifications sent on export complete

---

### Sprint 3 — Sentiment & Privacy (2 Weeks)

**Goal**: Client-side sentiment, privacy audit automation.

**Tasks**:

**Sentiment Analysis**:
1. Frontend: import sentiment.js library
2. On entry create: compute sentiment client-side
   - Input: body_text
   - Output: score (-1..1), label (positive|neutral|negative)
   - Model version: 'sentiment.js/v1.0'
3. Store sentiment with entry on POST /api/v1/entries
   - Include: sentiment_score, sentiment_label, sentiment_model
4. Display sentiment badge on entries: color-coded by label
5. Aggregate: show overall trend (average sentiment, chart)

**Privacy Audit**:
6. Implement: `no_training_use` flag enforcement
   - Check: before any external API call or data pipeline
   - Enforce: in code with comments + tests
7. Create daily audit job (cron: Sunday 02:00 UTC)
   - Scan: all users with no_training_use=true
   - Verify: no data in external services (API calls, logs, models)
   - Log: audit results to dedicated table
   - Alert: if violations found, create issue + notify team
8. Implement: GDPR export endpoint
   - GET /api/v1/export/gdpr (creates export with all data)
   - Return: complete user data + metadata + audit logs
   - Email: to user within 48 hours

**Testing**:
9. Unit tests: sentiment computation, score validation
10. Integration tests: sentiment stored with entry
11. Privacy audit tests: verify no_training_use checked

**Frontend**:
12. Display sentiment badge on entries
13. Show sentiment trend chart on dashboard

**Deliverables**:
- ✅ Sentiment scores computed client-side
- ✅ Sentiment displayed on entries + trend chart
- ✅ Privacy audit automation running (weekly)
- ✅ GDPR export working
- ✅ Tests covering privacy flows

**QA Gate**:
- [ ] Sentiment never leaves client (no API calls)
- [ ] Privacy audit logs created
- [ ] GDPR export complete within 48 hours
- [ ] No external data exposure detected

---

### Sprint 4 — Mindmap & UX Polish (2 Weeks)

**Goal**: Mindmap generation, UX refinement, accessibility.

**Tasks**:

**Mindmap Service**:
1. Implement: GET /api/v1/mindmap (generate graph)
   - Query params: scope (entry|category|user), id, threshold (0.1–0.95, default 0.6)
   - Algorithm: TF-IDF on body_text → cosine similarity
   - Nodes: entries + category node
   - Edges: if similarity > threshold
   - Max nodes: 100 (soft limit); if >100 → return 400, suggest async
2. Async mindmap job (if >100 nodes)
   - Enqueue: background job
   - Return: 202 (accepted), check via polling endpoint
3. Mindmap export: PNG/SVG/JSON
   - Use: headless browser (Puppeteer) for PNG/SVG
   - Return: signed S3 URL

**Frontend UX**:
4. Create Mindmap component (cytoscape.js)
   - Render graph with nodes + edges
   - Interactive: pan, zoom, expand/collapse
   - Style: per-category colors
5. Accessibility audit + fixes
   - WCAG 2.1 AA compliance check
   - Fix: color contrast, keyboard navigation, ARIA labels
   - Test: screen reader compatibility
6. Mobile responsiveness
   - Test: iOS Safari, Android Chrome
   - Fix: responsive layout, touch interactions

**UX Polish**:
7. Add: loading states, error messages, success notifications
8. Add: empty state guidance ("Create your first entry")
9. Add: onboarding tour (optional, first-time users)

**Testing**:
10. E2E tests: create entry → generate mindmap → view → export
11. Accessibility tests (axe, WAVE)
12. Mobile responsiveness tests

**Deliverables**:
- ✅ Mindmap generation working (<100 nodes sync, >100 async)
- ✅ Mindmap UI interactive (pan, zoom)
- ✅ Mindmap export (PNG/SVG)
- ✅ WCAG 2.1 AA compliance verified
- ✅ Mobile responsive

**QA Gate**:
- [ ] Mindmap <100 nodes returns within 2 sec
- [ ] Accessibility scan: zero critical violations
- [ ] Mobile tests pass (iOS + Android)

---

### Sprint 5 — Testing, Performance & Release (2 Weeks)

**Goal**: Full test coverage, performance optimization, release readiness.

**Tasks**:

**Testing**:
1. Integration tests: all API endpoints
2. E2E tests (Playwright): critical user flows
   - Create entry → edit → delete
   - Unlock private entry → view
   - Export → download
   - Import → verify
3. Contract tests: API schema validation
4. Security tests: OWASP Top 10 scan

**Performance**:
5. Database query optimization
   - Analyze slow queries: EXPLAIN ANALYZE
   - Optimize indexes (already in Sprint 1)
   - Test: GET list with 10k entries → P95 <300ms
   - Test: GET single entry → P95 <150ms
6. Frontend bundle optimization
   - Measure: initial load size (<150 KB gzipped)
   - Code split: lazy load routes
   - Compress: images, assets
7. API response time optimization
   - Pagination: enforce max per_page=100
   - Caching: add ETag headers for GET requests
   - Compression: gzip responses

**Deployment**:
8. Create: Kubernetes manifests (or App Service configs)
   - API deployment
   - Database backup strategy
   - Secrets management
   - Health checks + rolling updates
9. Create: runbooks for common operations
   - Database migration procedure
   - Secret rotation
   - Incident response
10. Set up: monitoring + alerting
    - Key metrics: API latency, error rates, export job failures
    - Dashboards: health, performance, privacy audit
    - Alerts: email/Slack on threshold breaches

**Deliverables**:
- ✅ Coverage report: ≥80% project, ≥90% core
- ✅ Performance benchmarks: documented & pass thresholds
- ✅ Security scan: vulnerabilities documented + remediated
- ✅ Kubernetes manifests or App Service configs
- ✅ Monitoring dashboards live
- ✅ Release notes ready
- ✅ Runbooks documented

**QA Gate**:
- [ ] Coverage ≥80% (or exceptions documented)
- [ ] Performance benchmarks met
- [ ] Security scan zero critical vulnerabilities
- [ ] Deployment procedure tested (dry run)
- [ ] Runbooks reviewed by ops

---

## Part 4: Development Environment

### Local Setup (Developer)

**Prerequisites**:
- Docker + Docker Compose
- .NET 8 SDK
- Node 18+ + npm/pnpm
- Git

**First-time setup**:
```bash
# Clone repo
git clone https://github.com/yourorg/journal-ai.git
cd journal-ai

# Copy environment template
cp .env.example .env

# Start services
docker compose up -d

# Backend setup
cd src/backend
dotnet restore
dotnet ef database update
dotnet run

# Frontend setup (new terminal)
cd src/frontend
pnpm install
pnpm run dev

# Access:
# Frontend: http://localhost:5173
# API: http://localhost:5000
# API Docs: http://localhost:5000/swagger
# MinIO: http://localhost:9000 (admin: minioadmin/minioadmin)
```

**docker-compose.yml structure**:
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: journalai
      POSTGRES_PASSWORD: dev-password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  minio:
    image: minio/minio
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    ports:
      - "9000:9000"
      - "9001:9001"
    command: server /data --console-address ":9001"

  redis:
    image: redis:7
    ports:
      - "6379:6379"

  api:
    build: ./src/backend
    ports:
      - "5000:5000"
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;User Id=postgres;Password=dev-password;Database=journalai"
      Blob__Endpoint: "http://minio:9000"
      Blob__AccessKey: minioadmin
      Blob__SecretKey: minioadmin
    depends_on:
      - postgres
      - minio

  frontend:
    build: ./src/frontend
    ports:
      - "5173:5173"
    volumes:
      - ./src/frontend/src:/app/src
    depends_on:
      - api

volumes:
  postgres_data:
```

---

## Part 5: CI/CD Pipeline

### GitHub Actions Workflow

**File**: `.github/workflows/ci.yml`

```yaml
name: CI

on: [push, pull_request]

jobs:
  lint-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      # Backend
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore src/backend/JournalAI.csproj
      
      - name: Format check
        run: dotnet format --verify-no-changes src/backend/JournalAI.csproj
      
      - name: Build
        run: dotnet build src/backend/JournalAI.csproj
      
      - name: Unit tests
        run: dotnet test src/backend/JournalAI.Tests.csproj --collect:"XPlat Code Coverage"
      
      - name: Upload coverage
        uses: codecov/codecov-action@v3
      
      # Frontend
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: '18'
          cache: 'pnpm'
      
      - name: Install dependencies
        run: cd src/frontend && pnpm install
      
      - name: Lint
        run: cd src/frontend && pnpm run lint
      
      - name: Type check
        run: cd src/frontend && pnpm run typecheck
      
      - name: Build
        run: cd src/frontend && pnpm run build
      
      - name: Unit tests
        run: cd src/frontend && pnpm run test

  integration-tests:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        env:
          POSTGRES_PASSWORD: test-password
    
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Run integration tests
        run: dotnet test src/backend/JournalAI.IntegrationTests.csproj
        env:
          ConnectionStrings__TestDatabase: "Host=localhost;User Id=postgres;Password=test-password;Database=journalai_test"

  performance-tests:
    runs-on: ubuntu-latest
    if: contains(github.event.pull_request.labels.*.name, 'perf-review')
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Run perf tests
        run: dotnet test src/backend/JournalAI.PerfTests.csproj
```

**Triggers**:
- ✅ Lint + typecheck on every push/PR
- ✅ Unit tests on every push/PR
- ✅ Integration tests on PR to main
- ✅ Performance tests on PR with label `perf-review`

---

## Part 6: Security & Deployment

### Security Checklist

- [ ] TLS 1.2+ for all traffic (enforce HTTPS)
- [ ] Secrets in KeyVault / Vault, not in code
- [ ] Bcrypt ≥12 for password hashing
- [ ] CORS configured (frontend only)
- [ ] CSRF token for state-changing requests
- [ ] SQL injection prevention (parameterized queries via EF Core)
- [ ] Rate limiting: 100 req/min per user, 10 req/sec per IP
- [ ] Dependency scanning (weekly)
- [ ] Security audit (quarterly)

### Production Deployment

**Option A: Kubernetes**
```yaml
# deployment.yaml for API
apiVersion: apps/v1
kind: Deployment
metadata:
  name: journal-ai-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: journal-ai-api
  template:
    metadata:
      labels:
        app: journal-ai-api
    spec:
      containers:
      - name: api
        image: yourregistry.azurecr.io/journal-ai:latest
        ports:
        - containerPort: 5000
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
```

**Option B: Azure App Service**
```bash
# Deploy via GitHub Actions to App Service
# .github/workflows/deploy.yml
- name: Deploy to Azure
  uses: azure/webapps-deploy@v2
  with:
    app-name: journal-ai-api
    publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
    package: ./publish
```

---

## Part 7: Monitoring & Observability

### Key Metrics

| Metric | Target | Alert Threshold |
|--------|--------|-----------------|
| API latency (P95) | <300 ms | >500 ms |
| Error rate | <0.5% | >1% |
| Export job success | >99% | <95% |
| Database response | <50 ms | >100 ms |
| Privacy audit pass | 100% | <99% |

### Dashboards

- **Health**: uptime, latency, error rates
- **Performance**: P50/P95/P99 latencies, throughput
- **Privacy**: audit results, no_training_use enforcement
- **Resources**: CPU, memory, DB connections

---

## Success Criteria

✅ **Sprint 0**: Local dev environment working  
✅ **Sprint 1**: CRUD operations + auth working  
✅ **Sprint 2**: Media + export working  
✅ **Sprint 3**: Sentiment + privacy audit working  
✅ **Sprint 4**: Mindmap + UX polish working  
✅ **Sprint 5**: Tests pass, performance meets SLA, ready to release

---

**Status**: 🚀 Ready for Sprint 0 kickoff.

**Next**: Run `/speckit.implement` to begin backend scaffolding.
