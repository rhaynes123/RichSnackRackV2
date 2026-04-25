# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

## [0.5.0] — 2026-04-23

### Added
- **Semantic product search** powered by pgvector and Ollama (`nomic-embed-text`, 768 dims).
  - `IEmbeddingService` interface and `OllamaEmbeddingService` implementation (`Services/`).
  - `DescriptionEmbedding vector(768)` column on the `products` table.
  - HNSW index (`vector_cosine_ops`) for approximate nearest-neighbour queries.
  - Search form on the Menu page; results ordered by cosine distance when a query is present.
  - Admin backfill page at `/Features/Admin/BackfillEmbeddings` to generate embeddings for existing products.
  - Migration `AddVectorExtension` — installs the `vector` PostgreSQL extension, adds the column, and creates the index.
- **Docker Compose** (`docker-compose.yml`) to run the full stack (app, PostgreSQL with pgvector, Ollama).
  - `ollama-pull` one-shot service automatically pulls `nomic-embed-text` on first start.
  - `pgvector/pgvector:pg16` replaces plain `postgres:16` so the extension is available.
- `Ollama:BaseUrl` added to `appsettings.Development.json`.
- `Pgvector.EntityFrameworkCore` (v0.3.0) NuGet package.

## [0.4.0] — 2026-01-01

### Added
- `remove_hyphens` PostgreSQL function via migration `AddRemoveHyphensFunction`.
- Product search in the Orders page uses `remove_hyphens()` for hyphen-insensitive name matching.
- Additional product seed data (`AddNewProducts` migration).

## [0.3.0] — 2024-12-29

### Added
- **Reviews** feature — authenticated users can leave a title + comment per product.
  - `reviews` table with foreign keys to `products` and `AspNetUsers`.
  - `AllReviews` page with product-name search (`ILike`) and "only my reviews" filter.
  - `AddReviews` and `AddReviewsTitle` migrations.

## [0.2.0] — 2024-12-28

### Added
- **ASP.NET Core Identity** — scaffolded Login, Register, and Logout pages.
  - `ApplicationUser` extends `IdentityUser` with a `Reviews` navigation property.
  - `IdetityTables` migration creates the standard Identity schema.
- Registered users are linked to their `Customer` record via `UserId`.
  - `AddUserIdToCustomer` and `AddCustomerTypeToCustomers` migrations.
- `CustomerType` enum (`Guest`, `Registered`); authenticated checkout populates customer details from the Identity profile.

## [0.1.0] — 2024-12-20

### Added
- Initial project scaffolding — ASP.NET Core 10 Razor Pages with Npgsql / Entity Framework Core.
- `products`, `customers`, `orders`, and `order_items` tables (`CreateProducts` migration).
- Menu page listing active products.
- Order creation page with AJAX add-item handler and order status flow (`Pending → Submitted`).
- Guest checkout page captures name, email, and phone number.
- Order confirmation page.
- `OrderStatus` migration adds the status column.
- Product seed data (`SeedProducts` migration).
