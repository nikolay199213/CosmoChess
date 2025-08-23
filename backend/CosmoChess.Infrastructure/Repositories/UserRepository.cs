using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CosmoChess.Domain.Repositories;
using CosmoChess.Infrastructure.Persistence;

namespace CosmoChess.Infrastructure.Repositories
{
    public class UserRepository(CosmoChessDbContext dbContext) : IUserRepository
    {
    }
}
