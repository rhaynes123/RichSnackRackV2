using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using SnackRack.Services;

namespace SnackRack.Tests.Unit;

public class OllamaEmbeddingServiceTests
{
    private static IConfiguration BuildConfig(string baseUrl = "http://localhost:11434") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ollama:BaseUrl"] = baseUrl })
            .Build();

    private static HttpClient BuildClient(HttpResponseMessage response) =>
        new(new FakeHandler(response));

    // ── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEmbeddingAsync_ReturnsFloatArray_WhenOllamaResponds()
    {
        var expected = new float[] { 0.1f, 0.2f, 0.3f };
        var json = $"{{\"embedding\":[{string.Join(",", expected)}]}}";
        var response = OkJson(json);

        var sut = new OllamaEmbeddingService(BuildClient(response), BuildConfig());
        var result = await sut.GetEmbeddingAsync("salty snacks");

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetEmbeddingAsync_ReturnsCorrectDimensionCount()
    {
        var floats = Enumerable.Range(0, 768).Select(i => (float)i / 768).ToArray();
        var json = $"{{\"embedding\":[{string.Join(",", floats)}]}}";

        var sut = new OllamaEmbeddingService(BuildClient(OkJson(json)), BuildConfig());
        var result = await sut.GetEmbeddingAsync("test");

        Assert.Equal(768, result.Length);
    }

    // ── URL routing ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEmbeddingAsync_PostsToCorrectEndpoint()
    {
        string? capturedUrl = null;
        var handler = new CapturingHandler(OkJson("{\"embedding\":[0.1]}"), u => capturedUrl = u);

        var sut = new OllamaEmbeddingService(new HttpClient(handler), BuildConfig("http://my-ollama:5000"));
        await sut.GetEmbeddingAsync("test");

        Assert.Equal("http://my-ollama:5000/api/embeddings", capturedUrl);
    }

    [Fact]
    public async Task GetEmbeddingAsync_FallsBackToDefaultUrl_WhenConfigMissing()
    {
        string? capturedUrl = null;
        var handler = new CapturingHandler(OkJson("{\"embedding\":[0.1]}"), u => capturedUrl = u);
        var emptyConfig = new ConfigurationBuilder().Build();

        var sut = new OllamaEmbeddingService(new HttpClient(handler), emptyConfig);
        await sut.GetEmbeddingAsync("test");

        Assert.StartsWith("http://localhost:11434", capturedUrl);
    }

    // ── Error handling ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetEmbeddingAsync_ThrowsHttpRequestException_OnServerError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var sut = new OllamaEmbeddingService(BuildClient(response), BuildConfig());

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetEmbeddingAsync("test"));
    }

    [Fact]
    public async Task GetEmbeddingAsync_ThrowsHttpRequestException_OnNotFound()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var sut = new OllamaEmbeddingService(BuildClient(response), BuildConfig());

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetEmbeddingAsync("test"));
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class FakeHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(response);
    }

    private sealed class CapturingHandler(HttpResponseMessage response, Action<string> capture) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            capture(request.RequestUri?.ToString() ?? "");
            return Task.FromResult(response);
        }
    }
}
