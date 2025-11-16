using MediatR;

namespace CosmoChess.Application.Commands
{
    public record GoogleAuthCommand(string IdToken) : IRequest<string>;
}
