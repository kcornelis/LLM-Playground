using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Playground.GameCatalog.Api.Tools;
using Playground.GameCatalog.Models;

namespace Playground.GameCatalog.Api.Services;

public class CatalogService(ILogger<CatalogService> _logger, GameCatalogContext _db, OpenAIService _openAIService) : Catalog.CatalogBase
{
    public override async Task<SearchResponse> Search(SearchRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Searching game catalog for {Query}", request.Query);

        var searchQueryEmbedding = await _openAIService.GenerateEmbeddingAsync(request.Query);

        // Enable commented code when Aspire releases mssql 2025 (currently on main)
        // Already tested this code without aspire and it works fine.

        var games = await _db.Games
            .Where(g => g.Embedding != null)
            .OrderBy(g => EF.Functions.VectorDistance("cosine", g.Embedding.Value, new SqlVector<float>(searchQueryEmbedding)))
            .Take(100)
            .ToListAsync();

        var response = new SearchResponse();
        response.Items.AddRange(games.Select(g => new Game
        {
            Id = g.Id,
            Title = g.Title,
            Description = g.Description ?? "",
            Score = VectorUtils.CosineSimilarity(g.Embedding.Value.Memory.ToArray(), searchQueryEmbedding.ToArray())
        }));

        return response;
    }

    public override async Task GenerateEmbeddings(Empty request, IServerStreamWriter<GenerateEmbeddingsResponse> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Generating embeddings for all games in the catalog");

        var batchSize = 1000;
        var totalGames = await _db.Games.CountAsync();
        var processed = 0;

        while (processed < totalGames)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested, stopping embedding generation");
                break;
            }

            var games = await _db.Games
                .OrderBy(g => g.Id)
                .Skip(processed)
                .Take(batchSize)
                .ToListAsync();

            var texts = games.Select(g => g.Describe()).ToArray();
            var embeddings = await _openAIService.GenerateEmbeddingsAsync(texts);

            for (var i = 0; i < games.Count; i++)
            {
                games[i].Embedding = new SqlVector<float>(embeddings[i]);
            }

            await _db.SaveChangesAsync();
            processed += games.Count;

            await responseStream.WriteAsync(new GenerateEmbeddingsResponse
            {
                Processed = processed,
                Total = totalGames
            });
        }

        _logger.LogInformation("Completed generating embeddings for all games in the catalog");
    }
}