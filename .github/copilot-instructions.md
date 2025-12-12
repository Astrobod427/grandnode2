<!-- Copilot / AI agent instructions for contributors working on GrandNode -->

# GrandNode — AI Assistant Quick Guide

Purpose: help AI coding agents become productive quickly by highlighting the project's architecture, entrypoints, developer workflows, conventions, and examples.

1. Big picture
- **What**: GrandNode is an ASP.NET Core 9.0 e‑commerce platform using MongoDB and a modular plugin/module architecture.
- **Key areas**: `src/Aspire` (host/bootstrap), `Core` (domain & infra), `Business` (business logic), `Modules` (deployment modules), `Plugins` (extensions), `Web` (UI projects), `Tests` (unit/integration tests).
- **Why**: Modular design lets plugins and modules be developed and published independently; `Aspire.AppHost` centralizes service discovery and host configuration.

2. Entrypoints & configuration
- Primary host: `src/Aspire/Aspire.AppHost/Program.cs` — uses `Aspire.AppHost` builder. Example: `builder.AddMongoDB("mongo").AddDatabase("Mongodb");` and `builder.ConfigureGrandWebProject(mongodb)`.
- Global SDK: see `global.json` (SDK `9.0.100`).
- Central package versions: `Directory.Packages.props` — use MSBuild central package management when adding/updating packages.
- Build helpers/targets: `Build/Grand.Common.props` and `Build/Grand.Targets.props` contain project-wide build conventions.

3. Build / run / test (concise commands)
- Restore and build solution:
  - `dotnet restore GrandNode.sln`
  - `dotnet build GrandNode.sln -c Debug`
- Run tests (project or solution):
  - `dotnet test ./Tests/Grand.Business.Catalog.Tests -c Debug`
  - or `dotnet test GrandNode.sln`
- Docker (dev & CI):
  - `docker compose -f docker-compose.grandnode.yml up --build` (also available as a workspace task: `Docker: Démarrer GrandNode (Build & Run)`).
  - For production-like containers, see `Dockerfile` and `Dockerfile.GrandNodeDev`.
- Local dev host: open `GrandNode.sln` in Visual Studio/VS Code, set startup to `Aspire.AppHost` or `Grand.Web` depending on role.

4. Runtime & environment notes
- Default dev settings: `src/Aspire/Aspire.AppHost/appsettings.Development.json` (logging + environment overrides).
- MongoDB service name in `Program.cs` is `mongo` — docker-compose and local launches expect a `mongo` host or explicit connection string in `appsettings`.
- To run with a specific environment: set `ASPNETCORE_ENVIRONMENT=Development` or `Production` before starting.

5. Project-specific patterns & conventions
- Central package versions: always update `Directory.Packages.props`, not individual projects, unless adding new package versions intentionally.
- Plugin/Module layout: `src/Plugins/*` contain self-contained functionality. Build or publish plugins separately as the README shows (see long `dotnet build` chain).
- Naming: projects use `Grand.*` or `Aspire.*`. Keep new projects consistent with existing namespace and folder style.
- Dependency injection & configuration: most wiring happens in `Aspire.AppHost` pipeline and extension methods like `ConfigureGrandWebProject(...)` — prefer adding configuration there rather than scattered Program.cs logic.

6. Where to look for common changes
- UI / storefront: `src/Web/Grand.Web` and `src/Web/Grand.Web.Admin`.
- Domain models: `src/Core/Grand.Domain`.
- Data access & infrastructure: `src/Core/Grand.Infrastructure` (MongoDB integration lives here and is wired via `Aspire.Hosting.MongoDB`).
- Background tasks & modules: `src/Modules` (scheduling, migration, installer).

7. Tests & CI
- Tests live in `Tests/*` with MSTest/NUnit adapters configured in `Directory.Packages.props`.
- CI uses GitHub Actions (`.github/workflows` exists in repo upstream). Keep tests fast and targeted; run specific project tests during development.

8. Helpful examples (copy-paste friendly)
- Build & test solution:
  - `dotnet restore GrandNode.sln && dotnet build GrandNode.sln && dotnet test GrandNode.sln`
- Start dev environment with docker-compose:
  - `docker compose -f docker-compose.grandnode.yml up --build`
- Inspect host wiring:
  - Open `src/Aspire/Aspire.AppHost/Program.cs` and trace `ConfigureGrandWebProject(...)` into project extension implementations.

9. When editing code, follow these rules
- Update `Directory.Packages.props` for package version changes.
- Keep changes isolated: modify the smallest number of projects for a change; respect plugin boundaries.
- Avoid adding direct DB connection strings in code — prefer `appsettings` and environment variables.

10. Quick pointers for AI agents
- If you need to change host startup, modify `src/Aspire/Aspire.AppHost/Program.cs` and any extension methods used by `ConfigureGrandWebProject`.
- Use `global.json` SDK version for local toolchain to avoid version skew.
- Search for functionality by prefixing with project names (e.g., `Grand.Business`, `Grand.Web`) — naming is consistent and helps locate code quickly.

If any section needs more detail (examples, extension points, CI specifics), tell me which area and I will expand or merge any existing `.github` instructions you prefer.
