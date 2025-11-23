using CosmoChess.Api.Hubs;
using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Enums;
using CosmoChess.Domain.Interface.Repositories;
using CosmoChess.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CosmoChess.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GamesController(
        IMediator mediator,
        IHubContext<GameHub> hubContext,
        IUserRepository userRepository,
        IBotMoveService botMoveService,
        IGameRepository gameRepository) : ControllerBase
    {
        [HttpGet("wait-join")]
        public async Task<List<Game>> GetGamesForJoin()
        {
            var games = await mediator.Send(new GetGamesWaitJoinQuery());
            return games;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserGames(Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var games = await mediator.Send(new GetUserGamesQuery(userId, skip, take));
            return Ok(games);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var gameDto = await mediator.Send(new GetGameByIdQuery(id));
            if (gameDto == null)
            {
                return NotFound();
            }
            return Ok(gameDto);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateGameCommand command)
        {
            var gameId = await mediator.Send(command);
            return Ok(gameId);
        }

        [HttpPost("vs-bot")]
        public async Task<IActionResult> CreateBotGame([FromBody] CreateBotGameDto dto)
        {
            var command = new CreateBotGameCommand(dto.CreatorId, dto.Difficulty, dto.Style, dto.TimeControl);
            var gameId = await mediator.Send(command);
            return Ok(gameId);
        }

        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] JoinGameCommand command)
        {
            await mediator.Send(command);

            // Get username for the joining player
            var joiningPlayer = await userRepository.GetByIdAsync(command.PlayerId);
            var username = joiningPlayer?.Username ?? "Player";

            // Notify all players in the game room that a new player joined
            var gameGroupName = $"game_{command.GameId}";
            await hubContext.Clients.Group(gameGroupName).SendAsync(
                "PlayerJoined",
                new
                {
                    gameId = command.GameId,
                    playerId = command.PlayerId,
                    username = username
                });

            return Ok();
        }

        [HttpPost("move")]
        public async Task<IActionResult> Move([FromBody] MakeMoveDto dto)
        {
            var command = new MakeMoveCommand(
                dto.GameId,
                dto.UserId,
                dto.Move,
                dto.NewFen,
                dto.IsCheckmate,
                dto.IsStalemate,
                dto.IsDraw
            );

            await mediator.Send(command);

            // Get updated game state with timers
            var game = await mediator.Send(new GetGameByIdQuery(dto.GameId));

            // Notify all players in the game room about the move
            var gameGroupName = $"game_{dto.GameId}";
            await hubContext.Clients.Group(gameGroupName).SendAsync(
                "MoveReceived",
                new
                {
                    gameId = dto.GameId,
                    userId = dto.UserId,
                    move = dto.Move,
                    newFen = dto.NewFen,
                    whiteTimeRemainingSeconds = game?.WhiteTimeRemainingSeconds ?? 0,
                    blackTimeRemainingSeconds = game?.BlackTimeRemainingSeconds ?? 0
                });

            // Notify about game state change if game ended
            if (game != null && (int)game.GameResult >= 2)
            {
                await hubContext.Clients.Group(gameGroupName).SendAsync(
                    "GameStateChanged",
                    new
                    {
                        gameId = dto.GameId,
                        gameResult = (int)game.GameResult,
                        endReason = (int)game.EndReason
                    });
            }

            // If it's a bot game and game is still in progress, trigger bot move
            if (game != null && game.GameType == GameType.HumanVsBot && (int)game.GameResult < 2)
            {
                var gameEntity = await gameRepository.GetById(dto.GameId, CancellationToken.None);
                if (gameEntity.IsBotTurn() && gameEntity.BotDifficulty.HasValue)
                {
                    var botRequest = new BotMoveRequest
                    {
                        GameId = dto.GameId,
                        CurrentFen = dto.NewFen,
                        Difficulty = gameEntity.BotDifficulty.Value,
                        Style = gameEntity.BotStyle ?? BotStyle.Balanced
                    };

                    // Process bot move asynchronously
                    botMoveService.EnqueueBotMove(botRequest, async result =>
                    {
                        // Send bot move to clients
                        await hubContext.Clients.Group(gameGroupName).SendAsync(
                            "MoveReceived",
                            new
                            {
                                gameId = result.GameId,
                                userId = Game.BotPlayerId,
                                move = result.Move,
                                newFen = result.NewFen,
                                whiteTimeRemainingSeconds = result.WhiteTimeRemainingSeconds,
                                blackTimeRemainingSeconds = result.BlackTimeRemainingSeconds
                            });

                        // Notify about game state change if game ended
                        if (result.GameResult.HasValue)
                        {
                            await hubContext.Clients.Group(gameGroupName).SendAsync(
                                "GameStateChanged",
                                new
                                {
                                    gameId = result.GameId,
                                    gameResult = (int)result.GameResult.Value,
                                    endReason = (int)(result.EndReason ?? GameEndReason.None)
                                });
                        }
                    });
                }
            }

            return Ok();
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] AnalyzePositionDto dto, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new AnalyzePositionCommand(dto.Fen, dto.Depth), cancellationToken);
            return Ok(new { bestMove = result });
        }

        [HttpPost("analyze-multipv")]
        public async Task<IActionResult> AnalyzeMultiPv([FromBody] AnalyzeMultiPvDto dto, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new AnalyzeMultiPvCommand(dto.Fen, dto.Depth, dto.MultiPv), cancellationToken);
            return Ok(result);
        }

    }

    public record MakeMoveDto(
        Guid GameId,
        Guid UserId,
        string Move,
        string NewFen,
        bool IsCheckmate = false,
        bool IsStalemate = false,
        bool IsDraw = false
    );

    public record CreateBotGameDto(
        Guid CreatorId,
        BotDifficulty Difficulty,
        BotStyle Style = BotStyle.Balanced,
        TimeControl TimeControl = TimeControl.None
    );

    public record AnalyzePositionDto(string Fen, int Depth);
    public record AnalyzeMultiPvDto(string Fen, int Depth, int MultiPv = 3);

}
