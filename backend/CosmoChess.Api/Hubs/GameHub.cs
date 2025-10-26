using CosmoChess.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CosmoChess.Api.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GameHub> _logger;

        public GameHub(IMediator mediator, ILogger<GameHub> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} connected to GameHub with connection {ConnectionId}", userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from GameHub. Exception: {Exception}", userId, exception?.Message);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join a game room to receive real-time updates
        /// </summary>
        /// <param name="gameId">The ID of the game to join</param>
        public async Task JoinGame(string gameId)
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} joining game {GameId}", userId, gameId);

            try
            {
                // Add connection to game-specific group
                await Groups.AddToGroupAsync(Context.ConnectionId, GetGameGroupName(gameId));

                // Notify others in the game that a player joined
                await Clients.OthersInGroup(GetGameGroupName(gameId)).SendAsync("PlayerJoined", new
                {
                    gameId,
                    playerId = userId,
                    connectionId = Context.ConnectionId
                });

                _logger.LogInformation("User {UserId} successfully joined game {GameId}", userId, gameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while user {UserId} was joining game {GameId}", userId, gameId);
                throw;
            }
        }

        /// <summary>
        /// Leave a game room
        /// </summary>
        /// <param name="gameId">The ID of the game to leave</param>
        public async Task LeaveGame(string gameId)
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} leaving game {GameId}", userId, gameId);

            try
            {
                // Remove connection from game-specific group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGameGroupName(gameId));

                // Notify others in the game that a player left
                await Clients.OthersInGroup(GetGameGroupName(gameId)).SendAsync("PlayerLeft", new
                {
                    gameId,
                    playerId = userId,
                    connectionId = Context.ConnectionId
                });

                _logger.LogInformation("User {UserId} successfully left game {GameId}", userId, gameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while user {UserId} was leaving game {GameId}", userId, gameId);
                throw;
            }
        }

        /// <summary>
        /// Get the current user's ID from JWT claims
        /// </summary>
        private string GetUserId()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst("sub")?.Value
                ?? Context.User?.FindFirst("userId")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Unable to extract userId from claims. Available claims: {Claims}",
                    string.Join(", ", Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
            }

            return userId ?? string.Empty;
        }

        /// <summary>
        /// Get the SignalR group name for a specific game
        /// </summary>
        private static string GetGameGroupName(string gameId) => $"game_{gameId}";
    }
}
