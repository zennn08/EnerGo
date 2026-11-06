using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace EnerGo.Tests
{
    // LobbyView looks elements up by name; a renamed or missing element would silently leave a button dead.
    public class LobbyUxmlTests
    {
        static VisualElement Clone(string path)
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.IsNotNull(tree, path);
            return tree.Instantiate();
        }

        static void AssertNames(VisualElement root, params string[] names)
        {
            foreach (var n in names) Assert.IsNotNull(root.Q(n), n);
        }

        [Test]
        public void LobbyHasEveryNamedElement() =>
            AssertNames(Clone("Assets/UI/Screens/Lobby.uxml"),
                "total-unit", "judul", "tile-sun", "tile-coal", "tile-gold", "tile-oil", "tile-uranium",
                "mode-kamera", "btn-berburu", "btn-panduan", "btn-latihan", "btn-pengaturan");

        [Test]
        public void LobbyHasDesaButton() => AssertNames(Clone("Assets/UI/Screens/Lobby.uxml"), "btn-desa", "desa-tanda");

        [Test]
        public void PrologHasEveryNamedElement() =>
            AssertNames(Clone("Assets/UI/Screens/Prolog.uxml"),
                "prolog-gambar", "prolog-kartu", "prolog-teks", "prolog-nama", "input-nama", "prolog-error", "btn-prolog-lanjut", "btn-prolog-lewati");

        [Test]
        public void DesaHasEveryNamedElement() =>
            AssertNames(Clone("Assets/UI/Screens/Desa.uxml"),
                "desa-latar", "desa-malam-terang", "desa-awan", "desa-kerbau", "desa-sya", "btn-pembangkit", "pembangkit-mati",
                "desa-nama", "btn-ganti-nama", "desa-sisa", "desa-bar", "desa-padam", "desa-menipis", "btn-isi", "btn-desa-kembali",
                "dialog-wajah", "dialog-teks", "lapis-isi", "desa-tirai", "sheet-isi", "isi-sisa", "isi-daftar", "btn-isi-tutup",
                "lapis-nama", "input-nama-desa", "btn-nama-simpan", "btn-nama-batal",
                "lapis-sorak", "sorak-gelap", "sorak-terang", "sorak-sya", "sorak-kartu", "sorak-teks", "btn-sorak-lanjut");

        [Test]
        public void VillageNameIsPlainText() =>
            Assert.IsFalse(Clone("Assets/UI/Screens/Desa.uxml").Q<Label>("desa-nama").enableRichText);

        [Test]
        public void PrologStoryIsPlainTextBecauseItHoldsTheVillageName() =>
            Assert.IsFalse(Clone("Assets/UI/Screens/Prolog.uxml").Q<Label>("prolog-teks").enableRichText);

        [Test]
        public void PracticeSheetSaysItPaysOneUnit() =>
            StringAssert.Contains("1 unit", Clone("Assets/UI/Screens/SheetLatihan.uxml").Q<Label>(className: "sheet__sub").text);

        [Test]
        public void RadarHasEveryNamedElement() =>
            AssertNames(Clone("Assets/UI/Screens/Radar.uxml"),
                "btn-kembali", "pil-spawn", "radar-disk", "info-kompas", "info-gps", "kartu-target", "target-ikon", "target-nama",
                "target-jarak", "target-panah", "target-kosong", "btn-tangkap", "kartu-gps", "gps-judul", "gps-isi", "btn-coba-lagi",
                "btn-latihan-gps", "kartu-tunggu", "lapis-aman", "btn-mengerti");

        [Test]
        public void CaptureFrameHasEveryNamedElement() =>
            AssertNames(Clone("Assets/UI/Screens/Tangkap.uxml"),
                "lapis-hud", "btn-kembali", "btn-mode", "pil-mode", "pil-sda", "pil-sda-ikon", "pil-sda-teks", "kotak-status", "status-tag",
                "status-teks", "status-sub", "status-bar", "status-angka", "pita-kompas", "btn-ulang", "riak", "lapis-panduan",
                "kartu-panduan", "panduan-ingatkan", "panduan-centang", "btn-panduan-tutup", "btn-panduan-utama");

        [Test]
        public void GuideReminderIsAButtonSoItDoesNotFlipThePage() =>
            Assert.IsNotNull(Clone("Assets/UI/Screens/KartuPanduan.uxml").Q<UnityEngine.UIElements.Button>("panduan-ingatkan"));

        [Test]
        public void FeedbackChipHasAnIconElement() => AssertNames(Clone("Assets/UI/Screens/Kuis.uxml"), "balik-jawab-ikon");

        [Test]
        public void TrendPillUsesTheFontThatHasArrows() =>
            Assert.IsTrue(Clone("Assets/UI/Screens/Overlay.uxml").Q("rad-tren").ClassListContains("eg-pil--angka"));

        [Test]
        public void OverlayHasEveryNamedElement() =>
            AssertNames(Clone("Assets/UI/Screens/Overlay.uxml"),
                "bidik", "bidik-persen", "dulang-pil", "dulang-waterpass", "dulang-hitung", "dulang-mulai", "kartu-terbuang", "btn-endapan-baru",
                "bor-label", "bor-gas", "pompa-cincin", "pompa-drum", "pompa-tekanan", "rad-angka", "rad-tren", "btn-jepit", "jepit-isi", "btn-tutup-wadah", "dosis-bar");

        [Test]
        public void QuizHasEveryNamedElement() =>
            AssertNames(Clone("Assets/UI/Screens/Kuis.uxml"),
                "lapis-kuis", "kuis-penanda", "pil-soal", "kuis-soal", "kartu-soal", "soal-waktu", "soal-detik", "soal-ikon", "soal-teks", "cap-jawab",
                "btn-benar", "btn-salah", "kartu-balik", "balik-selotip", "balik-jawab", "balik-ikon", "balik-teks", "balik-unit", "btn-lanjut", "lanjut-isi",
                "kuis-hasil", "hasil-judul", "kartu-hasil", "hasil-bintang", "hasil-skor", "hasil-ikon", "hasil-plus", "hasil-mutu", "hasil-rincian", "hasil-pesan",
                "btn-selesai-hasil", "kuis-error", "kuis-pendar-kiri", "kuis-pendar-kanan");

        [Test]
        public void SceneRootHasEveryLayer() =>
            AssertNames(Clone("Assets/UI/Screens/LobbyScene.uxml"),
                "lapis-splash", "lapis-lobby", "lapis-sheet", "lapis-panduan", "lapis-transisi");

        [Test]
        public void SheetsHaveEveryNamedElement()
        {
            AssertNames(Clone("Assets/UI/Screens/LobbyScene.uxml"), "tirai");
            AssertNames(Clone("Assets/UI/Screens/SheetPengaturan.uxml"),
                "sheet-pengaturan", "atur-ar", "atur-ar-sub", "atur-volume", "atur-volume-persen", "atur-getar", "atur-getar-sub",
                "btn-ke-latihan", "btn-reset", "reset-bar", "btn-selesai");
            AssertNames(Clone("Assets/UI/Screens/SheetLatihan.uxml"),
                "sheet-latihan", "latihan-sun", "latihan-coal", "latihan-gold", "latihan-oil", "latihan-uranium", "latihan-random",
                "btn-mulai-latihan");
        }

        [Test]
        public void GuideSheetAndCardHaveEveryNamedElement()
        {
            AssertNames(Clone("Assets/UI/Screens/SheetPanduan.uxml"),
                "sheet-panduan", "panduan-sun", "panduan-coal", "panduan-gold", "panduan-oil", "panduan-uranium",
                "btn-reset-panduan", "panduan-notice");
            AssertNames(Clone("Assets/UI/Screens/KartuPanduan.uxml"),
                "kartu-panduan", "panduan-selotip", "panduan-langkah", "panduan-ilustrasi", "panduan-judul", "panduan-isi",
                "panduan-titik", "btn-panduan-utama", "btn-panduan-tutup");
        }
    }
}
