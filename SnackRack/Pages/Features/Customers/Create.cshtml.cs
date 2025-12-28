using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Orders;

namespace SnackRack.Pages.Features.Customers;

public class Create : PageModel
{
    [BindProperty(SupportsGet = true)] public CustomerRequest CustomerRequest { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public Customer? Customer { get; set; }
    [BindProperty(SupportsGet = true)]
    public Guid? CustomerId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public Guid? OrderId { get; set; } 
    private readonly ApplicationDbContext _db;

    public Create(ApplicationDbContext db)
    {
        _db = db;
    }
    public async Task<IActionResult> OnGet()
    {
        var order =  await _db.Orders
            .Include(or => or.Customer)
            .FirstOrDefaultAsync(cu => cu.Id == OrderId);
        var customer = order?.Customer;
        Customer = customer;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        
        if (Customer == null || string.IsNullOrWhiteSpace(CustomerRequest.Name) 
                             || string.IsNullOrWhiteSpace(CustomerRequest.Email) 
                             || string.IsNullOrWhiteSpace(CustomerRequest.PhoneNumber))
        {
            return Page();
        }
        var order = await _db.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(cu => cu.Id == OrderId);
        var customer = order?.Customer;
        customer?.Name = CustomerRequest.Name ;
        customer?.Email = CustomerRequest.Email;
        customer?.PhoneNumber = CustomerRequest.PhoneNumber;
        if (OrderId is null)
        {
            return RedirectToPage("Index");
        }
        order?.Status = OrderStatus.Submitted;
        await _db.SaveChangesAsync();
        
        return  RedirectToPage(
            "/Features/Orders/Confirmation",
            new { OrderId = OrderId }
        );
    }
}

public record CustomerRequest
{
    [StringLength(800)]
    public string? Name { get; init; } = "Guest";

    [StringLength(800)] public string? Email { get; init; }
    [StringLength(800)]public string? PhoneNumber { get; init; }
}

public class Customer
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    [StringLength(800)]
    public string Name { get; set; } = "Guest";

    [StringLength(800)] public string? Email { get; set; }
    [StringLength(800)]public string? PhoneNumber { get; set; }
    [StringLength(800)]
    public string? UserId { get; set; }

    public int? CustomerTypeId { get; set; } = (int)CustomerType.Guest;
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}

public enum CustomerType
{
    Guest =1,
    Registered=2,
}