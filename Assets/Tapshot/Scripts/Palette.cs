using UnityEngine;

namespace Tapshot
{
    public static class Palette
    {
        public static readonly Color Background = Hex("#0E1116");
        public static readonly Color Card = Hex("#141922");
        public static readonly Color CardSelected = Hex("#1A1F28");
        public static readonly Color Border = Hex("#262D3A");
        public static readonly Color BorderHover = Hex("#3A4252");
        public static readonly Color Text = Hex("#E8ECF2");
        public static readonly Color Muted = Hex("#9AA4B2");
        public static readonly Color Accent = Hex("#FF7A1A");
        public static readonly Color AccentHover = Hex("#FF8E3D");
        public static readonly Color AccentPressed = Hex("#E86A10");
        public static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
