# JournalAI backend (minimal scaffold)

This folder contains a minimal ASP.NET Core 8 scaffold used for local development and testing.

Quickstart:

1. Ensure .NET 8 SDK is installed.
2. From repository root run:

3. ```powershell
dotnet build src/backend/JournalAI.csproj
dotnet run --project src/backend/JournalAI.csproj
```

nThe API will expose a health endpoint at `/health` and a simple `api/entries` controller.
