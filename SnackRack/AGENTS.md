# Agents

Guidelines for AI coding assistants working on this repository.

## Build and test

```bash
dotnet build                                              # must pass with 0 errors
dotnet test                                               # run after any code change
dotnet ef migrations add <Name> --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext
```

## Project layout

```
SnackRack/                   ← main app (working directory)
├── Data/
│   ├── ApplicationDbContext.cs   ← single EF Core context for the app
│   ├── ApplicationUser.cs        ← Identity user extension
│   └── Extensions/
│       └── DbFunctionsExtensions.cs  ← remove_hyphens() DB function mapping
├── Migrations/              ← EF Core migrations (real folder; Data/Migrations is a symlink)
├── Pages/Features/          ← all Razor Pages, organised by feature
│   ├── Admin/               ← BackfillEmbeddings
│   ├── Customers/           ← guest checkout
│   ├── Orders/              ← order creation + confirmation
│   ├── Products/            ← Menu with semantic search
│   └── Reviews/             ← create + all-reviews
├── Services/                ← IEmbeddingService, OllamaEmbeddingService
└── Areas/Identity/          ← scaffolded login/register/logout pages

SnackRack.Tests/             ← test project (sibling directory)
├── Unit/                    ← fast, no external deps
└── Integration/             ← requires Docker (Testcontainers)
```

## Key conventions

### Models live in the page model file
`Product`, `Order`, `Customer`, `Review`, `OrderItem` and their related enums are all defined at the bottom of their feature's `.cshtml.cs` file — **not** in a separate `Models/` folder. Keep them there.

### EF Core context
`ApplicationDbContext` inherits from `IdentityDbContext<ApplicationUser>`. When adding new entities, configure them in `OnModelCreating` inside the existing entity blocks.

### pgvector
- `UseVector()` is wired into the Npgsql options in `Program.cs` — do not remove it.
- `using Pgvector.EntityFrameworkCore;` is **required** in any file that calls `.CosineDistance()`. Without it, the query compiles but throws at runtime.
- The `DescriptionEmbedding` column is `vector(768)` (Ollama `nomic-embed-text` dimensions).
- The HNSW index uses `vector_cosine_ops` to match the `<=>` operator used in queries.

### Migrations
- Always pass `--context ApplicationDbContext` — the solution has a second Identity context.
- EF Core does not scaffold HNSW indexes. Hand-append the `CREATE INDEX ... USING hnsw` SQL in the `Up` method and `DROP INDEX IF EXISTS` in `Down`.
- Custom PostgreSQL functions (e.g. `remove_hyphens`) live in a dedicated migration and are hand-written with `migrationBuilder.Sql(...)`.

### Ollama embedding service
`OllamaEmbeddingService` takes a plain `HttpClient` (injected via `AddHttpClient<IEmbeddingService, OllamaEmbeddingService>()`) and reads `Ollama:BaseUrl` from configuration. In tests, pass a fake `HttpMessageHandler` directly — do not mock `HttpClient` itself.

### docker-compose
- `postgres` uses `pgvector/pgvector:pg16` (not plain `postgres:16`) so the `vector` extension is available.
- `ollama-pull` is a one-shot service that pulls `nomic-embed-text` after `ollama` is healthy. `app` waits for `ollama-pull` to complete before starting.

## Common tasks

### Add a new feature page
1. Create `Pages/Features/<Feature>/MyPage.cshtml` and `MyPage.cshtml.cs`.
2. Any new model classes go at the bottom of `MyPage.cshtml.cs`.
3. Register any new services in `Program.cs`.

### Add a new database column
1. Update the model class in the relevant `.cshtml.cs`.
2. Add EF configuration in `ApplicationDbContext.OnModelCreating`.
3. `dotnet ef migrations add <Name> --context ApplicationDbContext`.
4. Review the generated migration SQL before applying.

### Backfill embeddings
After adding or seeding new products, hit `GET /Features/Admin/BackfillEmbeddings` (shows count), then `POST` to generate missing embeddings via Ollama.

## Spec-driven development workflow

Every feature has a `SPEC.md` in its feature folder (`Pages/Features/<Feature>/SPEC.md`). This file is the single source of truth for what the feature does — it is edited as the feature evolves, not appended to.

### Starting a new feature or extending an existing one
1. Update (or create) `SPEC.md` with numbered acceptance criteria before writing any code.
2. Write test method stubs — one per criterion — using `[Fact(Skip = "spec: not yet implemented")]` so the spec is immediately visible in the test runner.
3. Implement the feature, removing `Skip` from each test as its criterion is satisfied.
4. All tests must pass before the feature is considered done.

### Test method naming
Use `[Subject]_[Condition]_[ExpectedOutcome]`, for example:
- `AddItem_ProductNotFound_ReturnsEmptyList`
- `SubmitOrder_MissingPhoneNumber_ReturnsNeedsCustomerInfo`

### Test summary block
Every test method should carry a structured summary comment:

```csharp
/// <summary>
/// SCENARIO: one-line description of the situation
/// GIVEN:    preconditions
/// WHEN:     the action taken
/// THEN:     expected outcome
/// SPEC:     #N — Feature Name
/// </summary>
```

### What SPEC.md is not
- Not a changelog — use `CHANGELOG.md` for history.
- Not generated from code — it describes intent, not implementation.

## Changelog

**Always update `CHANGELOG.md` when making code changes.** Add an entry under `## [Unreleased]` describing what was added, changed, or fixed. Follow the existing format — group by `### Added`, `### Changed`, `### Fixed`, and `### Tests` as appropriate.

## Control flow style

Prefer early returns over `if/else` branches. When a condition can exit or short-circuit, invert it and return immediately so the happy path continues at the outer indentation level. Do not add an `else` block after a `return`, `throw`, or `continue`.

```csharp
// Preferred
if (string.IsNullOrWhiteSpace(SearchTerm))
{
    ActiveProducts = await _db.Products.Where(p => p.IsActive == true).ToListAsync();
    return Page();
}

// do work with SearchTerm here...

// Avoid
if (!string.IsNullOrWhiteSpace(SearchTerm))
{
    // ... nested work ...
}
else
{
    ActiveProducts = await _db.Products.Where(p => p.IsActive == true).ToListAsync();
}
```

Apply the same pattern inside loops and nested blocks. The goal is a linear, top-to-bottom read with minimal nesting.

## Error handling style

- Use `ILogger<T>` — never `Console.WriteLine`. Inject `ILogger<T>` via the constructor.
- Wrap only the failure-prone call in try/catch, not the whole method.
- On catch: log the exception, set `TempData["ErrorMessage"]` with a user-friendly message, and return `Page()` or a redirect so the user stays informed.
- Guard against null with early returns (`NotFound()`, `Forbid()`, `return Page()`) rather than relying on downstream null-reference exceptions.
- `TempData["ErrorMessage"]` is rendered globally by `_Layout.cshtml` — no per-page markup is needed.

```csharp
// Preferred
var order = await _db.Orders.SingleOrDefaultAsync(o => o.Id == OrderId);
if (order is null)
    return NotFound();

try
{
    await _db.SaveChangesAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to submit order {OrderId}", OrderId);
    TempData["ErrorMessage"] = "An error occurred. Please try again.";
    return RedirectToPage(...);
}
```

## What to avoid

- Do not add a `Models/` folder — follow the existing co-location pattern.
- Do not use `--no-verify` to skip git hooks.
- Do not run `git push --force` to `main`.
- Do not mock `ApplicationDbContext` in integration tests — use the Testcontainers fixture which provides a real PostgreSQL instance with migrations applied.
- Do not hardcode the Ollama base URL — always read from `Ollama:BaseUrl` in configuration.
