# EnerGo — status checkpoint A (22 September 2026)

## Baseline

- Proyek: Unity 6000.3.24f1; AR Foundation/ARCore 6.3.5; dua scene build (`LobbyScene`, `ARPrototype`).
- Unity-MCP IvanMurzak tersambung: 38 tool terdaftar; scene `ARPrototype` terbuka sebelum dan sesudah pengujian.
- Snapshot skrip dan scene awal: `BaselineSnapshot-20260922/`.
- APK acuan yang sudah ada: `EnerGo.apk`, 47.716.928 byte, 21 September 2026 23:52; SHA-256 `69A0E98F41A3AC1DF3DCA343D0F98C1F0049B34DE07A21A952864605057B72EB`. APK ini mendahului perubahan dan belum diuji ulang di HP.
- Lobby, mesh emas `beveled_cuboid`, mode ARCore/360, dan kontrol drag tetap ada. Perangkat, izin kamera, tracking, dan FPS belum diuji dengan APK terbaru.

## Implementasi dan status

| ID | Status | Bukti / pekerjaan tersisa |
|---|---|---|
| EG-001 | IN PROGRESS | Tanggal 24 September dan scope D0 tercatat. Jam demo, rubrik AR, nama final, dan HP acuan belum dikonfirmasi. |
| EG-002 | IN PROGRESS | Snapshot dan APK acuan tercatat. Jalur lobby→AR terbuka di Editor; mesh emas dan placeholder ter-spawn. Regresi ARCore/360 dan save lama pada HP belum ada. |
| EG-003 | DONE (Editor) | ID tiga SDA, save JSON tervalidasi, migrasi `EnerGo.Prototype.Gold` sekali. Tes ulang load tidak menggandakan emas. |
| EG-004 | IN PROGRESS | Tiga SDA tersedia di lobby; uji fungsi target Matahari membuktikan lima hit membuka analisis tanpa saldo dan ketukan area UI tidak dihitung. Perlu uji sentuh layar nyata untuk semua target/mode. |
| EG-005 | DONE (konten D0) | Sembilan soal (tiga/SDA) berisi jawaban, penjelasan, URL sumber EIA/USGS, dan status reviewed setelah pemeriksaan sumber. |
| EG-006 | IN PROGRESS | Transaksi jawaban salah=2 Normal, benar=3 Murni, jawaban ganda ditolak, resume soal/hasil setelah load lolos di Editor. Panel belum diverifikasi di layar HP. |
| EG-007 | IN PROGRESS | Kartu inventori tiga SDA dan saldo lobby membaca satu save; angka energi dummy dihapus. Tata letak/safe area belum diperiksa di HP. |
| EG-008 | IN PROGRESS | Tes migrasi, reload, jawaban ganda, cadangan saat save utama rusak, dan perlindungan saat kedua file rusak lolos di Editor. Gagal tulis dan restart proses APK belum diuji. |

**Checkpoint A:** jalur data Editor lolos untuk tiga SDA (masing-masing 3 unit Murni sesudah reload); kelulusan pengguna pada APK masih tertunda. Tidak ada klaim bahwa APK lama memuat fitur baru.

## Pengujian Editor yang dijalankan

1. Kompilasi setelah `AssetDatabase.Refresh`; 9 soal terbaca dari assembly.
2. Play Mode: `LobbyScene` memuat `LobbyController` dan transisi ke `ARPrototype` memuat `ARCapturePrototype`.
3. Target Emas memakai mesh `beveled_cuboid`; Matahari memakai `Sphere`, Batu Bara `Cube` sebagai placeholder.
4. Simulasi input Matahari: ketukan UI tidak mengubah hit; lima hit mengubah state menjadi `awaiting-analysis`; saldo tetap 0 sampai jawaban.
5. Migrasi 7 emas PlayerPrefs dua kali tetap 7; sesi soal dan hasil pulih setelah `Load`.
6. Jawaban salah menambah 2 Normal; jawaban benar menambah 3 Murni; submit kedua ditolak.
7. Tiga SDA masing-masing tersimpan 3 Murni setelah reload; save utama rusak dipulihkan dari cadangan. Dua file rusak memblokir pembukuan dan tetap dipertahankan sampai reset terkonfirmasi.

## Uji APK yang membutuhkan HP

- Build APK baru dari source ini, catat versi/hash, pasang pada HP acuan.
- Lobby → pilih ketiga SDA → lima sentuhan target → soal → hasil → inventori → paksa tutup/buka ulang, termasuk tutup sebelum dan sesudah jawaban.
- ARCore dan sensor 360 secara terpisah: izin kamera ditolak, perangkat tanpa ARCore/gyro, tracking hilang, drag, target di luar pandangan, dan tombol munculkan ulang.
- Periksa safe area, keterbacaan, respons sentuh UI, suhu/FPS 10 menit, dan tiga pengulangan tanpa crash/duplikasi.
- Uji gagal tulis (ruang penuh/izin penyimpanan) dan pemulihan di perangkat.
