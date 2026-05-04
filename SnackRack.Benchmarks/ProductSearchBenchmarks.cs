using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pgvector;
using SnackRack.Data;
using SnackRack.Pages.Features.Products;
using SnackRack.Services;
using Testcontainers.PostgreSql;

namespace SnackRack.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class ProductSearchBenchmarks
{
    private PostgreSqlContainer _postgres = null!;
    private ApplicationDbContext _db = null!;
    private ProductSearchQuery _query = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .Build();
        await _postgres.StartAsync();

        _db = CreateDb();
        await _db.Database.MigrateAsync();

        var unitVector = new float[768];
        unitVector[0] = 1.0f;

        for (int i = 0; i < 20; i++)
        {
            _db.Products.Add(new Product
            {
                Id = Guid.CreateVersion7(),
                Name = $"Cookie {i + 1}",
                Description = $"A delicious snack {i + 1}",
                Price = 1.99m + i * 0.10m,
                IsActive = true,
                DescriptionEmbedding = new Vector(unitVector)
            });
        }
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var embeddingMock = new Mock<IEmbeddingService>();
        embeddingMock
            .Setup(m => m.GetEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(Result<float[]>.Success(unitVector));

        _query = new ProductSearchQuery(_db, embeddingMock.Object, NullLogger<ProductSearchQuery>.Instance);
    }

    [IterationSetup]
    public void IterationSetup() => _db.ChangeTracker.Clear();

    /// <summary>Hits the LIKE path — "Cookie" matches all 20 seeded product names.</summary>
    [Benchmark]
    public Task<ProductSearchResult> SearchLike() => _query.ExecuteAsync("Cookie", null);

    /// <summary>Hits the semantic path — no LIKE match, falls back to cosine distance.</summary>
    [Benchmark]
    public Task<ProductSearchResult> SearchSemantic() => _query.ExecuteAsync("xyzzy", null);

    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), o => o.UseVector())
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        return new ApplicationDbContext(options);
    }
}
