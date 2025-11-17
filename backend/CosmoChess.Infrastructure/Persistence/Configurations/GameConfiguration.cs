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

            // Timer fields
            builder.Property(x => x.TimeControl).IsRequired();
            builder.Property(x => x.WhiteTimeRemainingSeconds).IsRequired();
            builder.Property(x => x.BlackTimeRemainingSeconds).IsRequired();
            builder.Property(x => x.LastMoveTime);

            // Configure the Moves collection
            builder.OwnsMany(g => g.Moves, moves =>
            {
                moves.ToTable("game_moves");
                moves.WithOwner().HasForeignKey("GameId");
                moves.Property<int>("Id").ValueGeneratedOnAdd();
                moves.HasKey("Id");
                moves.Property(m => m.Move).IsRequired();
                moves.Property(m => m.FenAfterMove).IsRequired();
                moves.Property(m => m.UserId).IsRequired();
                moves.Property(m => m.MadeAt).IsRequired();
            });
        }
    }
}
