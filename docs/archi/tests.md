# Tests

> **Read this when** you write or change a test, add a test project, or touch the shared test infrastructure.

The essential rules (what to test, at which level, naming) are summarized in [AGENTS.md](../../AGENTS.md#tests). This
page has the full details.

## Stack

| Concern             | Technology |
|---------------------|------------|
| Framework           | xUnit v3 on Microsoft.Testing.Platform v2, run by `dotnet test` (`global.json`) |
| Assertions, doubles | Shouldly, NSubstitute |
| Infrastructure      | Testcontainers (PostgreSQL, Redis), Respawn, `WebApplicationFactory` |
| Results, coverage   | TRX (`Microsoft.Testing.Extensions.TrxReport`), coverlet.MTP (Cobertura, OpenCover) |

## Layout

`Tests/` mirrors `Src/` one-to-one, grouping folders included. There is no test project for `Services.Abstractions`
(contracts only), `ServiceDefaults` and `AppHost`. Test projects are in the `/Tests/...` solution folders of
`MyApp.slnx`.

`Tests/Directory.Build.props` applies to every project of `Tests/`. A project whose name ends with `.Tests` gets the
test packages, the reference to `MyApp.Tests.Shared`, the global usings (`Xunit`, `Shouldly`, the categories) and
`Tests/testconfig.json`. Its own `.csproj` only references what it tests.

Inside a project, folders mirror the source folder of the tested class:
`Services/UserServices/UserGetById.Integration.cs` tests `Services/UserServices/UserGetById.cs`. There are no
`Unit/`, `Integration/` or `Functional/` folders: all the tests of a class stay together. Two exceptions:

- endpoint functional tests are in `Endpoints/<Area>Endpoint.Functional.cs`;
- generator tests are grouped by behaviour (`Filters/`, `Sorts/`, `Paging/`, `Errors/`, `Diagnostics/`) on the
  entities of `TestModels/`.

Test-only code lives in `TestInfrastructure/` (base classes, fixtures, helpers) and in `TestModels/`,
`TestMigrations/`, `TestSpecifications/` (test entities, contexts, migrations, specifications).

## Conventions

- One `public sealed partial class <Class>Tests` per tested class, one file per kind of test: `<Class>.Unit.cs`,
  `<Class>.Integration.cs`, `<Class>.Functional.cs`. Each file holds a nested `public sealed class` named `Unit`,
  `Integration` or `Functional`.
- The nested class carries the xUnit category trait built from the `TestCategory` constants
  (`MyApp.Tests.Shared.Categories`): `[Trait(TestCategory.Name, TestCategory.Unit)]`, `TestCategory.Integration` or
  `TestCategory.Functional`. Never write the strings by hand. The built-in `[Trait]` is used on purpose instead of
  custom attributes: IDEs (Rider) read categories from the source and only understand `[Trait]`.
- Every project has a convention test (`Conventions/TestConvention.Unit.cs`, calling
  `assembly.ShouldFollowTestConventions()`). It fails when a nested class name and its category trait do not match,
  when a category trait is used elsewhere (assembly, method, other class), or when a class declares tests without
  category.
- Test names: `Method_Context_ExpectedResult` (`ExecuteAsync_UnknownId_ReturnsNotFound`,
  `Query_AsUser_ReturnsRequestedPage`). Functional tests use the HTTP method as `Method`.
- Pass `TestContext.Current.CancellationToken` (the `Ct` property of the base classes) to async calls.
- A test asserts the expected behaviour, never the current one, and is never skipped: a failing test means the code
  must be fixed, not the test.

## Levels

| Level       | Trait                  | What is real | Docker |
|-------------|------------------------|--------------|--------|
| Unit        | `Category=Unit`        | Code out of reach of HTTP, or with too many cases for it, in memory: specifications, `FileStorageService` on a temporary folder, migration script ordering, generator diagnostics (`CSharpGeneratorDriver`) and the rejection of invalid pagination requests. | no |
| Integration | `Category=Integration` | The real layer on PostgreSQL and Redis containers: the host composition (`AddMyAppPersistence`, `AddMyAppCache`, `AddMyAppServices`), the generated pagination code, migrations and SQL scripts. | yes |
| Functional  | `Category=Functional`  | The whole API (`Program`) over HTTP with `WebApplicationFactory`, on the containers. Only authentication is replaced (see [below](#api-functional-tests-and-authentication)). | yes |

Service and generator tests never depend on authentication.

## What deserves a test

- **Our code, not the frameworks.** No test that EF Core maps a column, that ASP.NET routing returns `405`, that the
  DI container binds options or that a library honours its own contract.
- **One behaviour, one level.** Business rules are tested on the service (integration). Functional tests only check
  what HTTP adds: status codes, authentication and authorization, problem details.
- **Behaviour, not implementation.** No assertion on the shape of an expression tree, on the type returned by a
  method, on an internal cache, and no test re-implementing the algorithm it checks. Internal classes are tested
  through their public entry point. A class deliberately left without tests carries
  `[ExcludeFromCodeCoverage(Justification = "...")]`.
- **Unit only where HTTP cannot reach or has too many cases.** Anything an endpoint exercises (request validators,
  claims, problem formatting, configuration, DI composition) is tested functionally. Filters and sorts are split by
  what needs a database: rejecting an invalid request is a unit test, the rows returned are checked on PostgreSQL
  (the result depends on the SQL translation: nulls, case, `LIKE`, `EXISTS`).
- **No test for a missing feature** ("page size is not capped"): document it instead.

## Shared infrastructure

`MyApp.Tests.Shared` is a library, not a test project. It references no `Src/` project.

- **`ContainersFixture`**: PostgreSQL and Redis (same images as the AppHost), one pair per test assembly. A project
  declares it in `AssemblyInfo.cs` with `[assembly: AssemblyFixture(typeof(ContainersFixture))]`, and tests receive
  it through their constructor. The containers start lazily: a test calls `await containers.EnsureStartedAsync()`
  (the base classes do it in `InitializeAsync`), so a unit-only run never needs Docker.
- **`DatabaseSchema.CreateAsync(context)`**: applies the migrations (SQL scripts included) when the model has some,
  otherwise creates the schema from the model (the template ships without migrations).
- **`ContainersFixture.ResetAsync()`**: empties every table of the container database except the migration history
  (`DatabaseReset`, Respawn) and flushes Redis. The base classes call it before each test, once the schema exists:
  the tables to empty are read on the first call and reused for the whole run. A host shared by several tests keeps
  its in-memory (L1) cache: `ApiFunctionalTest` clears it itself (`ICacheService.RemoveByTagAsync("*")`).
- **`TemporaryDatabase`**: an empty database created on the container and dropped on dispose, for tests that apply
  migrations.

Each project using the containers has a `TestInfrastructure/DatabaseCollection` (`[Collection(DatabaseCollection.Name)]`,
name `"Database"`). The integration and functional classes share the container database, so they join this collection
and run one after the other. Unit classes, and classes working on their own `TemporaryDatabase`, stay outside of it
and run in parallel.

### Base classes

- **`ServiceIntegrationTest`**: real composition, one service provider per test. `CreateProvider()` builds a second
  API instance, optionally with test-only services such as EF Core interceptors:
  - `ReadBarrierInterceptor` makes concurrent calls read the same state before any of them writes, so a race happens
    on every run;
  - `EventuallyAsync` retries an assertion for up to 5 s, for what the cache backplane propagates to another instance
    in the background.
- **`PaginationIntegrationTest`**: test database seeded once per run. Read only: never write to it.
- **`ApiFunctionalTest`**: API host, `CreateClient()`, `SeedUsersAsync`, and problem readers:
  `ReadProblemWithCodeAsync(response, code)` also checks the `code` of the problem,
  `ReadValidationErrorsAsync(response)` returns the `errors` of a validation problem.

Service tests reach the kind of error with `result.Value.ShouldBeServiceError<NotFoundError>()`
(`ServiceResponseAssertions`).

### Test data

A test that needs users calls `SeedUsersAsync()`, which inserts the whole `TestUsers` dataset
(`MyApp.Tests.Shared/Data`, soft-deleted users included) as new entities. Tests take the users they act on from it
(`TestUsers.Bob.Id`, `client.AsUser(TestUsers.Alice)`) and derive counts from it (`TestUsers.Active.Count`), so adding
a user does not break unrelated tests.

Rules:

- the dataset only holds plain records with fixed ids, never entities (a shared entity would be tracked and mutated
  across tests);
- expected orders and pages are written by hand (names are chosen so their order is obvious): never compute them by
  sorting the dataset, which would re-implement what the test checks;
- a test whose starting state is not the dataset (a first sync on an empty database, a race) sets it up itself;
- a new entity gets its own dataset next to `TestUsers`, and its own `Seed...Async()` in the base classes.

## API functional tests and authentication

- `ApiFactory` (`WebApplicationFactory<Program>`) runs the API in the `Testing` environment. It is not
  `Development`, so neither `appsettings.Development.json` nor the startup migration are used. Every required `Api:*`
  setting is given with `UseSetting`, and the schema is created once. It is the fixture of the `Database` collection
  and starts lazily.
- JWT bearer is replaced by `TestAuthHandler`, which produces the claims read by `ClaimsPrincipalExtension`.
- Tests only express who calls, with `ApiClientAuthExtensions`: clients are anonymous by default (`AsAnonymous()`),
  `AsUser(...)`, `AsAdmin(...)` (both also take a `TestUsers` user) or `As(TestUser)`.
  `await client.WithAntiforgeryTokenAsync()` sends the antiforgery token like a SPA. Other request options are in
  `ApiClientExtensions` (`WithAccept`, `WithApiVersionHeader`, `QueryAsJsonAsync`).
- No test builds tokens, cookies, claims or authentication headers itself. When authentication moves behind the BFF,
  only `ApiFactory`, `TestAuthHandler` and `ApiClientAuthExtensions` change, never the test bodies.

## Adding a test

1. Open the test project mirroring the source project, and the folder mirroring the source folder of the class.
2. Add the test to `<Class>.<Kind>.cs`, creating the file with the partial class and the nested class if needed:

   ```csharp
   public sealed partial class UserGetByIdTests
   {
       [Trait(TestCategory.Name, TestCategory.Integration)]
       [Collection(DatabaseCollection.Name)]
       public sealed class Integration(ContainersFixture containers) : ServiceIntegrationTest(containers)
       {
           [Fact]
           public async Task ExecuteAsync_UnknownId_ReturnsNotFound()
           {
               var result = await RunAsync<IUserGetById, ServiceResponse<UserServiceResponse>>(
                   x => x.ExecuteAsync(Guid.NewGuid(), Ct));

               result.Value.ShouldBeServiceError<NotFoundError>().Code.ShouldBe(ServiceConstant.Error.user_not_found);
           }
       }
   }
   ```

3. A unit class has no fixture and no collection. An integration or functional class inherits the base class of its
   project (or calls `EnsureStartedAsync`) and joins `[Collection(DatabaseCollection.Name)]`.

### Adding a test project

A new test project is named `<SourceProject>.Tests`, placed in the mirrored folder and added to `MyApp.slnx` under
the matching `/Tests/...` folder. It references the tested project, copies `Conventions/TestConvention.Unit.cs`, and
declares the `ContainersFixture` in `AssemblyInfo.cs` if it needs containers.

## Commands and results

```bash
dotnet test                                                  # every test (Docker required)
dotnet test --filter-trait "Category=Unit"                   # unit tests only, no Docker
dotnet test --project Tests/Services/MyApp.Services.Tests    # one project
dotnet test --coverlet --report-trx                          # with coverage and TRX, like the CI
```

Results land in `TestResults/` at the repository root (git-ignored): one `<Project>_net11.0_<arch>.trx` and one
`coverage.cobertura.<timestamp>.xml` / `coverage.opencover.<timestamp>.xml` pair per test project. File names are
timestamped: use globs. Coverage settings (included assemblies, exclusions, formats) are in `Tests/testconfig.json`.

## Pitfalls

- `dotnet test` fails with exit code `8` when a filter selects no test in a project (for example
  `--filter-trait "Category=Integration"` on `MyApp.Domain.Specifications.Tests`, which only has unit tests). Every
  project has a unit convention test, so `Category=Unit` always matches; for other filters, target the projects that
  have such tests, or add `--ignore-exit-code 8`.
- Integration and functional tests fail at `EnsureStartedAsync` when Docker is not running.
- Coverage: the `include` of `Tests/testconfig.json` (`[MyApp.*]*`) works around coverlet issue #2071, where the
  excludes computed from the `deps.json` can drop the tested assemblies. Keep it explicit (`setup.sh` renames it).
- A test class declaring tests outside of a nested `Unit` / `Integration` / `Functional` class fails the convention
  test of its project.
