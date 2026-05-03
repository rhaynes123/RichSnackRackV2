using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Data.Extensions;
using SnackRack.Pages.Features.Customers;
using SnackRack.Pages.Features.Products;

namespace SnackRack.Pages.Features.Orders;

public class AddItemToOrderCommand(ApplicationDbContext db, ILogger<AddItemToOrderCommand> logger)
{
    public async Task<List<OrderItem>> ExecuteAsync(Guid orderId, Guid productId, Customer? customer)
    {
        var product = await GetProduct(productId);
        if (product is null)
        {
            logger.LogWarning("Product {ProductId} not found when adding to order {OrderId}.", productId, orderId);
            return [];
        }

        var pendingOrder = await db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId) ?? new Order
        {
            Id = orderId,
            Customer = customer ?? new Customer(),
            Status = OrderStatus.Pending,
            OrderItems = new List<OrderItem>()
        };

        if (pendingOrder.Status == OrderStatus.Submitted)
            return pendingOrder.OrderItems.ToList();

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

        if (!await db.Orders.AnyAsync(o => o.Id == orderId))
            await db.Orders.AddAsync(pendingOrder);

        await db.SaveChangesAsync();
        return pendingOrder.OrderItems.ToList();
    }

    private async Task<Product?> GetProduct(Guid id) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id);

    private async Task<Product?> GetProduct(string name) =>
        await db.Products.FirstOrDefaultAsync(p => DbFunctions.RemoveHyphens(p.Name) == name);
}
