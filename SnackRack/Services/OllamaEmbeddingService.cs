using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SnackRack.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public OllamaEmbeddingService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var baseUrl = _config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var response = await _httpClient.PostAsJsonAsync(
            $"{baseUrl}/api/embeddings",
            new { model = "nomic-embed-text", prompt = text });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
        return result!.Embedding;
    }

    private record OllamaEmbeddingResponse(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
