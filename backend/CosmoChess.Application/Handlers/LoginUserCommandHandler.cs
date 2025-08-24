using CosmoChess.Application.Commands;
using CosmoChess.Domain.Interface.Auth;
using CosmoChess.Domain.Repositories;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class LoginUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtGenerator) : IRequestHandler<LoginUserCommand, string>
    {
        public async Task<string> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (user == null || !passwordHasher.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials");

            return jwtGenerator.GenerateJwtToken(user.Id, user.Username);
        }
    }
}
