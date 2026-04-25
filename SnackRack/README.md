# SnackRack

A learning project exploring ASP.NET Core Razor Pages with PostgreSQL, Entity Framework Core, and pgvector-powered semantic search.

## Features

- **Product menu** — browse active snack products with prices and descriptions
- **Semantic search** — find products by describing what you want (e.g. *"something salty and crunchy"*), powered by [Ollama](https://ollama.ai) embeddings and [pgvector](https://github.com/pgvector/pgvector)
- **Order management** — add products to a cart and submit orders
- **Guest & registered checkout** — guests fill in their contact details; authenticated users submit directly
- **Reviews** — authenticated users can leave and browse product reviews filtered by product name
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
├── Pages/Features/
│   ├── Admin/               # BackfillEmbeddings admin page
│   ├── Customers/           # Guest checkout customer detail capture
│   ├── Orders/              # Order creation (with AJAX add-item) and confirmation
│   ├── Products/            # Menu page with semantic search
│   └── Reviews/             # Create and browse product reviews
├── Services/                # IEmbeddingService + OllamaEmbeddingService
├── Areas/Identity/          # ASP.NET Identity scaffolded pages
└── wwwroot/                 # Static assets (Bootstrap, jQuery)
```

## Running tests

```bash
dotnet test
```

Integration tests require Docker — they spin up a `pgvector/pgvector:pg16` container via Testcontainers.
