namespace CosmoChess.Domain.Enums
{
    public enum BotStyle
    {
        Balanced = 0,    // Standard play - current logic
        Aggressive = 1,  // Prefers attacks, sacrifices, sharp positions
        Solid = 2        // Positional, defensive, avoiding complications
    }
}
