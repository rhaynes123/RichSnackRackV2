# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [Unreleased]

## [0.6.0] — 2026-04-26

### Added
- **Search audit log** — every search on the Menu page writes a row to the new `search_logs` table recording the term, search path taken (`Like`, `Semantic`, `SemanticUnavailable`), result counts, top cosine distance, matched product IDs + distances (stored as `jsonb`), timestamp, and user ID if authenticated.
- **Search Logs admin page** at `/Features/Admin/SearchLogs` — paginated table of all search events with a "zero-result searches only" toggle for identifying gaps in product coverage or embedding quality.
- **Admin index page** at `/Features/Admin` — lists all admin sub-pages with descriptions so they are discoverable without knowing the URLs. Link added to the main nav bar.
- `AddSearchLogs` migration — creates the `search_logs` table with indexes on `SearchedAt` and `SearchType`.

### Changed
- **Graceful error handling** across all pages — errors are now surfaced to the user via a Bootstrap danger alert rendered globally in `_Layout.cshtml` using `TempData["ErrorMessage"]`.
  - `Menu.OnGet` — Ollama / semantic search failure degrades gracefully (friendly message, empty results) instead of returning a 500.
  - `BackfillEmbeddings.OnPost` — error stays on the backfill page with a message instead of silently redirecting to `/Index`. Replaced `Console.WriteLine` with `ILogger`.
  - `Orders/Create.OnPostSubmit` — `SingleAsync` replaced with `SingleOrDefaultAsync` + `NotFound()` guard; `SaveChangesAsync` wrapped in try/catch. `OnPostAddItem` returns `StatusCode(500)` JSON on DB failure.
  - `Reviews/Create.OnPostAsync` — null guard on `currentUser` returns `Forbid()` if the session expired between GET and POST; `SaveChangesAsync` wrapped in try/catch.
  - `Customers/Create.OnPostAsync` — `SaveChangesAsync` wrapped in try/catch.
- **Control flow** in `Menu.OnGet` refactored to use early returns — no `else` blocks, linear top-to-bottom reading.
- `AGENTS.md` and `CONTRIBUTING.md` updated with explicit **control flow** and **error handling** style guidelines.

### Tests
- `SearchLogTests` — 14 integration tests covering log writes for all three search paths, jsonb round-trip for `Results`, admin page ordering, `ZeroResultsOnly` filter, and `TotalCount` accuracy.
- `SearchLogModelTests` — 10 unit tests covering `SearchLog` defaults, `SearchResultItem` construction, and `SearchType` enum shape.

## [0.5.1] — 2026-04-25

### Added
- **Two-step product search** on the Menu page — LIKE search runs first; semantic search is used as a fallback only when LIKE returns no results.
  - Avoids calling Ollama for straightforward keyword queries.
  - Cosine distance threshold of `0.3` applied to semantic results to filter out low-relevance matches.
- **Logging** added to `Menu.OnGet()` to trace which search path was taken, embedding dimensions, per-product distance scores, and result counts.
- **Guard clause** in `OllamaEmbeddingService.GetEmbeddingAsync` — throws `ArgumentException` immediately on null or whitespace input before any HTTP call is made.

### Tests
- `MenuSearchTests` integration tests cover all four search scenarios: LIKE hit, semantic fallback, no results from either path, and empty search term.
- `OllamaEmbeddingServiceTests` — three new theory cases asserting `ArgumentException` is thrown for `null`, `""`, and `"   "` inputs.

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
