# Feature: Products — Menu & Search

## Overview
The Menu page is the primary product discovery surface. It displays all active products and supports two-tier search: a fast LIKE match first, falling back to semantic (vector) search when no keyword results are found. Every search is logged for admin analysis.

## Pages
- `GET /Features/Products/Menu` — product listing with optional search

## Acceptance Criteria

### Listing
1. All active products are returned when no search term is provided.
2. Inactive products are never shown, regardless of search term.

### LIKE Search
3. A search term that matches a product name or description (case-insensitive substring) returns those products.
4. When LIKE returns at least one result, the embedding service is never called.
5. A search log entry with `SearchType.Like` is written when LIKE returns results.

### Semantic Search Fallback
6. When LIKE returns no results, the embedding service is called to generate a query vector.
7. Products with a `DescriptionEmbedding` within cosine distance `0.3` of the query vector are returned, ordered by ascending distance.
8. A maximum of 10 products are returned from semantic search.
9. Products without an embedding are excluded from semantic results.
10. A search log entry with `SearchType.Semantic` is written when semantic search runs.

### Semantic Unavailability
11. If the embedding service fails, an empty product list is returned along with a user-facing error message.
12. A search log entry with `SearchType.SemanticUnavailable` is written when the embedding service is unavailable.
13. The error message is surfaced to the user via `TempData["ErrorMessage"]`.

### Search Logging
14. All searches (LIKE, semantic, and unavailable) produce exactly one `SearchLog` record.
15. A failure to write the search log does not surface as an error to the user.

## Key Types
- `Product` — `Id`, `Name`, `Description`, `Price`, `IsActive`, `DescriptionEmbedding` (vector 768)
- `ProductSearchResult` — `Products`, `ErrorMessage`
- `SearchLog` — `SearchTerm`, `SearchType`, `LikeResultCount`, `SemanticResultCount`, `TopDistance`, `Results`, `UserId`
- `SearchType` — `Like`, `Semantic`, `SemanticUnavailable`

## Constraints
- Cosine distance threshold: `0.3` (lower = more similar).
- Embedding dimensions: `768` (Ollama `nomic-embed-text`).
- `using Pgvector.EntityFrameworkCore;` is required in any file calling `.CosineDistance()`.
