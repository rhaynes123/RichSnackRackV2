# Feature: Orders — Create & Submit

## Overview
The Orders feature allows users to build a basket of snack products and submit it as an order. Items can be added via a full-page GET or via an AJAX post handler. Submission requires the user to be authenticated with a complete profile; otherwise they are redirected to guest checkout.

## Pages
- `GET  /Features/Orders/Create`         — order builder (load existing or start new)
- `POST /Features/Orders/Create?handler=AddItem` — AJAX: add a product to the basket
- `POST /Features/Orders/Create?handler=Submit`  — submit the order
- `GET  /Features/Orders/Confirmation`   — order confirmation display

## Acceptance Criteria

### Loading an Existing Order
1. A `GET` with a valid `orderId` loads the order and its items from the database.
2. A `GET` with an unknown `orderId` returns `404 Not Found`.
3. A `GET` for an order that is not `Pending` returns `400 Bad Request`.

### Starting a New Order
4. A `GET` without an `orderId` generates a new `Guid.CreateVersion7()` order ID.
5. A `GET` without a `ProductId` renders the empty basket page.
6. A `GET` with a `ProductId` adds that product to the new order and renders the basket.

### Adding Items (AJAX — `OnPostAddItem`)
7. Posting a valid `productId` adds the product to the order and returns the updated item list as JSON.
8. Posting a `productId` for a product already in the order increments its quantity by 1.
9. Posting without a `productId` returns `400 Bad Request`.
10. Adding an item to an order that does not yet exist in the database creates the order record.
11. An internal error during add returns `500` with an `{ error: "..." }` JSON body.

### Adding Items (Command — `AddItemToOrderCommand`)
12. Adding a product that does not exist returns an empty list and logs a warning.
13. Adding to a submitted order returns the existing items unchanged (no-op).

### Submitting an Order (`OnPostSubmit`)
14. Submitting with a valid authenticated user (email + phone number present) sets `Status = Submitted` and redirects to the Confirmation page.
15. Submitting when the user lacks a phone number or email redirects to `/Features/Customers/Create` with the order ID.
16. Submitting an order that does not exist returns `404 Not Found`.
17. Submitting an order that is not `Pending` returns `400 Bad Request`.
18. A database failure during submit redirects back to the order page with `TempData["ErrorMessage"]` set.

### Confirmation
19. `GET /Features/Orders/Confirmation` with a valid `orderId` displays the order ID and status.
20. `GET /Features/Orders/Confirmation` with an unknown `orderId` returns `404 Not Found`.

### Cancelling an Order (`OnPostCancel`)
21. A `Pending` order can be cancelled; its status is set to `Cancelled` and the user is redirected to the Confirmation page.
22. Cancelling an order that does not exist returns `404 Not Found`.
23. Cancelling an order that is not `Pending` (e.g. `Submitted`, `Completed`, `Cancelled`) returns `400 Bad Request`.
24. A database failure during cancellation redirects back to the order page with `TempData["ErrorMessage"]` set.

## Key Types
- `Order` — `Id`, `Customer`, `OrderItems`, `Status`
- `OrderItem` — `ProductId`, `ProductName`, `Price`, `Quantity`
- `OrderStatus` — `Pending`, `Submitted`, `Completed`, `Cancelled`
- `AddItemToOrder` — request record: `ProductId`, `OrderId`
- `SubmitOrderResult` — `Outcome`, `OrderId`, `Error`
- `SubmitOutcome` — `Succeeded`, `NeedsCustomerInfo`, `NotFound`, `InvalidStatus`, `Failed`
- `CancelOrderResult` — `Outcome`, `OrderId`, `Error`
- `CancelOutcome` — `Succeeded`, `NotFound`, `InvalidStatus`, `Failed`

## Constraints
- Only `Pending` orders can have items added, be submitted, or be cancelled.
- The active product list shown on the order page is sorted by `Name` ascending.
