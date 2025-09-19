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
    public string? Title { get; set; }

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

    public string[] GetAvailablePlatforms()
    {
        return (new[] {
            SteamDeck == true ? "SteamDeck" : null,
            Windows == true ? "Windows" : null,
            Mac == true ? "Mac" : null,
            Linux == true ? "Linux" : null
        }).Where(p => p != null).ToArray();
    }
}