using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Services;

namespace SnackRack.Pages.Features.Products;

public record ProductSearchResult(List<Product> Products, string? ErrorMessage = null);

public class ProductSearchQuery(ApplicationDbContext db, IEmbeddingService embeddings, ILogger<ProductSearchQuery> logger)
{
    public async Task<ProductSearchResult> ExecuteAsync(string? searchTerm, string? userId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            logger.LogInformation("No search term provided, returning full active product list.");
            var all = await db.Products.Where(p => p.IsActive == true).ToListAsync();
            return new ProductSearchResult(all);
        }

        var term = searchTerm.Trim();
        logger.LogInformation("LIKE search triggered with term: '{SearchTerm}'", term);

        var likeResults = await db.Products
            .Where(p => p.IsActive == true &&
                        (EF.Functions.ILike(p.Name, $"%{term}%") ||
                         EF.Functions.ILike(p.Description, $"%{term}%")))
            .ToListAsync();

        logger.LogInformation("LIKE search returned {Count} product(s).", likeResults.Count);

        if (likeResults.Count > 0)
        {
            await WriteSearchLog(term, SearchType.Like, likeResults.Count, 0, [], userId);
            return new ProductSearchResult(likeResults);
        }

        logger.LogInformation("No LIKE matches, falling back to semantic search.");

        var embeddingResult = await embeddings.GetEmbeddingAsync(term);

        if (!embeddingResult.IsSuccess)
        {
            logger.LogWarning("Semantic search unavailable for term '{SearchTerm}': {Error}", term, embeddingResult.Error);
            await WriteSearchLog(term, SearchType.SemanticUnavailable, 0, 0, [], userId);
            return new ProductSearchResult([], "Semantic search is unavailable. Showing no results for your query.");
        }

        logger.LogInformation("Embedding generated ({Dimensions} dimensions), querying database.", embeddingResult.Value!.Length);

        var queryVector = new Vector(embeddingResult.Value!);
        const double similarityThreshold = 0.3;

        var results = await db.Products
            .Where(p => p.IsActive == true && p.DescriptionEmbedding != null)
            .Select(p => new { Product = p, Distance = p.DescriptionEmbedding!.CosineDistance(queryVector) })
            .Where(x => x.Distance < similarityThreshold)
            .OrderBy(x => x.Distance)
            .Take(10)
            .ToListAsync();

        foreach (var r in results)
            logger.LogInformation("  [{Distance:F4}] {Name}", r.Distance, r.Product.Name);

        var products = results.Select(r => r.Product).ToList();
        logger.LogInformation("Semantic search returned {Count} product(s) within threshold {Threshold}.", products.Count, similarityThreshold);

        if (products.Count == 0)
            logger.LogWarning("Semantic search also returned nothing — embeddings may not be backfilled, or the threshold ({Threshold}) may be too strict.", similarityThreshold);

        var resultItems = results.Select(r => new SearchResultItem(r.Product.Id, r.Distance)).ToList();
        await WriteSearchLog(term, SearchType.Semantic, 0, products.Count, resultItems, userId);

        return new ProductSearchResult(products);
    }

    private async Task WriteSearchLog(string term, SearchType type, int likeCount, int semanticCount, List<SearchResultItem> results, string? userId)
    {
        try
        {
            db.SearchLogs.Add(new SearchLog
            {
                SearchTerm = term,
                SearchType = type,
                LikeResultCount = likeCount,
                SemanticResultCount = semanticCount,
                TopDistance = results.Count > 0 ? results.Min(r => r.Distance) : null,
                Results = results,
                UserId = userId
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write search log for term '{SearchTerm}'", term);
        }
    }
}
