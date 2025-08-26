using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Enums;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class GetGamesWaitHandler(IGameRepository repository) : IRequestHandler<GetGamesWaitJoinQuery, List<Game>>
    {
        public async Task<List<Game>> Handle(GetGamesWaitJoinQuery request, CancellationToken cancellationToken)
        {
            return (await repository.GetByGameResult(GameResult.WaitJoin, 0, 10, cancellationToken)).ToList();
        }
    }
}
