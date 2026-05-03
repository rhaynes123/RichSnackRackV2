using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Pgvector;
using SnackRack.Data;
using SnackRack.Pages.Features.Orders;

namespace SnackRack.Pages.Features.Products;

public class Menu : PageModel
{
    public List<Product> ActiveProducts = [];
    private readonly ProductSearchQuery _search;
    private readonly ILogger<Menu> _logger;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public Menu(ProductSearchQuery search, ILogger<Menu> logger)
    {
        _search = search;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _search.ExecuteAsync(SearchTerm, userId);
        ActiveProducts = result.Products;
        if (result.ErrorMessage != null)
            TempData["ErrorMessage"] = result.ErrorMessage;
        return Page();
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
