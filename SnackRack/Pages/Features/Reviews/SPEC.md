# Feature: Reviews — Create & Browse

## Overview
Authenticated users can leave a titled review comment on any product. All users (including unauthenticated) can browse the full list of reviews, with optional filtering by product name or by their own reviews.

## Pages
- `GET  /Features/Reviews/Create`   — review form for a specific product
- `POST /Features/Reviews/Create`   — save the review
- `GET  /Features/Reviews/AllReviews` — paginated list of all reviews with filtering

## Acceptance Criteria

### Creating a Review (GET)
1. An unauthenticated user is forbidden (`403`) and cannot access the form.
2. The form pre-populates the product name when the `productId` resolves to a known product.
3. If the `productId` does not resolve to a known product, the form renders without a product name (no error).

### Saving a Review (POST)
4. A valid submission by an authenticated user for a known product saves the review and redirects to `./MyReviews`.
5. Submitting for an unknown product re-renders the form without saving.
6. Submitting while unauthenticated returns `403 Forbidden`.
7. A database failure during save redirects back to the form with `TempData["ErrorMessage"]` set.

### Browsing Reviews (AllReviews)
8. All reviews are returned when no filters are applied, each including their associated product and user.
9. Providing a `SearchTerm` filters reviews to those whose product name matches the term (case-insensitive LIKE).
10. Enabling `OnlyMyReviews` filters results to reviews authored by the currently authenticated user.
11. `OnlyMyReviews` with an unauthenticated user returns an empty list (no error).
12. Both filters can be applied simultaneously.

## Key Types
- `Review` — `Id`, `Product`, `Title`, `Comment`, `User`
- `ReviewModel` — form input record: `Title`, `ReviewText`, `ProductName`

## Constraints
- Review creation requires authentication; browsing does not.
- `Title` and `Comment` are each capped at 1000 characters.
