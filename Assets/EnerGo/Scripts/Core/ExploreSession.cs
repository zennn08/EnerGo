namespace EnerGo
{
    // Hand-off from the radar to the capture scene: the world spawn being captured (null = practice mode).
    public static class ExploreSession
    {
        public static string PendingSpawnId;
        public static float? PendingBearingDeg; // compass bearing player -> spawn when the capture started (radar only)
    }
}
