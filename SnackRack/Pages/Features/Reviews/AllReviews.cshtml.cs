using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Pages.Features.Reviews;

public class AllReviews : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm{ get; set; }
    [BindProperty(SupportsGet = true)]
    public bool OnlyMyReviews{ get; set; } = false;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    public List<Review> Reviews { get; set; } 

    public AllReviews(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }
    public async Task<IActionResult> OnGet()
    {
        IQueryable<Review> productReviews =  _db.Reviews
            .Include(r => r.Product)
            .Include(r => r.User);
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            SearchTerm = SearchTerm.Trim();
            productReviews = productReviews.Where(r => 
                EF.Functions.ILike(r.Product.Name, SearchTerm));
        }

        if (OnlyMyReviews)
        {
            var user = await _userManager.GetUserAsync(User);
            productReviews = productReviews.Where(r => user != null 
                                                       && r.User.Id == user.Id);
        }
        Reviews = await productReviews
            .ToListAsync();
        return Page();
    }
}