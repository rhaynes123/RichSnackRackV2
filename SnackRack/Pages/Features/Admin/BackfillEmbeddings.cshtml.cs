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
    private readonly ILogger<BackfillEmbeddings> _logger;

    public BackfillEmbeddings(ApplicationDbContext db, IEmbeddingService embeddings, ILogger<BackfillEmbeddings> logger)
    {
        _db = db;
        _embeddings = embeddings;
        _logger = logger;
    }

    public int ProductsWithoutEmbedding { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }

    public async Task<IActionResult> OnGet()
    {
        ProductsWithoutEmbedding = await _db.Products
            .CountAsync(p => p.DescriptionEmbedding == null);
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var products = await _db.Products
            .Where(p => p.DescriptionEmbedding == null && p.IsActive == true)
            .ToListAsync();

        foreach (var product in products)
        {
            var result = await _embeddings.GetEmbeddingAsync(product.Description);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to generate embedding for product '{ProductId}': {Error}", product.Id, result.Error);
                FailedCount++;
                continue;
            }

            product.DescriptionEmbedding = new Vector(result.Value!);
            ProcessedCount++;
        }

        if (ProcessedCount > 0)
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error saving embeddings during backfill");
                TempData["ErrorMessage"] = "Embeddings were generated but could not be saved. Please try again.";
            }
        }

        if (FailedCount > 0)
            TempData["WarningMessage"] = $"{FailedCount} product(s) could not be embedded. Check logs for details.";

        ProductsWithoutEmbedding = await _db.Products
            .CountAsync(p => p.DescriptionEmbedding == null);

        return Page();
    }
}
