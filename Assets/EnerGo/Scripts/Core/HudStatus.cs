namespace EnerGo
{
    // What the status box says during a capture. The tag replaces the old green/orange rim: a word reads better than a colour.
    public struct HudStatus
    {
        public const string Aman = "AMAN", Awas = "AWAS", Gagal = "GAGAL", Info = "INFO";

        public string Tag, Line, Sub, Label;
        public bool HasBar, Warn;
        public float Bar;

        public static string Tags(bool warn) => warn ? Awas : Aman;

        public static HudStatus Plain(string line) => new HudStatus { Tag = Info, Line = line };

        public static HudStatus Text(string line, bool warn, string sub = null) => new HudStatus { Tag = Tags(warn), Line = line, Sub = sub, Warn = warn };

        public static HudStatus WithBar(string line, bool warn, float bar, string label) =>
            new HudStatus { Tag = Tags(warn), Line = line, Warn = warn, HasBar = true, Bar = bar, Label = label };

        public static HudStatus Failed(string line, float bar, string label) =>
            new HudStatus { Tag = Gagal, Line = line, Warn = true, HasBar = true, Bar = bar, Label = label };
    }

    // Taps in the bands behind the header and the bottom button never reach the game. Panel units.
    public static class HudTapZones
    {
        const float TopBand = 128f, BottomBand = 70f;

        // The header and bottom button move with the safe area (see SafeArea), so the bands do too.
        public static bool Blocks(float y, float panelHeight, float topInset = 0f, float bottomInset = 0f) =>
            y < TopBand + topInset || y > panelHeight - BottomBand - bottomInset;
    }
}
