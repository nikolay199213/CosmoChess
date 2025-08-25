using CosmoChess.Domain.Entities;
using CosmoChess.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CosmoChess.Infrastructure.Persistence
{
    public class CosmoChessDbContext(DbContextOptions<CosmoChessDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new GameConfiguration());
        }
    }
}
