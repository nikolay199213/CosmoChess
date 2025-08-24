namespace CosmoChess.Domain.Interface.Auth
{
    public interface IJwtTokenGenerator
    {
        string GenerateJwtToken(Guid userId, string username);
    }
}
