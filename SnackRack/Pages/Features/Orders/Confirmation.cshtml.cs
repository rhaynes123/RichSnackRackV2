using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;

namespace SnackRack.Pages.Features.Orders;

public class Confirmation : PageModel
{
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    private readonly ApplicationDbContext _db;
    public Confirmation(ApplicationDbContext db)
    {
        _db = db;
    }
    public async Task<IActionResult> OnGet(Guid? orderId)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return NotFound();

        OrderId = order.Id;
        Status = order.Status;

        return Page();
    }
}