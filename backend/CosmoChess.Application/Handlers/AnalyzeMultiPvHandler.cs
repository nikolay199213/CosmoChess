using CosmoChess.Application.Commands;
using CosmoChess.Domain.Interface.Engines;
using CosmoChess.Domain.ValueObject;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class AnalyzeMultiPvHandler(IEngineService stockfishService)
        : IRequestHandler<AnalyzeMultiPvCommand, AnalysisResult>
    {
        public async Task<AnalysisResult> Handle(AnalyzeMultiPvCommand request, CancellationToken cancellationToken)
        {
            return await stockfishService.AnalyzeMultiPvAsync(
                request.Fen,
                request.Depth,
                request.MultiPv,
                cancellationToken);
        }
    }
}
