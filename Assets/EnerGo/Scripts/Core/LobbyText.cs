namespace EnerGo
{
    // Lobby copy, kept free of UI calls so it can be tested.
    public static class LobbyText
    {
        public static string Badge(int n) => n > 99 ? "99+" : n.ToString();

        public static string TotalUnits(int total) => total + " unit";

        public static string CameraMode(bool arcore) => arcore ? "Mode kamera: AR (ARCore)" : "Mode kamera: Sensor 360";

        // Specimen catalogue number (design system: No.01 Matahari ... No.05 Uranium, Acak "???").
        public static string CatalogNo(string id) =>
            id == ResourceIds.Sun ? "No.01" : id == ResourceIds.Coal ? "No.02" : id == ResourceIds.Gold ? "No.03"
            : id == ResourceIds.Oil ? "No.04" : id == ResourceIds.Uranium ? "No.05" : "???";
    }
}
