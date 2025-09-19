using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Playground.GameCatalog.Models;

namespace Playground.GameCatalog.Api.Services;

public class CatalogService(ILogger<CatalogService> _logger, GameCatalogContext _db, OpenAIService _openAIService) : Catalog.CatalogBase
{
    public override async Task<SearchResponse> Search(SearchRequest request, ServerCallContext context)
    {
        // Move this to a seperate service that can be reused in other places.
        // Enable this when Aspire releases mssql 2025 (currently on main)
        // Already tested this codee without aspire and it works fine.
        
        _logger.LogInformation("Searching game catalog for {Query}", request.Query);

        var searchQueryEmbedding = await _openAIService.GenerateEmbeddingAsync(request.Query);

        var games = await _db.Games
            .Where(g => g.Title.Contains(request.Query))
            .Select(g => new Game
            {
                Id = g.Id,
                Title = g.Title,
                Description = g.Description,
            })
            // .Where(g => g.Embedding != null)
            // .OrderBy(g => EF.Functions.VectorDistance("cosine", g.Embedding.Value, embedding))
            .Take(100)
            .ToListAsync();

            // Get the score in code: VectorUtils.CosineSimilarity(g.Embedding.Value.Memory.ToArray(), response.Value.ToFloats().ToArray())

        var response = new SearchResponse();
        response.Items.AddRange(games);

        return response;
    }
}
