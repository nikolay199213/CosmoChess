using CosmoChess.Domain.Enums;
using MediatR;

namespace CosmoChess.Application.Commands
{
    public record CreateBotGameCommand(
        Guid CreatorId,
        BotDifficulty Difficulty,
        TimeControl TimeControl = TimeControl.None
    ) : IRequest<Guid>;
}