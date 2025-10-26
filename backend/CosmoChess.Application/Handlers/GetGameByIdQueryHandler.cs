using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class GetGameByIdQueryHandler(IGameRepository repository) : IRequestHandler<GetGameByIdQuery, Game?>
    {
        public async Task<Game?> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
        {
            return await repository.GetById(request.GameId, cancellationToken);
        }
    }
}
