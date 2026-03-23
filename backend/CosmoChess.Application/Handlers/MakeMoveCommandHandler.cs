using CosmoChess.Application.Commands;
using CosmoChess.Application.DTOs;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class MakeMoveCommandHandler(IGameRepository repository) : IRequestHandler<MakeMoveCommand, MakeMoveResult>
    {
        public async Task<MakeMoveResult> Handle(MakeMoveCommand request, CancellationToken cancellationToken)
        {
            var game = await repository.GetById(request.GameId, cancellationToken);
            game.MakeMove(request.UserId, request.Move, request.NewFen, request.IsCheckmate, request.IsStalemate, request.IsDraw);
            await repository.Update(game, cancellationToken);

            return new MakeMoveResult
            {
                GameId = game.Id,
                GameType = game.GameType,
                GameResult = game.GameResult,
                EndReason = game.EndReason,
                WhiteTimeRemainingSeconds = game.WhiteTimeRemainingSeconds,
                BlackTimeRemainingSeconds = game.BlackTimeRemainingSeconds,
                CurrentFen = game.CurrentFen,
                IsBotTurn = game.IsBotTurn(),
                BotDifficulty = game.BotDifficulty,
                BotStyle = game.BotStyle
            };
        }
    }
}
