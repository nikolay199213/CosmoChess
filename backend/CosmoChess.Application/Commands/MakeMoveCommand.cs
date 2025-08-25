using MediatR;

namespace CosmoChess.Application.Commands
{
    public record MakeMoveCommand(Guid GameId, Guid UserId, string Move, string NewFen) : IRequest<Unit>;

}
