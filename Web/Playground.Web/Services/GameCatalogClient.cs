using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Polly.Timeout;

namespace Playground.Web.Services;

public class GameCatalogClient(Catalog.CatalogClient client)
{
    public async Task<Game[]> SearchAsync(string query)
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

    public async IAsyncEnumerable<GenerateEmbeddingsResponse> GenerateEmbeddingsStreamAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var call = client.GenerateEmbeddings(new Empty(), cancellationToken: cancellationToken);
        await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }
}
