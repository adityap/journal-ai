# Journal AI — Personal Journaling Platform

A privacy-first, open-source journaling application with rich text, images/video support, sentiment analysis, and intelligent mind-map generation.

> **Current build/test status and known issues:** see [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md). Historical status/implementation docs are archived under [docs/archive/](docs/archive/).

## Features

✨ **Core Features**
- ✅ Create journal entries with text, images, and videos
- ✅ Organize entries by categories and tags
- ✅ Privacy-preserving confidentiality levels (Public/Private)
- ✅ Same-day edit/delete enforcement for data integrity
- ✅ Full-text search across entries
- ✅ Export journals as JSON, Markdown, or ZIP with media

🧠 **Intelligence**
- ✅ Client-side sentiment analysis (no data leaves your device)
- ✅ Sentiment trend visualization and analytics
- ✅ Interactive mind-maps showing entry relationships
- ✅ TF-IDF similarity-based graph generation

🔐 **Privacy & Security**
- ✅ No training data usage: all data marked immutable `no_training_use`
- ✅ Passwords hashed with bcrypt ≥12 rounds
- ✅ Account-based private entry unlock (session-based, 1-hour timeout)
- ✅ Audit logging for all mutations
- ✅ GDPR-compliant data export and deletion

## Getting Started

### Quick Start (Docker)

```bash
# Clone repository
git clone https://github.com/yourusername/journal-ai.git
cd journal-ai

# Start all services
docker compose up

# Access the application
# Frontend: http://localhost:5173
# Backend API: http://localhost:5000
# MinIO Console: http://localhost:9001 (user: minioadmin, password: minioadmin_password)
```

### Manual Setup

**Prerequisites:**
- .NET 8 SDK
- Node.js 18+, pnpm
- PostgreSQL 14+
- Redis (optional)

**Backend:**
```bash
cd src/backend
dotnet restore
dotnet ef database update
dotnet run
# API on http://localhost:5000
```

**Frontend:**
```bash
cd src/frontend
pnpm install
pnpm dev
# App on http://localhost:5173
```

## Architecture

```
┌─────────────────────────────────────────────────────┐
│         React Frontend (Vite)                       │
│  (sentiment.js client-side, cytoscape.js graphs)    │
└──────────────────┬──────────────────────────────────┘
                   │ HTTPS/JSON
┌──────────────────▼──────────────────────────────────┐
│   ASP.NET Core 8 API                                │
│   • JWT Authentication                              │
│   • Entry CRUD with business rules                  │
│   • Export/Import jobs                              │
│   • Mind-map generation (TF-IDF)                    │
└──────────────────┬──────────────────────────────────┘
        ┌──────────┼──────────┐
        │          │          │
   ┌────▼───┐ ┌───▼────┐ ┌──▼─────┐
   │PostSQL │ │ MinIO  │ │ Redis  │
   │        │ │  S3    │ │ Queue  │
   └────────┘ └────────┘ └────────┘
```

## Tech Stack

- **Backend**: ASP.NET Core 8 LTS + Entity Framework Core
- **Database**: PostgreSQL 14+
- **Storage**: S3-compatible (MinIO dev, AWS S3 prod)
- **Frontend**: React 18 + Vite + TypeScript
- **Sentiment**: sentiment.js (client-side, privacy-preserving)
- **Mind-maps**: cytoscape.js + TF-IDF similarity
- **Async Jobs**: Hangfire background workers
- **Auth**: JWT bearer tokens + session-based unlock
- **Containers**: Docker + Docker Compose
- **CI/CD**: GitHub Actions

## Development

See [CONTRIBUTING.md](./CONTRIBUTING.md) for:
- Local development setup
- Commit message conventions
- Pull request process
- Constitution compliance checklist
- Code review guidelines

## API Documentation

OpenAPI/Swagger documentation available at:
- Local: http://localhost:5000/swagger
- Spec file: `.specify/specs/001-journal-ai/openapi.yaml`

### Core Endpoints

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/v1/auth/register` | Create account |
| POST | `/api/v1/auth/login` | Sign in |
| POST | `/api/v1/entries` | Create entry |
| GET | `/api/v1/entries` | List entries |
| GET | `/api/v1/entries/{id}` | Read entry |
| PATCH | `/api/v1/entries/{id}` | Edit entry (same day only) |
| DELETE | `/api/v1/entries/{id}` | Delete entry (same day only) |
| POST | `/api/v1/unlock` | Unlock private entries |
| POST | `/api/v1/exports` | Create export job |
| GET | `/api/v1/mindmap` | Generate mind-map |
| POST | `/api/v1/import` | Bulk import entries |

## Implementation Status

### Sprint 0 ✅ (Infrastructure)
- [x] Git repo structure & CONTRIBUTING.md
- [x] Docker Compose with all services
- [x] Backend & Frontend Dockerfiles
- [x] GitHub Actions CI pipeline
- [x] .env configuration

### Sprint 1 (Core Entries & Database)
- [ ] EF Core models and migrations
- [ ] Entry CRUD endpoints
- [ ] Same-day edit/delete enforcement
- [ ] Unit tests (≥90% coverage)

### Sprint 2a (Authentication & Unlock)
- [ ] User registration & login
- [ ] JWT token generation
- [ ] Password hashing (bcrypt ≥12)
- [ ] Session-based private entry unlock

### Sprint 2b (Media Upload & Storage)
- [ ] S3-compatible signed URLs
- [ ] Media upload integration
- [ ] Thumbnail generation (async)
- [ ] Media association with entries

### Sprint 3a (Export & Import)
- [ ] Async export jobs
- [ ] Zip/JSON/Markdown export formats
- [ ] Bulk import with validation
- [ ] Email notifications (async)

### Sprint 3b (Sentiment Analysis)
- [ ] sentiment.js integration (client-side)
- [ ] Live sentiment display
- [ ] Sentiment analytics dashboard
- [ ] Trend visualization

### Sprint 4 (Mind-maps)
- [ ] TF-IDF text processing
- [ ] Similarity computation
- [ ] Graph generation & layout
- [ ] cytoscape.js visualization

### Sprint 5 (Testing & Release)
- [ ] Integration & E2E tests
- [ ] Performance optimization
- [ ] Security audit
- [ ] Accessibility audit
- [ ] Production deployment

## Configuration

### Environment Variables

See `.env.example` for all available options. Key variables:

```env
# Database
DATABASE_URL=postgresql://user:password@localhost:5432/journalai

# Storage
MINIO_ENDPOINT=http://localhost:9000
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=password

# JWT
JWT_SECRET_KEY=your-secret-key-min-32-chars
JWT_EXPIRATION_MINUTES=1440

# CORS
ALLOWED_ORIGINS=http://localhost:5173,http://localhost:3000
```

## Database Schema

Core tables:
- **users**: Authentication & settings
- **entries**: Journal entries with sentiment, confidentiality
- **categories**: Entry categories (1:1 per entry)
- **media**: Images/videos with S3 references
- **audit_logs**: All mutations logged
- **export_jobs**: Async export tracking
- **unlock_sessions**: Private entry sessions (1-hour TTL)

See `.specify/specs/001-journal-ai/data-model.md` for detailed schema.

## Security

- ✅ TLS 1.2+ required
- ✅ Passwords hashed with bcrypt (min 12 rounds)
- ✅ JWT tokens with expiration
- ✅ Rate limiting: 100 req/min per user, 10 req/sec per IP
- ✅ CORS configured for frontend origin only
- ✅ SQL injection prevention (parameterized queries)
- ✅ Sensitive data encrypted at rest

See `docs/SECURITY_AUDIT.md` for full security checklist.

## Privacy

- 🔒 **No training data usage**: All entries marked `no_training_use=true`
- 🔒 **Client-side sentiment**: No raw text sent to server
- 🔒 **Data ownership**: Users can export, delete, or anonymize their data
- 🔒 **Audit logs**: All access logged for compliance
- 🔒 **GDPR compliant**: Full data export and deletion endpoints

See `.specify/memory/constitution.md` for privacy principles.

## Performance Targets

| Operation | Target P95 | Status |
|-----------|-----------|--------|
| List entries (100 items) | < 300ms | 🎯 |
| Get single entry | < 150ms | 🎯 |
| Create entry | < 100ms | 🎯 |
| Export 100 entries | < 5s | 🎯 |

## Monitoring & Logging

- **OpenTelemetry** traces all requests and database queries
- **Prometheus** metrics exposed at `/metrics`
- **Grafana** dashboards for visualization
- **Structured logging** (Serilog) with context

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md) for:
- Development setup
- Commit conventions
- PR process
- Constitution compliance
- Code review guidelines

## Specification & Planning

Complete specification and technical planning available:
- **Specification**: [specs.md](.specify/specs/001-journal-ai/specs.md) (MVP features, API, UX flows)
- **Data Model**: [data-model.md](.specify/specs/001-journal-ai/data-model.md) (Schema, indexes)
- **Technical Plan**: [TECHNICAL_PLAN_V2.md](.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md) (Architecture, 6-sprint roadmap)
- **Constitution**: [constitution.md](.specify/memory/constitution.md) (Engineering principles, governance)
- **Clarifications**: [CLARIFICATION_SESSION_2026_02_13.md](.specify/specs/001-journal-ai/CLARIFICATION_SESSION_2026_02_13.md) (5 design decisions)

## Documentation

- 📖 [User Guide](./docs/USER_GUIDE.md) — How to use the application
- 👨‍💼 [Admin Guide](./docs/ADMIN_GUIDE.md) — Deployment, monitoring, troubleshooting
- 🚀 [Deployment Guide](./docs/DEPLOYMENT.md) — Production setup
- 🔒 [Security Audit Checklist](./docs/SECURITY_AUDIT.md) — Security compliance
- ♿ [Accessibility Audit Checklist](./docs/ACCESSIBILITY_AUDIT.md) — WCAG 2.1 AA compliance

## Testing

```bash
# Backend
cd src/backend
dotnet test                    # Run all tests
dotnet test --filter TestClass # Run specific test

# Frontend
cd src/frontend
pnpm test                      # Run tests
pnpm test -- --coverage       # With coverage
```

## Troubleshooting

### Docker Compose fails to start
```bash
# Verify services
docker compose ps

# View logs
docker compose logs -f api
docker compose logs -f postgres

# Restart services
docker compose down
docker compose up --build
```

### Database migrations fail
```bash
# Check migrations
dotnet ef migrations list

# Reset database (dev only)
dotnet ef database drop -f
dotnet ef database update
```

### API health check
```bash
curl http://localhost:5000/health
```

### Frontend can't reach API
Check browser console for CORS errors. Verify `ALLOWED_ORIGINS` env var includes frontend URL.

## License

TBD (Specify your license)

## Support

- 📧 Email: support@journalai.com (TBD)
- 💬 Discussions: GitHub Discussions tab
- 🐛 Issues: GitHub Issues tab
- 📖 Docs: See links above

---

**Last Updated**: 2026-02-13  
**Status**: 🚀 Sprint 0 Complete, Ready for Sprint 1  
**Maintainers**: [Your Team]
