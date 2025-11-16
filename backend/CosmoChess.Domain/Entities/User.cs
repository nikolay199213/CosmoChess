namespace CosmoChess.Domain.Entities
{
    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Username { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public string? Email { get; private set; }
        public string? GoogleId { get; private set; }

        private User() { } // Для EF Core

        public User(string username, string passwordHash)
        {
            Id = Guid.NewGuid();
            Username = username;
            PasswordHash = passwordHash;
        }

        public User(string username, string email, string googleId)
        {
            Id = Guid.NewGuid();
            Username = username;
            Email = email;
            GoogleId = googleId;
            PasswordHash = string.Empty; // Google users don't have password
        }
    }
}
