using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SnackRack.Data;
using SnackRack.Pages.Features.Products;
using Testcontainers.PostgreSql;

namespace SnackRack.Tests.UI.Infrastructure;

/// <summary>
/// Starts a real Kestrel server backed by a Testcontainers PostgreSQL instance
/// and seeds a known set of products so UI tests have predictable data.
/// Shared across the "UI" xUnit collection.
/// </summary>
public class WebServerFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("pgvector/pgvector:pg16")
        .Build();

    private IHost? _kestrelHost;

    /// <summary>The base URL of the running Kestrel server, e.g. "http://127.0.0.1:54321".</summary>
    public string ServerAddress { get; private set; } = null!;

    // Stable product data seeded once; tests assert against these names.
    public const string ProductAName = "UI-Test Chips";
    public const string ProductBName = "UI-Test Pretzels";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Signal Program.cs to skip its auto-migration (the fixture owns migration).
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the production DbContext registration with one that points
            // at the Testcontainers database.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString(), o => o.UseVector()));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the normal in-process TestServer host that WebApplicationFactory expects.
        var testHost = builder.Build();

        // Build a second host that uses a real Kestrel listener on a random port.
        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel(options =>
                options.Listen(IPAddress.Loopback, 0)));

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        // Resolve the actual bound address so tests can navigate to it.
        var server = _kestrelHost.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()!;
        ServerAddress = addresses.Addresses.First();

        return testHost;
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();

        // CreateClient() triggers CreateHost() → starts the Kestrel host.
        CreateClient();

        // Run EF migrations and seed test products.
        using var scope = _kestrelHost!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await SeedAsync(db);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_kestrelHost is not null)
        {
            await _kestrelHost.StopAsync();
            _kestrelHost.Dispose();
        }

        await _postgres.DisposeAsync();
    }

    private static async Task SeedAsync(ApplicationDbContext db)
    {
        // Only seed if the named products don't already exist (idempotent).
        if (await db.Products.AnyAsync(p => p.Name == ProductAName))
            return;

        db.Products.AddRange(
            new Product
            {
                Id = Guid.CreateVersion7(),
                Name = ProductAName,
                Description = "Crispy and lightly salted",
                Price = 1.99m,
                IsActive = true
            },
            new Product
            {
                Id = Guid.CreateVersion7(),
                Name = ProductBName,
                Description = "Hard and salty pretzels",
                Price = 2.49m,
                IsActive = true
            });

        await db.SaveChangesAsync();
    }
}
