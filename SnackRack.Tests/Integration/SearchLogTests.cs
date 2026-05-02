using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pgvector;
using SnackRack.Pages.Features.Admin;
using SnackRack.Pages.Features.Products;
using SnackRack.Services;

namespace SnackRack.Tests.Integration;

/// <summary>
/// Integration tests for the search logging feature.
///
/// Verifies that <see cref="Menu.OnGet"/> writes a <see cref="SearchLog"/> row for
/// every search path (LIKE hit, semantic fallback, Ollama unavailable), that no log
/// is written when there is no search term, and that the jsonb Results column
/// round-trips correctly.
///
/// Also tests the <see cref="SearchLogs"/> admin page ordering and ZeroResultsOnly filter.
/// </summary>
[Collection("Database")]
public class SearchLogTests(TestDatabaseFixture fixture)
{
    private readonly TestDatabaseFixture _fixture = fixture;

    // Both products and query vectors point in a single dimension.
    // Cosine distance = 0 when dimensions match, = 1 when they differ.
    private static float[] MakeDirectionalEmbedding(int hotDimension)
    {
        var floats = new float[768];
        floats[hotDimension] = 1.0f;
        return floats;
    }

    // Gives Menu an HttpContext so User.FindFirstValue does not throw inside WriteSearchLog,
    // and a no-op TempData so the catch block's TempData assignment does not throw.
    private Menu BuildMenuPage(string? searchTerm, IEmbeddingService embeddings) =>
        new(_fixture.DbContext, embeddings, NullLogger<Menu>.Instance)
        {
            SearchTerm = searchTerm,
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
            TempData = Mock.Of<ITempDataDictionary>()
        };

    // ── Log writing ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LikeMatch_WritesLog_WithTypeLikeAndCorrectCount()
    {
        var uid = Guid.NewGuid();
        var term = $"LikeLog{uid}";
        _fixture.DbContext.Products.Add(new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"Product {term}",
            Description = "A tasty snack",
            Price = 1.99m,
            IsActive = true
        });
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        await BuildMenuPage(term, new Mock<IEmbeddingService>(MockBehavior.Strict).Object).OnGet();
        _fixture.DbContext.ChangeTracker.Clear();

        var log = await _fixture.DbContext.SearchLogs
            .Where(l => l.SearchTerm == term)
            .SingleOrDefaultAsync();

        Assert.NotNull(log);
        Assert.Equal(SearchType.Like, log.SearchType);
        Assert.Equal(1, log.LikeResultCount);
        Assert.Equal(0, log.SemanticResultCount);
        Assert.Null(log.TopDistance);
        Assert.Empty(log.Results);
    }

    [Fact]
    public async Task SemanticFallback_WritesLog_WithTypeSemanticAndResultCount()
    {
        var uid = Guid.NewGuid();
        var term = $"NOMATCH_SEMANTIC_{uid}";
        _fixture.DbContext.Products.Add(new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"NOMATCH_{uid}",
            Description = $"NOMATCH_DESC_{uid}",
            Price = 2.49m,
            IsActive = true,
            DescriptionEmbedding = new Vector(MakeDirectionalEmbedding(hotDimension: 10))
        });
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var mockEmbeddings = new Mock<IEmbeddingService>();
        mockEmbeddings.Setup(s => s.GetEmbeddingAsync(term))
            .ReturnsAsync(Result<float[]>.Success(MakeDirectionalEmbedding(hotDimension: 10)));

        await BuildMenuPage(term, mockEmbeddings.Object).OnGet();
        _fixture.DbContext.ChangeTracker.Clear();

        var log = await _fixture.DbContext.SearchLogs
            .Where(l => l.SearchTerm == term)
            .SingleOrDefaultAsync();

        Assert.NotNull(log);
        Assert.Equal(SearchType.Semantic, log.SearchType);
        Assert.Equal(0, log.LikeResultCount);
        Assert.Equal(1, log.SemanticResultCount);
    }

    [Fact]
    public async Task SemanticFallback_LogContains_ProductIdAndDistance()
    {
        var uid = Guid.NewGuid();
        var term = $"NOMATCH_RESULTS_{uid}";
        var productId = Guid.CreateVersion7();
        _fixture.DbContext.Products.Add(new Product
        {
            Id = productId,
            Name = $"NOMATCH_{uid}",
            Description = $"NOMATCH_DESC_{uid}",
            Price = 2.49m,
            IsActive = true,
            DescriptionEmbedding = new Vector(MakeDirectionalEmbedding(hotDimension: 11))
        });
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var mockEmbeddings = new Mock<IEmbeddingService>();
        mockEmbeddings.Setup(s => s.GetEmbeddingAsync(term))
            .ReturnsAsync(Result<float[]>.Success(MakeDirectionalEmbedding(hotDimension: 11)));

        await BuildMenuPage(term, mockEmbeddings.Object).OnGet();
        _fixture.DbContext.ChangeTracker.Clear();

        var log = await _fixture.DbContext.SearchLogs
            .Where(l => l.SearchTerm == term)
            .SingleOrDefaultAsync();

        Assert.NotNull(log);
        Assert.Contains(log.Results, r => r.ProductId == productId);
        Assert.All(log.Results, r => Assert.True(r.Distance >= 0));
    }

    [Fact]
    public async Task SemanticFallback_TopDistance_EqualsMinimumDistanceInResults()
    {
        var uid = Guid.NewGuid();
        var term = $"NOMATCH_TOPDIST_{uid}";
        _fixture.DbContext.Products.Add(new Product
        {
            Id = Guid.CreateVersion7(),
            Name = $"NOMATCH_{uid}",
            Description = $"NOMATCH_DESC_{uid}",
            Price = 1.00m,
            IsActive = true,
            DescriptionEmbedding = new Vector(MakeDirectionalEmbedding(hotDimension: 12))
        });
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var mockEmbeddings = new Mock<IEmbeddingService>();
        mockEmbeddings.Setup(s => s.GetEmbeddingAsync(term))
            .ReturnsAsync(Result<float[]>.Success(MakeDirectionalEmbedding(hotDimension: 12)));

        await BuildMenuPage(term, mockEmbeddings.Object).OnGet();
        _fixture.DbContext.ChangeTracker.Clear();

        var log = await _fixture.DbContext.SearchLogs
            .Where(l => l.SearchTerm == term)
            .SingleOrDefaultAsync();

        Assert.NotNull(log);
        Assert.NotNull(log.TopDistance);
        Assert.Equal(log.Results.Min(r => r.Distance), log.TopDistance!.Value, precision: 4);
    }

    [Fact]
    public async Task OllamaUnavailable_WritesLog_WithTypeSemanticUnavailable()
    {
        var uid = Guid.NewGuid();
        var term = $"NOMATCH_UNAVAIL_{uid}";

        var mockEmbeddings = new Mock<IEmbeddingService>();
        mockEmbeddings.Setup(s => s.GetEmbeddingAsync(term))
            .ReturnsAsync(Result<float[]>.Failure("Ollama is down"));

        await BuildMenuPage(term, mockEmbeddings.Object).OnGet();
        _fixture.DbContext.ChangeTracker.Clear();

        var log = await _fixture.DbContext.SearchLogs
            .Where(l => l.SearchTerm == term)
            .SingleOrDefaultAsync();

        Assert.NotNull(log);
        Assert.Equal(SearchType.SemanticUnavailable, log.SearchType);
        Assert.Equal(0, log.LikeResultCount);
        Assert.Equal(0, log.SemanticResultCount);
        Assert.Null(log.TopDistance);
        Assert.Empty(log.Results);
    }

    [Fact]
    public async Task NoSearchTerm_DoesNotWriteLog()
    {
        var countBefore = await _fixture.DbContext.SearchLogs.CountAsync();

        await BuildMenuPage(null, new Mock<IEmbeddingService>(MockBehavior.Strict).Object).OnGet();

        var countAfter = await _fixture.DbContext.SearchLogs.CountAsync();
        Assert.Equal(countBefore, countAfter);
    }

    // ── jsonb round-trip ──────────────────────────────────────────────────────

    [Fact]
    public async Task Results_RoundTrip_ThroughJsonb()
    {
        var productId = Guid.CreateVersion7();
        var log = new SearchLog
        {
            SearchTerm = $"roundtrip_{Guid.NewGuid()}",
            SearchType = SearchType.Semantic,
            Results = [new SearchResultItem(productId, 0.1234)]
        };

        _fixture.DbContext.SearchLogs.Add(log);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var loaded = await _fixture.DbContext.SearchLogs.FindAsync(log.Id);

        Assert.NotNull(loaded);
        Assert.Single(loaded.Results);
        Assert.Equal(productId, loaded.Results[0].ProductId);
        Assert.Equal(0.1234, loaded.Results[0].Distance, precision: 4);
    }

    [Fact]
    public async Task Results_EmptyList_RoundTrips_ThroughJsonb()
    {
        var log = new SearchLog
        {
            SearchTerm = $"empty_roundtrip_{Guid.NewGuid()}",
            SearchType = SearchType.Like
        };

        _fixture.DbContext.SearchLogs.Add(log);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var loaded = await _fixture.DbContext.SearchLogs.FindAsync(log.Id);

        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Results);
        Assert.Empty(loaded.Results);
    }

    // ── Admin page ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AdminPage_ReturnsLogs_InDescendingDateOrder()
    {
        var baseTime = DateTime.UtcNow;
        var older = new SearchLog
        {
            SearchTerm = $"order_older_{Guid.NewGuid()}",
            SearchType = SearchType.Like,
            SearchedAt = baseTime.AddSeconds(-60)
        };
        var newer = new SearchLog
        {
            SearchTerm = $"order_newer_{Guid.NewGuid()}",
            SearchType = SearchType.Like,
            SearchedAt = baseTime
        };

        _fixture.DbContext.SearchLogs.AddRange(older, newer);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var adminPage = new SearchLogs(_fixture.DbContext) { CurrentPage = 1 };
        await adminPage.OnGet();

        var newerIndex = adminPage.Logs.FindIndex(l => l.Id == newer.Id);
        var olderIndex = adminPage.Logs.FindIndex(l => l.Id == older.Id);

        Assert.True(newerIndex >= 0, "Newer log should appear on page 1.");
        Assert.True(olderIndex >= 0, "Older log should appear on page 1.");
        Assert.True(newerIndex < olderIndex, "Newer log should precede older log in descending order.");
    }

    [Fact]
    public async Task AdminPage_ZeroResultsOnly_IncludesZeroResultSemanticLogs()
    {
        var zeroResultLog = new SearchLog
        {
            SearchTerm = $"zero_semantic_{Guid.NewGuid()}",
            SearchType = SearchType.Semantic,
            LikeResultCount = 0,
            SemanticResultCount = 0
        };
        var likeHitLog = new SearchLog
        {
            SearchTerm = $"like_hit_{Guid.NewGuid()}",
            SearchType = SearchType.Like,
            LikeResultCount = 3
        };

        _fixture.DbContext.SearchLogs.AddRange(zeroResultLog, likeHitLog);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var adminPage = new SearchLogs(_fixture.DbContext) { CurrentPage = 1, ZeroResultsOnly = true };
        await adminPage.OnGet();

        Assert.Contains(adminPage.Logs, l => l.Id == zeroResultLog.Id);
        Assert.DoesNotContain(adminPage.Logs, l => l.Id == likeHitLog.Id);
    }

    [Fact]
    public async Task AdminPage_ZeroResultsOnly_ExcludesSemanticUnavailableLogs()
    {
        var unavailLog = new SearchLog
        {
            SearchTerm = $"unavail_filter_{Guid.NewGuid()}",
            SearchType = SearchType.SemanticUnavailable,
            LikeResultCount = 0,
            SemanticResultCount = 0
        };

        _fixture.DbContext.SearchLogs.Add(unavailLog);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var adminPage = new SearchLogs(_fixture.DbContext) { CurrentPage = 1, ZeroResultsOnly = true };
        await adminPage.OnGet();

        Assert.DoesNotContain(adminPage.Logs, l => l.Id == unavailLog.Id);
    }

    [Fact]
    public async Task AdminPage_TotalCount_ReflectsFilteredResults()
    {
        // Insert one zero-result semantic log and one like-hit log so we have known data
        _fixture.DbContext.SearchLogs.AddRange(
            new SearchLog
            {
                SearchTerm = $"count_zero_{Guid.NewGuid()}",
                SearchType = SearchType.Semantic,
                LikeResultCount = 0,
                SemanticResultCount = 0
            },
            new SearchLog
            {
                SearchTerm = $"count_hit_{Guid.NewGuid()}",
                SearchType = SearchType.Like,
                LikeResultCount = 2
            }
        );
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        var unfilteredPage = new SearchLogs(_fixture.DbContext) { CurrentPage = 1 };
        await unfilteredPage.OnGet();

        var filteredPage = new SearchLogs(_fixture.DbContext) { CurrentPage = 1, ZeroResultsOnly = true };
        await filteredPage.OnGet();

        Assert.True(unfilteredPage.TotalCount > filteredPage.TotalCount,
            "Unfiltered count should be higher than zero-results-only count.");
    }
}
