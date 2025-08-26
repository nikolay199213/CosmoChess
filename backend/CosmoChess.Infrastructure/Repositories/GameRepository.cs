using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Enums;
using CosmoChess.Domain.Interface.Repositories;
using CosmoChess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CosmoChess.Infrastructure.Repositories
{
    public class GameRepository(CosmoChessDbContext dbContext) : IGameRepository
    {
        public async Task<Game?> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            return await dbContext.Games.FirstOrDefaultAsync(g=>g.Id == id, cancellationToken);
        }

        public async Task Update(Game game, CancellationToken cancellationToken = default)
        {
            dbContext.Games.Update(game);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<Game> Add(Game game, CancellationToken cancellationToken = default)
        {
            dbContext.Games.Add(game);
            await dbContext.SaveChangesAsync(cancellationToken);
            return game;
        }

        public async Task<IEnumerable<Game>> GetByGameType(GameType gameType, int skip = 0, int take = 10, CancellationToken cancellationToken = default)
        {
           return await dbContext.Games
               .Where(g => g.GameType == gameType)
               .OrderByDescending(g => g.StartedAt)
               .Skip(skip)
               .Take(take)
               .ToListAsync(cancellationToken);
        }
        public async Task<IEnumerable<Game>> GetByGameResult(GameResult gameResult, int skip = 0, int take = 10, CancellationToken cancellationToken = default)
        {
           return await dbContext.Games
               .Where(g => g.GameResult == gameResult)
               .OrderByDescending(g => g.StartedAt)
               .Skip(skip)
               .Take(take)
               .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Game>> GetByUserId(Guid userId, int skip = 0, int take = 10, CancellationToken cancellationToken = default)
        {
            return await dbContext.Games
                .Where(g => g.WhitePlayerId == userId || g.BlackPlayerId == userId)
                .OrderByDescending(g => g.StartedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }
    }
}
