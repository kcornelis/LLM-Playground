using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

//using Microsoft.Data.SqlTypes;

namespace Playground.GameCatalog.Models;

public class Game
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string[]? Tags { get; set; }

    // [Column(TypeName = "vector(1998)")]
    // public SqlVector<float>? Embedding { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public bool? Windows { get; set; }
    public bool? Mac { get; set; }
    public bool? Linux { get; set; }
    public bool? SteamDeck { get; set; }

    [MaxLength(64)]
    public string Rating { get; set; }

    public int? PositiveRatio { get; set; }
    public int? Reviews { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Price { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? OriginalPrice { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Discount { get; set; }

    [NotMapped]
    public string[] AvailablePlatforms
    {
        get
        {
            return [.. (new[] {
                SteamDeck == true ? "SteamDeck" : null,
                Windows == true ? "Windows" : null,
                Mac == true ? "Mac" : null,
                Linux == true ? "Linux" : null
            })
            .Where(p => p != null)
            .Select(p => p!)];
        }
    }

    public string Describe()
    {
        return $"{Title} is a {string.Join(", ", Tags ?? [])} game. " +
               $"{(ReleaseDate.HasValue ? "Released in " + ReleaseDate.Value.Year : "Release date unknown")}. " +
               $"{(!string.IsNullOrWhiteSpace(Description) ? Description : "No description available.")} " +
               $"Available on {string.Join(", ", AvailablePlatforms)}. " +
               $"{(!string.IsNullOrWhiteSpace(Rating) ? "Rated " + Rating.ToLowerInvariant() : "Rating unknown")}" +
               $"{(Reviews > 0 ? " with " + Reviews + " reviews and a positive ratio of " + PositiveRatio + "%. " : ". ")}" +
               $"{(Price.HasValue ? (Price == OriginalPrice ? "It is currently priced at " + Price + ". " : "It is currently discounted from " + OriginalPrice + " to " + Price + ", that's a " + Discount + "% discount.") : string.Empty)}";
    }
}