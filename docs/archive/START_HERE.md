# START_HERE.md — Project Overview & Navigation

**Journal AI** — Privacy-First Personal Journaling Platform  
**Status**: 🚀 Sprint 0 Complete, Sprint 1 Ready to Start  
**Last Updated**: 2026-02-13

---

## 🎯 Quick Links

### For Team Members
- 👨‍💼 **[PHASED_IMPLEMENTATION_PLAN.md](./PHASED_IMPLEMENTATION_PLAN.md)** — 6-sprint overview, timeline, team assignment
- 📖 **[docs/SPRINT_1_IMPLEMENTATION_GUIDE.md](./docs/SPRINT_1_IMPLEMENTATION_GUIDE.md)** — Detailed Sprint 1 tasks with code examples (2,500+ lines)
- 🔧 **[CONTRIBUTING.md](./CONTRIBUTING.md)** — Dev setup, commit conventions, PR process, constitution checklist

### For Project Leads
- 📊 **[IMPLEMENTATION_STATUS.md](./IMPLEMENTATION_STATUS.md)** — Phase status, deliverables, success metrics
- 🏗️ **[README.md](./README.md)** — Project overview, architecture, features, getting started

### For Specification & Architecture
- 📋 **[.specify/specs/001-journal-ai/specs.md](./.specify/specs/001-journal-ai/specs.md)** — MVP requirements (335 lines)
- 🗄️ **[.specify/specs/001-journal-ai/data-model.md](./.specify/specs/001-journal-ai/data-model.md)** — Database schema (119 lines)
- 🏛️ **[.specify/memory/constitution.md](./.specify/memory/constitution.md)** — Engineering principles & governance
- 🛣️ **[.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md](./.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md)** — Detailed 6-sprint roadmap (1,200+ lines)

---

## 🚀 Getting Started

### For New Developers

1. **Clone & Setup** (5 minutes)
   ```bash
   git clone <repo>
   cd journal-ai
   docker compose up -d
   ```
   - Frontend: http://localhost:5173
   - Backend API: http://localhost:5000
   - MinIO Console: http://localhost:9001

2. **Read Documentation** (30 minutes)
   - This file (START_HERE.md) → Overview
   - PHASED_IMPLEMENTATION_PLAN.md → Timeline & phases
   - CONTRIBUTING.md → Dev guidelines
   - docs/SPRINT_1_IMPLEMENTATION_GUIDE.md → Your sprint details

3. **Check out Specification** (1 hour)
   - .specify/specs/001-journal-ai/specs.md → What we're building
   - .specify/specs/001-journal-ai/data-model.md → Database schema
   - .specify/memory/constitution.md → Engineering principles

4. **Start Sprint 1** (Week 1)
   - Follow docs/SPRINT_1_IMPLEMENTATION_GUIDE.md
   - Begin with Task T101 (EF Core DbContext)
   - Commit with conventional messages
   - Create PR when ready

### For Code Reviewers

- Review against CONTRIBUTING.md checklist
- Verify constitution compliance (CONTRIBUTING.md §Constitution Compliance Checklist)
- Check tests pass, coverage ≥80%
- Ensure OpenAPI spec updated
- Request 2+ approvals before merge

### For Project Managers

- Track progress in PHASED_IMPLEMENTATION_PLAN.md
- Monitor sprint velocity (target: 2 weeks per sprint)
- Escalate blockers early
- Run sprint ceremonies (standup, review, retro)

---

## 📅 Timeline

```
Week 1       : Sprint 0 ✅ (Infrastructure)        COMPLETE
Weeks 2-3    : Sprint 1 🚀 (Core Entries)          NEXT (10 business days)
Weeks 4-5    : Sprint 2a/2b (Auth + Media)         Depends on Sprint 1
Weeks 6-7    : Sprint 3a/3b (Export + Sentiment)   Depends on Sprint 2
Weeks 8-9    : Sprint 4 (Mind-maps)                Depends on Sprint 3
Weeks 10-12  : Sprint 5 (Testing & Release)        Depends on Sprint 4
```

**Total**: 12 weeks, 6 sprints, 2-3 developers

---

## 📦 What's Included

### Phase 1: Sprint 0 ✅ (Complete)
- Docker Compose with all services (postgres, minio, redis, api, frontend)
- GitHub Actions CI pipeline (lint, test, build)
- CONTRIBUTING.md with dev guidelines
- Comprehensive README.md
- Optimized .gitignore

### Phase 2: Sprint 1 🚀 (Ready)
- 11 detailed tasks with code examples
- 2,500+ line implementation guide
- Unit test templates (15 test cases)
- Integration test templates (8 test cases)
- React component examples (Timeline, Entry Form)
- Success criteria checklist

### Phases 3-6: Sprints 2-5 (Planned)
- Detailed task breakdown in PHASED_IMPLEMENTATION_PLAN.md
- Architecture diagrams
- Dependency maps
- Risk mitigation strategies

---

## 🎯 Success Criteria

### Sprint 0 ✅ (Complete)
- [X] Docker Compose starts all services
- [X] GitHub Actions CI passes
- [X] Developer can run in 5 minutes
- [X] All documentation complete

### Sprint 1 (Next 2 weeks)
- [ ] EF Core models and migrations
- [ ] Entry CRUD API endpoints
- [ ] Same-day edit/delete enforcement working
- [ ] Unit tests ≥90% coverage
- [ ] Integration tests passing
- [ ] Frontend Timeline component
- [ ] Frontend Entry Form component
- [ ] Performance P95: list <300ms, read <150ms

### Final (Sprint 5)
- [ ] All 12 MVP features working
- [ ] Test coverage ≥80%, core modules ≥90%
- [ ] Performance & scalability verified
- [ ] Security audit passed
- [ ] Accessibility (WCAG 2.1 AA) passed
- [ ] Production deployment ready

---

## 🏗️ Architecture

```
┌─────────────────────────────────────┐
│    React Frontend (Vite)            │
│  - Timeline, Entry Form             │
│  - sentiment.js (client-side)       │
│  - cytoscape.js (mindmaps)          │
└──────────────────┬──────────────────┘
         ↓ HTTPS/JSON
┌──────────────────▼──────────────────┐
│  ASP.NET Core 8 REST API            │
│  - JWT Authentication               │
│  - Entry CRUD                       │
│  - Export/Import                    │
│  - Mindmap generation               │
└──────────┬──────────┬──────────┬─────┘
         ↓          ↓          ↓
    PostgreSQL  MinIO S3    Redis
    (Database) (Blob Store) (Queue)
```

**Tech Stack**:
- **Backend**: ASP.NET Core 8 + EF Core + PostgreSQL
- **Frontend**: React 18 + Vite + TypeScript
- **Storage**: S3-compatible (MinIO dev, AWS S3 prod)
- **Auth**: JWT bearer tokens + session-based unlock
- **Async**: Hangfire background workers
- **Sentiment**: sentiment.js (client-side, privacy-preserving)
- **Mindmaps**: TF-IDF similarity + cytoscape.js
- **Containers**: Docker + Docker Compose
- **CI/CD**: GitHub Actions

---

## 📝 Key Files

| File | Purpose | Lines | Audience |
|------|---------|-------|----------|
| [PHASED_IMPLEMENTATION_PLAN.md](./PHASED_IMPLEMENTATION_PLAN.md) | 6-sprint overview, timeline | 800 | Team leads, developers |
| [IMPLEMENTATION_STATUS.md](./IMPLEMENTATION_STATUS.md) | Phase status, deliverables | 400 | Project leads, stakeholders |
| [README.md](./README.md) | Project overview, quick start | 300 | Everyone |
| [CONTRIBUTING.md](./CONTRIBUTING.md) | Dev guidelines, PR process | 250 | Developers |
| [docs/SPRINT_1_IMPLEMENTATION_GUIDE.md](./docs/SPRINT_1_IMPLEMENTATION_GUIDE.md) | Sprint 1 details, code examples | 2,500 | Developers (Week 2-3) |
| [specs.md](./.specify/specs/001-journal-ai/specs.md) | MVP requirements | 335 | Architects, developers |
| [data-model.md](./.specify/specs/001-journal-ai/data-model.md) | Database schema | 119 | Database architects |
| [constitution.md](./.specify/memory/constitution.md) | Engineering principles | 500 | Everyone (reference) |
| [TECHNICAL_PLAN_V2.md](./.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md) | Detailed 6-sprint roadmap | 1,200 | Architects, leads |

**Total Documentation**: 6,000+ lines of specifications, guides, and examples

---

## 🔄 Development Process

### Before Starting Work
1. Read CONTRIBUTING.md
2. Pull latest `main` branch
3. Create feature branch: `git checkout -b feat/your-feature`
4. Follow conventional commits (e.g., `feat(entries): add immutability check`)

### During Development
1. Write tests first (TDD)
2. Implement feature
3. Run tests: `dotnet test` (backend), `npm test` (frontend)
4. Format code: `dotnet format` (backend), `npm lint --fix` (frontend)
5. Commit frequently with clear messages

### Before Creating PR
1. Pull latest `main`, merge locally
2. Run full test suite
3. Check no compiler warnings
4. Verify OpenAPI spec updated (if API change)
5. Check coverage ≥80% (new code ≥90%)

### PR Process
1. Create PR with template
2. Link related issue
3. Add screenshots (UI changes)
4. Include constitution checklist (from CONTRIBUTING.md)
5. Request 2+ reviews
6. Address feedback
7. Merge once approved & CI passes

### After Merge
1. Monitor CI/CD pipeline
2. Verify deployment (if auto-deploy enabled)
3. Test on staging/production

---

## 🎓 Learning Resources

### For Specification & Design
- [specs.md](./.specify/specs/001-journal-ai/specs.md) — Features, UX flows, API surface
- [data-model.md](./.specify/specs/001-journal-ai/data-model.md) — Database design
- [TECHNICAL_PLAN_V2.md](./.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md) — Architecture deep-dive

### For Development
- [CONTRIBUTING.md](./CONTRIBUTING.md) — Commit conventions, PR process
- [docs/SPRINT_1_IMPLEMENTATION_GUIDE.md](./docs/SPRINT_1_IMPLEMENTATION_GUIDE.md) — Code examples
- [constitution.md](./.specify/memory/constitution.md) — Engineering principles

### For Operations
- [README.md](./README.md) — Quick start
- Future: docs/DEPLOYMENT.md, docs/ADMIN_GUIDE.md (Sprint 5)

---

## ❓ FAQ

### Q: How do I run the project locally?
**A**: 
```bash
git clone <repo> && cd journal-ai
docker compose up
# Frontend: http://localhost:5173
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

### Q: What if I don't know how to start?
**A**: Read in this order:
1. This file (START_HERE.md) — 5 min
2. PHASED_IMPLEMENTATION_PLAN.md — 20 min
3. CONTRIBUTING.md — 10 min
4. docs/SPRINT_1_IMPLEMENTATION_GUIDE.md — 60 min (your sprint)

### Q: What's the commit message format?
**A**: Conventional commits (see CONTRIBUTING.md):
```
type(scope): subject

body

Closes #123
```
Example:
```
feat(entries): add same-day edit/delete enforcement

- Compute read_only_after based on user timezone
- Return 403 ENTRY_IMMUTABLE if past boundary
- Add unit tests for edge cases

Closes #42
```

### Q: What tests do I need to write?
**A**: 
- Unit tests for business logic (≥90% coverage)
- Integration tests for API endpoints (≥80%)
- E2E tests for critical user flows (Sprint 5)

### Q: How do I ensure constitution compliance?
**A**: Use the checklist in CONTRIBUTING.md before creating PR. Covers:
- Privacy & data protection
- Test-first development
- API contracts
- Security
- Accessibility

### Q: What if I'm blocked?
**A**: 
1. Check docs for answers
2. Ask in GitHub Issues
3. Post in GitHub Discussions
4. Schedule team sync
5. Escalate to tech lead

### Q: When do I deploy?
**A**: 
- Sprint 0-4: Never (features only)
- Sprint 5: Deployment pipeline ready, manual approval required
- Post-launch: GitHub Actions auto-deploys on merge to `main`

---

## 📞 Support

**Documentation**: See links above (START_HERE.md has everything)

**Questions?**
- GitHub Issues (bug reports)
- GitHub Discussions (questions)
- Daily standup (blockers)
- Tech lead sync (architecture questions)

**Found a bug?**
1. Reproduce it
2. Create GitHub Issue with:
   - Description
   - Steps to reproduce
   - Expected behavior
   - Actual behavior
   - Browser/OS (if frontend)
3. Link to related code
4. Assign to relevant person

---

## 🎉 Ready to Begin?

✅ Sprint 0 is complete — all infrastructure ready  
🚀 Sprint 1 is detailed — 2,500+ lines of guidance  
📖 Documentation is comprehensive — 6,000+ lines  
🔧 Development process is clear — CONTRIBUTING.md explains  

**Next Step**: Read [PHASED_IMPLEMENTATION_PLAN.md](./PHASED_IMPLEMENTATION_PLAN.md) for a 10-minute overview, then jump to [docs/SPRINT_1_IMPLEMENTATION_GUIDE.md](./docs/SPRINT_1_IMPLEMENTATION_GUIDE.md) when ready to code.

---

**Questions? Start here:**
1. This file (START_HERE.md)
2. PHASED_IMPLEMENTATION_PLAN.md
3. docs/SPRINT_1_IMPLEMENTATION_GUIDE.md
4. CONTRIBUTING.md
5. .specify/specs/001-journal-ai/specs.md

**Happy coding! 🚀**

---

*Last Updated: 2026-02-13*  
*Status: 🚀 Sprint 0 Complete, Sprint 1 Ready*  
*Next Phase: Infrastructure foundation complete, begin core features*
