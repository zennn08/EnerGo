namespace EnerGo
{
    // Copy and image names of the village and the prologue, kept free of UI calls so they can be tested.
    public static class DesaText
    {
        public static readonly string[] Prolog =
        {
            "Radarmu berbunyi di tengah malam. Ada sinyal energi di balik bukit.",
            "Kamu sampai di sebuah desa. Tidak ada satu pun lampu yang menyala.",
            "Halo, penjelajah! Aku Sya, ahli geologi di sini. Pembangkit desa kami kehabisan energi.",
            "Desa ini belum tertulis di petamu. Mau kamu beri nama apa?",
            "Yuk, cari SDA di sekitar sini. Bawa pulang, lalu kita nyalakan {nama} bersama!"
        };

        public static readonly string[] PrologImages =
        {
            "prolog_1_perjalanan", "prolog_2_desa_gelap", "prolog_3_pemandu", "prolog_4_nama", "prolog_5_ajakan"
        };

        public const int PrologNamePanel = 3;

        public static string Status(DesaStatus status, bool everFilled)
        {
            if (!everFilled) return "Kamu sudah bawa SDA? Ketuk pembangkit, yuk kita isi.";
            switch (status)
            {
                case DesaStatus.Padam: return "Desa gelap lagi. Tenang, begitu ada SDA kita nyalakan lagi.";
                case DesaStatus.Menipis: return "Listriknya mulai menipis. Sempat cari SDA lagi?";
                default: return "Terima kasih, desa terang berkat kamu!";
            }
        }

        // What each SDA does for the plant. Gold is a conductor, never a fuel (quiz gold-02 and gold-16 teach the same).
        public static string SourceOf(string id)
        {
            switch (id)
            {
                case ResourceIds.Sun: return "Dialirkan dari panel surya";
                case ResourceIds.Gold: return "Memperbaiki kabel dan sambungan";
                case ResourceIds.Uranium: return "Bahan bakar reaktor";
                default: return "Bahan bakar pembangkit";
            }
        }

        // Rounded up, so it never reads 0 while some stock is left.
        public static string FormatSisa(long seconds)
        {
            long minutes = (seconds + 59) / 60;
            long h = minutes / 60, m = minutes % 60;
            if (h == 0) return m + " m";
            return m == 0 ? h + " j" : h + " j " + m + " m";
        }

        public static string BackgroundFor(DesaStatus status, bool isDay) =>
            isDay ? "desa_siang" : status == DesaStatus.Padam ? "desa_malam_padam" : "desa_malam_terang";

        public static string PoseFor(DesaStatus status, bool isDay, bool everFilled)
        {
            if (!everFilled) return "pemandu_menunjuk";
            switch (status)
            {
                case DesaStatus.Padam: return "pemandu_khawatir";
                case DesaStatus.Menipis: return "pemandu_menunjuk";
                default: return isDay ? "pemandu_melambai" : "pemandu_santai";
            }
        }

        public static string WajahFor(DesaStatus status, bool everFilled)
        {
            if (!everFilled) return "pemandu_wajah_netral";
            switch (status)
            {
                case DesaStatus.Padam: return "pemandu_wajah_khawatir";
                case DesaStatus.Menipis: return "pemandu_wajah_berpikir";
                default: return "pemandu_wajah_senang";
            }
        }
    }
}
