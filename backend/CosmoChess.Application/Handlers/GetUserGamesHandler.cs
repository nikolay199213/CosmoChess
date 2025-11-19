using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class GetUserGamesHandler(IGameRepository gameRepository) : IRequestHandler<GetUserGamesQuery, IEnumerable<Game>>
    {
        public async Task<IEnumerable<Game>> Handle(GetUserGamesQuery request, CancellationToken cancellationToken)
        {
            return await gameRepository.GetByUserId(request.UserId, request.Skip, request.Take, cancellationToken);
        }
    }
}
