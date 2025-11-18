using CosmoChess.Application.DTOs;
using MediatR;

namespace CosmoChess.Application.Commands
{
    public record GetGameByIdQuery(Guid GameId) : IRequest<GameWithPlayersDto?>;
}
