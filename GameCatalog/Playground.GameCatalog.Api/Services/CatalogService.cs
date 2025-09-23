using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
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
            .AsNoTracking()
            .Where(g => g.Title.Contains(request.Query)) // remove when we use sql server 2025
            //.Where(g => g.Embedding != null)
            //.OrderBy(g => EF.Functions.VectorDistance("cosine", g.Embedding.Value, new SqlVector<float>(searchQueryEmbedding)))
            .Take(20)
            .ToListAsync();

        var response = new SearchResponse();
        response.Items.AddRange(games.Select(g => new Game
        {
            Id = g.Id,
            Title = g.Title,
            Description = g.Description ?? "",
            Score = 0,//Score = VectorUtils.CosineSimilarity(g.Embedding.Value.Memory.ToArray(), searchQueryEmbedding.ToArray())
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

    public override async Task<AnswerCatalogQuestionResponse> AnswerCatalogQuestion(AnswerCatalogQuestionRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Answer catalog question request: {Message}", request.Message);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new AnswerCatalogQuestionResponse { Answer = "Please provide a question about the game catalog." };
        }

        var chatMessageEmbedding = await _openAIService.GenerateEmbeddingAsync(request.Message);

        // Retrieve candidate set and score in-memory via cosine similarity (portable fallback)
        var candidates = await _db.Games
            .AsNoTracking()
            .Where(g => g.Title.Contains("fifa")) // remove when we use sql server 2025
            //.Where(g => g.Embedding != null)
            //.OrderBy(g => EF.Functions.VectorDistance("cosine", g.Embedding.Value, new SqlVector<float>(searchQueryEmbedding)))
            .Take(20)
            .ToListAsync(context.CancellationToken);

        var snippets = candidates.Select(g => g.Describe()).ToList();

        // Ask LLM constrained by context
        var answer = await _openAIService.GenerateCatalogAnswerAsync(request.Message, snippets);

        var response = new AnswerCatalogQuestionResponse { Answer = answer };
        // Add found games descriptions for debugging purposes
        response.Sources.AddRange(candidates.Select(g => g.Describe()));

        return response;
    }
}