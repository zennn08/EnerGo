namespace EnerGo
{
    public static class ResourceIds
    {
        public const string Sun = "sun";
        public const string Coal = "coal";
        public const string Gold = "gold";
        public const string Oil = "oil";         // "Bor & Pompa"
        public const string Uranium = "uranium"; // rarest
        public static string Selected = Gold;

        public static string Name(string id) =>
            id == Sun ? "Matahari" : id == Coal ? "Batu Bara" : id == Oil ? "Minyak Bumi" : id == Uranium ? "Uranium" : "Emas";

        // Has a capture mini-game + quiz bank.
        public static bool IsPlayable(string id) => id == Sun || id == Coal || id == Gold || id == Oil || id == Uranium;
    }
}
