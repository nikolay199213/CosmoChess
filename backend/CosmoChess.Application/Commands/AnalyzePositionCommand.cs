using MediatR;

namespace CosmoChess.Application.Commands
{
    public record AnalyzePositionCommand(string Fen, int Depth) : IRequest<string>;
}
