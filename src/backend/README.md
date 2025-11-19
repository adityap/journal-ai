# JournalAI backend (minimal scaffold)

This folder contains a minimal ASP.NET Core 8 scaffold used for local development and testing.

Quickstart:

1. Ensure .NET 8 SDK is installed.
2. From repository root run:

```powershell
dotnet build src/backend/JournalAI.csproj
dotnet run --project src/backend/JournalAI.csproj
```

The API will expose a health endpoint at `/health` and a simple `api/entries` controller.

Migrations and local DB
-----------------------

This project uses EF Core migrations for schema management. For local development we use SQLite so you can run migrations without Postgres.

From the `src/backend` folder:

```powershell
dotnet tool restore
dotnet ef migrations list --project JournalAI.csproj --startup-project JournalAI.csproj
dotnet ef migrations add <Name> -o Migrations --project JournalAI.csproj --startup-project JournalAI.csproj
dotnet ef database update --project JournalAI.csproj --startup-project JournalAI.csproj
```

The local SQLite file `journalai-dev.db` is created in `src/backend` when you run `database update`. That file is ignored by git (see repository `.gitignore`).
