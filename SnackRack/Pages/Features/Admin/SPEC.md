# Feature: Admin Tools

## Overview
The Admin area provides operational tooling for maintaining the SnackRack catalogue and monitoring search behaviour. It is not publicly accessible and assumes an admin-level operator.

## Pages
- `GET  /Features/Admin/Index`              — admin dashboard / landing page
- `GET  /Features/Admin/BackfillEmbeddings` — show count of products needing embeddings
- `POST /Features/Admin/BackfillEmbeddings` — generate and save missing embeddings
- `GET  /Features/Admin/SearchLogs`         — paginated, filterable search audit log

## Acceptance Criteria

### BackfillEmbeddings (GET)
1. The page displays the count of active products that currently have no `DescriptionEmbedding`.

### BackfillEmbeddings (POST)
2. Only active products with a `null` embedding are processed; already-embedded products are skipped.
3. For each qualifying product, the embedding service is called with the product's description.
4. A successfully generated embedding is set on the product and counted in `ProcessedCount`.
5. A failed embedding logs a warning and increments `FailedCount`; processing continues for remaining products.
6. All successfully generated embeddings are saved in a single `SaveChangesAsync` call after the loop.
7. A save failure sets `TempData["ErrorMessage"]` without throwing.
8. If any products failed embedding, `TempData["WarningMessage"]` is set with the failure count.
9. After processing, `ProductsWithoutEmbedding` is refreshed to reflect the current database state.

### SearchLogs (GET)
10. Logs are displayed in descending order by `SearchedAt` (most recent first).
11. Page size is 25 entries per page.
12. `CurrentPage` defaults to 1; subsequent pages skip the correct number of records.
13. `TotalPages` is calculated correctly from `TotalCount` and page size, rounding up.
14. Enabling `ZeroResultsOnly` filters to logs where both `LikeResultCount` and `SemanticResultCount` are 0, excluding `SemanticUnavailable` entries.

## Key Types
- `SearchLog` — `SearchTerm`, `SearchType`, `LikeResultCount`, `SemanticResultCount`, `TopDistance`, `Results`, `SearchedAt`, `UserId`
- `SearchType` — `Like`, `Semantic`, `SemanticUnavailable`

## Constraints
- Page size is fixed at 25 and is not user-configurable.
- The embedding service is called once per product, sequentially (not in parallel).
