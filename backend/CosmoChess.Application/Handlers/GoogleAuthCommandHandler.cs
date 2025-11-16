using CosmoChess.Application.Commands;
using CosmoChess.Domain.Entities;
using CosmoChess.Domain.Interface.Auth;
using CosmoChess.Domain.Interface.Repositories;
using Google.Apis.Auth;
using MediatR;

namespace CosmoChess.Application.Handlers
{
    public class GoogleAuthCommandHandler(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtGenerator) : IRequestHandler<GoogleAuthCommand, string>
    {
        public async Task<string> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
        {
            // Verify the Google ID token
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Invalid Google token", ex);
            }

            if (payload.Subject == null || payload.Email == null)
            {
                throw new UnauthorizedAccessException("Invalid Google token payload");
            }

            // Check if user already exists
            var user = await userRepository.GetByGoogleIdAsync(payload.Subject, cancellationToken);

            if (user == null)
            {
                // Create new user
                var username = payload.Email.Split('@')[0]; // Use email prefix as username
                var existingUser = await userRepository.GetByUsernameAsync(username, cancellationToken);

                // If username exists, append random string
                if (existingUser != null)
                {
                    username = $"{username}_{Guid.NewGuid().ToString().Substring(0, 8)}";
                }

                user = new User(username, payload.Email, payload.Subject);
                await userRepository.AddAsync(user, cancellationToken);
            }

            return jwtGenerator.GenerateJwtToken(user.Id, user.Username);
        }
    }
}
