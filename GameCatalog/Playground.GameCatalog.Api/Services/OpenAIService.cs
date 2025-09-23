using System.Text;
using OpenAI;
using OpenAI.Responses;

namespace Playground.GameCatalog.Api.Services;

public class OpenAIService(OpenAIClient _openAiClient)
{
    private const string EmbeddingModel = "text-embedding-3-large";
    private const int EmbeddingDimensions = 1998; // max supported in SQL Server 2025
    private const string RagChatModel = "gpt-4o-mini";

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

    public async Task<string> GenerateCatalogAnswerAsync(string userMessage, IEnumerable<string> contextSnippets)
    {
        #pragma warning disable OPENAI001 // Responses API marked experimental in SDK
        var responses = _openAiClient.GetOpenAIResponseClient(RagChatModel);

        var sb = new StringBuilder();
        sb.AppendLine("System:");
        sb.AppendLine("You are a helpful Game Catalog Assistant.");
        sb.AppendLine("- Only answer using the facts provided in the Context.");
        sb.AppendLine("- If the answer is not in the Context, reply exactly: I don't know, i can only answer questions related to my game catalog.");
        sb.AppendLine("- Refuse any non-game-related questions politely.");
        sb.AppendLine("- Keep answers concise.");
        sb.AppendLine();
        sb.AppendLine("Examples:");
        sb.AppendLine("Q: Do you have any racing games under $20?");
        sb.AppendLine("A: Yes, we have several racing games under $20. Gran Turismo 3 is one of them and is priced at $19.99.");
        sb.AppendLine("Q: Show me details for 'Stardew Valley'.");
        sb.AppendLine("A: Stardew Valley is an open-ended country-life RPG!");
        sb.AppendLine("Q: Can you give me some prime numbers?");
        sb.AppendLine("A: I don't know, i can only answer questions related to my game catalog.");
        sb.AppendLine();
        sb.Append("Question: ").Append(userMessage).AppendLine();
        sb.AppendLine("Context:");

        foreach (var s in contextSnippets)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                sb.Append("- ").AppendLine(s.Trim());
            }
        }

        var result = await responses.CreateResponseAsync(
            userInputText: sb.ToString(),
            new ResponseCreationOptions());

        foreach (var item in result.Value.OutputItems)
        {
            if (item is MessageResponseItem message)
            {
                var text = message.Content?.FirstOrDefault()?.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text!;
                }
            }
        }

        return "I don't know based on the catalog.";
        #pragma warning restore OPENAI001
    }
}

