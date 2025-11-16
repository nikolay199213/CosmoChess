using CosmoChess.Domain.Enums;

namespace CosmoChess.Domain.ValueObject
{
    public class TimeControlSettings
    {
        public TimeControl TimeControl { get; init; }
        public int InitialTimeSeconds { get; init; }
        public int IncrementSeconds { get; init; }

        public TimeControlSettings(TimeControl timeControl)
        {
            TimeControl = timeControl;
            (InitialTimeSeconds, IncrementSeconds) = GetTimeSettings(timeControl);
        }

        private static (int initialTime, int increment) GetTimeSettings(TimeControl control)
        {
            return control switch
            {
                TimeControl.None => (0, 0),
                TimeControl.Bullet1_0 => (60, 0),
                TimeControl.Bullet1_1 => (60, 1),
                TimeControl.Blitz3_0 => (180, 0),
                TimeControl.Blitz3_2 => (180, 2),
                TimeControl.Blitz5_0 => (300, 0),
                TimeControl.Rapid10_0 => (600, 0),
                TimeControl.Rapid10_5 => (600, 5),
                TimeControl.Rapid15_10 => (900, 10),
                TimeControl.Daily => (86400, 0), // 24 hours
                _ => (0, 0)
            };
        }

        public static string GetDisplayName(TimeControl control)
        {
            return control switch
            {
                TimeControl.None => "No time control",
                TimeControl.Bullet1_0 => "Bullet 1+0",
                TimeControl.Bullet1_1 => "Bullet 1+1",
                TimeControl.Blitz3_0 => "Blitz 3+0",
                TimeControl.Blitz3_2 => "Blitz 3+2",
                TimeControl.Blitz5_0 => "Blitz 5+0",
                TimeControl.Rapid10_0 => "Rapid 10+0",
                TimeControl.Rapid10_5 => "Rapid 10+5",
                TimeControl.Rapid15_10 => "Rapid 15+10",
                TimeControl.Daily => "Daily",
                _ => "Unknown"
            };
        }
    }
}
