namespace CosmoChess.Domain.Enums
{
    public enum GameResult
    {
        WaitJoin,
        InProgress,
        WhiteWins,
        BlackWins,
        Draw
    }

    public enum GameType
    {
        HumanVsHuman,
        HumanVsBot,
        Analysis
    }

    public enum GameEndReason
    {
        None,
        Checkmate,
        Stalemate,
        Resignation,
        Timeout,
        Draw,
        Abandonment,
        InsufficientMaterial,
        ThreefoldRepetition,
        FiftyMoveRule
    }
}
