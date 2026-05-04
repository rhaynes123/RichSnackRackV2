using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Customers;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Pages.Features.Orders;

public class CreateModel : PageModel
{
    public List<OrderItem> Items { get; set; } = [];
    public List<Product> AvailableProducts { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? ProductId { get; set; }
    [BindProperty(SupportsGet = true)]
    public Customer? Customer { get; set; } = null!;
    [BindProperty(SupportsGet = true)]
    public Guid? OrderId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    private readonly ApplicationDbContext _db;
    private readonly AddItemToOrderCommand _addItem;
    private readonly SubmitOrderCommand _submitOrder;
    private readonly CancelOrderCommand _cancelOrder;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(ApplicationDbContext db, AddItemToOrderCommand addItem, SubmitOrderCommand submitOrder, CancelOrderCommand cancelOrder, ILogger<CreateModel> logger)
    {
        _db = db;
        _addItem = addItem;
        _submitOrder = submitOrder;
        _cancelOrder = cancelOrder;
        _logger = logger;
    }

    public async Task<IActionResult> OnGet()
    {
        AvailableProducts = await _db.Products
            .Where(p => p.IsActive == true)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (OrderId.HasValue)
        {
            var order = await _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == OrderId.Value);

            if (order == null)
                return NotFound();

            if (order.Status != OrderStatus.Pending)
                return BadRequest("Only pending orders can be modified.");

            OrderId = order.Id;
            Items = order.OrderItems
                .Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity
                })
                .ToList();

            return Page();
        }

        OrderId = Guid.CreateVersion7();
        if (!ProductId.HasValue) return Page();

        Items = await _addItem.ExecuteAsync(OrderId.Value, ProductId.Value, Customer);
        return Page();
    }

    public async Task<IActionResult> OnPostSubmit()
    {
        var result = await _submitOrder.ExecuteAsync(OrderId, User);

        return result.Outcome switch
        {
            SubmitOutcome.NotFound => NotFound(),
            SubmitOutcome.InvalidStatus => BadRequest(),
            SubmitOutcome.NeedsCustomerInfo => RedirectToPage("/Features/Customers/Create", new { orderId = result.OrderId }),
            SubmitOutcome.Failed => RedirectWithError(result.Error, result.OrderId),
            SubmitOutcome.Succeeded => RedirectToPage("/Features/Orders/Confirmation", new { orderId = result.OrderId }),
            _ => BadRequest()
        };
    }

    // AJAX handler
    public async Task<IActionResult> OnPostAddItem([FromBody] AddItemToOrder? request)
    {
        if (request?.ProductId == null) return BadRequest();

        var orderId = request.OrderId ?? Guid.CreateVersion7();
        try
        {
            Items = await _addItem.ExecuteAsync(orderId, request.ProductId.Value, Customer);
            OrderId = orderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add item to order {OrderId}", orderId);
            return StatusCode(500, new { error = "An error occurred while adding the item." });
        }

        return new JsonResult(Items);
    }

    public async Task<IActionResult> OnPostCancel()
    {
        var result = await _cancelOrder.ExecuteAsync(OrderId);

        return result.Outcome switch
        {
            CancelOutcome.NotFound => NotFound(),
            CancelOutcome.InvalidStatus => BadRequest(),
            CancelOutcome.Failed => RedirectWithError(result.Error, result.OrderId),
            CancelOutcome.Succeeded => RedirectToPage("/Features/Orders/Confirmation", new { orderId = result.OrderId }),
            _ => BadRequest()
        };
    }

    private IActionResult RedirectWithError(string? error, Guid? orderId)
    {
        TempData["ErrorMessage"] = error;
        return RedirectToPage("/Features/Orders/Create", new { orderId });
    }
}

public class OrderItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
}

public enum OrderStatus
{
    Pending = 1,
    Submitted = 2,
    Completed = 3,
    Cancelled = 4
}

public class Order
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public required Customer Customer { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public record AddItemToOrder
{
    public Guid? ProductId { get; init; }
    public Guid? OrderId { get; init; }
}
