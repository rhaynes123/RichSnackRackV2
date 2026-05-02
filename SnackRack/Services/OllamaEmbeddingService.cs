using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SnackRack.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(HttpClient httpClient, IConfiguration config, ILogger<OllamaEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<Result<float[]>> GetEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<float[]>.Failure("Cannot generate an embedding for null or empty text.");

        var baseUrl = _config["Ollama:BaseUrl"] ?? "http://localhost:11434";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{baseUrl}/api/embeddings",
                new { model = "nomic-embed-text", prompt = text });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama returned {StatusCode} for text '{Text}'.", (int)response.StatusCode, text);
                return Result<float[]>.Failure($"Ollama returned {(int)response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
            if (result is null)
            {
                _logger.LogWarning("Ollama returned a null response body for text '{Text}'.", text);
                return Result<float[]>.Failure("Ollama returned an empty response.");
            }

            return Result<float[]>.Success(result.Embedding);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP request to Ollama failed for text '{Text}'.", text);
            return Result<float[]>.Failure($"Ollama request failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize Ollama response for text '{Text}'.", text);
            return Result<float[]>.Failure($"Failed to parse Ollama response: {ex.Message}");
        }
    }

    private record OllamaEmbeddingResponse(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
