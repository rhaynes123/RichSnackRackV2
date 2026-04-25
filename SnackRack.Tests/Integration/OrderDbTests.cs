using Microsoft.EntityFrameworkCore;
using SnackRack.Pages.Features.Customers;
using SnackRack.Pages.Features.Orders;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Tests.Integration;

[Collection("Database")]
public class OrderDbTests(TestDatabaseFixture fixture)
{
    private readonly TestDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Order_CanBeCreated_WithPendingStatus()
    {
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = "Test Customer",
            Email = $"test{Guid.NewGuid()}@example.com",
            PhoneNumber = "555-0100"
        };

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = customer,
            Status = OrderStatus.Pending
        };

        _fixture.DbContext.Orders.Add(order);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var found = await _fixture.DbContext.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        Assert.NotNull(found);
        Assert.Equal(OrderStatus.Pending, found.Status);
        Assert.Equal("Test Customer", found.Customer.Name);
    }

    [Fact]
    public async Task Order_StatusCanTransition_ToSubmitted()
    {
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = "Submit Customer",
            Email = $"submit{Guid.NewGuid()}@example.com",
            PhoneNumber = "555-0200"
        };

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = customer,
            Status = OrderStatus.Pending
        };

        _fixture.DbContext.Orders.Add(order);
        await _fixture.DbContext.SaveChangesAsync();

        order.Status = OrderStatus.Submitted;
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var updated = await _fixture.DbContext.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Submitted, updated!.Status);
    }

    [Fact]
    public async Task Order_CanHaveMultipleOrderItems()
    {
        var product1 = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Chips {Guid.NewGuid()}",
            Description = "Salty chips",
            Price = 1.99m,
            IsActive = true
        };
        var product2 = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Pretzels {Guid.NewGuid()}",
            Description = "Crunchy pretzels",
            Price = 2.49m,
            IsActive = true
        };

        _fixture.DbContext.Products.AddRange(product1, product2);

        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = "Multi Item Customer",
            Email = $"multi{Guid.NewGuid()}@example.com",
            PhoneNumber = "555-0300"
        };

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            Customer = customer,
            Status = OrderStatus.Pending,
            OrderItems = new List<OrderItem>
            {
                new() { ProductId = product1.Id, ProductName = product1.Name, Price = product1.Price, Quantity = 2 },
                new() { ProductId = product2.Id, ProductName = product2.Name, Price = product2.Price, Quantity = 1 }
            }
        };

        _fixture.DbContext.Orders.Add(order);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var found = await _fixture.DbContext.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        Assert.NotNull(found);
        Assert.Equal(2, found.OrderItems.Count);
        Assert.Contains(found.OrderItems, i => i.ProductId == product1.Id && i.Quantity == 2);
        Assert.Contains(found.OrderItems, i => i.ProductId == product2.Id && i.Quantity == 1);
    }
}
