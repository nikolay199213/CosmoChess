﻿﻿using CosmoChess.Api.Hubs;
using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CosmoChess.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GamesController(IMediator mediator, IHubContext<GameHub> hubContext) : ControllerBase
    {
        [HttpGet("wait-join")]
        public async Task<List<Game>> GetGamesForJoin()
        {
            var games = await mediator.Send(new GetGamesWaitJoinQuery());
            return games;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var game = await mediator.Send(new GetGameByIdQuery(id));
            if (game == null)
            {
                return NotFound();
            }
            return Ok(game);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateGameCommand command)
        {
            var gameId = await mediator.Send(command);
            return Ok(gameId);
        }

        [HttpPost("join")]
        public async Task<IActionResult> Join([FromBody] JoinGameCommand command)
        {
            await mediator.Send(command);

            // Notify all players in the game room that a new player joined
            var gameGroupName = $"game_{command.GameId}";
            await hubContext.Clients.Group(gameGroupName).SendAsync(
                "PlayerJoined",
                new
                {
                    gameId = command.GameId,
                    playerId = command.PlayerId
                });

            return Ok();
        }

        [HttpPost("move")]
        public async Task<IActionResult> Move([FromBody] MakeMoveCommand command)
        {
            await mediator.Send(command);

            // Notify all players in the game room about the move
            var gameGroupName = $"game_{command.GameId}";
            await hubContext.Clients.Group(gameGroupName).SendAsync(
                "MoveReceived",
                new
                {
                    gameId = command.GameId,
                    userId = command.UserId,
                    move = command.Move,
                    newFen = command.NewFen
                });

            return Ok();
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] AnalyzePositionDto dto, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(new AnalyzePositionCommand(dto.Fen, dto.Depth), cancellationToken);
            return Ok(new { bestMove = result });
        }

    }
    public record AnalyzePositionDto(string Fen, int Depth);

}
