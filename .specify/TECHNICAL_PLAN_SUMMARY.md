# Technical Plan Summary

**File**: [TECHNICAL_PLAN_V2.md](TECHNICAL_PLAN_V2.md)  
**Created**: 2026-02-13  
**Status**: ✅ Ready for implementation

---

## Quick Reference

### Tech Stack
- **Backend**: ASP.NET Core 8 + EF Core + PostgreSQL
- **Frontend**: React 18 + Vite + TypeScript
- **Storage**: S3-compatible (MinIO dev, AWS S3 prod)
- **Sentiment**: Client-side (sentiment.js)
- **Observability**: OpenTelemetry + Prometheus/Grafana
- **Container**: Docker + Docker Compose
- **CI/CD**: GitHub Actions

### Architecture
```
React Frontend → ASP.NET Core API → PostgreSQL
                                  ↘ MinIO/S3 (Media)
                                  ↘ Hangfire Worker (Async Jobs)
```

### Sprint Timeline
| Sprint | Goal | Duration |
|--------|------|----------|
| 0 | Setup & scaffolding | 1 week |
| 1 | Core entries & auth | 2 weeks |
| 2 | Media & export | 2 weeks |
| 3 | Sentiment & privacy audit | 2 weeks |
| 4 | Mindmap & UX polish | 2 weeks |
| 5 | Testing, performance, release | 2 weeks |

**Total**: 12 weeks (6 sprints)

### Key Decisions
✅ Single category per entry (1:1 relationship)  
✅ Client-side sentiment only (sentiment.js)  
✅ Edit/delete window: until 23:59:59 in user timezone  
✅ Unlock: account password only (1-hour session)  
✅ Export: user chooses sync vs async  
✅ No per-entry passwords in MVP (deferred to v2)  

### Database Schema (7 Tables)
- **users**: Account + timezone + settings
- **categories**: Flat, no hierarchy
- **entries**: Core journal data (single category, flat tags)
- **media**: Images, videos, thumbnails
- **audit_logs**: All mutations + access
- **export_jobs**: Export tracking
- **unlock_sessions**: Private entry unlocks (1-hour TTL)

### API Endpoints (12 Core)
- `POST /api/v1/auth/register` — Sign up
- `POST /api/v1/auth/login` — Sign in
- `POST /api/v1/unlock` — Unlock private entries
- `POST /api/v1/entries` — Create entry
- `GET /api/v1/entries` — List entries
- `GET /api/v1/entries/{id}` — Read entry
- `PATCH /api/v1/entries/{id}` — Edit entry
- `DELETE /api/v1/entries/{id}` — Delete entry
- `POST /api/v1/exports` — Create export job
- `GET /api/v1/exports/{id}` — Check export status
- `POST /api/v1/imports` — Bulk import entries
- `GET /api/v1/mindmap` — Generate graph

### Error Schema
```json
{
  "code": "ERROR_CODE",
  "message": "User-friendly message",
  "details": { "field": "value" }
}
```

Standard HTTP codes:
- `400` — Validation error
- `403` — Permission denied (immutable, unauthorized)
- `404` — Not found
- `409` — Conflict (duplicate)
- `429` — Rate limit exceeded
- `500` — Server error

### Development Environment
```bash
docker compose up              # Start all services
cd src/backend && dotnet run   # Start API (5000)
cd src/frontend && pnpm dev    # Start frontend (5173)
# Access: http://localhost:5173
```

### Quality Gates
✅ **Unit tests**: ≥80% baseline, ≥90% core modules  
✅ **Integration tests**: All critical flows  
✅ **E2E tests**: User journeys (Playwright)  
✅ **Performance**: P95 <300ms list, <150ms read  
✅ **Security**: TLS, bcrypt, rate-limiting  
✅ **Privacy**: Client-side sentiment, weekly audit  
✅ **Accessibility**: WCAG 2.1 AA compliance  

### Before Sprint 0
- [ ] Review technical plan
- [ ] Confirm tech stack with team
- [ ] Set up Azure/AWS account for production
- [ ] Create git repo structure
- [ ] Create PR template + CONTRIBUTING.md

### Checklist for Each Sprint
- [ ] Unit tests written first (TDD)
- [ ] All error codes documented
- [ ] OpenAPI spec updated
- [ ] Privacy declaration complete (if data handling)
- [ ] Security review done (if auth/crypto changes)
- [ ] Tests pass, coverage ≥80%
- [ ] Code reviewed by 2+ people
- [ ] Performance benchmarks run
- [ ] Accessibility audit passed (UI changes)

---

## Files Created/Updated

| File | Purpose | Status |
|------|---------|--------|
| [specs.md](specs.md) | Specification (with clarifications) | ✅ Complete |
| [TECHNICAL_PLAN_V2.md](TECHNICAL_PLAN_V2.md) | Detailed technical plan | ✅ Complete |
| [constitution.md](../.specify/memory/constitution.md) | Engineering principles | ✅ Complete (v1.1.0) |
| [CLARIFICATION_SESSION_2026_02_13.md](CLARIFICATION_SESSION_2026_02_13.md) | Clarifications summary | ✅ Complete |
| [CONSTITUTION_IMPLEMENTATION_MAP.md](CONSTITUTION_IMPLEMENTATION_MAP.md) | Implementation guidance | ✅ Complete |
| [requirements-quality.md](checklists/requirements-quality.md) | QA checklist (120 points) | ✅ Complete |

---

## Next Steps

1. **Approve tech plan** — Tech lead review + team agreement
2. **Create git repo** — Set up GitHub + branch protection rules
3. **Create PR template** — Include constitution checklist
4. **Set up CI/CD** — GitHub Actions workflow
5. **Kick off Sprint 0** — Backend + frontend scaffolding

---

**Ready to begin implementation.** 🚀
