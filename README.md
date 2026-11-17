# MyApp Backend

Backend API template built with ASP.NET Core Minimal API on .NET 11.

Architecture, conventions and step-by-step recipes (adding an entity, a service, an endpoint, a setting...) are
documented in [AGENTS.md](AGENTS.md).

## Tech stack

- **Runtime**: .NET 11, C# 15
- **API**: ASP.NET Core Minimal API with API versioning
- **Database**: PostgreSQL through Entity Framework Core
- **Cache**: HybridCache (in-memory + Redis)
- **Authentication**: JWT bearer (Keycloak in development)
- **Documentation**: OpenAPI with Scalar UI
- **Observability**: OpenTelemetry, Serilog, health checks
- **Local orchestration**: .NET Aspire

## Features

- JWT authentication and role-based authorization
- Generated pagination, filtering and sorting
- Reusable query specifications
- Soft deletion
- Localized error messages (English, French)
- Health checks and OpenTelemetry

## Prerequisites

- .NET SDK 11.0 (see `global.json`)
- Docker, to run the development dependencies through Aspire

## Getting started

```bash
dotnet restore
dotnet build MyApp.slnx
```

### Run with Aspire (recommended)

Starts the API together with PostgreSQL, Redis, Keycloak and MailPit:

```bash
dotnet run --project Src/AppHost
```

Ports and credentials of the dependencies are set in `Src/AppHost/appsettings.Development.json`, under the
`api_config` section. The Aspire dashboard gives access to every resource and to the API.

### Run the API alone

```bash
dotnet run --project Src/Host/MyApp.Host.Api
```

The API listens on `http://localhost:5230`. Every required setting of the `Api` section must be provided (see
below), otherwise the application stops at startup and lists the missing keys.

In the `Development` environment:

- pending migrations and SQL scripts are applied automatically at startup;
- the Scalar UI is available at `/scalar`.

## Configuration

The API reads a single `Api` section, from `Src/Host/MyApp.Host.Api/appsettings.json` or from environment variables
(`__` as separator, for example `Api__Database__ConnectionString`).

| Key                            | Required | Description                         |
|--------------------------------|----------|-------------------------------------|
| `Api:Database:ConnectionString`| yes      | PostgreSQL connection string        |
| `Api:Cache:ConnectionString`   | yes      | Redis connection string             |
| `Api:Auth:Domain`              | yes      | JWT authority (issuer)              |
| `Api:Auth:Audience`            | yes      | JWT audience                        |
| `Api:Cors:Origins`             | no       | Allowed CORS origins (array)        |
| `Api:Services:DataPath`        | yes      | Folder used for file storage        |

When running through Aspire, these values are injected by `Src/AppHost/Program.cs`.

## Project structure

```
Src/
├── AppHost/                          Aspire orchestration (development only)
├── ServiceDefaults/                  Logging, OpenTelemetry, health endpoints
├── Host/MyApp.Host.Api/              Endpoints, API contracts, host configuration
├── Domain/
│   ├── MyApp.Domain.Models/          Entities, DbContext, migrations, SQL scripts
│   └── MyApp.Domain.Specifications/  Reusable query predicates
└── Services/
    ├── MyApp.Services.Abstractions/  Service contracts
    ├── MyApp.Services/               Service implementations
    └── MyApp.Services.Generators/    Pagination source generator
```

## Docker

The image is built from `docker/myApp/Dockerfile` and listens on port `8080`:

```bash
docker build -f docker/myApp/Dockerfile -t myapp .
```

A multi-architecture image (`linux/amd64`, `linux/arm64`) is pushed to `ghcr.io` on every published GitHub release.

## Renaming the project

To rename the template, run `setup.sh` from the repository root with the camelCase replacement of `myApp` and the
PascalCase replacement of `MyApp`:

```bash
./setup.sh mywiki MyWiki
```

The script updates file contents and renames files and directories. Try it on a copy first, then commit the result.
