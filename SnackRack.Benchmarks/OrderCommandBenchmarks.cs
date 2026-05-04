using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnackRack.Data;
using SnackRack.Pages.Features.Customers;
using SnackRack.Pages.Features.Orders;
using SnackRack.Pages.Features.Products;
using Testcontainers.PostgreSql;

namespace SnackRack.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class OrderCommandBenchmarks
{
    private PostgreSqlContainer _postgres = null!;
    private string _connectionString = null!;
    private Guid _productId;

    private UserManager<ApplicationUser> _userManager = null!;
    private ClaimsPrincipal _principal = null!;

    // Per-iteration state
    private ApplicationDbContext _db = null!;
    private Guid _orderId;
    private AddItemToOrderCommand _addItem = null!;
    private SubmitOrderCommand _submit = null!;
    private CancelOrderCommand _cancel = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .Build();
        await _postgres.StartAsync();
        _connectionString = _postgres.GetConnectionString();

        using var db = CreateDb();
        await db.Database.MigrateAsync();

        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = "Bench Snack",
            Description = "A benchmark snack",
            Price = 1.99m,
            IsActive = true
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        _productId = product.Id;

        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                PhoneNumber = "555-0100",
                Email = "bench@example.com"
            });
        _userManager = mgr.Object;

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "BenchUser") }, "BenchAuth");
        _principal = new ClaimsPrincipal(identity);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _db = CreateDb();
        _orderId = Guid.CreateVersion7();
        _db.Orders.Add(new Order
        {
            Id = _orderId,
            Customer = new Customer(),
            Status = OrderStatus.Pending,
            OrderItems = []
        });
        _db.SaveChangesAsync().GetAwaiter().GetResult();
        _db.ChangeTracker.Clear();

        _addItem = new AddItemToOrderCommand(_db, NullLogger<AddItemToOrderCommand>.Instance);
        _submit = new SubmitOrderCommand(_db, _userManager, NullLogger<SubmitOrderCommand>.Instance);
        _cancel = new CancelOrderCommand(_db, NullLogger<CancelOrderCommand>.Instance);
    }

    [IterationCleanup]
    public void IterationCleanup() => _db.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark]
    public Task AddItem() => _addItem.ExecuteAsync(_orderId, _productId, null);

    [Benchmark]
    public Task SubmitOrder() => _submit.ExecuteAsync(_orderId, _principal);

    [Benchmark]
    public Task CancelOrder() => _cancel.ExecuteAsync(_orderId);

    [GlobalCleanup]
    public async Task GlobalCleanup() => await _postgres.DisposeAsync();

    private ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_connectionString, o => o.UseVector())
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new ApplicationDbContext(options);
    }
}
