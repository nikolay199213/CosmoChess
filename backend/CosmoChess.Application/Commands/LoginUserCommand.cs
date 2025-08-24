using MediatR;

namespace CosmoChess.Application.Commands
{
    public record LoginUserCommand(string Username, string Password) : IRequest<string>;
}
