using UnityEngine;

namespace Tapshot
{
    public enum DrillType { Grid, Flick, Track, JumboCubes }

    public enum TargetSize { S, M, L }

    /// <summary>
    /// Choices made on the main menu. Static, so they survive scene loads and the
    /// menu can restore the last selection when the player comes back.
    /// </summary>
    public static class SessionSettings
    {
        public const string MenuScene = "my scene";

        public static DrillType DrillType = DrillType.Grid;
        public static TargetSize TargetSize = TargetSize.M;
        public static int RoundSeconds = 30;

        public static readonly int[] RoundLengths = { 30, 60 };

        public static string SceneFor(DrillType drill) => drill switch
        {
            DrillType.Grid => "Drill_Grid",
            DrillType.Flick => "Drill_Flick",
            DrillType.Track => "Drill_Track",
            DrillType.JumboCubes => "Drill_JumboCubes",
            _ => "Drill_Grid",
        };

        public static string DisplayName(DrillType drill) => drill == DrillType.JumboCubes ? "Jumbo Cubes" : drill.ToString();

        // Restore defaults when entering Play mode with domain reload disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetToDefaults()
        {
            DrillType = DrillType.Grid;
            TargetSize = TargetSize.M;
            RoundSeconds = 30;
        }
    }
}
