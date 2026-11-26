# AGENTS.md

Guide for anyone working on this repository, human or AI agent. This file holds what applies to every task: the
stack, the commands, the architecture and the conventions. The detailed recipes live in [`docs/archi/`](docs/archi),
one file per topic: read the one matching your task before starting (see [Documentation](#documentation)).

When in doubt, follow the existing code: consistency with what is already there wins over personal preference.

## Overview

Backend API template built with:

| Concern           | Technology                                                                      |
|-------------------|---------------------------------------------------------------------------------|
| Runtime           | .NET 11, C# 15 (`LangVersion` default)                                          |
| API               | ASP.NET Core Minimal API, Asp.Versioning                                        |
| Database          | PostgreSQL through EF Core (Npgsql provider, lazy loading proxies)              |
| Cache             | FusionCache (in-memory L1 + Redis L2 + Redis backplane), behind `ICacheService` |
| Authentication    | JWT bearer (Keycloak in development)                                            |
| Validation        | FluentValidation                                                                |
| Documentation     | OpenAPI + Scalar (development only)                                             |
| Observability     | OpenTelemetry, Serilog, health checks                                           |
| Dev orchestration | .NET Aspire (`AppHost`), used for local development only                        |
| Test runner       | Microsoft.Testing.Platform (set in `global.json`)                               |
| Tests             | xUnit v3, Shouldly, NSubstitute, Testcontainers, Respawn, coverlet.MTP          |

## Commands

Run every command from the repository root.

```bash
dotnet restore
dotnet build MyApp.slnx
```

Tests (Docker is required, except for unit tests; more commands in [tests](docs/archi/tests.md#commands-and-results)):

```bash
dotnet test
dotnet test --filter-trait "Category=Unit"
```

Run the whole stack (API, PostgreSQL, Redis, Keycloak, MailPit) through Aspire, or the API alone (the `Api`
configuration section must then be complete, see [configuration](docs/archi/configuration.md)):

```bash
dotnet run --project Src/AppHost
dotnet run --project Src/Host/MyApp.Host.Api
```

EF Core migrations (generated in `Src/Domain/MyApp.Domain.Models/Migrations`). The tools start the API in
`Development`, so they read `appsettings.Development.json`; override with `Api__...` environment variables to target
another database. Applying or scripting migrations includes their SQL scripts (see
[entities and migrations](docs/archi/entities-and-migrations.md#sql-scripts-around-a-migration)).

```bash
dotnet ef migrations add <MigrationName> --project Src/Domain/MyApp.Domain.Models --startup-project Src/Host/MyApp.Host.Api --output-dir Migrations
dotnet ef database update --project Src/Domain/MyApp.Domain.Models --startup-project Src/Host/MyApp.Host.Api
dotnet ef migrations script --idempotent --project Src/Domain/MyApp.Domain.Models --startup-project Src/Host/MyApp.Host.Api --output migrations.sql
```

Rename the template (camelCase then PascalCase replacement of `myApp` / `MyApp`):

```bash
./setup.sh mywiki MyWiki
```

## Architecture

### Projects

```
Src/
├── AppHost/                          Aspire orchestration (development only)
├── ServiceDefaults/                  Shared host setup: logging, OpenTelemetry, health endpoints
├── Host/
│   └── MyApp.Host.Api/               HTTP layer: endpoints, API contracts, host configuration
├── Domain/
│   ├── MyApp.Domain.Models/          EF Core entities, AppDbContext, migrations, SQL scripts
│   └── MyApp.Domain.Specifications/  Reusable query predicates (specification pattern)
└── Services/
    ├── MyApp.Services.Abstractions/  Service contracts: interfaces, requests, responses
    ├── MyApp.Services/               Service implementations (use cases)
    └── MyApp.Services.Generators/    Roslyn source generator (pagination), netstandard2.0
Tests/                                Mirrors Src/ one-to-one
├── MyApp.Tests.Shared/               Shared test infrastructure: categories, conventions, containers, database
├── Host/MyApp.Host.Api.Tests/
├── Domain/
│   ├── MyApp.Domain.Models.Tests/
│   └── MyApp.Domain.Specifications.Tests/
└── Services/
    ├── MyApp.Services.Tests/
    └── MyApp.Services.Generators.Tests/
```

`Domain/` holds the domain model (entities and their shared conditions). `Services/` holds the application layer:
the use cases built on top of the domain.

### Dependency rules

```
Host.Api ──► Services ──► Services.Abstractions
   │            ├──────► Domain.Specifications ──► Domain.Models
   │            ├──────► Domain.Models
   │            └──────► Services.Generators (analyzer only)
   └──────► ServiceDefaults
AppHost ──► Host.Api (orchestration only)
```

- `Services.Abstractions` depends on nothing. It only contains contracts.
- `Domain.Specifications` only knows entities and expression trees. It never references EF Core APIs.
- `Services` does not know the database provider. Npgsql is only referenced by `Domain.Models`.
- `Host.Api` is the composition root. It is the only place that decides which infrastructure is used.
- The generator is referenced with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`.
- A test project references the project it tests (plus what its test models need) and `MyApp.Tests.Shared`.
  `MyApp.Tests.Shared` references no `Src/` project. `Src/` never references `Tests/`.

## Documentation

Read the matching file before working on a topic. They are plain Markdown, written for humans as much as for agents.

| Read before... | File |
|----------------|------|
| adding or reading a setting, wiring a value from the AppHost | [configuration.md](docs/archi/configuration.md) |
| touching `Program.cs`, a host extension, the middleware order or the DI registration | [host-composition.md](docs/archi/host-composition.md) |
| adding or changing an entity, a migration or a migration SQL script | [entities-and-migrations.md](docs/archi/entities-and-migrations.md) |
| adding a service (use case), a specification, a projection or a business error | [services.md](docs/archi/services.md) |
| reading from or writing to the cache | [cache.md](docs/archi/cache.md) |
| adding a paginated list, changing a paginated entity, making a type filterable, working on the generator | [pagination-generator.md](docs/archi/pagination-generator.md) |
| adding a route, an API request or response, a validator | [endpoints.md](docs/archi/endpoints.md) |
| working on auth, users, CORS, error responses, soft deletion, versioning, health checks, file storage, logging | [cross-cutting.md](docs/archi/cross-cutting.md) |
| writing a test, adding a test project, touching the test infrastructure | [tests.md](docs/archi/tests.md) |
| changing a GitHub workflow, the Docker image build or Codecov | [ci.md](docs/archi/ci.md) |

The [README](README.md) is the user-facing documentation (getting started, pagination request format, Docker).

## Code conventions

- File-scoped namespaces matching the folder path. Nullable reference types and implicit usings are enabled.
- Classes are `sealed` unless designed for inheritance. Entities are not sealed (lazy loading proxies).
- Services and specifications use explicit constructors with `private readonly` fields.
- Use the C# 14 `extension` block syntax for new extension methods.
- Results are modelled with C# 15 unions (`ServiceResponse`) and consumed with exhaustive `switch` expressions.
- Prefer pattern matching (including list patterns) over chains of `if` when it reads better.
- Every setting belongs to the typed `Api` configuration: never read `builder.Configuration["..."]` directly.
- Services never return API types and endpoints never expose service types.
- The API is not localized: errors are stable codes with parameters, never translated messages.
- Constants are grouped in a static `<Layer>Constant` class (`ApiConstant`, `ServiceConstant`, `ModelConstant`).
- Remove unused `using` directives.

Naming suffixes:

| Suffix                               | Meaning                       | Project               |
|--------------------------------------|-------------------------------|-----------------------|
| `ApiRequest` / `ApiResponse`         | HTTP contracts                | Host.Api              |
| `ServiceRequest` / `ServiceResponse` | service contracts             | Services.Abstractions |
| `Spec`                               | specification                 | Domain.Specifications |
| `Projection`                         | expression projections        | Services              |
| `Configuration`                      | host configuration extensions | Host.Api              |
| `Option`                             | configuration schema classes  | Host.Api, Services    |
| `Endpoint`                           | endpoint group                | Host.Api              |

Members of a type are declared in this order:

| # | Members                                             | Inside the group                             |
|---|-----------------------------------------------------|----------------------------------------------|
| 1 | Constants                                           | `public`, `internal`, `protected`, `private` |
| 2 | Static fields and properties                        | `public`, `internal`, `protected`, `private` |
| 3 | Instance properties and fields                      | `public`, `internal`, `protected`, `private` |
| 4 | Constructors                                        | `public`, `internal`, `protected`, `private` |
| 5 | Public (and `internal`) methods, `extension` blocks |                                              |
| 6 | Protected methods (overrides included)              |                                              |
| 7 | Private methods                                     |                                              |
| 8 | Nested types                                        |                                              |

Static and instance methods follow the same rule (only their accessibility counts). Inside a group, keep the logical
order (entity columns before interface properties, tests in reading order). Static field initializers run in
declaration order: a static field used by another one is declared before it, which the order above already gives when
both have the same accessibility.

## Tests

The essentials, always applicable. Details, infrastructure and recipes are in [tests.md](docs/archi/tests.md).

- `Tests/` mirrors `Src/`, folders included. All the tests of a class stay together: one
  `public sealed partial class <Class>Tests`, one file per kind (`<Class>.Unit.cs`, `.Integration.cs`,
  `.Functional.cs`), each holding a nested `Unit` / `Integration` / `Functional` class with its
  `[Trait(TestCategory.Name, TestCategory.Xxx)]`. A convention test enforces it.
- Levels: **unit** (in memory, no Docker) only where HTTP cannot reach or has too many cases; **integration** for the
  real layer on PostgreSQL and Redis containers (business rules are tested on the service); **functional** for what
  HTTP adds (status codes, auth, problem details).
- Test our code, not the frameworks; behaviour, not implementation; one behaviour at one level. No test for a
  missing feature: document it instead.
- Names: `Method_Context_ExpectedResult`. Pass `Ct` (`TestContext.Current.CancellationToken`) to async calls.
- A test asserts the expected behaviour, never the current one, and is never skipped: a failing test means the code
  must be fixed, not the test.

## Git

- Commit messages follow Conventional Commits: `feat:`, `fix:`, `refactor:`, `perf:`, `chore:`, `docs:`, `test:`,
  optional scope (`refactor(generator): ...`).
- Work on a branch, merge into `main` through a pull request. The CI builds and runs every test (see
  [ci.md](docs/archi/ci.md)).

## Known pitfalls

- Adding an interface to `Services.Abstractions` without exactly one implementation in `Services` makes startup fail
  ([host composition](docs/archi/host-composition.md#automatic-service-registration)).
- Missing required configuration makes startup fail with the list of missing `Api:...` keys
  ([configuration](docs/archi/configuration.md)).
- Migrations and SQL scripts run automatically in `Development` only. Other environments must apply them explicitly.
  A SQL script in a folder not matching the exact migration id is silently ignored
  ([entities and migrations](docs/archi/entities-and-migrations.md#pitfalls)).
- Integration and functional tests fail when Docker is not running. A `dotnet test` filter selecting no test in a
  project exits with code `8` ([tests](docs/archi/tests.md#pitfalls)).
