using MediatR;

namespace CosmoChess.Application.Commands
{
    public record RegisterUserCommand(string UserName, string Password) : IRequest<Guid>;
}
