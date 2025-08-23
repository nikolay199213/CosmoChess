using MediatR;

namespace CosmoChess.Application.Commands
{
    public record StartGameCommand(Guid WhitePlayerId, Guid BlackPlayerId) : IRequest<Guid>;
}
