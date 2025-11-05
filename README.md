# MyApp Backend

A backend API.

## Tech Stack

- **Framework**: ASP.NET Core
- **API**: FastEndpoints
- **Database**: PostgreSQL (via Entity Framework Core)
- **Cache**: Redis
- **Authentication**: JWT-based
- **Documentation**: OpenAPI/Scalar

## Features

- User authentication and authorization
- CRUD operations
- Pagination support
- Health checks
- OpenTelemetry for observability

## Installation

1. Ensure you have .NET 10+ installed.
2. Clone the repository.
3. Navigate to the project root.
4. Restore dependencies: `dotnet restore`

## Configuration

Update `appsettings.json` or environment variables for:
- Database connection string
- Redis connection
- Smtp connection
- Authentication settings (domain, audience, origins)

## Running

1. Start the application: `dotnet run --project src/host/MyApp.Host.Api`
2. In development, migrations will run automatically.
3. Access API at `https://localhost:5230` (or configured port)
4. Swagger UI at `/scalar`

## Development

- Use `dotnet build` to build the solution.
- Run tests with `dotnet test`.
- For local development, use the AppHost project for orchestration.

## Renaming the Project

To rename the project from "MyApp" to your desired names, run the provided script with two arguments: the camelCase replacement for "myApp" and the PascalCase replacement for "MyApp". For example:

```bash
./rename_script.sh mywiki MyWiki
```

This will update all file contents and rename files/directories accordingly. Run it from the project root, and commit changes afterward. Test in a copy first to avoid issues.