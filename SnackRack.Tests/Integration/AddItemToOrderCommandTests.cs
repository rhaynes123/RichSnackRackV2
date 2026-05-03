using Microsoft.Extensions.Logging.Abstractions;
using SnackRack.Pages.Features.Customers;
using SnackRack.Pages.Features.Orders;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Tests.Integration;

[Collection("Database")]
public class AddItemToOrderCommandTests(TestDatabaseFixture fixture)
{
    private readonly TestDatabaseFixture _fixture = fixture;

    private AddItemToOrderCommand BuildCommand() =>
        new(_fixture.DbContext, NullLogger<AddItemToOrderCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_NewOrder_AddsItemWithQuantityOne()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Chips {Guid.NewGuid()}",
            Description = "Crunchy chips",
            Price = 1.99m,
            IsActive = true
        };
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var orderId = Guid.CreateVersion7();

        // Act
        var items = await BuildCommand().ExecuteAsync(orderId, product.Id, null);

        // Assert
        Assert.Single(items);
        Assert.Equal(product.Id, items[0].ProductId);
        Assert.Equal(1, items[0].Quantity);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingItemInOrder_IncrementsQuantity()
    {
        // Arrange
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Pretzels {Guid.NewGuid()}",
            Description = "Salted pretzels",
            Price = 2.49m,
            IsActive = true
        };
        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var orderId = Guid.CreateVersion7();
        var command = BuildCommand();

        // Act — add the same product twice
        await command.ExecuteAsync(orderId, product.Id, null);
        _fixture.DbContext.ChangeTracker.Clear();

        var items = await command.ExecuteAsync(orderId, product.Id, null);

        // Assert
        Assert.Single(items);
        Assert.Equal(2, items[0].Quantity);
    }

    [Fact]
    public async Task ExecuteAsync_TwoDistinctProducts_CreatesTwoLineItems()
    {
        // Arrange
        var product1 = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Granola Bar {Guid.NewGuid()}",
            Description = "Oaty bar",
            Price = 1.49m,
            IsActive = true
        };
        var product2 = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Trail Mix {Guid.NewGuid()}",
            Description = "Nutty mix",
            Price = 3.29m,
            IsActive = true
        };
        _fixture.DbContext.Products.AddRange(product1, product2);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var orderId = Guid.CreateVersion7();
        var command = BuildCommand();

        // Act
        await command.ExecuteAsync(orderId, product1.Id, null);
        _fixture.DbContext.ChangeTracker.Clear();
        var items = await command.ExecuteAsync(orderId, product2.Id, null);

        // Assert
        Assert.Equal(2, items.Count);
        Assert.Contains(items, i => i.ProductId == product1.Id);
        Assert.Contains(items, i => i.ProductId == product2.Id);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownProductId_ReturnsEmptyList()
    {
        // Arrange
        var orderId = Guid.CreateVersion7();

        // Act
        var items = await BuildCommand().ExecuteAsync(orderId, Guid.NewGuid(), null);

        // Assert
        Assert.Empty(items);
    }
}
