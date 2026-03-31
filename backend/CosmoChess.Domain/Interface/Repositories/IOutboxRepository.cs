using CosmoChess.Domain.Entities;

namespace CosmoChess.Domain.Interface.Repositories
{
    public interface IOutboxRepository
    {
        Task Add(OutboxMessage message, CancellationToken cancellationToken = default);
    }
}
