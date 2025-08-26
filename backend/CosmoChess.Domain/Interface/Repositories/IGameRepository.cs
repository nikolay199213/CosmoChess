using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Enums;

namespace CosmoChess.Domain.Interface.Repositories
{
    public interface IGameRepository
    {
        Task<Game?> GetById(Guid id, CancellationToken cancellationToken = default);
        Task Update(Game game, CancellationToken cancellationToken = default);
        Task<Game> Add(Game game, CancellationToken cancellationToken = default);
        Task<IEnumerable<Game>> GetByGameType(GameType gameType, int skip = 0, int take = 10, CancellationToken cancellationToken = default);
        Task<IEnumerable<Game>> GetByGameResult(GameResult gameResult, int skip = 0, int take = 10, CancellationToken cancellationToken = default);
        Task<IEnumerable<Game>> GetByUserId(Guid userId, int skip = 0, int take = 10, CancellationToken cancellationToken = default);
    }
}
