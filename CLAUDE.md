# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GrandNode is an ASP.NET Core 9.0 e-commerce platform using MongoDB as its database. It supports B2B, B2C, multi-store, multi-vendor, multi-tenant, multi-language, and multi-currency business models.

## Build and Run Commands

```bash
# Restore and build
dotnet restore GrandNode.sln
dotnet build GrandNode.sln -c Debug

# Run all tests (requires MongoDB running on localhost:27017)
dotnet test GrandNode.sln

# Run a single test project
dotnet test ./src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj

# Docker development (starts MongoDB + GrandNode)
docker compose -f docker-compose.grandnode.yml up --build

# Install Aspire workload (required for host project)
dotnet workload install aspire
```

## Architecture

### Layer Structure

- **src/Aspire/** - Host and service defaults for .NET Aspire orchestration
  - `Aspire.AppHost/Program.cs` - Main entry point, configures MongoDB and web project
- **src/Core/** - Domain models and infrastructure
  - `Grand.Domain` - Entity models
  - `Grand.Data` - MongoDB data access
  - `Grand.Infrastructure` - Cross-cutting concerns
  - `Grand.SharedKernel` - Shared utilities
- **src/Business/** - Business logic layer (services, validation)
  - Organized by domain: Catalog, Checkout, Customers, Marketing, Messages, etc.
  - `Grand.Business.Core` - Core interfaces and DTOs
- **src/Modules/** - Deployment modules
  - `Grand.Module.Api` - REST API
  - `Grand.Module.Installer` - Installation wizard
  - `Grand.Module.Migration` - Database migrations
  - `Grand.Module.ScheduledTasks` - Background jobs
- **src/Plugins/** - Self-contained extensions (payments, shipping, themes, widgets)
- **src/Web/** - UI projects
  - `Grand.Web` - Customer storefront
  - `Grand.Web.Admin` - Administration panel
  - `Grand.Web.Vendor` - Vendor portal
  - `Grand.Web.Common` - Shared web components
- **src/Tests/** - Unit tests (MSTest + Moq)

### Key Configuration Files

- `global.json` - SDK version (9.0.100)
- `Directory.Packages.props` - Central package version management (always update versions here, not in individual .csproj files)
- `src/Build/Grand.Common.props` - Shared MSBuild properties (target framework, version)

### Host Startup

The application uses .NET Aspire for orchestration. Entry point is `src/Aspire/Aspire.AppHost/Program.cs`:
```csharp
var mongo = builder.AddMongoDB("mongo").WithLifetime(ContainerLifetime.Persistent);
var mongodb = mongo.AddDatabase("Mongodb");
builder.ConfigureGrandWebProject(mongodb);
```

## Development Conventions

- **Package versions**: Always update `Directory.Packages.props`, not individual project files
- **Naming**: Projects use `Grand.*` or `Aspire.*` prefixes
- **Plugins**: Build independently - see README for the full plugin build chain
- **Configuration**: Prefer `appsettings` and environment variables over hardcoded connection strings
- **DI/Configuration**: Most wiring happens via extension methods like `ConfigureGrandWebProject()`

## Testing

Tests require a running MongoDB instance on localhost:27017. In CI, this is started via Docker:
```bash
docker run -d -p 27017:27017 mongo
```

Tests use MSTest framework with Moq for mocking.

## Release Workflow

Single branch (`main`) with semantic version tags. No dev branch needed.

```bash
# 1. Commit your changes
git add .
git commit -m "feat: description of changes"

# 2. Create a version tag when ready to release
git tag v1.0.0
git push origin main --tags

# This triggers GitHub Actions to:
# - Build Docker image
# - Push to ghcr.io/astrobod427/grandnode2:v1.0.0 and :latest
```

### Image registry

Images are published to GitHub Container Registry:
- `ghcr.io/astrobod427/grandnode2:latest`
- `ghcr.io/astrobod427/grandnode2:v1.0.0` (version-specific)

## Production Deployment (VPS Hostinger KVM4)

### 1. Configurer le registry ghcr.io dans Portainer

1. Portainer → **Settings** → **Registries** → **Add registry**
2. Sélectionner **Custom registry**
3. Remplir :
   - Name: `GitHub Container Registry`
   - Registry URL: `ghcr.io`
   - Username: `astrobod427`
   - Password: votre GitHub PAT (scope `read:packages`)

### 2. Déployer le stack via Portainer

1. Portainer → **Stacks** → **Add stack**
2. Name: `grandnode`
3. Coller le contenu de `docker-compose.prod.yml`
4. **Deploy the stack** (pas de variables d'environnement nécessaires)

Note: Le stack utilise le Traefik existant via le réseau `portainer-stack_traefik-net`.

### Mise à jour en production

Dans Portainer → **Stacks** → `grandnode` → **Editor** → **Update the stack** (cocher "Re-pull image")

### Volumes persistants

- `grandnode_images` - Images produits uploadées
- `grandnode_appdata` - Configuration et données applicatives
- `mongo_data` - Base de données MongoDB

### URLs après déploiement

- Site: `https://labaraque.shop` (+ `www.labaraque.shop`)

### Configuration DNS requise

Pointer vers l'IP du VPS :
- `labaraque.shop` → A record → IP_VPS
- `www.labaraque.shop` → A record → IP_VPS (ou CNAME → labaraque.shop)
