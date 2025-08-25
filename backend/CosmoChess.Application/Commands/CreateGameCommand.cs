using MediatR;

namespace CosmoChess.Application.Commands
{
    public record CreateGameCommand(Guid CreatorId) : IRequest<Guid>;
}
