using CosmoChess.Application.Commands;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class JoinGameCommandHandler(IGameRepository repository) : IRequestHandler<JoinGameCommand, Unit>
    {
        public async Task<Unit> Handle(JoinGameCommand request, CancellationToken cancellationToken)
        {
            var game = await repository.GetById(request.GameId, cancellationToken);
            game.Join(request.PlayerId);
            await repository.Update(game, cancellationToken);
            return Unit.Value;
        }
    }
}
