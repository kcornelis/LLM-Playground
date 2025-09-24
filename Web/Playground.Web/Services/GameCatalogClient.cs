using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Polly.Timeout;

namespace Playground.Web.Services;

public class GameCatalogClient(Catalog.CatalogClient client)
{
    public async Task<Game[]> Search(string query)
    {
        try
        {
            var response = await client.SearchAsync(new SearchRequest { Query = query });
            return [.. response.Items];
        }
        catch (RpcException ex) when (
            // Service name could not be resolved
            ex.StatusCode is StatusCode.Unavailable ||
            // Polly resilience timed out after retries
            (ex.StatusCode is StatusCode.Internal && ex.Status.DebugException is TimeoutRejectedException))
        {
            return [];
        }
    }

    public async IAsyncEnumerable<GenerateEmbeddingsResponse> GenerateEmbeddingsStream([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var call = client.GenerateEmbeddings(new Empty(), cancellationToken: cancellationToken);
        await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }

    public record CatalogQuestionResult(string Answer, string[] Sources);

    public async Task<CatalogQuestionResult> AnswerCatalogQuestion(string systemPrompt, string fewShotExamples, string question, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.AnswerCatalogQuestionAsync(new AnswerCatalogQuestionRequest { SystemPrompt = systemPrompt, FewShotExamples = fewShotExamples, Question = question }, cancellationToken: cancellationToken);
            return new CatalogQuestionResult(response.Answer, [.. response.Sources]);
        }
        catch (RpcException ex) when (
            ex.StatusCode is StatusCode.Unavailable ||
            (ex.StatusCode is StatusCode.Internal && ex.Status.DebugException is TimeoutRejectedException))
        {
            return new CatalogQuestionResult("I couldn't reach the catalog service. Please try again.", []);
        }
    }
}
