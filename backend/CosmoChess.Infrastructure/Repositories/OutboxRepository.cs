using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Repositories;
using CosmoChess.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmoChess.Infrastructure.Repositories
{
    public class OutboxRepository(CosmoChessDbContext dbContext) : IOutboxRepository
    {
        public Task Add(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            dbContext.OutboxMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
