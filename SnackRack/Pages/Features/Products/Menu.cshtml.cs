using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Orders;
using SnackRack.Services;

namespace SnackRack.Pages.Features.Products;

public class Menu : PageModel
{
    public List<Product> ActiveProducts = [];
    private readonly ApplicationDbContext _db;
    private readonly IEmbeddingService _embeddings;
    private readonly ILogger<Menu> _logger;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public Menu(ApplicationDbContext db, IEmbeddingService embeddings, ILogger<Menu> logger)
    {
        _db = db;
        _embeddings = embeddings;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            _logger.LogInformation("No search term provided, returning full active product list.");
            ActiveProducts = await _db.Products.Where(p => p.IsActive == true).ToListAsync();
            return Page();
        }

        var term = SearchTerm.Trim();
        _logger.LogInformation("LIKE search triggered with term: '{SearchTerm}'", term);

        ActiveProducts = await _db.Products
            .Where(p => p.IsActive == true &&
                        (EF.Functions.ILike(p.Name, $"%{term}%") ||
                         EF.Functions.ILike(p.Description, $"%{term}%")))
            .ToListAsync();

        _logger.LogInformation("LIKE search returned {Count} product(s).", ActiveProducts.Count);

        if (ActiveProducts.Count > 0)
        {
            await WriteSearchLog(term, SearchType.Like, ActiveProducts.Count, 0, []);
            return Page();
        }

        _logger.LogInformation("No LIKE matches, falling back to semantic search.");

        try
        {
            var floats = await _embeddings.GetEmbeddingAsync(term);
            _logger.LogInformation("Embedding generated ({Dimensions} dimensions), querying database.", floats.Length);

            var queryVector = new Vector(floats);
            const double similarityThreshold = 0.3;

            var results = await _db.Products
                .Where(p => p.IsActive == true && p.DescriptionEmbedding != null)
                .Select(p => new { Product = p, Distance = p.DescriptionEmbedding!.CosineDistance(queryVector) })
                .Where(x => x.Distance < similarityThreshold)
                .OrderBy(x => x.Distance)
                .Take(10)
                .ToListAsync();

            foreach (var r in results)
                _logger.LogInformation("  [{Distance:F4}] {Name}", r.Distance, r.Product.Name);

            ActiveProducts = results.Select(r => r.Product).ToList();
            _logger.LogInformation("Semantic search returned {Count} product(s) within threshold {Threshold}.", ActiveProducts.Count, similarityThreshold);

            if (ActiveProducts.Count == 0)
                _logger.LogWarning("Semantic search also returned nothing — embeddings may not be backfilled, or the threshold ({Threshold}) may be too strict.", similarityThreshold);

            var resultItems = results.Select(r => new SearchResultItem(r.Product.Id, r.Distance)).ToList();
            await WriteSearchLog(term, SearchType.Semantic, 0, ActiveProducts.Count, resultItems);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic search unavailable for term '{SearchTerm}'.", term);
            TempData["ErrorMessage"] = "Semantic search is unavailable. Showing no results for your query.";
            await WriteSearchLog(term, SearchType.SemanticUnavailable, 0, 0, []);
        }

        return Page();
    }

    private async Task WriteSearchLog(string term, SearchType type, int likeCount, int semanticCount, List<SearchResultItem> results)
    {
        try
        {
            _db.SearchLogs.Add(new SearchLog
            {
                SearchTerm = term,
                SearchType = type,
                LikeResultCount = likeCount,
                SemanticResultCount = semanticCount,
                TopDistance = results.Count > 0 ? results.Min(r => r.Distance) : null,
                Results = results,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write search log for term '{SearchTerm}'", term);
        }
    }
}

public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsActive { get; set; }
    public Vector? DescriptionEmbedding { get; set; }
    public virtual ICollection<Review> Reviews { get; set; } = [];
    public virtual ICollection<Order> Orders { get; set; } = [];
}

[Table("reviews")]
public class Review
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required Product Product { get; set; }
    [StringLength(1000)]
    public required string Title { get; set; }
    [StringLength(1000)]
    public required string Comment { get; set; }
    public required ApplicationUser User { get; set; }
}

public enum SearchType { Like, Semantic, SemanticUnavailable }

public record SearchResultItem(Guid ProductId, double Distance);

public class SearchLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string SearchTerm { get; init; }
    public SearchType SearchType { get; init; }
    public int LikeResultCount { get; init; }
    public int SemanticResultCount { get; init; }
    public double? TopDistance { get; init; }
    public List<SearchResultItem> Results { get; init; } = [];
    public DateTime SearchedAt { get; init; } = DateTime.UtcNow;
    public string? UserId { get; init; }
}
