# Feature: Customers — Guest Checkout

## Overview
When an authenticated user submits an order but their profile is missing contact details (email or phone), they are redirected here to supply that information. Guest users who are not logged in also land here to provide their details before an order is finalised.

## Pages
- `GET  /Features/Customers/Create` — display current customer details for the order
- `POST /Features/Customers/Create` — save customer details and submit the order

## Acceptance Criteria

### Loading the Form (GET)
1. The page loads and displays the existing customer record associated with the given `orderId`.
2. If no `orderId` is provided or the order has no customer, the form renders empty.

### Saving Customer Details (POST)
3. Submitting with all three fields (name, email, phone number) saves the details to the customer record and sets the order status to `Submitted`.
4. After a successful save, the user is redirected to `/Features/Orders/Confirmation` with the `orderId`.
5. Submitting with any field missing (name, email, or phone number) re-renders the form without saving.
6. If `OrderId` is null after a successful save, the user is redirected to the index page.
7. A database failure during save re-renders the form with `TempData["ErrorMessage"]` set.

## Key Types
- `Customer` — `Id`, `Name`, `Email`, `PhoneNumber`, `UserId`, `CustomerTypeId`
- `CustomerRequest` — form input record: `Name`, `Email`, `PhoneNumber` (all `StringLength(800)`)
- `CustomerType` — `Guest`, `Registered`

## Constraints
- All three contact fields (name, email, phone number) are required before the order can be saved.
- Field lengths are capped at 800 characters each.
