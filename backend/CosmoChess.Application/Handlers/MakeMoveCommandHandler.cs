using System.Text.Json;
using CosmoChess.Application.Commands;
using CosmoChess.Application.DTOs;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Enums;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class MakeMoveCommandHandler(IGameRepository gameRepository, IOutboxRepository outboxRepository) : IRequestHandler<MakeMoveCommand, MakeMoveResult>
    {
        public async Task<MakeMoveResult> Handle(MakeMoveCommand request, CancellationToken cancellationToken)
        {
            var game = await gameRepository.GetById(request.GameId, cancellationToken);
            game.MakeMove(request.UserId, request.Move, request.NewFen, request.IsCheckmate, request.IsStalemate, request.IsDraw);

            var gameEnded = (int)game.GameResult >= 2;

            if (game.GameType == GameType.HumanVsBot && !gameEnded && game.IsBotTurn() &&
                game.BotDifficulty.HasValue)
            {
                var payload = JsonSerializer.Serialize(new
                {
                    GameId = game.Id,
                    Fen = game.CurrentFen,
                    Difficulty = (int)game.BotDifficulty.Value,
                    Style = (int)(game.BotStyle ?? BotStyle.Balanced),
                    Timestamp = DateTime.UtcNow
                });

                await outboxRepository.Add(new OutboxMessage
                {
                    Type = "BotMoveRequest",
                    Payload = payload
                }, cancellationToken);
            }

            await gameRepository.Update(game, cancellationToken);

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
