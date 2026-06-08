# Journal AI — Phased Implementation Plan

**Project**: Journal AI — Privacy-First Personal Journaling Platform  
**Date**: 2026-02-13  
**Status**: ✅ Sprint 0 Complete, 🚀 Sprint 1 Ready  
**Total Duration**: 12 weeks (6 sprints, 2 weeks each)  
**Team Size**: 2-3 developers

---

## Overview

Journal AI is being built in 6 phases (sprints) following a strict **test-first development** approach with **constitution-driven engineering**. Each sprint builds on the previous one with clear success criteria and quality gates.

```
PHASE 1       PHASE 2a      PHASE 3a      PHASE 4         PHASE 5
Sprint 0      Sprint 2a     Sprint 3a     Sprint 4        Sprint 5
Week 1        Weeks 4-5     Weeks 6-7     Weeks 8-9       Weeks 10-12
INFRA         AUTH          EXPORT        MINDMAPS        TESTS/DEPLOY
✅DONE        [BLOCKED      [BLOCKED      [BLOCKED        [BLOCKED
              BY Sprint1]   BY Sprint2b]  BY Sprint3]     BY ALL]
                  ↓             ↓            ↓              ↓
              ╔═══════════════════════════════════════════════════╗
              ║           CRITICAL PATH: Sprint 1                 ║
              ║     (Core Entries & Database Foundation)         ║
              ║         Weeks 2-3 (10 business days)             ║
              ║      All other sprints depend on this!           ║
              ╚═══════════════════════════════════════════════════╝
                  ↓             ↓            ↓              ↓
              PHASE 2b      PHASE 3b      
              Sprint 2b     Sprint 3b     
              Weeks 4-5     Weeks 6-7     
              MEDIA         SENTIMENT     
              [BLOCKED      [BLOCKED      
              BY Sprint1]   BY Sprint2b]
```

---

## Detailed Timeline

### Phase 1: Sprint 0 — Infrastructure & Setup (Week 1) ✅ COMPLETE

**Goal**: Create project foundation with CI/CD, Docker, and developer onboarding.

**Deliverables** (8/8 completed):
1. ✅ Git repository structure with CONTRIBUTING.md
2. ✅ docker-compose.yml with all services (postgres, minio, redis)
3. ✅ Backend Dockerfile (multi-stage ASP.NET Core 8)
4. ✅ Frontend Dockerfile (Node 18 + Vite)
5. ✅ .env.example configuration template
6. ✅ GitHub Actions CI pipeline (lint, test, build)
7. ✅ Comprehensive README.md
8. ✅ Enhanced .gitignore with all patterns

**Outputs**:
- Docker Compose starts all services: `docker compose up`
- CI/CD pipeline runs on every commit
- Developer can clone + run in 5 minutes
- All documentation in place

**Status**: 🎉 COMPLETE, PRODUCTION READY

---

### Phase 2: Sprint 1 — Core Entries & Database (Weeks 2-3) 🚀 NEXT

**Goal**: Implement complete entry CRUD API with immutability business logic and comprehensive tests.

**User Story**: Create, read, edit, delete journal entries with same-day edit/delete enforcement

**Tasks** (11 total):

| # | Task | Duration | Dependencies | Files |
|---|------|----------|--------------|-------|
| T101 | EF Core DbContext | 2 days | Sprint 0 | Data/AppDbContext.cs |
| T102 | Entry model class | 1 day | T101 | Models/Entry.cs |
| T103 | Other models (User, Category, Media, etc.) | 2 days | T101 | Models/*.cs |
| T104 | EF Core initial migration | 1 day | T102-T103 | Migrations/ |
| T105 | Entry DTOs | 1 day | T102 | Data/Dtos/EntryDtos.cs |
| T106 | EntryService business logic | 2 days | T102-T105 | Services/EntryService.cs |
| T107 | Entries REST controller | 2 days | T105-T106 | Controllers/EntriesController.cs |
| T108 | Unit tests (EntryService) | 2 days | T106 | Tests/EntryServiceTests.cs |
| T109 | Integration tests (API) | 2 days | T107 | Tests/EntriesControllerTests.cs |
| T110 | Frontend Timeline component | 1 day | T107 | components/Timeline.tsx |
| T111 | Frontend Entry Form | 1 day | T110 | components/EntryForm.tsx |

**Key Implementation**:
- Timezone-aware immutability: entries read-only after 23:59:59 on created_at date in user's timezone
- CalculateReadOnlyAfter() function handling DST, edge cases
- Error responses with codes (ENTRY_IMMUTABLE, VALIDATION_ERROR, etc.)
- ≥90% unit test coverage on business logic
- Full API testing with mocked database

**Documentation**:
- 📖 **docs/SPRINT_1_IMPLEMENTATION_GUIDE.md** (2,500+ lines)
  - Complete code examples for each task
  - Unit test template (15 test cases)
  - Integration test template (8 test cases)
  - React component examples
  - Daily standup template
  - Risk mitigation

**Success Criteria**:
- [ ] All 11 tasks complete
- [ ] Unit test coverage ≥90% on EntryService
- [ ] Integration test coverage ≥80%
- [ ] POST /api/v1/entries returns 201
- [ ] GET /api/v1/entries returns paginated list (DESC by created_at)
- [ ] PATCH /api/v1/entries/{id} returns 403 ENTRY_IMMUTABLE after boundary
- [ ] DELETE /api/v1/entries/{id} returns 403 ENTRY_IMMUTABLE after boundary
- [ ] Frontend Timeline displays entries
- [ ] Frontend Entry Form creates/edits entries
- [ ] No compiler warnings
- [ ] Performance P95: list <300ms, read <150ms

**Effort**: 10 business days (2 developers)

**Status**: 🚀 READY TO START (detailed guide created)

---

### Phase 3a: Sprint 2a — Authentication & Unlock (Weeks 4-5)

**Goal**: Implement user registration, JWT authentication, and session-based unlock for private entries.

**User Story**: User registration, login, and confidentiality unlock with account password

**Dependencies**: Sprint 1 (Entry model must exist)

**Key Tasks**:
- User registration endpoint with bcrypt password hashing (≥12 rounds)
- JWT token generation and validation middleware
- Login endpoint returning bearer token
- Private entry unlock with account password
- Session-based unlock (1-hour TTL)
- Password validation (12+ chars, 3/4 character classes)

**Parallel Work**: Can run in parallel with Sprint 2b (Media)

**Files to Create**:
- Controllers/AuthController.cs
- Services/AuthService.cs
- Services/UserService.cs
- Middleware/AuthMiddleware.cs
- Models/UnlockSession.cs
- Dtos/Auth/LoginDto.cs
- Dtos/Auth/RegisterDto.cs
- Tests/AuthControllerTests.cs
- Frontend pages: LoginPage.tsx, RegisterPage.tsx
- Frontend components: UnlockModal.tsx

**Effort**: 10 business days

---

### Phase 3b: Sprint 2b — Media Upload & Storage (Weeks 4-5)

**Goal**: Implement S3-compatible media upload, storage, and thumbnail generation.

**User Story**: Upload and store images/videos with S3-compatible storage and thumbnails

**Dependencies**: Sprint 1 (Entry model must exist)

**Key Tasks**:
- S3-compatible service integration (MinIO dev, AWS S3 prod)
- Signed URL generation for client uploads
- Media completion endpoint (verify file in S3)
- Thumbnail generation async job (Hangfire)
- Media association with entries
- File size validation (max 100 MB)
- MIME type whitelist

**Parallel Work**: Runs in parallel with Sprint 2a (Auth)

**Files to Create**:
- Services/S3Service.cs
- Services/ImageService.cs
- Controllers/MediaController.cs
- Workers/ThumbnailWorker.cs
- Models/Media.cs
- Dtos/Media/RequestUploadDto.cs
- Tests/MediaControllerTests.cs
- Frontend components: MediaUploader.tsx, MediaGallery.tsx

**Effort**: 10 business days

---

### Phase 4a: Sprint 3a — Export & Import (Weeks 6-7)

**Goal**: Implement async export jobs and bulk import with confidentiality validation.

**User Story**: Export journal as JSON/Markdown/ZIP and import entries with confidentiality metadata

**Dependencies**: Sprint 1 (Entry), Sprint 2b (Media completion)

**Key Tasks**:
- Async export job (Hangfire) for large exports
- Export formats: JSON, Markdown, ZIP (with media)
- Export status tracking and signed download URLs
- Bulk import endpoint
- Import validation (confidentiality required per entry)
- Email notifications when export ready
- Deduplication on import (optional)

**Parallel Work**: Runs in parallel with Sprint 3b (Sentiment)

**Files to Create**:
- Services/ExportService.cs
- Services/ImportService.cs
- Workers/ExportWorker.cs
- Controllers/ExportController.cs
- Controllers/ImportController.cs
- Models/ExportJob.cs
- Tests/ExportControllerTests.cs
- Tests/ImportControllerTests.cs
- Frontend components: ExportDialog.tsx, ImportDialog.tsx

**Effort**: 10 business days

---

### Phase 4b: Sprint 3b — Sentiment Analysis (Weeks 6-7)

**Goal**: Implement client-side sentiment analysis using sentiment.js (privacy-preserving).

**User Story**: Analyze entry sentiment client-side and display trend visualization

**Dependencies**: Sprint 1 (Entry sentiment fields), Sprint 2a (Authentication for user data)

**Key Decisions Locked** (from clarification):
- ✅ Client-side only (sentiment.js library)
- ✅ No server-side processing
- ✅ No raw text sent to server
- ✅ Score (-1 to 1), label (positive/neutral/negative)
- ✅ Persist sentiment_score, sentiment_label, sentiment_model on Entry

**Key Tasks**:
- sentiment.js installation and configuration
- Sentiment analysis service (client-side)
- Live sentiment display in entry form
- Sentiment analytics endpoint (GET /api/v1/analytics/sentiment)
- Sentiment dashboard with:
  - Pie chart: % positive, neutral, negative
  - Line chart: daily average sentiment trend (30 days)
  - Top 5 most positive/negative entries

**Parallel Work**: Runs in parallel with Sprint 3a (Export)

**Files to Create**:
- Frontend: services/sentimentService.ts
- Frontend: pages/SentimentAnalyticsPage.tsx
- Frontend: components/SentimentIndicator.tsx
- Backend: Controllers/AnalyticsController.cs
- Backend: Services/AnalyticsService.cs
- Tests/SentimentAnalyticsTests.cs

**Effort**: 8 business days (lower complexity than concurrent Sprint 3a)

---

### Phase 5: Sprint 4 — Mind-maps (Weeks 8-9)

**Goal**: Implement TF-IDF-based mind-map generation with interactive visualization.

**User Story**: Generate interactive mind-maps at category/journal/entry level with cytoscape.js visualization

**Dependencies**: Sprint 1 (Entry), Sprint 3b (Sentiment for node coloring)

**Key Tasks**:
- TF-IDF text processing and tokenization
- Cosine similarity computation
- Node/edge graph generation
- cytoscape.js visualization setup
- Interactive node/edge manipulation
- PNG/SVG export (async job)
- User-created edge persistence
- Custom relationship creation UI

**Key Algorithm**:
1. Extract keywords: TF-IDF from body_text + tags
2. Tokenize: lowercase, remove stopwords, lemmatize
3. Similarity: cosine distance between entries
4. Graph: nodes (entries), edges (similarity > threshold)
5. Layout: dagre or concentric clustering
6. Coloring: sentiment scores (positive=green, neutral=gray, negative=red)

**Files to Create**:
- Backend: Services/MindmapService.cs
- Backend: Utils/TfidfUtils.cs
- Backend: Controllers/MindmapController.cs
- Backend: Models/MindmapEdge.cs
- Frontend: components/MindmapViewer.tsx
- Frontend: components/MindmapEditor.tsx
- Frontend: pages/MindmapPage.tsx
- Tests/MindmapServiceTests.cs

**Effort**: 10 business days

---

### Phase 6: Sprint 5 — Testing & Release (Weeks 10-12)

**Goal**: Complete integration/E2E testing, optimize performance, conduct security audit, prepare for production.

**User Story**: Full integration testing, performance optimization, and production readiness

**Dependencies**: All previous sprints (Sprints 1-4)

**Key Tasks**:
- Integration test suite covering all user flows
- E2E tests (Playwright) for critical journeys
- Database query optimization (indexes, query plans)
- Performance benchmarking (latency, memory, throughput)
- Security audit (passwords, JWT, SQL injection, CORS)
- Accessibility audit (WCAG 2.1 AA compliance)
- Rate limiting implementation (100 req/min per user, 10 req/sec per IP)
- Monitoring setup (OpenTelemetry, Prometheus, Grafana)
- Canary rollout plan
- Documentation (admin guide, runbooks, incident response)
- GitHub Actions deployment workflow

**Testing Targets**:
- Integration test coverage ≥90% on critical paths
- E2E test coverage: all 12 MVP features
- Database P95: list <300ms, read <150ms
- Memory: stable after 1 hour continuous use
- No SQL injections, password breaches, CORS bypasses
- All UI elements keyboard accessible
- Color contrast ≥4.5:1

**Files to Create**:
- Integration test suite (50+ tests)
- E2E test suite (Playwright, 15+ tests)
- Performance benchmarking script
- docs/SECURITY_AUDIT.md
- docs/ACCESSIBILITY_AUDIT.md
- docs/DEPLOYMENT.md
- docs/ADMIN_GUIDE.md
- docs/INCIDENT_RESPONSE.md
- .github/workflows/deploy.yml (deployment pipeline)
- Monitoring dashboards (Grafana)

**Effort**: 15 business days (full sprint focus on quality)

---

## Team Assignment Suggestion

### Option 1: 2-Person Team (Recommended)

**Developer 1** (Full-Stack):
- Sprints 1-2: Backend (entries, auth, media)
- Sprint 3-4: Backend (export, mindmap)
- Sprint 5: Testing, deployment

**Developer 2** (Full-Stack):
- Sprint 1: Frontend (timeline, form)
- Sprint 2: Frontend (auth UI, upload)
- Sprint 3: Frontend (export/import, sentiment)
- Sprint 4: Frontend (mindmap)
- Sprint 5: E2E tests, deployment

### Option 2: 3-Person Team

**Developer 1** (Backend):
- Sprints 1-5: All backend tasks
- Sprint 5: Performance optimization

**Developer 2** (Frontend):
- Sprints 1-5: All frontend tasks
- Sprint 5: Accessibility audit

**Developer 3** (QA/DevOps):
- Sprint 0: CI/CD setup ✅
- Sprints 2-4: Testing as features complete
- Sprint 5: Full QA, security audit, deployment

---

## Key Principles & Constraints

### Constitution-Driven Development

Every task must comply with:
1. **Privacy-First**: No user data to external services, client-side sentiment only
2. **Test-First**: Unit tests before code, ≥80% coverage
3. **API Excellence**: OpenAPI specs, error schemas, rate limiting
4. **Security**: TLS, bcrypt ≥12, JWT validation, SQL injection prevention
5. **Accessibility**: WCAG 2.1 AA compliance

### Mandatory Code Review Checklist

Every PR must have:
- [ ] Conventional commit message
- [ ] Unit tests passing (≥90% coverage if new service)
- [ ] Integration tests passing
- [ ] No compiler warnings
- [ ] Code formatted (`dotnet format`, `npm lint`)
- [ ] Constitution compliance verified
- [ ] OpenAPI spec updated (if API change)
- [ ] Error codes documented
- [ ] Performance tested (if critical path)
- [ ] Accessibility tested (if UI change)

### Continuous Quality Gates

**Per Sprint**:
- ✅ All unit tests pass
- ✅ All integration tests pass
- ✅ Overall coverage ≥80%
- ✅ Core modules ≥90% coverage
- ✅ Performance benchmarks met
- ✅ Security checklist complete
- ✅ No unresolved issues from previous sprint

**Per Release** (Post-Sprint 5):
- ✅ Full regression test suite passing
- ✅ E2E tests passing on staging
- ✅ Performance targets met on production-equivalent hardware
- ✅ Security audit passed (external review recommended)
- ✅ Accessibility audit passed (WAVE, Lighthouse, axe)
- ✅ Deployment runbook tested
- ✅ Canary rollout plan ready

---

## File Structure Checklist

By end of Sprint 5, the repository should contain:

```
journal-ai/
├── .github/
│   └── workflows/
│       ├── ci.yml .......................... ✅ Sprint 0
│       └── deploy.yml ..................... 🚀 Sprint 5
├── .specify/
│   ├── memory/
│   │   └── constitution.md ............... ✅ Completed
│   └── specs/001-journal-ai/
│       ├── specs.md ...................... ✅ Completed
│       ├── data-model.md ................. ✅ Completed
│       ├── TECHNICAL_PLAN_V2.md ......... ✅ Completed
│       ├── openapi.yaml .................. 🚀 Sprint 1-5
│       └── checklists/ ................... ✅ Completed
├── docker-compose.yml .................... ✅ Sprint 0
├── CONTRIBUTING.md ....................... ✅ Sprint 0
├── README.md ............................. ✅ Sprint 0
├── IMPLEMENTATION_STATUS.md .............. ✅ Sprint 0
├── src/
│   ├── backend/
│   │   ├── JournalAI.csproj
│   │   ├── Program.cs
│   │   ├── Dockerfile ................... ✅ Sprint 0
│   │   ├── Data/ ......................... 🚀 Sprint 1-4
│   │   ├── Models/ ....................... 🚀 Sprint 1-5
│   │   ├── Controllers/ .................. 🚀 Sprint 1-5
│   │   ├── Services/ ..................... 🚀 Sprint 1-5
│   │   ├── Middleware/ ................... 🚀 Sprint 2-5
│   │   ├── Migrations/ ................... 🚀 Sprint 1-5
│   │   ├── Workers/ ...................... 🚀 Sprint 2-5
│   │   ├── Utils/ ........................ 🚀 Sprint 4
│   │   ├── Tests/ ........................ 🚀 Sprint 1-5
│   │   └── Logging/ ...................... 🚀 Sprint 5
│   └── frontend/
│       ├── vite.config.ts
│       ├── Dockerfile.dev ............... ✅ Sprint 0
│       ├── package.json
│       ├── tsconfig.json
│       └── src/
│           ├── App.tsx
│           ├── main.tsx
│           ├── pages/ .................... 🚀 Sprint 1-5
│           ├── components/ ............... 🚀 Sprint 1-5
│           ├── services/ ................. 🚀 Sprint 2-5
│           ├── hooks/ .................... 🚀 Sprint 1-5
│           ├── styles/ ................... 🚀 Sprint 1-5
│           └── tests/ .................... 🚀 Sprint 5
└── docs/
    ├── SPRINT_1_IMPLEMENTATION_GUIDE.md .. ✅ Sprint 0
    ├── USER_GUIDE.md ..................... 🚀 Sprint 5
    ├── ADMIN_GUIDE.md .................... 🚀 Sprint 5
    ├── DEPLOYMENT.md ..................... 🚀 Sprint 5
    ├── SECURITY_AUDIT.md ................. 🚀 Sprint 5
    ├── ACCESSIBILITY_AUDIT.md ............ 🚀 Sprint 5
    ├── INCIDENT_RESPONSE.md .............. 🚀 Sprint 5
    └── README.md ......................... 🚀 Sprint 1-5
```

Legend: ✅ Complete | 🚀 In progress

---

## Success Criteria - Final Delivery (Sprint 5)

### Functionality ✅
- [ ] All 12 MVP features working
- [ ] Entry CRUD with immutability enforcement
- [ ] User auth (registration, login, JWT)
- [ ] Private entry unlock (session-based, 1-hour TTL)
- [ ] Media upload (S3-compatible, thumbnails)
- [ ] Export/import (JSON, Markdown, ZIP)
- [ ] Sentiment analysis (client-side, visualized)
- [ ] Mind-maps (TF-IDF, interactive, customizable)
- [ ] Categories, tags, search
- [ ] Audit logging (all mutations)
- [ ] Rate limiting (100 req/min user, 10 req/sec IP)
- [ ] GDPR compliance (data export, deletion)

### Quality ✅
- [ ] Test coverage ≥80% overall, ≥90% core
- [ ] All tests passing (unit, integration, E2E)
- [ ] No compiler warnings
- [ ] Code formatted per conventions
- [ ] No security vulnerabilities
- [ ] No accessibility issues (WCAG 2.1 AA)

### Performance ✅
- [ ] List entries: P95 <300ms (100 items)
- [ ] Read entry: P95 <150ms
- [ ] Create entry: <100ms
- [ ] Export 100 entries: <5 seconds
- [ ] Memory stable (no leaks) after 1 hour
- [ ] Database indexes optimized

### Operations ✅
- [ ] Docker Compose dev environment working
- [ ] GitHub Actions CI passing
- [ ] GitHub Actions CD pipeline ready
- [ ] Deployment runbook tested
- [ ] Monitoring dashboards ready
- [ ] Runbooks for common issues created
- [ ] Incident response plan documented

### Documentation ✅
- [ ] README.md with quick start
- [ ] User guide (how to use app)
- [ ] Admin guide (deploy, monitor, troubleshoot)
- [ ] Deployment guide (production setup)
- [ ] API documentation (OpenAPI/Swagger)
- [ ] CONTRIBUTING.md (dev guidelines)
- [ ] Security audit checklist (passed)
- [ ] Accessibility audit checklist (passed)
- [ ] Constitution compliance verified

---

## Risk Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Sprint 1 overruns | Medium | CRITICAL | Break into smaller tasks, daily progress tracking, escalate early |
| Timezone bugs | High | Medium | Comprehensive unit tests for DST, edge cases, code review required |
| Performance regression | Medium | Medium | Benchmark each sprint, monitor in CI, fix before merge |
| Security vulnerability | Low | CRITICAL | Security audit Sprint 5, manual penetration testing, dependency scanning |
| Team turnover | Low | HIGH | Document heavily, pair programming, knowledge transfer sessions |
| Database migration issues | Medium | High | Test migrations in CI, test rollbacks, separate migration PR |
| API contract changes | Low | High | Finalize OpenAPI before implementation, version APIs, backward compatibility |

---

## Post-Launch Roadmap

After Sprint 5 (v1.0), future sprints may include:

**Sprint 6** (Optional):
- Advanced search (Elasticsearch)
- Shared journals/collaboration
- Custom themes & personalization
- Mobile app (React Native)

**Sprint 7** (Optional):
- AI-powered journaling prompts
- Advanced sentiment (server-side option, opt-in)
- Voice-to-text entry
- Calendar heat-map view

**Sprint 8+** (Long-term):
- Real-time sync (WebSockets)
- Offline-first (Synced local DB)
- Browser extension
- CLI tool for power users

---

## Getting Help

**Questions?** See these documents in order:

1. **For specifications**: `.specify/specs/001-journal-ai/specs.md`
2. **For architecture**: `.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md`
3. **For Sprint 1 details**: `docs/SPRINT_1_IMPLEMENTATION_GUIDE.md`
4. **For principles**: `.specify/memory/constitution.md`
5. **For clarifications**: `.specify/specs/001-journal-ai/CLARIFICATION_SESSION_2026_02_13.md`

**Still stuck?**
- Post to GitHub Discussions
- Open an issue with context
- Schedule team sync

---

## Summary

✅ **Sprint 0**: Infrastructure foundation complete  
🚀 **Sprint 1**: Ready to start (detailed guide, code examples, test cases)  
📅 **Sprints 2-5**: Planned, with dependencies tracked  

**Total Effort**: 12 weeks  
**Developers**: 2-3  
**Lines of Code**: ~20,000 (est.)  
**Tests**: 150+ unit + integration + E2E  
**Documentation**: 10,000+ lines (specs, guides, code comments)  

---

**Status**: 🎉 **READY TO BEGIN IMPLEMENTATION**

Next step: Start Sprint 1 with T101 (EF Core DbContext)

**Let's build Journal AI! 🚀**
