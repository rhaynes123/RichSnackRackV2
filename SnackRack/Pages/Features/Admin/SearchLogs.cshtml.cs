using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Pages.Features.Admin;

public class SearchLogs : PageModel
{
    public List<SearchLog> Logs { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    [BindProperty(SupportsGet = true)]
    public bool ZeroResultsOnly { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    private const int PageSize = 25;
    private readonly ApplicationDbContext _db;

    public SearchLogs(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> OnGet()
    {
        var query = _db.SearchLogs.AsQueryable();

        if (ZeroResultsOnly)
            query = query.Where(l => l.LikeResultCount == 0 && l.SemanticResultCount == 0
                                  && l.SearchType != SearchType.SemanticUnavailable);

        TotalCount = await query.CountAsync();

        Logs = await query
            .OrderByDescending(l => l.SearchedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return Page();
    }
}
