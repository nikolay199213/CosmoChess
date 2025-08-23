using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class StartGameHandler(IGameRepository repository) : IRequestHandler<StartGameCommand, Guid>
    {
        public async Task<Guid> Handle(StartGameCommand request, CancellationToken cancellationToken)
        {
            var game = new Game(request.WhitePlayerId, request.BlackPlayerId);
            await repository.Save(game);
            return game.Id;
        }
    }
}
