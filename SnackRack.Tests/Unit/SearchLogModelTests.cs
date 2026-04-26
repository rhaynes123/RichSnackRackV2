using SnackRack.Pages.Features.Products;

namespace SnackRack.Tests.Unit;

public class SearchLogModelTests
{
    // ── SearchLog defaults ────────────────────────────────────────────────────

    [Fact]
    public void SearchLog_Id_IsGeneratedAutomatically()
    {
        var a = new SearchLog { SearchTerm = "test" };
        var b = new SearchLog { SearchTerm = "test" };
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void SearchLog_SearchedAt_DefaultsToApproximateUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var log = new SearchLog { SearchTerm = "test" };
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(log.SearchedAt, before, after);
    }

    [Fact]
    public void SearchLog_Results_DefaultsToEmptyList()
    {
        var log = new SearchLog { SearchTerm = "test" };
        Assert.NotNull(log.Results);
        Assert.Empty(log.Results);
    }

    [Fact]
    public void SearchLog_UserId_DefaultsToNull()
    {
        var log = new SearchLog { SearchTerm = "test" };
        Assert.Null(log.UserId);
    }

    [Fact]
    public void SearchLog_TopDistance_DefaultsToNull()
    {
        var log = new SearchLog { SearchTerm = "test" };
        Assert.Null(log.TopDistance);
    }

    // ── SearchResultItem ──────────────────────────────────────────────────────

    [Fact]
    public void SearchResultItem_StoresProductIdAndDistance()
    {
        var id = Guid.NewGuid();
        var item = new SearchResultItem(id, 0.25);
        Assert.Equal(id, item.ProductId);
        Assert.Equal(0.25, item.Distance);
    }

    [Fact]
    public void SearchResultItem_WithZeroDistance_IsValid()
    {
        var item = new SearchResultItem(Guid.NewGuid(), 0.0);
        Assert.Equal(0.0, item.Distance);
    }

    // ── SearchType enum ───────────────────────────────────────────────────────

    [Fact]
    public void SearchType_AllValuesAreDistinct()
    {
        var values = Enum.GetValues<SearchType>().Cast<int>().ToList();
        Assert.Equal(values.Count, values.Distinct().Count());
    }

    [Theory]
    [InlineData(SearchType.Like, "Like")]
    [InlineData(SearchType.Semantic, "Semantic")]
    [InlineData(SearchType.SemanticUnavailable, "SemanticUnavailable")]
    public void SearchType_ToStringMatchesExpectedName(SearchType type, string expected)
    {
        Assert.Equal(expected, type.ToString());
    }

    [Fact]
    public void SearchType_HasExactlyThreeValues()
    {
        Assert.Equal(3, Enum.GetValues<SearchType>().Length);
    }
}
