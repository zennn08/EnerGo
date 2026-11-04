# EnerGo – Tutorial Produksi Redesign

Nov 3, 2026 · @Daffandri Faiz

Tutorial ini membawa redesign EnerGo dari gaya Jurnal Ekspedisi yang sudah dikunci sampai siap rilis, dalam empat tahap kerja setelah Tahap 0. Acuannya dua dokumen: design system EnerGo untuk tampilan, dan dokumen Prompt Aset 3D & Audio untuk Blender dan ElevenLabs.

## Peta alur

Diagram alur produksi: Tahap 0 kunci gaya → Tahap 1 fondasi (UI Toolkit + tema, Blender MCP + batu bara uji, 3 suara UI) → gerbang cek gaya → Tahap 2 vertical slice Batu Bara + Emas → gerbang uji lapangan → Tahap 3 produksi sisanya → Tahap 4 polish → nanti Desa Teknik. (Lihat versi bergambar di dokumen online.)

Tiga jalur (UI, aset 3D, audio) berjalan bersamaan di setiap tahap, tapi tahap berikutnya baru dimulai setelah gerbang pengecekannya lolos.

## Persiapan alat

Pasang semua alat ini sekali di awal, supaya tiga jalur kerja bisa jalan tanpa berhenti di tengah.

| Alat | Untuk | Catatan |
| --- | --- | --- |
| Unity 6000.3.24f1 + Android Build Support | Game | Versi yang sama dengan build sekarang, jangan upgrade di tengah redesign |
| Git + Git LFS | Versi semua file | LFS untuk `.blend`, `.fbx`, `.wav`, `.ttf` supaya repo tidak membengkak |
| Blender 3.0 atau lebih baru | Model 3D | Syarat minimal BlenderMCP |
| Python 3.10+ dan `uv` | Menjalankan BlenderMCP | Pasang `uv` sebelum yang lain |
| Claude Code + MCP | Mengerjakan semua tahap | Satu alat untuk semua jalur: menulis USS, UXML, dan C#, menyetir Blender lewat MCP, dan menjalankan perintah terminal |
| Akun ElevenLabs | Sound effect | Siapkan sekitar 6.500 kredit untuk 3 variasi semua suara |
| Audacity (gratis) | Memotong hening, menyamakan keras suara | Opsional tapi sangat membantu |
| fonttools (`pip install fonttools`) | Membuat font statis dari font variable | Unity tidak membaca sumbu variable font |
| HP Android dengan ARCore + satu HP tanpa ARCore | Uji di lapangan | Untuk menguji mode ARCore dan Sensor 360 |

Struktur folder yang disarankan di proyek Unity:

```text
Assets/
  UI/
    Theme.uss          (token warna, jarak, radius dari design system)
    Components.uss     (kelas tombol, kartu, pil, bar, tile)
    Screens/           (satu UXML per layar: Lobby.uxml, Radar.uxml, ...)
    PanelSettings.asset
  Fonts/               (TTF statis + Font Asset)
  Models/              (FBX dari Blender)
  Audio/               (WAV/MP3 dari ElevenLabs)
  Resources/           (prefab SDA, nama sesuai yang dipanggil kode)
```

Buat tiga branch git sejak awal: `ui-redesign`, `aset-3d`, dan `audio`, lalu gabungkan ke branch utama setiap satu bagian selesai dan sudah dites.

## Cara kerja dengan Claude Code

Semua tahap di tutorial ini dikerjakan dari Claude Code di folder proyek Unity. Supaya hasilnya konsisten di setiap sesi, Claude Code perlu tiga hal: acuan desain di dalam repo, aturan proyek di `CLAUDE.md`, dan satu tugas kecil per sesi.

### Siapkan acuan di repo

1. Buat folder `docs/` di root proyek.
2. Taruh berkas design system di `docs/design-system/`: `README.md`, `tokens.json`, `components/bundle.css`, dan README tiap komponen.
3. Ekspor dokumen Prompt Aset 3D & Audio dan tutorial ini sebagai Markdown, lalu simpan di `docs/prompt-aset.md` dan `docs/tutorial.md`.
4. Cek MCP yang tersedia dengan perintah `/mcp` di dalam sesi Claude Code. Blender harus terlihat terhubung sebelum mengerjakan jalur aset.

### CLAUDE.md

Buat `CLAUDE.md` di root proyek. Claude Code membacanya setiap sesi dimulai:

```markdown
# EnerGo

Game edukasi AR berbasis GPS untuk pelajar SMP/SMA, Unity 6000.3.24f1, URP, Android portrait.

## Acuan
- Tampilan: docs/design-system/ (gaya Jurnal Ekspedisi). Nama variabel USS = nama token di tokens.json.
- Aset 3D dan audio: docs/prompt-aset.md (material, nama file, durasi suara).
- Urutan kerja: docs/tutorial.md.

## Aturan
- UI baru memakai UI Toolkit (Assets/UI). Jangan ubah logika game saat memindahkan layar dari IMGUI.
- Satuan UI = unit dasar 480 x 800, sama dengan token.
- Teks di atas kamera AR selalu di atas kartu atau pil kertas-100.
- Nama file aset dan suara persis seperti di docs/prompt-aset.md, karena dipanggil kode.
- Sebelum perintah Blender: simpan file .blend. Satu objek per perintah.
- Bahasa UI: Bahasa Indonesia.
```

### Pola satu sesi

1. Mulai dengan satu tugas dari tutorial, misalnya "Tahap 1a langkah 3".
2. Untuk tugas yang menyentuh banyak file, minta rencana dulu (plan mode), baca, baru setujui.
3. Minta Claude Code menjelaskan cara mengecek hasilnya di Unity, lalu cek sendiri di editor atau HP.
4. Commit begitu satu bagian selesai.

Contoh perintah per jalur:

| Jalur | Contoh perintah ke Claude Code |
| --- | --- |
| UI | Buat Assets/UI/Theme.uss dari semua token warna, jarak, dan radius di docs/design-system/tokens.json, dengan nama variabel yang sama. |
| UI | Pindahkan layar Lobby dari IMGUI ke UI Toolkit sesuai komponen di docs/design-system. Jangan ubah logika; matikan OnGUI Lobby saja. |
| 3D | Lewat Blender MCP, jalankan prompt setup di docs/prompt-aset.md, lalu tampilkan daftar material. |
| 3D | Buat batu bara tahap 1 sesuai docs/prompt-aset.md, ekspor ke Assets/Models/sda\_batubara\_1.fbx, lalu buat prefab-nya. |
| Audio | Buat sfx\_button\_click sesuai tabel di docs/prompt-aset.md dan simpan ke Assets/Audio/. |

Untuk suara: kalau MCP ElevenLabs yang terpasang punya alat sound effect, Claude Code bisa langsung generate dan menyimpan file ke `Assets/Audio/`. Kalau tidak ada, generate lewat web ElevenLabs seperti di Tahap 1c, lalu minta Claude Code memasang file dan pengaturan import-nya.

## Tahap 1a: UI Toolkit di Unity

Tujuan tahap ini: satu layar Lobby versi baru tampil di HP dengan tema Jurnal Ekspedisi, sementara layar lain masih memakai IMGUI lama. Logika game tidak disentuh; yang diganti hanya cara menggambar.

### Langkah 1 — Panel Settings

1. Buat aset: Assets → Create → UI Toolkit → Panel Settings Asset, simpan sebagai `Assets/UI/PanelSettings.asset`.
2. Isi: Scale Mode **Scale With Screen Size**, Reference Resolution **480 × 800** (sama dengan unit dasar IMGUI dan token), Screen Match Mode **Match Width Or Height**, Match **0** (ikut lebar, karena HP portrait lebih tinggi dari 480 × 800).
3. Buat GameObject `UI` di LobbyScene, tambahkan komponen **UI Document**, isi Panel Settings dengan aset tadi dan Source Asset dengan `Lobby.uxml` (dibuat di langkah 5).

### Langkah 2 — Font

Font di design system berformat variable. Buat versi statis dulu di terminal:

```bash
fonttools varLib.instancer BricolageGrotesque-Variable.ttf wght=800 opsz=24 wdth=100 -o Bricolage-ExtraBold.ttf
fonttools varLib.instancer BricolageGrotesque-Variable.ttf wght=800 opsz=24 wdth=75 -o Bricolage-ExtraBold-Sempit.ttf
fonttools varLib.instancer Lexend-Variable.ttf wght=400 -o Lexend-Regular.ttf
fonttools varLib.instancer Lexend-Variable.ttf wght=600 -o Lexend-SemiBold.ttf
fonttools varLib.instancer Lexend-Variable.ttf wght=700 -o Lexend-Bold.ttf
fonttools varLib.instancer Caveat-Variable.ttf wght=600 -o Caveat-SemiBold.ttf
```

Space Mono sudah statis. Masukkan semua TTF ke `Assets/Fonts/`, lalu klik kanan tiap file → Create → Text → Font Asset (nama menu bisa sedikit berbeda antar versi Unity). Versi sempit Bricolage dipakai untuk selotip dan cap.

### Langkah 3 — Theme.uss dari token

Salin nilai dari `tokens.json` design system ke variabel USS. Cuplikan awalnya:

```css
:root {
    --kertas-100: #FBF6EA;
    --kertas-200: #F3EAD6;
    --kertas-300: #E7D9BA;
    --kraft-400: #CDB68A;
    --tinta: #1F2A36;
    --tinta-pudar: #4A5361;
    --oranye: #F07F3C;
    --oranye-tua: #A6481A;
    --oker: #E2A72E;
    --stempel: #B5362B;
    --hutan: #2E7A47;
    --laut: #2F6E8F;
    --sda-matahari: #F2B33D;
    --sda-batubara: #4B4F58;
    --sda-emas: #C99A2E;
    --sda-minyak: #5B4BB5;
    --sda-uranium: #4FA83A;
    --radius-md: 10px;
    --radius-lg: 16px;
    --space-4: 16px;
    -unity-font-definition: url("project://database/Assets/Fonts/Lexend-Regular SDF.asset");
    color: var(--tinta);
}
```

Lanjutkan untuk semua token warna, jarak, dan radius. Nama variabelnya sama persis dengan token supaya design system dan Unity bisa dibandingkan satu lawan satu.

### Langkah 4 — Components.uss

Terjemahkan kelas di `bundle.css` design system ke USS. USS tidak punya `box-shadow`, jadi bibir tombol dibuat dari garis bawah yang lebih tebal:

```css
.eg-btn {
    min-height: 64px;
    padding: 0 24px;
    border-width: 2px;
    border-bottom-width: 6px;
    border-color: var(--tinta);
    border-radius: var(--radius-md);
    background-color: var(--kertas-100);
    -unity-font-definition: url("project://database/Assets/Fonts/Bricolage-ExtraBold SDF.asset");
    font-size: 22px;
    -unity-text-align: middle-center;
    color: var(--tinta);
}
.eg-btn:active {
    border-bottom-width: 3px;
    translate: 0 3px;
}
.eg-btn--utama { background-color: var(--oranye); }
.eg-btn--ok { background-color: var(--hutan); color: var(--kertas-100); }
.eg-btn--bahaya { background-color: var(--stempel); color: var(--kertas-100); }
.eg-btn:disabled { background-color: var(--kertas-300); color: var(--tinta-pudar); border-bottom-width: 2px; }

.eg-kartu {
    background-color: var(--kertas-100);
    border-width: 2px;
    border-bottom-width: 5px;
    border-color: var(--tinta);
    border-radius: var(--radius-lg);
    padding: 24px 16px 16px 16px;
}
```

Urutan yang disarankan: tombol, kartu + selotip, pil + chip, bar ukur, tile spesimen, toggle + slider. Selotip dan cap yang miring memakai properti `rotate`. Tepi bergerigi selotip lebih mudah dibuat sebagai sprite 9-slice.

### Langkah 5 — Lobby.uxml dan menghubungkan logika

Buka Window → UI Toolkit → UI Builder, susun Lobby memakai kelas di atas (judul, kartu koleksi berisi 5 tile, tombol BERBURU!, dock 3 tombol bulat), dan beri nama elemen yang perlu diakses kode, misalnya `btn-berburu`. Hubungkan ke logika lama:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyView : MonoBehaviour
{
    [SerializeField] UIDocument doc;

    void OnEnable()
    {
        var root = doc.rootVisualElement;
        root.Q<Button>("btn-berburu").clicked += () => LobbyController.MulaiBerburu();
        root.Q<Label>("total-unit").text = $"{Progres.TotalUnit} unit";
    }
}
```

`LobbyController.MulaiBerburu()` dan `Progres.TotalUnit` adalah contoh; pakai fungsi yang sekarang dipanggil kode IMGUI Lobby. Matikan bagian `OnGUI` untuk layar yang sudah pindah, layar lain biarkan.

### Langkah 6 — Safe area

Pasang script ini di objek yang sama dengan UI Document supaya isi tidak tertutup notch:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class SafeArea : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.RegisterCallback<GeometryChangedEvent>(_ => Terapkan(root));
    }

    void Terapkan(VisualElement root)
    {
        var aman = Screen.safeArea;
        var skala = root.layout.width / Screen.width;
        root.style.paddingTop = (Screen.height - aman.yMax) * skala;
        root.style.paddingBottom = aman.yMin * skala;
        root.style.paddingLeft = aman.xMin * skala;
        root.style.paddingRight = (Screen.width - aman.xMax) * skala;
    }
}
```

Cek hasilnya dengan Window → General → Device Simulator (pilih HP dengan notch), lalu build ke HP sungguhan.

**Selesai kalau:** Lobby baru tampil di HP, font benar, tombol BERBURU! membuka radar lama, dan tidak ada yang tertutup notch.

## Tahap 1b: Blender MCP sampai aset uji di Unity

Tujuan tahap ini: satu aset, batu bara tahap 1, dibuat lewat Claude di Blender, diekspor, lalu tampil di layar tangkap dengan ukuran dan outline yang benar. Kalau satu aset ini lolos, sebelas sisanya tinggal mengulang.

### Langkah 1 — Pasang BlenderMCP

1. Cek apakah Blender MCP sudah terdaftar di Claude Code: jalankan `claude mcp list` di terminal. Kalau belum ada, pasang `uv` lalu daftarkan dengan perintah di bawah.
2. Unduh `addon.py` dari repo [ahujasid/blender-mcp](https://github.com/ahujasid/blender-mcp). Di Blender: Edit → Preferences → Add-ons → Install, pilih `addon.py`, lalu centang addon Blender MCP.
3. Di Blender, tekan N untuk membuka sidebar, buka tab BlenderMCP, lalu klik tombol connect. Biarkan opsi Poly Haven mati.

```bash
# hanya kalau Blender MCP belum terdaftar
claude mcp add blender -- uvx blender-mcp

# cek daftar MCP
claude mcp list
```

Simpan file `.blend` di folder `Art/` di root repo, bukan di dalam `Assets/`. Kalau ada di `Assets/`, Unity akan mencoba mengimpornya sendiri; yang masuk ke Unity cukup FBX hasil ekspor.

4. Buka sesi Claude Code di folder proyek Unity, ketik `/mcp`, dan pastikan Blender terlihat terhubung.

Aturan pakai: hanya satu klien yang tersambung ke Blender (jangan Claude Code dan aplikasi lain bersamaan), jangan jalankan `uvx blender-mcp` sendiri di terminal, dan simpan file (Ctrl+S) sebelum setiap perintah.

### Langkah 2 — File gaya

Di Claude Code, minta: "Lewat Blender MCP, jalankan prompt setup di docs/prompt-aset.md." Periksa daftar material dan fungsi `tambah_outline` yang dilaporkan, lalu simpan sebagai `Art/energo_gaya.blend`.

### Langkah 3 — Aset uji: batu bara tahap 1

1. Kirim prompt Batu bara, tapi minta tahap 1 saja dulu.
2. Lihat screenshot viewport. Perbaiki dengan pesan pendek, satu hal per pesan, misalnya "buat lebih bersudut", "kurangi jumlah segitiga", atau "outline terlalu tebal, ubah ke 0.006".
3. Kalau sudah pas, buat tahap 2–4 dengan prompt yang sama.

### Langkah 4 — Ekspor dan import

1. Kirim prompt ekspor dari dokumen prompt (ganti nama koleksi menjadi batu bara).
2. Salin FBX ke `Assets/Models/`. Atur import sesuai tabel di dokumen prompt: material ke URP/Lit, outline ke material bersama `mat_tinta` dengan **Render Face = Front** (bawaan; face outline sudah dibalik di Blender).
3. Buat prefab berisi keempat tahap sebagai child, lalu taruh di `Assets/Resources/` dengan nama yang sekarang dipanggil kode (pengganti `bara_1` sampai `bara_4`).

### Langkah 5 — Uji di HP

Mainkan mode Latihan → Batu Bara di HP, di luar ruangan. Periksa:

- Ukuran bongkahan wajar pada jarak 1,2–2 m.
- Outline tinta terlihat jelas di depan latar terang dan gelap, tidak menutupi objek.
- Warna cocok dengan tile Batu Bara di Lobby baru.
- FPS tetap stabil saat 5 bongkahan muncul bersamaan.

**Selesai kalau:** keempat tahap batu bara tampil dan pecah dengan benar di HP, dengan gaya yang serasi dengan UI baru.

## Tahap 1c: ElevenLabs sampai suara terdengar di game

Tujuan tahap ini: tiga suara UI (klik, benar, salah) dibuat, dirapikan, dan menggantikan file lama tanpa mengubah kode. Ini menguji apakah karakter "perlengkapan penjelajah" cocok sebelum 18 suara lain dibuat.

### Langkah 1 — Generate

1. Buka fitur Sound Effects di ElevenLabs.
2. Untuk `sfx_button_click`, `sfx_quiz_benar`, dan `sfx_quiz_salah`, salin prompt dari tabel di dokumen prompt.
3. Atur durasi sesuai tabel (0,3 dan 0,8 detik), loop mati, prompt influence sekitar 0,6.
4. Generate 3–4 variasi per suara, dengarkan dengan speaker HP (bukan hanya headset), lalu pilih satu.
5. Unduh sebagai WAV.

Kalau hasilnya meleset, ubah satu bagian prompt saja per percobaan, misalnya tambah "softer", "shorter attack", atau "no reverb".

### Langkah 2 — Rapikan di Audacity

1. Potong hening di awal dan akhir. Suara UI harus langsung berbunyi tanpa jeda.
2. Effect → Normalize, puncak −1 dB, supaya ketiganya sama keras.
3. Ekspor WAV dengan nama file persis seperti di tabel, misalnya `sfx_button_click.wav`.

### Langkah 3 — Masuk ke Unity

1. Timpa file lama di folder yang sekarang dimuat kode dengan nama yang sama. Karena namanya sama, kode tidak perlu diubah.
2. Atur import: Load Type **Decompress On Load**, Compression **Vorbis** kualitas 70, **Force To Mono** aktif.
3. Untuk suara baru di tahap berikutnya (misalnya `sfx_radar_ping`), panggil lewat pemutar audio yang sudah ada supaya ikut pengaturan volume:

```csharp
// contoh: dipanggil saat SDA baru masuk jarak tangkap
AudioManager.Putar("sfx_radar_ping");
```

`AudioManager.Putar` adalah contoh nama; pakai fungsi pemutar SFX yang sekarang dipakai kode untuk `sfx_button_click`.

### Langkah 4 — Uji

Mainkan Lobby dan satu kuis di HP, di luar ruangan, volume HP 50%. Periksa: klik terdengar jelas tapi tidak mengganggu, benar dan salah bisa dibedakan tanpa melihat layar, dan cap BENAR/SALAH di kuis terasa "pas" dengan bunyi capnya.

**Selesai kalau:** ketiga suara terasa satu keluarga dengan tampilan Jurnal Ekspedisi. Kalau tidak, ubah paragraf karakter suara di dokumen prompt dulu, baru lanjut ke suara lain.

## Tahap 2: vertical slice Batu Bara dan Emas

Tujuan tahap ini: satu putaran main penuh (Lobby → Radar → Tangkap → Kuis → Hasil) dengan tampilan, model, dan suara baru, untuk Batu Bara dan Emas. Keduanya dipilih karena level 1–3 di GDD hanya memunculkan dua SDA ini, jadi hasil tahap ini sudah bisa dimainkan orang lain.

Kerjakan berurutan dari atas; tiap baris bisa dibagi ke tiga jalur bersamaan.

| Urutan | UI (Unity) | Aset 3D (Blender) | Audio (ElevenLabs) |
| --- | --- | --- | --- |
| 1 | Splash, transisi | — | `sfx_splash`, `sfx_button_back` |
| 2 | Radar: piringan, node, kartu target, pil info, 4 state GPS, peringatan keselamatan | — | `sfx_radar_ping` |
| 3 | HUD tangkap umum: header, kotak status, pita kompas, tombol munculkan ulang | — | — |
| 4 | Kartu panduan (mode main dan baca) | — | — |
| 5 | HUD Tambang: pip di atas bongkahan, label hancur/total | Batu bara (sudah dari Tahap 1b) | `sfx_bara_pukul`, `sfx_bara_hancur` |
| 6 | HUD Dulang: waterpass, hitung mundur, pil EMAS n/18, kartu EMAS TERBUANG! | Endapan emas, dulang, keping besar dan kecil | `sfx_sungai`, `sfx_aliran_air`, `sfx_dulang_emas`, `sfx_hitung_mundur`, `sfx_gagal` |
| 7 | Kuis, kartu umpan balik, Hasil | — | `sfx_hasil_sempurna` |

Catatan teknis:

- **Label di atas objek 3D** (pip ketukan batu bara): buat sebagai elemen UI biasa, lalu setiap frame pindahkan posisinya dengan `Camera.WorldToScreenPoint` dan konversi ke koordinat panel. Ukurannya tetap, tidak mengecil saat objek jauh.
- **Kartu soal yang digeser**: pakai event pointer UI Toolkit (`PointerDownEvent`, `PointerMoveEvent`, `PointerUpEvent`) dan atur `translate` serta `rotate` kartu; munculkan cap BENAR/SALAH setelah lewat ambang 90 unit.
- **Bar ukur**: lebar isian diatur dari kode lewat `style.width = Length.Percent(nilai)`.

### Definisi selesai per SDA

- [ ] Model dan prefab sudah masuk, outline terlihat benar
- [ ] Semua state HUD mini-game sesuai design system (normal, awas, gagal, selesai)
- [ ] Semua suara mini-game terpasang dan sama keras
- [ ] Panduan SDA itu tampil dengan teks dari PRD
- [ ] Diuji di HP, mode ARCore dan Sensor 360, di luar ruangan

### Uji lapangan

Setelah urutan 1–7 selesai, minta 3–5 pelajar SMP/SMA (atau teman yang belum pernah main) memainkan 2 tangkapan tanpa dibantu, di dua lokasi: tempat terbuka siang hari dan tempat teduh. Catat:

- Bagian mana mereka bingung atau bertanya.
- Teks HUD mana yang sulit dibaca di bawah matahari.
- Apakah suara terdengar di speaker HP dengan bising sekitar.
- FPS dan penurunan baterai setelah 20 menit main.

Perbaiki tema dan komponen berdasarkan catatan ini sebelum lanjut ke Tahap 3, karena perbaikan di sini otomatis ikut ke semua layar berikutnya.

## Tahap 3: Matahari, Minyak, Uranium, dan layar sisanya

Polanya sudah teruji di Tahap 2, jadi tahap ini tinggal mengulang "definisi selesai per SDA" untuk tiga SDA berikutnya. Kerjakan dari yang paling sederhana ke yang paling rumit.

| Urutan | Bagian | UI (Unity) | Aset 3D | Audio | Yang perlu diperhatikan |
| --- | --- | --- | --- | --- | --- |
| 1 | Matahari (Ekstraksi) | Lingkaran bidik biasa dan terkunci, petunjuk putar kiri/kanan, kotak status gagal | Matahari | `sfx_matahari` (loop), `sfx_ekstrak_matahari` | `sfx_matahari` harus mono supaya bisa di-pan ke arah cahaya |
| 2 | Minyak Bumi (Bor & Pompa) | Kartu penampang 6 lapisan, pengukur gas vertikal, cincin pompa dan irama, bar drum dan tekanan | Pompa angguk | `sfx_bor` (loop), `sfx_pompa_detak`, `sfx_blowout` | Lengan pompa diputar dari script mengikuti tekanan jari |
| 3 | Uranium (Pemburu Geiger) | Pita kompas mode sektor, meter RAD, tombol JEPIT dengan progres, bar dosis, penghitung n/3 | Bijih dan serpihan | `sfx_jepit`, `sfx_serpihan_masuk` | Klik Geiger tetap dari kode; serpihan tanpa outline supaya tetap menyala |
| 4 | Sheet Lobby | Pengaturan (toggle, slider, reset dua langkah), Latihan (6 tile, Acak), Panduan | — | — | Toggle harus terbaca tanpa warna (kenop berisi ✓ atau ✕) |
| 5 | Pesan error simpan | Teks `kecil` warna stempel di bawah kuis, umpan balik, dan hasil | — | — | Uji dengan file save yang sengaja dirusak |

Setelah urutan 3 selesai, ulangi uji lapangan singkat (2–3 orang) khusus untuk Minyak dan Uranium, karena mekanik keduanya paling rumit.

## Tahap 4: polish dan persiapan rilis

Tahap ini tidak menambah fitur; semua isinya merapikan dan memastikan game aman dirilis.

- [ ] Gambar 13 ilustrasi kartu panduan dengan gaya sketsa jurnal (baru 1 contoh di design system)
- [ ] Animasi kecil: tombol turun saat ditekan, sheet naik dari bawah, cap muncul dari 140% ke 100%
- [ ] Samakan keras semua suara dan cek ulang setiap loop tidak berbunyi klik di sambungan
- [ ] Pastikan setiap getaran gagal disertai tag GAGAL atau cap SALAH di layar
- [ ] Uji performa di HP Android kelas bawah: target 60 fps, catat suhu dan baterai setelah 20 menit
- [ ] Hapus kode IMGUI lama yang sudah tidak dipakai, termasuk `sfx_batu_jatuh` dan `sfx_pickaxe`
- [ ] Cek lisensi komersial paket ElevenLabs yang dipakai
- [ ] Cek lisensi font (keempatnya dari Google Fonts, berlisensi OFL) dan cantumkan di halaman kredit
- [ ] Naikkan versi aplikasi dari 0.1.0 dan buat build rilis

Setelah game inti stabil, Desa Teknik dikerjakan sebagai tahap terpisah: model rumah dan pembangkit dari dokumen prompt, layar Desa menyala dan padam dari design system, plus logika stok durasi dari GDD.

## Masalah umum dan solusinya

| Masalah | Penyebab yang paling mungkin | Solusi |
| --- | --- | --- |
| Blender tidak muncul di /mcp Claude Code | MCP belum didaftarkan, addon di Blender belum connect, atau `uv` belum ada di PATH | Cek dengan claude mcp list, klik connect di tab BlenderMCP, lalu mulai sesi Claude Code baru, jalankan `uv --version` di terminal |
| Perintah ke Blender timeout | Permintaan terlalu besar dalam satu pesan | Pecah jadi beberapa pesan, satu objek per pesan |
| Model di Unity terbalik atau sangat kecil | Pengaturan ekspor FBX atau Convert Units | Ekspor ulang dengan prompt ekspor (Apply Scalings FBX All, −Z Forward, Y Up), aktifkan Convert Units di Unity |
| Outline menutupi objek | Face outline tidak dibalik, atau material outline diset Render Face = Back | Jalankan ulang `tambah_outline` (membalik face), dan biarkan `mat_tinta` di Render Face = Front |
| Outline hilang atau berkedip | Outline dibuat dengan Solidify (lapisan asli menempel di objek) | Buat ulang dengan `tambah_outline` versi Displace |
| Huruf hilang atau tampil kotak | Font Asset dibuat dari font variable, atau karakter belum ada di atlas | Pakai TTF statis hasil fonttools, buat ulang Font Asset |
| UI terlalu kecil atau terlalu besar di HP | Panel Settings belum sesuai | Reference Resolution 480 × 800, Match 0 |
| Suara UI terasa telat | Ada hening di awal file, atau Load Type Streaming | Potong hening di Audacity, pakai Decompress On Load |
| Loop berbunyi klik di sambungan | File dibuat tanpa opsi loop | Generate ulang dengan opsi loop, atau beri crossfade pendek di Audacity |
| Suara matahari tidak terdengar dari arah cahaya | File stereo | Aktifkan Force To Mono |
| Teks HUD sulit dibaca di bawah matahari | Teks diletakkan langsung di atas kamera | Selalu taruh teks di atas kartu atau pil `kertas-100` |
| FPS turun saat banyak objek | Terlalu banyak segitiga atau material berbeda | Cek batas segitiga di dokumen prompt, pakai material bersama untuk semua outline |
