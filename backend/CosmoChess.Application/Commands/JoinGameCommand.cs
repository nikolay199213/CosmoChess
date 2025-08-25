using MediatR;

namespace CosmoChess.Application.Commands
{
    public record JoinGameCommand(Guid GameId, Guid PlayerId) : IRequest<Unit>;
}

