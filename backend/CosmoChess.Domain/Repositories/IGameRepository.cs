using CosmoChess.Domain.Entities;

namespace CosmoChess.Domain.Repositories
{
    public interface IGameRepository
    {
        public Task Save(Game  game);
    }
}
