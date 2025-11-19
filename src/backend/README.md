# Journal AI — Backend (ASP.NET Core)

This folder contains a minimal scaffold for the Journal AI backend using ASP.NET Core 8 and EF Core.

Quick start (local dev)

1. Ensure Docker Compose services are running (see `docker-compose.example.yml`).
2. Set `DefaultConnection` in `appsettings.Development.json` or via env var (e.g., `Host=localhost;Port=5432;Database=journaldb;Username=journal;Password=journalpw`).
3. Run migrations and start the API:

```powershell
cd src/backend
# apply migrations (requires dotnet-ef)
# dotnet ef database update
dotnet run
```

Notes
- EF Core and Npgsql package references are included in `JournalAI.csproj` — adjust versions as needed.
- This scaffold intentionally omits authentication and controllers. Next steps: add controllers for entries, auth, categories, and implement business rules.
