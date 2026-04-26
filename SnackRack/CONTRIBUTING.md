# Contributing

Thank you for taking the time to contribute to SnackRack. This project exists primarily as a learning exercise, but pull requests and issues are welcome.

## Development setup

1. Fork and clone the repository.
2. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Docker](https://www.docker.com/).
3. Start the backing services:
   ```bash
   docker compose up postgres ollama ollama-pull
   ```
4. Apply migrations:
   ```bash
   dotnet ef database update --context ApplicationDbContext
   ```
5. Run the app:
   ```bash
   dotnet run
   ```

## Branching

- Branch off `main`.
- Use descriptive branch names: `feature/add-product-categories`, `fix/order-status-bug`.
- Open a pull request against `main` when ready.

## Commit messages

Use the imperative mood and keep the subject line under 72 characters. No trailing period.

```
Add semantic search to the Menu page
Fix guest checkout redirect when OrderId is null
Update HNSW index to use cosine distance operator
```

## Code style

- Follow existing conventions. Razor Pages page models co-locate their model classes in the same `.cshtml.cs` file (e.g. `Product`, `Order`, `Customer` all live alongside their page models, not in a separate `Models/` folder).
- Keep page models thin. Push reusable or testable logic into services under `Services/`.
- Do not commit connection strings or secrets. Use `appsettings.Development.json` (git-ignored) or `dotnet user-secrets`.
- New migrations must be reviewed before merging — always check the generated SQL is correct.

### Control flow

Prefer early returns over `if/else` branches. Invert conditions that can exit early and return immediately — never add an `else` block after a `return`.

```csharp
// Preferred — guard first, happy path continues at top-level indentation
if (string.IsNullOrWhiteSpace(SearchTerm))
{
    ActiveProducts = await GetAllActiveProductsAsync();
    return Page();
}

// ... work with SearchTerm here ...

// Avoid — nesting the real work inside a positive branch
if (!string.IsNullOrWhiteSpace(SearchTerm))
{
    // ... nested work ...
}
else
{
    ActiveProducts = await GetAllActiveProductsAsync();
}
```

### Error handling

- Use `ILogger<T>` — never `Console.WriteLine`. Inject it via the constructor.
- Wrap only the failure-prone call in try/catch, not the whole method.
- On catch: log with `_logger.LogError`, set `TempData["ErrorMessage"]` with a friendly message, and return `Page()` or a redirect.
- Guard against null with early returns (`NotFound()`, `Forbid()`, `return Page()`) before doing any further work with the value.
- `TempData["ErrorMessage"]` is displayed globally by `_Layout.cshtml` — no per-page markup is needed.

## Adding a migration

Always specify the context, as the solution has more than one `DbContext`:

```bash
dotnet ef migrations add <MigrationName> --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext
```

Hand-edit the generated migration file when you need:
- Custom SQL (extensions, functions, stored procedures)
- HNSW or other vector indexes that EF Core doesn't scaffold automatically

## Tests

All non-trivial changes should include tests. Run the full suite before opening a PR:

```bash
dotnet test
```

- **Unit tests** live in `SnackRack.Tests/Unit/` and run in-process with no external dependencies.
- **Integration tests** live in `SnackRack.Tests/Integration/` and require Docker. They spin up a `pgvector/pgvector:pg16` container via Testcontainers and run EF Core migrations against it.
