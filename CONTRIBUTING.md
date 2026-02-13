# Contributing to Journal AI

Thank you for your interest in contributing to Journal AI! This document provides guidelines for development, testing, and submitting contributions.

## Development Setup

### Prerequisites
- .NET 8 SDK
- Node.js 18+ and pnpm
- Docker & Docker Compose
- PostgreSQL (via Docker is fine for local dev)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/journal-ai.git
   cd journal-ai
   ```

2. **Start services with Docker Compose**
   ```bash
   docker compose up -d
   ```
   This starts:
   - PostgreSQL (port 5432)
   - MinIO (ports 9000, 9001)
   - Redis (port 6379)

3. **Backend setup**
   ```bash
   cd src/backend
   dotnet restore
   dotnet ef database update
   dotnet run
   ```
   API runs on http://localhost:5000

4. **Frontend setup**
   ```bash
   cd src/frontend
   pnpm install
   pnpm dev
   ```
   Frontend runs on http://localhost:5173

## Commit Message Format

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
type(scope): subject

body

footer
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`, `ci`

**Example**:
```
feat(entries): add same-day edit/delete enforcement

- Compute read_only_after based on user timezone
- Return 403 ENTRY_IMMUTABLE if past boundary
- Add unit tests for timezone edge cases

Closes #42
```

## Pull Request Process

1. **Create a feature branch**
   ```bash
   git checkout -b feat/your-feature
   ```

2. **Make changes and commit**
   - Follow commit message format above
   - Keep commits atomic (one logical change per commit)
   - Write descriptive commit messages

3. **Before submitting PR**
   ```bash
   # Backend
   cd src/backend
   dotnet format
   dotnet build
   dotnet test

   # Frontend
   cd src/frontend
   pnpm lint
   pnpm test
   pnpm build
   ```

4. **Submit PR with template**
   - Link related issues
   - Describe changes clearly
   - Add screenshots for UI changes
   - Include test coverage info
   - Check constitution compliance (see below)

## Constitution Compliance Checklist

Every PR must include this checklist (from `.specify/memory/constitution.md`):

### Privacy & Data Protection
- [ ] No user data sent to external services (especially AI training)
- [ ] Sentiment analysis client-side only (no server transmission of raw text)
- [ ] Passwords hashed with bcrypt ≥12 rounds
- [ ] All sensitive data encrypted at rest
- [ ] Audit logs created for all mutations

### Test-First Development
- [ ] Unit tests written BEFORE implementation (TDD)
- [ ] All tests pass locally
- [ ] Coverage ≥80% overall, ≥90% for modified files
- [ ] Integration tests cover critical paths

### API Contract Excellence
- [ ] OpenAPI spec updated
- [ ] Error codes documented in spec
- [ ] All error responses match ErrorResponse schema
- [ ] Rate limiting enforced (100 req/min per user)

### Security
- [ ] No hardcoded secrets in code
- [ ] All endpoints validate user ownership
- [ ] SQL injection prevention (parameterized queries only)
- [ ] CORS/CSRF protection verified

### Accessibility
- [ ] UI changes include alt text, labels, semantic HTML
- [ ] Color contrast ≥4.5:1
- [ ] Keyboard navigation tested
- [ ] No console errors from accessibility linters

## Code Review Guidelines

Reviewers should check:
1. **Functionality**: Does code do what PR description claims?
2. **Tests**: Are tests comprehensive and passing?
3. **Performance**: Any N+1 queries, inefficient algorithms?
4. **Security**: No vulnerability or unvalidated input?
5. **Constitution**: Does it align with privacy, testing, API, security principles?
6. **Documentation**: Are changes documented? Is OpenAPI updated?

## Testing

### Backend (C#)
```bash
cd src/backend
# Run all tests
dotnet test

# Run specific test file
dotnet test --filter ClassName

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

### Frontend (React)
```bash
cd src/frontend
# Run tests
pnpm test

# Run with coverage
pnpm test -- --coverage
```

## Performance Expectations

All PRs should meet these performance targets:

| Operation | P95 Latency | Target |
|-----------|-------------|--------|
| List entries (100 items) | < 300ms | ✅ |
| Get single entry | < 150ms | ✅ |
| Create entry | < 100ms | ✅ |
| Export 100 entries | < 5 seconds | ✅ |

## Releasing

Releases follow [Semantic Versioning](https://semver.org/):
- Major (X.0.0): Breaking changes
- Minor (0.X.0): New features (backward compatible)
- Patch (0.0.X): Bug fixes

### Release Process
1. Create release branch: `release/vX.Y.Z`
2. Update CHANGELOG.md
3. Tag with version: `git tag -a vX.Y.Z -m "Release vX.Y.Z"`
4. Push tag: `git push origin vX.Y.Z`
5. GitHub Actions deploys automatically

## Questions?

- **Documentation**: See [TECHNICAL_PLAN_V2.md](.specify/specs/001-journal-ai/TECHNICAL_PLAN_V2.md)
- **Specification**: See [specs.md](.specify/specs/001-journal-ai/specs.md)
- **Constitution**: See [constitution.md](.specify/memory/constitution.md)
- **Issues**: Check GitHub Issues board
- **Discussions**: Use GitHub Discussions for questions

Thank you for contributing! 🎉
