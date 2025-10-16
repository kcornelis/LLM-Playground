using OpenAI;

namespace Playground.GameCatalog.Api.Services;

public class EmbeddingService(OpenAIClient _openAiClient)
{
    private const string EmbeddingModel = "text-embedding-3-large";
    private const int EmbeddingDimensions = 1998; // max supported in SQL Server 2025

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        var client = _openAiClient.GetEmbeddingClient(EmbeddingModel);
        var response = await client.GenerateEmbeddingAsync(text, new OpenAI.Embeddings.EmbeddingGenerationOptions
        {
            Dimensions = EmbeddingDimensions
        });

        return response.Value.ToFloats();
    }

    public async Task<ReadOnlyMemory<float>[]> GenerateEmbeddingsAsync(string[] texts)
    {
        var client = _openAiClient.GetEmbeddingClient(EmbeddingModel);
        var response = await client.GenerateEmbeddingsAsync(texts, new OpenAI.Embeddings.EmbeddingGenerationOptions
        {
            Dimensions = EmbeddingDimensions
        });

        return [.. response.Value.Select(e => e.ToFloats())];
    }
}
