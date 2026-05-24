# SnackRack

A learning project exploring ASP.NET Core Razor Pages with PostgreSQL, Entity Framework Core, and pgvector-powered semantic search.

## Features

- **Product menu** — browse active snack products with prices and descriptions
- **Semantic search** — find products by describing what you want (e.g. *"something salty and crunchy"*), powered by [Ollama](https://ollama.ai) embeddings and [pgvector](https://github.com/pgvector/pgvector)
- **Order management** — add products to an order via AJAX, submit or cancel orders
- **Order history** — authenticated users can view their past submitted and completed orders, with line items and links to leave reviews
- **Guest & registered checkout** — guests fill in their contact details; authenticated users submit directly
- **Reviews** — authenticated users can leave and browse product reviews filtered by product name
- **Admin sales view** — paginated table of all orders with line items, totals, and any associated reviews
- **Admin backfill** — generate missing pgvector embeddings for existing products at `/Features/Admin/BackfillEmbeddings`

## Tech stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 10 Razor Pages |
| Database | PostgreSQL 16 + pgvector |
| ORM | Entity Framework Core 10 (Npgsql) |
| Auth | ASP.NET Core Identity |
| Embeddings | Ollama `nomic-embed-text` (768 dims) |
| Containers | Docker / docker compose |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 16 with pgvector — or use `docker compose up`
- [Ollama](https://ollama.ai) with the `nomic-embed-text` model pulled

## Getting started

### With Docker Compose (recommended)

```bash
docker compose up
```

This starts PostgreSQL (with pgvector), Ollama, pulls `nomic-embed-text` automatically, and runs the app on `http://localhost:8080`.

### Locally

```bash
# 1. Start Ollama and pull the model
ollama serve
ollama pull nomic-embed-text

# 2. Apply migrations
dotnet ef database update --context ApplicationDbContext

# 3. Run the app
dotnet run
```

Then navigate to `/Features/Admin/BackfillEmbeddings` to generate embeddings for the seeded products, and try semantic search on the Menu page.

## Project structure

```
SnackRack/
├── Data/                    # EF Core DbContext, ApplicationUser, DB function extensions
├── Migrations/              # EF Core migrations
├── Pages/
│   ├── Features/
│   │   ├── Admin/           # Index, Sales (paginated order view), BackfillEmbeddings, SearchLogs
│   │   ├── Customers/       # Guest checkout customer detail capture
│   │   ├── Orders/          # Create (AJAX add-item), History, Confirmation; OrderHistoryQuery
│   │   ├── Products/        # Menu page with semantic search
│   │   └── Reviews/         # Create and browse product reviews
│   └── Shared/              # _Layout, _OrdersTabNav, _LoginPartial partials
├── Services/                # IEmbeddingService + OllamaEmbeddingService
├── Areas/Identity/          # ASP.NET Identity scaffolded pages
└── wwwroot/                 # Static assets (Bootstrap, jQuery)

SnackRack.Tests/             # Integration tests (Testcontainers PostgreSQL)
SnackRack.Tests.UI/          # Playwright UI tests (headed demo excluded by default)
SnackRack.Benchmarks/        # BenchmarkDotNet benchmarks
```

## Running tests

```bash
# Integration tests (requires Docker for Testcontainers)
dotnet test SnackRack.Tests/SnackRack.Tests.csproj

# Playwright UI tests (headless, requires Docker)
dotnet test SnackRack.Tests.UI/SnackRack.Tests.UI.csproj

# Headed demo — opens a visible browser with slow-mo so you can watch
dotnet test SnackRack.Tests.UI/SnackRack.Tests.UI.csproj --filter "Category=Demo"
```

Integration and UI tests spin up a `pgvector/pgvector:pg16` container via Testcontainers.
The headed demo tests are excluded from normal runs by `default.runsettings` and must be targeted explicitly.
