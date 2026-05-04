using System.Net;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SnackRack.Services;

namespace SnackRack.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class EmbeddingServiceBenchmarks
{
    private OllamaEmbeddingService _service = null!;
    private string _json = null!;

    [GlobalSetup]
    public void Setup()
    {
        var floats = Enumerable.Range(0, 768).Select(i => (float)i / 768);
        _json = $"{{\"embedding\":[{string.Join(",", floats)}]}}";

        var client = new HttpClient(new FakeHandler(() => _json));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Ollama:BaseUrl"] = "http://localhost:11434" })
            .Build();

        _service = new OllamaEmbeddingService(client, config, NullLogger<OllamaEmbeddingService>.Instance);
    }

    [Benchmark]
    public Task<Result<float[]>> GetEmbedding() => _service.GetEmbeddingAsync("chocolate chip cookie");

    private sealed class FakeHandler(Func<string> jsonFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonFactory(), Encoding.UTF8, "application/json")
            });
    }
}
