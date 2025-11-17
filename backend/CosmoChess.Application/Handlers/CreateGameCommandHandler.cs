using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class CreateGameCommandHandler(IGameRepository repository) : IRequestHandler<CreateGameCommand, Guid>
    {
        public async Task<Guid> Handle(CreateGameCommand request, CancellationToken cancellationToken)
        {
            var game = new Game(request.CreatorId, request.TimeControl);
            await repository.Add(game, cancellationToken);
            return game.Id;
        }
    }
}
