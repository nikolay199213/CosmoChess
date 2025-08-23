using CosmoChess.Application.Commands;
using CosmoChess.Domain;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class AnalyzePositionHandler(IEngineService stockfishService)
        : IRequestHandler<AnalyzePositionCommand, string>
    {
        public async Task<string> Handle(AnalyzePositionCommand request, CancellationToken cancellationToken)
        {
            return await stockfishService.AnalyzeAsync(request.Fen, request.Depth, cancellationToken);
        }
    }
}
