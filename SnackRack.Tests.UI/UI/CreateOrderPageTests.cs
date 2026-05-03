namespace SnackRack.Tests.UI.UI;

/// <summary>
/// Playwright UI tests for the Create Order page.
/// Covers the product dropdown, AJAX add-item flow, and quantity increment.
/// </summary>
[Collection("UI")]
public class CreateOrderPageTests(WebServerFixture server) : PageTest
{
    private string CreateUrl => $"{server.ServerAddress}/Features/Orders/Create";

    [Fact]
    public async Task CreateOrder_ShowsProductDropdown_WithSeededProducts()
    {
        await Page.GotoAsync(CreateUrl);

        var select = Page.Locator("#productSelect");
        await Expect(select).ToBeVisibleAsync();

        // Both seeded products should be options in the dropdown.
        await Expect(select.Locator($"option:has-text(\"{WebServerFixture.ProductAName}\")"))
            .ToHaveCountAsync(1);
        await Expect(select.Locator($"option:has-text(\"{WebServerFixture.ProductBName}\")"))
            .ToHaveCountAsync(1);
    }

    [Fact]
    public async Task CreateOrder_AddItem_AppearsInOrderTable()
    {
        await Page.GotoAsync(CreateUrl);

        await Page.SelectOptionAsync("#productSelect", new SelectOptionValue { Label = $"{WebServerFixture.ProductAName} — $1.99" });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        // Wait for the AJAX response to update the DOM.
        var row = Page.Locator("#orderItems tr", new() { HasText = WebServerFixture.ProductAName });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.Locator("td").Nth(2)).ToHaveTextAsync("1"); // Qty column
    }

    [Fact]
    public async Task CreateOrder_AddSameItemTwice_IncrementsQuantityToTwo()
    {
        await Page.GotoAsync(CreateUrl);

        await Page.SelectOptionAsync("#productSelect", new SelectOptionValue { Label = $"{WebServerFixture.ProductAName} — $1.99" });

        var addButton = Page.GetByRole(AriaRole.Button, new() { Name = "Add" });

        // Use RunAndWaitForResponseAsync so the listener is registered before the click fires.
        await Page.RunAndWaitForResponseAsync(
            () => addButton.ClickAsync(),
            r => r.Url.Contains("?handler=AddItem"));

        await Page.RunAndWaitForResponseAsync(
            () => addButton.ClickAsync(),
            r => r.Url.Contains("?handler=AddItem"));

        var row = Page.Locator("#orderItems tr", new() { HasText = WebServerFixture.ProductAName });
        await Expect(row.Locator("td").Nth(2)).ToHaveTextAsync("2"); // Qty column
    }

    [Fact]
    public async Task CreateOrder_AddTwoDistinctItems_ShowsTwoRows()
    {
        await Page.GotoAsync(CreateUrl);

        var addButton = Page.GetByRole(AriaRole.Button, new() { Name = "Add" });

        await Page.SelectOptionAsync("#productSelect", new SelectOptionValue { Label = $"{WebServerFixture.ProductAName} — $1.99" });
        await Page.RunAndWaitForResponseAsync(
            () => addButton.ClickAsync(),
            r => r.Url.Contains("?handler=AddItem"));

        await Page.SelectOptionAsync("#productSelect", new SelectOptionValue { Label = $"{WebServerFixture.ProductBName} — $2.49" });
        await Page.RunAndWaitForResponseAsync(
            () => addButton.ClickAsync(),
            r => r.Url.Contains("?handler=AddItem"));

        await Expect(Page.Locator("#orderItems tr", new() { HasText = WebServerFixture.ProductAName })).ToBeVisibleAsync();
        await Expect(Page.Locator("#orderItems tr", new() { HasText = WebServerFixture.ProductBName })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateOrder_SubmitButton_IsVisible()
    {
        await Page.GotoAsync(CreateUrl);

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Submit Order" })).ToBeVisibleAsync();
    }
}
