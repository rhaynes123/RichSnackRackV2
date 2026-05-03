using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SnackRack.Data;
using SnackRack.Pages.Features.Customers;

namespace SnackRack.Pages.Features.Orders;

public enum SubmitOutcome { Succeeded, NeedsCustomerInfo, NotFound, InvalidStatus, Failed }

public record SubmitOrderResult(SubmitOutcome Outcome, Guid? OrderId = null, string? Error = null);

public class SubmitOrderCommand(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger<SubmitOrderCommand> logger)
{
    public async Task<SubmitOrderResult> ExecuteAsync(Guid? orderId, ClaimsPrincipal user)
    {
        var order = await db.Orders
            .Include(o => o.Customer)
            .SingleOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return new SubmitOrderResult(SubmitOutcome.NotFound);

        if (order.Status != OrderStatus.Pending)
            return new SubmitOrderResult(SubmitOutcome.InvalidStatus);

        var currentUser = await userManager.GetUserAsync(user);
        if (currentUser is null
            || user.Identity?.IsAuthenticated == false
            || string.IsNullOrWhiteSpace(currentUser.PhoneNumber)
            || string.IsNullOrWhiteSpace(currentUser.Email))
        {
            return new SubmitOrderResult(SubmitOutcome.NeedsCustomerInfo, order.Id);
        }

        order.Customer.CustomerTypeId = (int)CustomerType.Registered;
        order.Customer.Name = user.Identity?.Name ?? "";
        order.Customer.PhoneNumber = currentUser.PhoneNumber;
        order.Customer.Email = currentUser.Email;
        order.Customer.UserId = currentUser.Id.ToString();
        order.Status = OrderStatus.Submitted;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to submit order {OrderId}", orderId);
            return new SubmitOrderResult(SubmitOutcome.Failed, orderId, "An error occurred while submitting your order. Please try again.");
        }

        return new SubmitOrderResult(SubmitOutcome.Succeeded, order.Id);
    }
}
