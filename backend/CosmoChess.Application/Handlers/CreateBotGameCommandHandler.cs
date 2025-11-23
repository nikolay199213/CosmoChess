using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class CreateBotGameCommandHandler(IGameRepository repository) : IRequestHandler<CreateBotGameCommand, Guid>
    {
        public async Task<Guid> Handle(CreateBotGameCommand request, CancellationToken cancellationToken)
        {
            var game = new Game(request.CreatorId, request.Difficulty, request.Style, request.TimeControl);
            await repository.Add(game, cancellationToken);
            return game.Id;
        }
    }
}