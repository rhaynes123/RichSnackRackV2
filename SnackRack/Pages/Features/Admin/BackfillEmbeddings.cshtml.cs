using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using SnackRack.Data;
using SnackRack.Pages.Features.Products;
using SnackRack.Services;

namespace SnackRack.Pages.Features.Admin;

public class BackfillEmbeddings : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly IEmbeddingService _embeddings;

    public BackfillEmbeddings(ApplicationDbContext db, IEmbeddingService embeddings)
    {
        _db = db;
        _embeddings = embeddings;
    }

    public int ProductsWithoutEmbedding { get; set; }
    public int ProcessedCount { get; set; }

    public async Task<IActionResult> OnGet()
    {
        ProductsWithoutEmbedding = await _db.Products
            .CountAsync(p => p.DescriptionEmbedding == null);
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        try
        {
            var products = await _db.Products
                .Where(p => p.DescriptionEmbedding == null && p.IsActive == true)
                .ToListAsync();

            foreach (var product in products)
            {
                var floats = await _embeddings.GetEmbeddingAsync(product.Description);
                product.DescriptionEmbedding = new Vector(floats);
                ProcessedCount++;
            }

            await _db.SaveChangesAsync();

            ProductsWithoutEmbedding = await _db.Products
                .CountAsync(p => p.DescriptionEmbedding == null);

            return Page();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return RedirectToPage("/Index");
        }
    }
}
