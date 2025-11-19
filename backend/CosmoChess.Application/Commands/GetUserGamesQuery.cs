using CosmoChess.Domain.Entities;
using MediatR;

namespace CosmoChess.Application.Commands
{
    public record GetUserGamesQuery(Guid UserId, int Skip = 0, int Take = 20) : IRequest<IEnumerable<Game>>;
}
