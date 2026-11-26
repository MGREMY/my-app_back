# Continuous integration

> **Read this when** you change a GitHub workflow, the Docker image build, or the coverage reporting.

## Workflows

| File | Trigger | What it does |
|------|---------|--------------|
| `pull_request.yml` | Every pull request and push to `main` | Job `test`: restore, build, `dotnet test --no-build --coverlet --report-trx` (Testcontainers uses the Docker engine of the runner), then uploads the `test-results` (TRX) and `coverage` (Cobertura, OpenCover) artifacts, also when tests fail. Job `codecov`: opt-in, calls `reusable_codecov.yml` after `test`. |
| `reusable_codecov.yml` | `workflow_call` | Downloads the `coverage` artifact and uploads the Cobertura files with `codecov/codecov-action`, authenticated with OIDC. |
| `create_docker_image.yml` | A published release | Builds `docker/myApp/Dockerfile` for `linux/amd64` and `linux/arm64` and pushes it to `ghcr.io`. |

## Conventions

- GitHub forbids sub-folders in `.github/workflows`: reusable workflows are prefixed `reusable_` and named
  `"Reusable - <Name>"`. File names are snake_case.
- Permissions: `contents: read` at workflow level. A job needing more declares it itself (`id-token: write` on the
  `codecov` job only).

## What the CI produces

The job status (exit code of the test platform) is the only built-in signal, and the artifacts are on the run summary
page. The template posts nothing on pull requests: integrations are provided, never imposed.

## Codecov

Codecov is the only reporting integration, disabled by default. To enable it:

1. link the repository on codecov.io (Codecov GitHub app);
2. set the repository variable `CODECOV_ENABLED` to `true` (Settings > Secrets and variables > Actions > Variables).

No token is stored: the upload uses OIDC. Codecov then posts its own comment and checks.

Test results are not sent to Codecov: Test Analytics only accepts JUnit XML, not TRX. Once
`Microsoft.Testing.Extensions.JUnitReport` is stable, add `--report-junit` and a second upload with
`report_type: test_results` (see the comment in `reusable_codecov.yml`).
