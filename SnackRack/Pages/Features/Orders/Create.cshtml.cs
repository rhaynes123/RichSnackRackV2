using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
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

    [BindProperty(SupportsGet = true)]
    public Guid? ProductId { get; set; }
    [BindProperty(SupportsGet = true)]
    public Customer? Customer { get; set; } = null!;
    [BindProperty(SupportsGet = true)]
    public Guid? OrderId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ApplicationDbContext db
    , UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
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
        var product = await GetProduct(ProductId.Value);
        if (product != null)
        {
            await AddOrIncrement(product);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSubmit()
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .SingleAsync(o => o.Id == OrderId);

        if (order.Status != OrderStatus.Pending)
            return BadRequest();
        
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser is null 
            || User.Identity?.IsAuthenticated == false 
            || string.IsNullOrWhiteSpace(currentUser.PhoneNumber)
            || string.IsNullOrWhiteSpace(currentUser.Email)
            )
        {
            // I'm going to redirect to a page that makes customers fill in their information
            return RedirectToPage("/Features/Customers/Create", new { orderId = order.Id });
        }

        order.Customer.CustomerTypeId = (int)CustomerType.Registered;
        order.Customer.Name = User.Identity?.Name ?? "";
        order.Customer.PhoneNumber = currentUser.PhoneNumber;
        order.Customer.Email = currentUser.Email;
        order.Customer.UserId = currentUser.Id.ToString();

        order.Status = OrderStatus.Submitted;
        await _db.SaveChangesAsync();

        return RedirectToPage("/Features/Orders/Confirmation", new { orderId = order.Id });
    }

    // AJAX handler
    public async Task<IActionResult> OnPostAddItem([FromBody] AddItemToOrder? request)
    {
        if(string.IsNullOrWhiteSpace(request?.ProductName)) return BadRequest();
        var product = await GetProduct(request.ProductName);
        if (product == null)
            return NotFound();
        OrderId = request.OrderId;

        await AddOrIncrement(product);

        return new JsonResult(Items);
    }

    // ----- Helpers -----

    private async Task AddOrIncrement(Product product)
    {
        var pendingOrder = await _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(or => or.Id == OrderId) ?? new Order
        {
            Id = OrderId ?? Guid.CreateVersion7(),
            Customer = Customer ?? new Customer(),
            Status = OrderStatus.Pending,
            OrderItems = Items
        };
        if (pendingOrder.Status == OrderStatus.Submitted) return;
        OrderId = pendingOrder.Id;
        var existing = pendingOrder.OrderItems.FirstOrDefault(i => i.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            pendingOrder.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = 1
            });
        }

        if (!await _db.Orders.AnyAsync(o => o.Id == OrderId))
        {
            await _db.Orders.AddAsync(pendingOrder);
        }
        
        await _db.SaveChangesAsync();
        Items = pendingOrder.OrderItems.ToList();
        
    }

    private async Task<Product?> GetProduct(Guid id)
    {
        return await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
    }
    private async Task<Product?> GetProduct(string name)
    {
        return await _db.Products.FirstOrDefaultAsync(p => p.Name == name);
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
    Pending = 1 ,
    Submitted = 2 ,
    Completed = 3 ,
    Cancelled = 4
}

public class Order
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public required Customer Customer { get; set; } 
    public OrderStatus Status { get; set; }
}


public record AddItemToOrder
{
    public string? ProductName { get; init; }
    public Guid? OrderId { get; init; }
}