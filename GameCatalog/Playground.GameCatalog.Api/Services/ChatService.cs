using System.ComponentModel;
using Grpc.Core;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Playground.GameCatalog.Models;

namespace Playground.GameCatalog.Api.Services;

public class ChatService(ILogger<ChatService> _logger, GameCatalogContext _db, IChatClient _chatClient, EmbeddingService _embeddingService) : Chat.ChatBase
{
    public override async Task<GetResponseResponse> GetResponse(GetResponseRequest request, ServerCallContext context)
    {
        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create(SearchAsync)]
        };

        var chatResponse = await _chatClient.GetResponseAsync(request.Messages.Select(m => new ChatMessage(new ChatRole(m.Role), m.Content)), chatOptions);

        var response = new GetResponseResponse
        {
            Message = new Message
            {
                Role = ChatRole.Assistant.ToString(),
                Content = chatResponse.Text
            }
        };

        return response;
    }

    [Description("Searches for games using a search phrase or specific characteristics")]
    private async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("The minimum price to search for.")] decimal? minPrice = null,
        [Description("The maximum price to search for.")] decimal? maxPrice = null,
        [Description("The minimum release date to search for.")] DateTime? minReleaseDate = null,
        [Description("The maximum release date to search for.")] DateTime? maxReleaseDate = null)
    {
        _logger.LogInformation("Searching for games with phrase: {SearchPhrase}, MinPrice: {MinPrice}, MaxPrice: {MaxPrice}, MinReleaseDate: {MinReleaseDate}, MaxReleaseDate: {MaxReleaseDate}",
            searchPhrase, minPrice, maxPrice, minReleaseDate, maxReleaseDate);

        var searchPhraseEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchPhrase);

        var query = _db.Games
            .AsNoTracking()
            .Where(g => g.Embedding != null);

        if (minPrice.HasValue)
            query = query.Where(g => g.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(g => g.Price <= maxPrice.Value);

        if (minReleaseDate.HasValue)
            query = query.Where(g => g.ReleaseDate >= minReleaseDate.Value);

        if (maxReleaseDate.HasValue)
            query = query.Where(g => g.ReleaseDate <= maxReleaseDate.Value);

        var games = await query
            .OrderBy(g => EF.Functions.VectorDistance("cosine", g.Embedding!.Value, new SqlVector<float>(searchPhraseEmbedding)))
            .Take(20)
            .ToListAsync();

        return [.. games.Select(g => $@"Title: {g.Title}
Release date: {(g.ReleaseDate.HasValue ? g.ReleaseDate.Value.Year : "Unknown release date")}
Tags: {string.Join(", ", g.Tags ?? [])}
Description: {(!string.IsNullOrWhiteSpace(g.Description) ? g.Description : "No description available.")}
Platforms: {string.Join(", ", g.AvailablePlatforms)}
Rating: {(!string.IsNullOrWhiteSpace(g.Rating) ? "Rated " + g.Rating.ToLowerInvariant() : "Rating unknown")}
Reviews: {(g.Reviews > 0 ? g.Reviews + " reviews and a positive ratio of " + g.PositiveRatio + "%" : "No reviews available")}
Price: {(g.Price.HasValue ? (g.Price >= g.OriginalPrice ? g.Price : "Discounted from " + g.OriginalPrice + " to " + g.Price + " (" + g.Discount + "% discount)") : string.Empty)}"
            )];
    }
}
