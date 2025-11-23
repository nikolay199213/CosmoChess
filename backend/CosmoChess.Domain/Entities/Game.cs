using CosmoChess.Domain.Enums;
using CosmoChess.Domain.ValueObject;

namespace CosmoChess.Domain.Entities
{
    public class Game
    {
        public const string InitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        public static readonly Guid BotPlayerId = new("00000000-0000-0000-0000-000000000001");

        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime StartedAt { get; private set; } = DateTime.UtcNow;

        private readonly List<GameMove> _moves = [];
        public IReadOnlyCollection<GameMove> Moves => _moves.AsReadOnly();
        public GameType GameType { get; private set; }
        public GameResult GameResult { get; private set; } = GameResult.WaitJoin;
        public GameEndReason EndReason { get; private set; } = GameEndReason.None;
        public string CurrentFen { get; private set; } = InitialFen;
        public BotDifficulty? BotDifficulty { get; private set; }
        public BotStyle? BotStyle { get; private set; }

        // Timer fields
        public TimeControl TimeControl { get; private set; } = TimeControl.None;
        public int WhiteTimeRemainingSeconds { get; private set; }
        public int BlackTimeRemainingSeconds { get; private set; }
        public DateTime? LastMoveTime { get; private set; }

        // Constructor for human vs human games
        public Game(Guid whitePlayerId, TimeControl timeControl = TimeControl.None)
        {
            WhitePlayerId = whitePlayerId;
            GameType = GameType.HumanVsHuman;
            TimeControl = timeControl;

            var settings = new TimeControlSettings(timeControl);
            WhiteTimeRemainingSeconds = settings.InitialTimeSeconds;
            BlackTimeRemainingSeconds = settings.InitialTimeSeconds;
        }

        // Constructor for human vs bot games
        public Game(Guid whitePlayerId, BotDifficulty botDifficulty, BotStyle botStyle = Enums.BotStyle.Balanced, TimeControl timeControl = TimeControl.None)
        {
            WhitePlayerId = whitePlayerId;
            BlackPlayerId = BotPlayerId;
            GameType = GameType.HumanVsBot;
            BotDifficulty = botDifficulty;
            BotStyle = botStyle;
            GameResult = GameResult.InProgress; // Bot games start immediately
            TimeControl = timeControl;
            LastMoveTime = DateTime.UtcNow;

            var settings = new TimeControlSettings(timeControl);
            WhiteTimeRemainingSeconds = settings.InitialTimeSeconds;
            BlackTimeRemainingSeconds = settings.InitialTimeSeconds;
        }

        public bool IsBotGame() => GameType == GameType.HumanVsBot;

        public bool IsBotTurn() => IsBotGame() && !CurrentFen.Contains(" w ");

        public void Join(Guid blackPlayerId)
        {
            BlackPlayerId = blackPlayerId;
            GameResult = GameResult.InProgress;
            LastMoveTime = DateTime.UtcNow; // Start the timer when game begins
        }

        public Guid WhitePlayerId { get; private set; }
        public Guid BlackPlayerId { get; private set; }

        public void MakeMove(Guid userId, string move, string newFen, bool isCheckmate = false, bool isStalemate = false, bool isDraw = false)
        {
            // Validate that it's the player's turn
            var isWhiteTurn = CurrentFen.Contains(" w ");
            var isWhitePlayer = userId == WhitePlayerId;
            var isBlackPlayer = userId == BlackPlayerId;

            if (isWhiteTurn && !isWhitePlayer)
            {
                throw new InvalidOperationException("It's white's turn");
            }

            if (!isWhiteTurn && !isBlackPlayer)
            {
                throw new InvalidOperationException("It's black's turn");
            }

            // Update time if time control is active
            if (TimeControl != TimeControl.None && LastMoveTime.HasValue)
            {
                var timeElapsed = (int)(DateTime.UtcNow - LastMoveTime.Value).TotalSeconds;
                var settings = new TimeControlSettings(TimeControl);

                if (isWhiteTurn)
                {
                    WhiteTimeRemainingSeconds -= timeElapsed;
                    if (WhiteTimeRemainingSeconds <= 0)
                    {
                        WhiteTimeRemainingSeconds = 0;
                        GameResult = GameResult.BlackWins;
                        EndReason = GameEndReason.Timeout;
                        throw new InvalidOperationException("White ran out of time");
                    }
                    // Add increment
                    WhiteTimeRemainingSeconds += settings.IncrementSeconds;
                }
                else
                {
                    BlackTimeRemainingSeconds -= timeElapsed;
                    if (BlackTimeRemainingSeconds <= 0)
                    {
                        BlackTimeRemainingSeconds = 0;
                        GameResult = GameResult.WhiteWins;
                        EndReason = GameEndReason.Timeout;
                        throw new InvalidOperationException("Black ran out of time");
                    }
                    // Add increment
                    BlackTimeRemainingSeconds += settings.IncrementSeconds;
                }
            }

            var gameMove = new GameMove(Id, userId, move, newFen, DateTime.UtcNow);
            _moves.Add(gameMove);
            CurrentFen = newFen;
            LastMoveTime = DateTime.UtcNow;

            // Check for game end conditions
            if (isCheckmate)
            {
                // The player who just moved wins
                GameResult = isWhiteTurn ? GameResult.WhiteWins : GameResult.BlackWins;
                EndReason = GameEndReason.Checkmate;
            }
            else if (isStalemate)
            {
                GameResult = GameResult.Draw;
                EndReason = GameEndReason.Stalemate;
            }
            else if (isDraw)
            {
                GameResult = GameResult.Draw;
                EndReason = GameEndReason.Draw;
            }
        }
    }
}
