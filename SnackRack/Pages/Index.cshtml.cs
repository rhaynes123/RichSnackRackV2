using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Orders;

namespace SnackRack.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public IndexModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public List<OrderDto> Orders { get; private set; } = [];

    public async Task OnGet()
    {
        Orders = await _db.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.Id)
            .Select(o => new OrderDto
            {
                OrderId = o.Id,
                Status = o.Status,
                CustomerName = o.Customer.Name,
                Items = o.OrderItems.Select(i => new OrderItemDto
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            })
            .ToListAsync();
    }

    public sealed class OrderDto
    {
        public Guid OrderId { get; init; }
        public OrderStatus Status { get; init; }
        public string CustomerName { get; init; } = "";
        public List<OrderItemDto> Items { get; init; } = [];
    }

    public sealed class OrderItemDto
    {
        public string ProductName { get; init; } = "";
        public int Quantity { get; init; }
        public decimal Price { get; init; }
    }
}