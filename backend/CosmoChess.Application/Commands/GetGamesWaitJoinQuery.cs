using MediatR;
using CosmoChess.Domain.Entities;

namespace CosmoChess.Application.Commands
{
    public record GetGamesWaitJoinQuery : IRequest<List<Game>>;
}
