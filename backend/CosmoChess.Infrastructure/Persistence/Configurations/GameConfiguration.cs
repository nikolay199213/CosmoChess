using CosmoChess.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CosmoChess.Infrastructure.Persistence.Configurations
{
    internal class GameConfiguration : IEntityTypeConfiguration<Game>
    {
        public void Configure(EntityTypeBuilder<Game> builder)
        {
            builder.ToTable("games");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CurrentFen).IsRequired();
            builder.Property(x => x.WhitePlayerId).IsRequired();
            builder.Property(x => x.BlackPlayerId).IsRequired();
        }
    }
}
