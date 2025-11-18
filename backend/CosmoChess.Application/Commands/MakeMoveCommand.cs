using MediatR;

namespace CosmoChess.Application.Commands
{
    public record MakeMoveCommand(
        Guid GameId,
        Guid UserId,
        string Move,
        string NewFen,
        bool IsCheckmate = false,
        bool IsStalemate = false,
        bool IsDraw = false
    ) : IRequest<Unit>;

}
