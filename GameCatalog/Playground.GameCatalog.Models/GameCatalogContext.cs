using Microsoft.EntityFrameworkCore;

namespace Playground.GameCatalog.Models;

public class GameCatalogContext(DbContextOptions<GameCatalogContext> options) : DbContext(options)
{
    public DbSet<Game> Games { get; set; }
}
