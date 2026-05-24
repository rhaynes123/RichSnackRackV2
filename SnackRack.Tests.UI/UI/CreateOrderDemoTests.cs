namespace SnackRack.Tests.UI.UI;

/// <summary>
/// Headed, slow-motion demo of the Create Order product-selection flow.
/// Run with: dotnet test --filter "FullyQualifiedName~CreateOrderDemoTests"
/// Watch the browser window to see each step.
/// </summary>
[Trait("Category", "Demo")]
[Collection("UI")]
public class CreateOrderDemoTests(WebServerFixture server)
{
    private string CreateUrl => $"{server.ServerAddress}/Features/Orders/Create";

    [Fact]
    public async Task Demo_SelectProductAndAddToOrder()
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,
            SlowMo = 800
        });

        var page = await browser.NewPageAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

        await page.GotoAsync(CreateUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Select the first test product.
        var select = page.Locator("#productSelect");
        await select.WaitForAsync();
        await page.SelectOptionAsync("#productSelect",
            new SelectOptionValue { Label = $"{WebServerFixture.ProductAName} — $1.99" });

        // Click Add and wait for the AJAX response.
        var addButton = page.GetByRole(AriaRole.Button, new() { Name = "Add" });
        await page.RunAndWaitForResponseAsync(
            () => addButton.ClickAsync(),
            r => r.Url.Contains("?handler=AddItem"));

        // Verify row appeared.
        var rowA = page.Locator("#orderItems tr", new() { HasText = WebServerFixture.ProductAName });
        await rowA.WaitForAsync();

        // Select and add the second product.
        await page.SelectOptionAsync("#productSelect",
            new SelectOptionValue { Label = $"{WebServerFixture.ProductBName} — $2.49" });

        await page.RunAndWaitForResponseAsync(
            () => addButton.ClickAsync(),
            r => r.Url.Contains("?handler=AddItem"));

        var rowB = page.Locator("#orderItems tr", new() { HasText = WebServerFixture.ProductBName });
        await rowB.WaitForAsync();

        // Linger so you can inspect the final state before the browser closes.
        await Task.Delay(2000);
    }
}
