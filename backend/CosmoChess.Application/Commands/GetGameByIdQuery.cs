using CosmoChess.Domain.Entities;
using MediatR;

namespace CosmoChess.Application.Commands
{
    public record GetGameByIdQuery(Guid GameId) : IRequest<Game?>;
}
