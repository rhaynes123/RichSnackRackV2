using Microsoft.Extensions.Logging.Abstractions;
using SnackRack.Data;
using SnackRack.Pages.Features.Customers;
using SnackRack.Pages.Features.Orders;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Tests.Integration;

[Collection("Database")]
public class OrderHistoryQueryTests(TestDatabaseFixture fixture)
{
    [Fact]
    public async Task ExecuteAsync_ReviewedItemHasReviewData_UnreviewedItemHasNulls()
    {
        var userId = Guid.CreateVersion7().ToString();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"hist_{userId}@test.com",
            NormalizedUserName = $"HIST_{userId.ToUpper()}@TEST.COM",
            Email = $"hist_{userId}@test.com",
            NormalizedEmail = $"HIST_{userId.ToUpper()}@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var product1 = new Product { Id = Guid.CreateVersion7(), Name = $"Chips {Guid.NewGuid()}", Description = "Salty", Price = 1.99m, IsActive = true };
        var product2 = new Product { Id = Guid.CreateVersion7(), Name = $"Pretzels {Guid.NewGuid()}", Description = "Crunchy", Price = 2.49m, IsActive = true };

        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = "History User",
            Email = $"hist{Guid.NewGuid()}@example.com",
            UserId = userId
        };

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = customer,
            Status = OrderStatus.Submitted,
            OrderItems =
            [
                new() { ProductId = product1.Id, ProductName = product1.Name, Price = product1.Price, Quantity = 1 },
                new() { ProductId = product2.Id, ProductName = product2.Name, Price = product2.Price, Quantity = 2 }
            ]
        };

        var review = new Review
        {
            Product = product1,
            Title = "Great chips!",
            Comment = "Very crunchy indeed.",
            User = user
        };

        fixture.DbContext.Users.Add(user);
        fixture.DbContext.Products.AddRange(product1, product2);
        fixture.DbContext.Orders.Add(order);
        fixture.DbContext.Reviews.Add(review);
        await fixture.DbContext.SaveChangesAsync();
        fixture.DbContext.ChangeTracker.Clear();

        var query = new OrderHistoryQuery(fixture.DbContext, NullLogger<OrderHistoryQuery>.Instance);
        var rows = await query.ExecuteAsync(userId);

        Assert.Equal(2, rows.Count());

        var reviewedRow = rows.Single(r => r.ProductId == product1.Id);
        Assert.NotNull(reviewedRow.ReviewId);
        Assert.Equal("Great chips!", reviewedRow.ReviewTitle);
        Assert.Equal("Very crunchy indeed.", reviewedRow.ReviewComment);

        var unreviewedRow = rows.Single(r => r.ProductId == product2.Id);
        Assert.Null(unreviewedRow.ReviewId);
        Assert.Null(unreviewedRow.ReviewTitle);
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesPendingAndCancelledOrders()
    {
        var userId = Guid.CreateVersion7().ToString();
        var product = new Product { Id = Guid.CreateVersion7(), Name = $"Gum {Guid.NewGuid()}", Description = "Chewy", Price = 0.99m, IsActive = true };
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = "Filter User",
            Email = $"filter{Guid.NewGuid()}@example.com",
            UserId = userId
        };

        var pendingOrder = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = customer,
            Status = OrderStatus.Pending,
            OrderItems = [new() { ProductId = product.Id, ProductName = product.Name, Price = product.Price, Quantity = 1 }]
        };
        var cancelledOrder = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = customer,
            Status = OrderStatus.Cancelled,
            OrderItems = [new() { ProductId = product.Id, ProductName = product.Name, Price = product.Price, Quantity = 1 }]
        };

        fixture.DbContext.Products.Add(product);
        fixture.DbContext.Orders.AddRange(pendingOrder, cancelledOrder);
        await fixture.DbContext.SaveChangesAsync();
        fixture.DbContext.ChangeTracker.Clear();

        var query = new OrderHistoryQuery(fixture.DbContext, NullLogger<OrderHistoryQuery>.Instance);
        var rows = await query.ExecuteAsync(userId);

        Assert.DoesNotContain(rows, r => r.OrderId == pendingOrder.Id);
        Assert.DoesNotContain(rows, r => r.OrderId == cancelledOrder.Id);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotReturnOtherUsersOrders()
    {
        var userId = Guid.CreateVersion7().ToString();
        var otherUserId = Guid.CreateVersion7().ToString();

        var product = new Product { Id = Guid.CreateVersion7(), Name = $"Cookie {Guid.NewGuid()}", Description = "Sweet", Price = 1.49m, IsActive = true };

        var otherCustomer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = "Other User",
            Email = $"other{Guid.NewGuid()}@example.com",
            UserId = otherUserId
        };

        var otherOrder = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = otherCustomer,
            Status = OrderStatus.Submitted,
            OrderItems = [new() { ProductId = product.Id, ProductName = product.Name, Price = product.Price, Quantity = 1 }]
        };

        fixture.DbContext.Products.Add(product);
        fixture.DbContext.Orders.Add(otherOrder);
        await fixture.DbContext.SaveChangesAsync();
        fixture.DbContext.ChangeTracker.Clear();

        var query = new OrderHistoryQuery(fixture.DbContext, NullLogger<OrderHistoryQuery>.Instance);
        var rows = await query.ExecuteAsync(userId);

        Assert.DoesNotContain(rows, r => r.OrderId == otherOrder.Id);
    }
}
