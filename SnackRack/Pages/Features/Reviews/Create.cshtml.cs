using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SnackRack.Data;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Pages.Features.Reviews;

public class Create : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid ProductId { get; init; }
    [BindProperty(SupportsGet = true)]
    public ReviewModel Review { get; set; } = new();
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<Create> _logger;

    public Create(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<Create> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }
    public async Task<IActionResult> OnGet()
    {
        if (User.Identity?.IsAuthenticated == false)
        {
            return Forbid();
        }
        var product = await _db.Products.FindAsync(ProductId);
        if (product is null)
        {
            return Page();
        }

        
        
        Review = new ReviewModel
        {
            ProductName = product.Name,
            ReviewText = string.Empty,
            Title = string.Empty
        };
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        
        var product = await _db.Products.FindAsync(ProductId);
        var currentUser = await _userManager.GetUserAsync(User);
        if (product is null)
        {
            return Page();
        }

        if (currentUser is null)
            return Forbid();

        var newReview = new Review
        {
            Product = product,
            Title = Review.Title,
            Comment = Review.ReviewText ?? string.Empty,
            User = currentUser!
        };
        await _db.Reviews.AddAsync(newReview);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save review for product {ProductId}", ProductId);
            TempData["ErrorMessage"] = "An error occurred while saving your review. Please try again.";
            return RedirectToPage(new { ProductId });
        }
        return RedirectToPage("./AllReviews");
    }
}

public record ReviewModel
{
    public string? Title { get; set; }
    public string? ReviewText { get; set; }
    public string? ProductName { get; set; }
}