namespace EnerGo
{
    // Step-by-step guide cards shown before each SDA mini-game and from the lobby's PANDUAN sheet.
    public static class GuideBook
    {
        public enum Art { Reticle, Compass, Tap, PhoneFlat, PhoneSwirl, Hold, Layers, PumpRing, RadMeter, Shake }

        public readonly struct Card
        {
            public readonly string Title, Body;
            public readonly Art Art;
            public Card(string title, string body, Art art) { Title = title; Body = body; Art = art; }
        }

        public static readonly string[] Ids = { ResourceIds.Sun, ResourceIds.Coal, ResourceIds.Gold, ResourceIds.Oil, ResourceIds.Uranium };

        private static readonly Card Compass = new Card("Ikuti kompas", "Hadap ke titik oranye di pita kompas sampai SDA terlihat di kamera.", Art.Compass);

        public static Card[] For(string resourceId)
        {
            switch (resourceId)
            {
                case ResourceIds.Sun: return new[]
                {
                    new Card("Kejar cahaya", "Arahkan lingkaran di tengah layar ke cahaya matahari yang bergerak.", Art.Reticle),
                    new Card("Kumpulkan sebelum waktu habis", "Minimal 50% cahaya. Makin penuh barnya, makin banyak bonus.", Art.Reticle)
                };
                case ResourceIds.Coal: return new[]
                {
                    Compass,
                    new Card("Pecahkan bongkahan", "Ketuk tiap bongkahan batu bara berkali-kali sampai hancur.", Art.Tap)
                };
                case ResourceIds.Gold: return new[]
                {
                    Compass,
                    new Card("Letakkan HP datar", "Setelah endapan diketuk, letakkan HP datar sampai hitungan mulai.", Art.PhoneFlat),
                    new Card("Putar pelan seperti mendulang", "Putar HP perlahan. Terlalu kencang, emasnya tumpah.", Art.PhoneSwirl)
                };
                case ResourceIds.Oil: return new[]
                {
                    new Card("Tahan untuk mengebor", "Tahan layar untuk mengebor ke bawah. Bor tidak langsung berhenti saat dilepas.", Art.Hold),
                    new Card("Lepas di lapisan minyak", "Pelan-pelan di kantong gas, lalu lepas tepat di lapisan minyak.", Art.Layers),
                    new Card("Pompa ikuti lingkaran", "Tekan saat lingkaran mengecil pas di target. Terlalu cepat, tekanan naik.", Art.PumpRing)
                };
                case ResourceIds.Uranium: return new[]
                {
                    new Card("Ikuti sinyal RAD", "Hadap ke sektor hijau di kompas, lalu putar pelan sampai angka RAD paling tinggi.", Art.RadMeter),
                    new Card("Jepit serpihan", "Bijih pecah jadi 3 serpihan di sekelilingmu. Arahkan lingkaran ke serpihan, lalu tahan JEPIT. Penjepit menjaga jarak tanganmu dari radiasi.", Art.Hold),
                    new Card("Cepat & stabil", "Dosis terus naik, tiap serpihan di wadah timbal memperlambatnya. HP terguncang, jepitan lepas.", Art.Shake)
                };
                default: return new Card[0];
            }
        }
    }
}
