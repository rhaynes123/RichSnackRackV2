using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Pages.Features.Reviews;

public class MyReviews : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    public List<Review> Reviews { get; set; } 

    public MyReviews(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }
    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Forbid();
        }
        Reviews = await _db.Reviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .Where(re => re.User.Id == user.Id).ToListAsync();
        return Page();
    }
}