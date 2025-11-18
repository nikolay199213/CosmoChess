using CosmoChess.Domain.ValueObject;
using MediatR;

namespace CosmoChess.Application.Commands
{
    public record AnalyzeMultiPvCommand(string Fen, int Depth, int MultiPv = 3) : IRequest<AnalysisResult>;
}
