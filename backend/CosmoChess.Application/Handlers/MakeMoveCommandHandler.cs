using CosmoChess.Application.Commands;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class MakeMoveCommandHandler(IGameRepository repository) : IRequestHandler<MakeMoveCommand, Unit>
    {
        public async Task<Unit> Handle(MakeMoveCommand request, CancellationToken cancellationToken)
        {
            var game = await repository.GetById(request.GameId, cancellationToken);
            game.MakeMove(request.UserId, request.Move, request.NewFen, request.IsCheckmate, request.IsStalemate, request.IsDraw);
            await repository.Update(game, cancellationToken);
            return Unit.Value;
        }
    }
}
