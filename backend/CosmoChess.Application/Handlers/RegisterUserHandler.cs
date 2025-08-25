using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Auth;
using CosmoChess.Domain.Interface.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class RegisterUserHandler(IUserRepository userRepository, IPasswordHasher passwordHashe)
        : IRequestHandler<RegisterUserCommand, Guid>
    {
        public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var hash = passwordHashe.Generate(request.Password);
            var user = new User(request.UserName, hash);

            await userRepository.AddAsync(user, cancellationToken);
            return user.Id;
        }
    }
}
