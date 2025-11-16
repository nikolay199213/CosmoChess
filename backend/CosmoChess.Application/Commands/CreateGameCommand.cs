using CosmoChess.Domain.Enums;
using MediatR;

namespace CosmoChess.Application.Commands
{
    public record CreateGameCommand(Guid CreatorId, TimeControl TimeControl = TimeControl.None) : IRequest<Guid>;
}
