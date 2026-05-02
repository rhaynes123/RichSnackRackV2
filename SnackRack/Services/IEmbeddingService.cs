namespace SnackRack.Services;

public interface IEmbeddingService
{
    Task<Result<float[]>> GetEmbeddingAsync(string text);
}
