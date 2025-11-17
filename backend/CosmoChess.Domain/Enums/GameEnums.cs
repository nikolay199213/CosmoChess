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

    public enum TimeControl
    {
        None,          // No time control
        Bullet1_0,     // 1 min + 0 sec
        Bullet1_1,     // 1 min + 1 sec
        Blitz3_0,      // 3 min + 0 sec
        Blitz3_2,      // 3 min + 2 sec
        Blitz5_0,      // 5 min + 0 sec
        Rapid10_0,     // 10 min + 0 sec
        Rapid10_5,     // 10 min + 5 sec
        Rapid15_10,    // 15 min + 10 sec
        Daily          // Daily chess (24h per move)
    }
}
