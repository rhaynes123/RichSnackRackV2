using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pgvector;
using SnackRack.Pages.Features.Products;
using SnackRack.Services;

namespace SnackRack.Tests.Integration;

/// <summary>
/// Integration tests for the two-step Menu page search:
///   1. LIKE search against name/description (fast, exact substring match)
///   2. Semantic/vector search fallback when LIKE returns nothing
///
/// The PostgreSQL container (via TestDatabaseFixture) runs both queries for real.
/// The embedding service is mocked so tests don't need Ollama running.
/// </summary>
[Collection("Database")]
public class MenuSearchTests(TestDatabaseFixture fixture)
{
    private readonly TestDatabaseFixture _fixture = fixture;

    // Build a 768-float vector that "points" in one dimension only.
    // Two vectors pointing in the same dimension → cosine distance 0.0 (identical).
    // Two vectors pointing in different dimensions → cosine distance 1.0 (unrelated).
    private static float[] MakeDirectionalEmbedding(int hotDimension)
    {
        var floats = new float[768];
        floats[hotDimension] = 1.0f;
        return floats;
    }

    /// <summary>
    /// SCENARIO: Search term matches a product name/description directly.
    ///
    /// Expected: LIKE search returns the match and the embedding service is
    /// never called — no need to hit Ollama for a straightforward keyword hit.
    /// </summary>
    [Fact]
    public async Task Search_LikeMatch_ReturnsProductWithoutCallingEmbeddingService()
    {
        // --- Arrange ---
        var uid = Guid.NewGuid();
        var chipsProduct = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Classic Chips {uid}",
            Description = "Lightly salted potato chips",
            Price = 1.99m,
            IsActive = true
        };

        _fixture.DbContext.Products.Add(chipsProduct);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Strict mock — any call to GetEmbeddingAsync will throw, proving it is never reached.
        var mockEmbeddings = new Mock<IEmbeddingService>(MockBehavior.Strict);

        var page = new Menu(_fixture.DbContext, mockEmbeddings.Object, NullLogger<Menu>.Instance)
        {
            SearchTerm = $"Classic Chips {uid}"
        };

        // --- Act ---
        await page.OnGet();

        // --- Assert ---
        Assert.Contains(page.ActiveProducts, p => p.Id == chipsProduct.Id);
        mockEmbeddings.VerifyNoOtherCalls();
    }

    /// <summary>
    /// SCENARIO: Search term does not match any product name/description, but
    /// one product has a semantically similar embedding.
    ///
    /// Expected: LIKE returns nothing, semantic search runs as fallback and
    /// returns the product whose embedding is closest to the query.
    /// </summary>
    [Fact]
    public async Task Search_NoLikeMatch_FallsBackToSemanticSearch_ReturnsSimilarProduct()
    {
        // --- Arrange ---
        // Both products have unique nonsense names so LIKE will never match the search term.
        var similarProduct = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"NOMATCH_SIMILAR_{Guid.NewGuid()}",
            Description = $"NOMATCH_DESC_{Guid.NewGuid()}",
            Price = 2.99m,
            IsActive = true,
            DescriptionEmbedding = new Vector(MakeDirectionalEmbedding(hotDimension: 0))
        };

        var dissimilarProduct = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"NOMATCH_DISSIMILAR_{Guid.NewGuid()}",
            Description = $"NOMATCH_DESC_{Guid.NewGuid()}",
            Price = 2.99m,
            IsActive = true,
            DescriptionEmbedding = new Vector(MakeDirectionalEmbedding(hotDimension: 1))
        };

        _fixture.DbContext.Products.AddRange(similarProduct, dissimilarProduct);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Query embedding also points in dimension 0 → cosine distance 0.0 to similarProduct,
        // 1.0 to dissimilarProduct. Only similarProduct is within the 0.3 threshold.
        var searchTerm = $"SEMANTIC_QUERY_{Guid.NewGuid()}";
        var mockEmbeddings = new Mock<IEmbeddingService>();
        mockEmbeddings
            .Setup(s => s.GetEmbeddingAsync(searchTerm))
            .ReturnsAsync(Result<float[]>.Success(MakeDirectionalEmbedding(hotDimension: 0)));

        var page = new Menu(_fixture.DbContext, mockEmbeddings.Object, NullLogger<Menu>.Instance)
        {
            SearchTerm = searchTerm
        };

        // --- Act ---
        await page.OnGet();

        // --- Assert ---
        Assert.Contains(page.ActiveProducts, p => p.Id == similarProduct.Id);
        Assert.DoesNotContain(page.ActiveProducts, p => p.Id == dissimilarProduct.Id);
        mockEmbeddings.Verify(s => s.GetEmbeddingAsync(searchTerm), Times.Once);
    }

    /// <summary>
    /// SCENARIO: Search term does not match by LIKE and no products have embeddings.
    ///
    /// Expected: both paths return nothing — empty list, not the full catalogue.
    /// </summary>
    [Fact]
    public async Task Search_NoLikeMatch_NoEmbeddings_ReturnsEmptyList()
    {
        // --- Arrange ---
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"NOMATCH_{Guid.NewGuid()}",
            Description = $"NOMATCH_{Guid.NewGuid()}",
            Price = 1.29m,
            IsActive = true
            // DescriptionEmbedding intentionally null
        };

        _fixture.DbContext.Products.Add(product);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var searchTerm = $"SEMANTIC_QUERY_{Guid.NewGuid()}";
        var mockEmbeddings = new Mock<IEmbeddingService>();
        mockEmbeddings
            .Setup(s => s.GetEmbeddingAsync(searchTerm))
            .ReturnsAsync(Result<float[]>.Success(MakeDirectionalEmbedding(hotDimension: 0)));

        var page = new Menu(_fixture.DbContext, mockEmbeddings.Object, NullLogger<Menu>.Instance)
        {
            SearchTerm = searchTerm
        };

        // --- Act ---
        await page.OnGet();

        // --- Assert ---
        Assert.DoesNotContain(page.ActiveProducts, p => p.Id == product.Id);
    }

    /// <summary>
    /// SCENARIO: No search term provided.
    ///
    /// Expected: full active product list returned without touching the embedding service.
    /// </summary>
    [Fact]
    public async Task Search_WithEmptySearchTerm_ReturnsAllActiveProducts()
    {
        // --- Arrange ---
        var activeProduct = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Pretzels {Guid.NewGuid()}",
            Description = "Salted hard pretzels",
            Price = 1.79m,
            IsActive = true
        };

        var inactiveProduct = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Discontinued Bar {Guid.NewGuid()}",
            Description = "No longer available",
            Price = 3.00m,
            IsActive = false
        };

        _fixture.DbContext.Products.AddRange(activeProduct, inactiveProduct);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var mockEmbeddings = new Mock<IEmbeddingService>(MockBehavior.Strict);

        var page = new Menu(_fixture.DbContext, mockEmbeddings.Object, NullLogger<Menu>.Instance)
        {
            SearchTerm = null
        };

        // --- Act ---
        await page.OnGet();

        // --- Assert ---
        Assert.Contains(page.ActiveProducts, p => p.Id == activeProduct.Id);
        Assert.DoesNotContain(page.ActiveProducts, p => p.Id == inactiveProduct.Id);
        mockEmbeddings.VerifyNoOtherCalls();
    }
}
