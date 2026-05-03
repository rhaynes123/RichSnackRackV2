namespace SnackRack.Tests.UI.UI;

/// <summary>
/// Playwright UI tests for the Menu (product listing + search) page.
/// </summary>
[Collection("UI")]
public class MenuPageTests(WebServerFixture server) : PageTest
{
    private string MenuUrl => $"{server.ServerAddress}/Features/Products/Menu";

    [Fact]
    public async Task Menu_LoadsProductTable_WithSeededProducts()
    {
        await Page.GotoAsync(MenuUrl);

        // Both seeded products should appear in the table.
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = WebServerFixture.ProductAName }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = WebServerFixture.ProductBName }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Menu_Search_FiltersToMatchingProduct()
    {
        await Page.GotoAsync(MenuUrl);

        await Page.GetByPlaceholder("Describe what you're craving").FillAsync(WebServerFixture.ProductAName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Search" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Product A should be present; Product B should not.
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = WebServerFixture.ProductAName }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = WebServerFixture.ProductBName }))
            .Not.ToBeVisibleAsync();
    }

    [Fact]
    public async Task Menu_ClearSearch_ShowsAllProducts()
    {
        // Start with an active search.
        await Page.GotoAsync($"{MenuUrl}?SearchTerm={Uri.EscapeDataString(WebServerFixture.ProductAName)}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // The "Clear" link should be visible when a search term is active.
        await Page.GetByRole(AriaRole.Link, new() { Name = "Clear" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // After clearing, both products should be visible again.
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = WebServerFixture.ProductAName }))
            .ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = WebServerFixture.ProductBName }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Menu_EachProduct_HasOrderButton()
    {
        await Page.GotoAsync(MenuUrl);

        // Expect at least one "Order" button in the table.
        var orderButtons = Page.GetByRole(AriaRole.Link, new() { Name = "Order" });
        await Expect(orderButtons.First).ToBeVisibleAsync();
    }
}
