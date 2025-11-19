FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/backend/JournalAI.csproj", "src/backend/"]
RUN dotnet restore "src/backend/JournalAI.csproj"
COPY . .
WORKDIR "/src/src/backend"
RUN dotnet publish "JournalAI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JournalAI.dll"]
