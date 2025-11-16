using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Repositories;
using CosmoChess.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CosmoChess.Infrastructure.Repositories
{
    public class UserRepository(CosmoChessDbContext dbContext) : IUserRepository
    {
        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        }

        public async Task<User?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
        {
            return await dbContext.Users
                .FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);
        }

        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            dbContext.Update(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
