# Configuration

> **Read this when** you add, rename or read a setting of the API, or wire a value from the Aspire AppHost.

The API declares its own typed configuration schema, bound from a single `Api` section. It deliberately does not use
the framework conventions (the `ConnectionStrings` section, Aspire's `WithReference` injection): every value the API
needs is visible in one class, and a missing value is reported at startup instead of failing later.

## The schema

The schema is defined in `Src/Host/MyApp.Host.Api/ApiOption.cs`:

```
Api
├── Database:ConnectionString
├── Cache:ConnectionString
├── Auth:Domain
├── Auth:Audience
├── Cors:Origins[]
└── Services:DataPath          (bound to MyApp.Services.ServiceOption)
```

How it is read:

- `builder.GetAppConfig()` (`Extension/Configuration/ApiOptionConfiguration.cs`) binds the section and validates the
  required values. When some are missing, it throws a single `InvalidOperationException` listing every missing key,
  so you fix them all in one go.
- Every configuration extension that needs a setting calls `builder.GetAppConfig()`. Never read
  `builder.Configuration["..."]` directly.
- Environment variables use `__` as separator: `Api__Database__ConnectionString`, `Api__Cors__Origins__0`.

## Adding a setting

1. Add the property (or a new `ApiXxxOption` sub-class) in `ApiOption.cs`.
2. If it is required, add a `Require(...)` line in `GetAppConfig()`.
3. Declare the key with an empty value in `Src/Host/MyApp.Host.Api/appsettings.json`. This file is the schema only:
   it never holds real values. Put the local value in `appsettings.Development.json`.
4. Wire it from `Src/AppHost/Program.cs` with `.WithEnvironment("Api__Xxx__Yyy", ...)`, reading the value from the
   AppHost `api_config` section (`Src/AppHost/appsettings.Development.json`).

To use the setting in a host extension, see [Host composition](host-composition.md): settings are read inside
`AddAppXxx()`, never passed from `Program.cs`.
