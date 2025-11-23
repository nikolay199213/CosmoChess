using CosmoChess.Domain.Enums;
using MediatR;

namespace CosmoChess.Application.Commands
{
    public record CreateBotGameCommand(
        Guid CreatorId,
        BotDifficulty Difficulty,
        BotStyle Style = BotStyle.Balanced,
        TimeControl TimeControl = TimeControl.None
    ) : IRequest<Guid>;
}