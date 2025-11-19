# Journal AI — Research Notes

This document captures research and design decisions for features where trade-offs matter: sentiment analysis, embeddings/emb privacy, mindmap algorithms, and compliance concerns.

## Sentiment analysis — options & trade-offs

1. On-device (preferred for privacy)
   - Pros: no user text leaves device; clear compliance with 'no training' promise.
   - Cons: limited model accuracy; extra bundle size on web/mobile.
   - Libraries: `sentiment` (npm), `compromise`, lightweight rule-based approaches (VADER-like). For React Native, use small native libs or run WASM models.

2. Server ephemeral compute (strict non-retention)
   - Pros: higher accuracy using larger models; centralized updates.
   - Cons: risk of leakage if policies/tools fail; must implement strict non-retention, auditing, and engineering controls.
   - Pattern: receive text, compute sentiment, return score, do not persist raw text. Log only entry id and model_version.

Recommendation: MVP uses on-device rule-based sentiment. Evaluate user opt-in for server compute if higher quality is requested.

## Embeddings & privacy for mindmap

- Embeddings are useful for semantic similarity; however, sending text to a third-party embedding service is a privacy risk.
- Approaches:
  - Local lightweight embeddings (use a small sentence-transformer on server but only with explicit user opt-in and non-retention enforcement).
  - TF-IDF + cosine similarity locally (client or server) for MVP.

Recommendation: implement TF-IDF + cosine for MVP. Reserve embeddings for opt-in, audited flow.

## Mindmap algorithms

- MVP: TF-IDF on entries (per-user corpus) combined with tag/category links.
- For graph layout and UX: use force-directed layout (d3-force or cytoscape) with clustering for groups.
- Scalability: limit nodes to 100 for interactive generation; larger scopes should be async with pre-computation and pagination.

## Privacy & legal considerations

- Explicitly store a `no_training_use` boolean per user and require all ML pipelines to check it.
- Create automated audits (jobs) that scan training datasets for user ids or entry ids and fail if any records from `no_training_use` users are present.
- Ensure export flows require unlock/re-auth for Private entries.

## Libraries and tooling considered

- Sentiment: `sentiment` (npm), VADER (python), `compromise` (npm)
- Embeddings: sentence-transformers (Python), or commercial APIs (if opt-in only)
- Mindmap UI: cytoscape.js, vis.js, d3-force
- Backend: ML.NET is an option for .NET-hosted models but may be heavier than needed for MVP.

## Open questions

- Do you prefer on-device sentiment only, or do you want a server-side API option for higher-quality results (opt-in)?
- Preferred deployment model: managed cloud (Azure App Service + Azure Postgres + Blob) or containerized on K8s? (I can prepare both options.)

## .NET Aspire — targeted research (versions, compatibility, guidance)

Context
- You mentioned using ".NET Aspire" for the project. Because Aspire and its ecosystem are evolving, we should lock down compatible package versions, runtime targets, and integration patterns before scaffolding large parts of the backend.

Goals
- Validate the recommended runtime and library versions for the Journal AI application (ASP.NET Core, EF Core, Npgsql, and any Aspire-specific packages).
- Identify breaking changes, migration guides, and best practices for hosting Aspire-based services in Docker/Kubernetes and integrating with EF Core/Postgres.
- Confirm recommended authentication integrations (JWT/session), background worker patterns, and any Aspire-specific middleware or extension points for telemetry and security.

Candidate targets to verify
- .NET runtime: .NET 8 (primary target) and the latest Aspire recommendations if it requires a newer or different runtime.
- EF Core: EF Core 8 (verify compatibility with Aspire and any provider constraints).
- Npgsql: latest stable provider compatible with chosen EF Core/.NET runtime.
- Swashbuckle/OpenAPI: verify latest stable that works with the chosen ASP.NET runtime and Aspire middleware (if Aspire provides OpenAPI helpers).
- Any Aspire-specific NuGet packages: determine package names, stable versions, and supported features (e.g., Aspire.Hosting, Aspire.Extensions.*).

Research questions (to be answered by web research)
1. What is the latest stable release of .NET Aspire and which .NET runtimes does it support? Are there known breaking changes when used with ASP.NET Core 8?
2. Does Aspire provide helper packages for EF Core, authentication, background workers, or OpenAPI generation? If so, which package versions are stable and recommended?
3. Are there documented best practices for using Aspire with Postgres/Npgsql, particularly around connection pooling, migrations, and large-batch operations?
4. Are there known issues integrating Aspire with common .NET observability stacks (OpenTelemetry) or APMs (Application Insights, Datadog)?
5. Are there recommended Docker images or container base images for Aspire services (OS, SDK/runtime tags)?
6. Any community or vendor-provided guidance for production hardening (security headers, Kestrel tuning, thread/connection limits) specific to Aspire services?

Planned research actions (create separate tasks)
- Confirm Aspire latest stable release and its compatibility matrix with .NET 8, EF Core 8, and Npgsql.
- Find migration notes or breaking-change guides for Aspire relevant to our planned stack.
- Collect official docs or reputable blog posts showing Aspire + EF Core + Postgres usage patterns.
- Gather recommendations for container images and production tuning for Aspire apps.

How results will be used
- Update `plan.md` and `data-model.md` with locked versions and configuration snippets.
- Update CI pipelines to check for compatible SDK/runtime versions and include any Aspire-specific analyzers.
- Add a short RFC if Aspire requires significant deviations from the plan (different runtime, different worker pattern, etc.).

*Aspire research section added: 2025-11-19*
