namespace EnerGo
{
    // Decides which GPS fixes move the player. A fix that would mean moving faster than walking/driving speed is
    // held back once instead of trusted; if the next fix agrees with it, the jump was real (e.g. the first fix was
    // a stale cached location) and the player moves. Speed is always measured from the last *accepted* fix.
    public sealed class FixFilter
    {
        public bool HasFix { get; private set; }
        public double Lat { get; private set; }
        public double Lon { get; private set; }
        public double Time { get; private set; }
        public double Accuracy { get; private set; }

        private bool hasHeld;
        private double heldLat, heldLon, heldTime;

        // Returns true when the fix was accepted as the new position.
        public bool Offer(double lat, double lon, double time, double accuracy)
        {
            if (time <= 0) return false; // the legacy service reports an empty (0,0) fix before it has one

            bool fromAccepted = !HasFix || GeoSpawner.PlausibleMove(GeoSpawner.DistanceM(Lat, Lon, lat, lon), time - Time);
            bool fromHeld = hasHeld && GeoSpawner.PlausibleMove(GeoSpawner.DistanceM(heldLat, heldLon, lat, lon), time - heldTime);
            if (fromAccepted || fromHeld)
            {
                HasFix = true;
                Lat = lat; Lon = lon; Time = time; Accuracy = accuracy;
                hasHeld = false;
                return true;
            }

            hasHeld = true;
            heldLat = lat; heldLon = lon; heldTime = time;
            return false;
        }
    }
}
