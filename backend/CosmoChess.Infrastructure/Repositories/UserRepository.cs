using CosmoChess.Domain.Repositories;
using CosmoChess.Infrastructure.Persistence;

namespace CosmoChess.Infrastructure.Repositories
{
    public class UserRepository(CosmoChessDbContext dbContext) : IUserRepository
    {
    }
}
