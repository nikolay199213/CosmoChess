using CosmoChess.Application.Commands;
using CosmoChess.Application.DTOs;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class GetGameByIdQueryHandler(IGameRepository gameRepository, IUserRepository userRepository)
        : IRequestHandler<GetGameByIdQuery, GameWithPlayersDto?>
    {
        public async Task<GameWithPlayersDto?> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
        {
            var game = await gameRepository.GetById(request.GameId, cancellationToken);
            if (game == null) return null;

            // Get player usernames
            string? whiteUsername = null;
            string? blackUsername = null;

            var whitePlayer = await userRepository.GetByIdAsync(game.WhitePlayerId, cancellationToken);
            if (whitePlayer != null)
            {
                whiteUsername = whitePlayer.Username;
            }

            if (game.BlackPlayerId != Guid.Empty)
            {
                var blackPlayer = await userRepository.GetByIdAsync(game.BlackPlayerId, cancellationToken);
                if (blackPlayer != null)
                {
                    blackUsername = blackPlayer.Username;
                }
            }

            return new GameWithPlayersDto
            {
                Id = game.Id,
                StartedAt = game.StartedAt,
                Moves = game.Moves.Select(m => new GameMoveDto
                {
                    GameId = m.GameId,
                    UserId = m.UserId,
                    Move = m.Move,
                    ResultFen = m.ResultFen,
                    MadeAt = m.MadeAt
                }).ToList(),
                GameType = game.GameType,
                GameResult = game.GameResult,
                EndReason = game.EndReason,
                CurrentFen = game.CurrentFen,
                TimeControl = game.TimeControl,
                WhiteTimeRemainingSeconds = game.WhiteTimeRemainingSeconds,
                BlackTimeRemainingSeconds = game.BlackTimeRemainingSeconds,
                LastMoveTime = game.LastMoveTime,
                WhitePlayerId = game.WhitePlayerId,
                BlackPlayerId = game.BlackPlayerId,
                WhitePlayerUsername = whiteUsername,
                BlackPlayerUsername = blackUsername
            };
        }
    }
}
