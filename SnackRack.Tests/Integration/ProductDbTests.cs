using Microsoft.EntityFrameworkCore;
using Pgvector;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Tests.Integration;

[Collection("Database")]
public class ProductDbTests(TestDatabaseFixture fixture)
{
    private readonly TestDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Products_SeedData_ShouldBePresent()
    {
        var count = await _fixture.DbContext.Products.CountAsync();
        Assert.True(count > 0, "Expected seeded products from migrations.");
    }

    [Fact]
    public async Task Product_CanBeCreatedAndQueried()
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Test Pretzel {Guid.NewGuid()}",
            Description = "A crunchy twisted snack",
            Price = 2.99m,
            IsActive = true
        };

        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var found = await _fixture.DbContext.Products.FindAsync(product.Id);
        Assert.NotNull(found);
        Assert.Equal(product.Name, found.Name);
        Assert.Equal(2.99m, found.Price);
    }

    [Fact]
    public async Task Product_CanStoreAndRetrieveEmbedding()
    {
        var floats = Enumerable.Range(0, 768).Select(i => (float)i / 768f).ToArray();
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Embedding Snack {Guid.NewGuid()}",
            Description = "Testing vector storage",
            Price = 1.49m,
            IsActive = true,
            DescriptionEmbedding = new Vector(floats)
        };

        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var found = await _fixture.DbContext.Products.FindAsync(product.Id);
        Assert.NotNull(found?.DescriptionEmbedding);
        Assert.Equal(768, found.DescriptionEmbedding!.ToArray().Length);
    }

    [Fact]
    public async Task Product_NullEmbedding_IsStoredAndRetrievedAsNull()
    {
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"No Embedding {Guid.NewGuid()}",
            Description = "No embedding yet",
            Price = 0.99m,
            IsActive = true
        };

        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var found = await _fixture.DbContext.Products.FindAsync(product.Id);
        Assert.NotNull(found);
        Assert.Null(found.DescriptionEmbedding);
    }

    [Fact]
    public async Task Products_ActiveFilter_ExcludesInactiveProducts()
    {
        var inactive = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Discontinued {Guid.NewGuid()}",
            Description = "Not for sale",
            Price = 5.00m,
            IsActive = false
        };

        _fixture.DbContext.Products.Add(inactive);
        await _fixture.DbContext.SaveChangesAsync();

        var activeProducts = await _fixture.DbContext.Products
            .Where(p => p.IsActive == true)
            .ToListAsync();

        Assert.DoesNotContain(activeProducts, p => p.Id == inactive.Id);
    }
}
