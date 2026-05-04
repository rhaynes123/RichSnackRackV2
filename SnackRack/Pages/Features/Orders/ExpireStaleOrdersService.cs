using Microsoft.EntityFrameworkCore;
using SnackRack.Data;

namespace SnackRack.Pages.Features.Orders;

public class ExpireStaleOrdersService(IServiceScopeFactory scopeFactory, ILogger<ExpireStaleOrdersService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await CancelStaleOrdersAsync(db, stoppingToken);
        }
    }

    internal async Task CancelStaleOrdersAsync(ApplicationDbContext db, CancellationToken ct)
    {
        try
        {
            var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
            var count = await db.Orders
                .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < cutoff)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Cancelled), ct);
            logger.LogInformation("Auto-cancelled {Count} stale orders", count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to auto-cancel stale orders");
        }
    }
}
