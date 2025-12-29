using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Orders;

namespace SnackRack.Pages.Features.Products;

public class Menu : PageModel
{
    public List<Product> ActiveProducts = [];
    private readonly ApplicationDbContext _db;

    public Menu(ApplicationDbContext db)
    {
        _db = db;
    }
    public async Task<IActionResult> OnGet()
    {
        var products = await _db.Products.ToListAsync();
        ActiveProducts = products.Where(x => x.IsActive == true).ToList();
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