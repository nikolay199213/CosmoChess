using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Repositories;
using CosmoChess.Infrastructure.Persistence;

namespace CosmoChess.Infrastructure.Repositories
{
    public class GameRepository(CosmoChessDbContext dbContext) : IGameRepository
    {
        public async Task Save(Game game)
        {
            dbContext.Games.Add(game);
            await dbContext.SaveChangesAsync();
        }
    }
}
