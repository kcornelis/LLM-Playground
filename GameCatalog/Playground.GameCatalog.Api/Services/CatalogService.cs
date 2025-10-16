using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Playground.GameCatalog.Api.Tools;
using Playground.GameCatalog.Models;

namespace Playground.GameCatalog.Api.Services;

public class CatalogService(ILogger<CatalogService> _logger, GameCatalogContext _db, EmbeddingService _embeddingService, ResponseService _responseService) : Catalog.CatalogBase
{
    public override async Task<SimpleSemanticSearchResponse> SimpleSemanticSearch(SimpleSemanticSearchRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Searching game catalog for {Query}", request.Query);

        var searchQueryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Query);

        var games = await _db.Games
            .AsNoTracking()
            .Where(g => g.Embedding != null)
            .OrderBy(g => EF.Functions.VectorDistance("cosine", g.Embedding!.Value, new SqlVector<float>(searchQueryEmbedding)))
            .Take(20)
            .ToListAsync();

        var response = new SimpleSemanticSearchResponse();
        response.Items.AddRange(games.Select(g => new Game
        {
            Id = g.Id,
            Title = g.Title,
            Description = g.Describe(),
            Score = VectorUtils.CosineSimilarity(g.Embedding!.Value.Memory.ToArray(), searchQueryEmbedding.ToArray())
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
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);

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

    public override async Task<SimpleRagResponse> SimpleRag(SimpleRagRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Answer catalog question request: {Message}", request.Question);

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return new SimpleRagResponse { Answer = "Please provide a question about the game catalog." };
        }

        var chatMessageEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question);

        // Retrieve candidate set and score in-memory via cosine similarity (portable fallback)
        var candidates = await _db.Games
            .AsNoTracking()
            .Where(g => g.Embedding != null)
            .OrderBy(g => EF.Functions.VectorDistance("cosine", g.Embedding!.Value, new SqlVector<float>(chatMessageEmbedding)))
            .Take(20)
            .ToListAsync(context.CancellationToken);

        var snippets = candidates.Select(g => g.Describe()).ToList();

        // Ask LLM constrained by context
        var answer = await _responseService.GenerateAnswerAsync(request.SystemPrompt, request.FewShotExamples, request.Question, snippets);

        var response = new SimpleRagResponse { Answer = answer };
        // Add found games descriptions for debugging purposes
        response.Sources.AddRange(candidates.Select(g => g.Describe()));

        return response;
    }
}