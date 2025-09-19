using Microsoft.EntityFrameworkCore;

namespace Playground.GameCatalog.Models;

public class GameCatalogContext : DbContext
{
    public GameCatalogContext(DbContextOptions<GameCatalogContext> options)
        : base(options)
    {
    }

    public DbSet<Game> Games { get; set; }
}
