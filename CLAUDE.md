# CLAUDE.md — SnackRack

Full conventions are in `SnackRack/AGENTS.md`. The rules below are mandatory and enforced for every session.

## Mandatory: run tests after any code change

```bash
dotnet test SnackRack.Tests/SnackRack.Tests.csproj
```

This must run after **every** code change, without being asked. Do not finish a task without a passing test run.

## Mandatory: update the changelog

After any code change, add an entry under `## [Unreleased]` in `SnackRack/CHANGELOG.md`. Group by `### Added`, `### Changed`, `### Fixed`, or `### Tests`.

## Key conventions (summary — see AGENTS.md for full detail)

- Models live in their feature's `.cshtml.cs` file — no `Models/` folder.
- Prefer early returns over `if/else` nesting.
- Use `ILogger<T>` — never `Console.WriteLine`.
- Wrap only the failure-prone call in `try/catch`, not the whole method.
- `Result<T>` pattern: `Result<T>.Success(value)` / `Result<T>.Failure(error)`.
- pgvector: add `using Pgvector.EntityFrameworkCore;` in any file calling `.CosineDistance()`.
- Migrations: always pass `--context ApplicationDbContext`.
- Do not mock `ApplicationDbContext` in integration tests — use the Testcontainers fixture.

## What to avoid

- No `--no-verify` to skip git hooks.
- No `git push --force` to `main`.
- Do not hardcode the Ollama base URL.
