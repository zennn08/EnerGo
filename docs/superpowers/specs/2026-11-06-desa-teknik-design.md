# Desa Teknik, prolog, dan karakter pemandu

Tanggal: 2026-11-06. Dokumen ini melengkapi `docs/PRD-EnerGo.md` bagian 3.13. Kalau bertentangan, dokumen ini yang berlaku, dan PRD diperbarui saat dibangun.

## Tujuan dan alasan

Sekarang hasil tangkapan SDA hanya menambah angka di koleksi, jadi pemain tidak punya alasan untuk terus keluar mencari SDA. Fitur ini memberi hasil tangkapan sebuah tujuan: menyalakan sebuah desa yang pemain rasa sebagai miliknya, dengan seorang pemandu yang menemani.

Momen yang harus terasa: desa yang gelap gulita menyala karena SDA yang baru saja pemain tangkap sendiri.

## Sasaran

1. Prolog saat pertama kali membuka game, yang memperkenalkan cerita, si pemandu, dan desa.
2. Pemain memberi nama desanya sendiri.
3. Layar Desa yang hidup: desa pegunungan 2D yang menyala atau padam.
4. Pembangkit yang diisi unit SDA, dengan stok durasi yang berkurang mengikuti waktu nyata.
5. Karakter pemandu yang berbicara lewat kotak dialog di prolog dan di layar Desa.

## Bukan sasaran

- Level, indikator level, dan pembatasan SDA per level (GDD Level 1-3). Belum ada di kode dan tidak ikut di sini. Akibatnya kelima SDA tetap muncul sejak awal.
- Desa yang tumbuh, bangunan baru, upgrade, Gulden, beberapa pembangkit.
- Notifikasi HP saat stok menipis.
- Pengaman jam HP yang diubah pemain. Game ini offline tanpa server, jadi jam HP dipercaya. Stok hanya dibatasi agar tidak melebihi kapasitas.
- Video prolog. Prolog berupa panel di dalam game.
- Model 3D `desa_rumah` dan `desa_pembangkit` di `docs/prompt-aset.md`. Diganti gambar 2D.
- Pemandu tampil di layar lain (Lobby, kuis, hasil, peringatan keselamatan). Pose untuk layar itu sudah ada, tetapi pemasangannya tahap terpisah setelah fitur ini diuji di HP.

## Yang harus dihindari

- Hukuman saat desa padam. Padam hanya berarti gelap sampai ada SDA lagi: unit, koleksi, dan nama desa tidak berkurang.
- Rasa bersalah atau tekanan harian (tuning-and-balancing: tidak ada dark pattern).
- Teks yang menyebut emas sebagai bahan bakar atau sumber energi. Bank soal punya `gold-02` dan `gold-16` yang menyatakan sebaliknya.
- Prolog yang tidak bisa dilewati.

## Keputusan pengguna

- **Cerita:** pemain adalah penjelajah yang menemukan sebuah desa yang butuh energi. SDA yang dikumpulkan dikonversi menjadi listrik berdurasi, dan durasinya tergantung SDA.
- **Pemandu:** bernama Sya, perempuan, ahli geologi muda, gaya 2D chibi. Gambarnya dibuat pengguna dengan generator gambar AI dari `docs/prompt-karakter.md`, sumbernya di `D:\Downloads\character-energo`.
- **Nama desa:** diketik sendiri oleh pemain.
- **Emas:** tidak menjadi bahan bakar. Emas dipakai memperbaiki kabel dan sambungan pembangkit, dengan durasi yang tetap dihitung 30 menit per unit.
- **Isi layar Desa:** pohon, sawah, rumah tradisional dari kayu, jalan tanah, kerbau, rumput, latar pegunungan, langit biru berawan.
- **Tampilan mengikuti jam HP:** siang seperti gambaran di atas, malam dengan lampu menyala atau gelap gulita.
- **Prolog:** panel ilustrasi dengan teks, bukan video, seperti usulan yang disetujui.
- **Pengisian pertama:** selalu memutar adegan malam, jam berapa pun, supaya momen desa menyala tetap terasa di siang hari.
- **Mode Latihan:** memberi 1 unit per ronde, apa pun hasilnya.
- **Gambar:** generator pengguna bisa membuat gambar tegak dan mengubah gambar yang ada, jadi latar dibuat dengan cara itu. Kalau hasil editnya ternyata bergeser, versi malam diturunkan dari gambar siang lewat pengolahan warna.

## Alur pemain

1. **Pembukaan pertama:** Splash, lalu prolog, lalu Lobby. Prolog muncul sekali dan ada tombol LEWATI di setiap panel. Melewati prolog tetap meminta nama desa, karena hanya sekali dan pendek.
2. **Prolog**, sekitar 40 detik, panel ilustrasi dengan gerak pelan dan teks di kartu kertas:
   1. Penjelajah berjalan malam hari, radar menangkap sinyal energi.
   2. Tiba di desa yang gelap gulita.
   3. Pemandu muncul membawa senter dan bercerita bahwa pembangkit kehabisan energi.
   4. Pemandu bertanya nama desa di peta pemain. Pemain mengetiknya.
   5. Pemandu mengajak mencari SDA di sekitar. Selesai, masuk Lobby.
3. **Tangkapan pertama** berjalan seperti biasa dari radar atau Mode Latihan.
4. **Setelah tangkapan pertama:** selama pembangkit belum pernah diisi, tombol Desa di Lobby diberi penanda, dan layar Desa menampilkan dialog pemandu yang mengarahkan ke pembangkit.
5. **Pengisian pertama:** jam berapa pun, layar berganti ke `desa_malam_padam`, lalu berubah menjadi `desa_malam_terang`, dan pemandu bersorak. Inilah puncak prolog. Setelah adegan selesai, latar kembali mengikuti jam HP. Status "sudah pernah diisi" disimpan, jadi adegan ini hanya sekali.
6. **Seterusnya:** pemain membuka Desa dari tombol baru di dock Lobby, melihat sisa stok, dan mengisi kapan saja.

Pemain tidak pernah digambar. Pemain adalah "kamu", dilihat dari sudut pandang orang pertama.

## Aturan stok

Angka berikut adalah titik awal dari `tuning-and-balancing-ringkas.md` dan harus berupa konstanta yang mudah diubah.

| SDA | Durasi per unit | Teks sumber listrik |
|---|---|---|
| Batu Bara | 20 menit | Bahan bakar |
| Matahari | 20 menit | Panel surya |
| Emas | 30 menit | Perbaikan kabel dan sambungan |
| Minyak Bumi | 45 menit | Bahan bakar |
| Uranium | 3 jam | Bahan bakar reaktor |

- Kapasitas maksimal 24 jam.
- Stok berkurang mengikuti waktu nyata, termasuk saat aplikasi ditutup.
- Disimpan sebagai satu waktu habis (UTC), bukan sisa durasi, jadi tidak perlu simulasi saat aplikasi dibuka.
- `sisa = max(0, waktuHabis - sekarang)`, dibatasi paling banyak kapasitas saat dibaca, supaya jam yang dimundurkan tidak membuat stok melebihi 24 jam.
- Mengisi satu unit: `waktuHabis = max(sekarang, waktuHabis) + durasi`.
- Unit yang akan membuat stok melebihi kapasitas ditolak dan tidak berkurang, dengan pesan singkat.
- Pengisian mengurangi koleksi di file save yang sama secara atomik: unit berkurang dan stok bertambah dalam satu `Commit`.
- Unit Normal dipakai lebih dulu, baru Murni.
- Saat Lobby menampilkan jumlah unit, jumlah itu berkurang setelah dipakai.
- **Menipis:** sisa kurang dari 3 jam. Ditampilkan di kartu stok ("Laporan Singkat") di layar Desa dan sebagai penanda di tombol Desa di Lobby.
- **Padam:** sisa 0. Berlaku juga sebelum pengisian pertama.
- **Reset progres** di Pengaturan menghapus juga stok, nama desa, dan status prolog, jadi prolog muncul lagi.

**Mode Latihan** memberi tepat 1 unit per ronde, apa pun skor mini-game dan jawaban kuis. Tangkapan dari radar tetap memakai rumus PRD 4.3 (1-6 unit). Dibedakan dari `spawnId` yang kosong, tanpa mencatat asal unit, jadi `InventoryEntry` tidak berubah. Layar hasil Latihan harus menampilkan angka 1 unit ini dengan jujur, dan PRD 2 serta 4.3 diperbarui. Tangkapan lama yang masih tertunda di save memakai `rewardQuantity` yang sudah tersimpan.

Sasaran balancing (playtest, bukan pengujian otomatis, dan hanya untuk bermain lewat radar): satu sesi sekitar 5 tangkapan menyalakan desa 8-10 jam, dan pemain yang main dua kali sehari mengalami padam kurang dari 6 jam per hari.

## Nama desa

- Panjang 1-16 karakter setelah spasi di tepi dipotong. Kosong berarti "Desa Teknik".
- Nama tampil sebagai judul layar Desa dan bisa diubah dengan mengetuknya.
- Tidak ada penyaring kata, karena offline dan hanya tampil di HP pemain.

## Layar Desa

- Scene atau layar UI Toolkit baru, satuan dasar 480 x 800, memakai token design system. Teks selalu di atas kartu atau pil `kertas-100`.
- **Latar** adalah gambar penuh, dipilih dari jam HP dengan `GeoSpawner.IsDaytime` dan status stok:
  - Siang, baik menyala maupun padam: `desa_siang`.
  - Malam menyala: `desa_malam_terang`.
  - Malam padam: `desa_malam_padam`.
- **Gerak kecil:** awan bergeser, kerbau bergerak halus. Saat malam menyala, cahaya lampu berdenyut lembut.
- **Pembangkit** adalah area pada gambar yang bisa diketuk dan membuka sheet pengisian. Ada tanda yang menunjukkan pembangkit mati saat padam, supaya siang hari pun terbaca padam.
- **Kartu stok** berisi nama desa, sisa durasi dalam `oker`, bar stok, dan cap `PADAM` bila habis. Penanda menipis memakai warna dan kata, tidak hanya warna.
- **Sheet pengisian** menampilkan kelima SDA dengan ikon `sda-*`, jumlah unit, durasi per unit, dan teks sumber listrik. Satu ketukan mengisi satu unit, dengan tombol untuk mengisi semua unit SDA itu sampai kapasitas.
- **Dialog pemandu** ada di kotak `kertas-100` di bawah layar dengan potret `pemandu_wajah_*`. Dipakai di prolog, panduan pengisian pertama, serta satu kalimat pendek berdasarkan status (menyala, menipis, padam).
- **Dock Lobby:** tombol Desa dengan ikon `rumah` dan isian `kertas-100` (`docs/design-system/components/TombolBulat`).
- Tombol kembali ke Lobby, dengan pola dan suara seperti layar lain.

## Aset

Semua gambar dibuat pengguna dengan generator AI. Tugas pembuat fitur adalah menulis promptnya ke `docs/prompt-karakter.md` dan mendaftarkan nama filenya, lalu memprosesnya.

- **Pemandu, sudah ada:** 11 pose, 6 potret, 1 lembar acuan, semuanya JPEG 1600 x 1194 berlatar putih. Prosesnya: hapus putih yang tersambung ke tepi, lubangi celah tertutup (pinggang, bangku), potong, simpan PNG transparan. Di prolog dan fitur ini yang dipakai hanya sebagian pose.
- **`pemandu_mengisi`:** perlu dibuat ulang tanpa cahaya, karena cahayanya menyisakan lingkaran putih setelah dipotong. Efek cahaya dibuat di game.
- **`wajah_berpikir` dan `wajah_sedih`:** lebih kecil dari potret lain dan disamakan ukurannya saat diproses.
- **Perlu dibuat (prompt ada di `docs/prompt-karakter.md`):** 5 panel prolog tegak 9:16, 3 latar desa tegak 9:16 (versi malam diedit dari `desa_siang` dengan posisi yang sama), serta `desa_kerbau` dan `desa_awan` sebagai gambar terpisah supaya bisa digerakkan di game.
- **Rasio layar:** gambar tegak diisi penuh (cover) dan dipotong di tepi kiri-kanan pada HP yang lebih lebar atau atas-bawah pada HP yang lebih tinggi, jadi isi penting tidak boleh di tepi.

Berkas sumber disimpan di `Art/`, hasil proses di `Assets/`. Tanpa aset final, layar harus tetap bisa dibangun dengan gambar sementara.

## Teks

- Bahasa Indonesia sesuai `docs/design-system/README.md`: "kamu", instruksi dalam huruf kapital dan pendek, tanpa emoji.
- Naskah prolog dan dialog pemandu ditulis pembuat fitur dan ditunjukkan ke pengguna sebelum dianggap selesai.
- Sisipan: `docs/PRD-EnerGo.md` bagian 3.13 dan `tuning-and-balancing*.md` diperbarui agar emas tidak disebut bahan bakar.

## Batasan teknis

- Unity 6000.3.24f1, URP, Android portrait, UI baru dengan UI Toolkit di `Assets/UI`. APK dibangun hanya bila pengguna meminta.
- Logika stok dan nama berupa kelas murni di `Assets/EnerGo/Scripts/Core/` dengan pengujian NUnit EditMode di `Assets/EnerGo/Tests/Editor/`, mengikuti `UraniumMath` dan `DulangMath`. Waktu diberikan sebagai parameter, bukan dibaca di dalam, supaya bisa diuji.
- Penyimpanan di `SaveGame` (`EnerGoProgress.cs`), tambahan field saja, tanpa menaikkan `schemaVersion`, karena `Valid` menolak versi selain 1 dan `JsonUtility` mengisi field baru dengan nilai bawaan pada save lama. Field baru ikut divalidasi: waktu habis tidak negatif dan panjang nama wajar.
- **Save lama:** prolog tetap muncul sekali, stok 0, dan unit yang sudah terkumpul bisa langsung dipakai.

## Pengujian

- Otomatis: Mode Latihan selalu 1 unit dan radar tetap 1-6 unit, adegan pertama hanya sekali, mengisi saat kosong dan saat masih ada sisa, batas kapasitas, unit ditolak, stok berkurang seiring waktu, jam dimundurkan, urutan Normal lalu Murni, unit dan stok tersimpan bersama, nama kosong atau terlalu panjang, save lama terbaca.
- Manual di HP (setelah pengguna meminta APK): prolog, mengetik nama dengan keyboard Android, pengisian pertama, tutup aplikasi lalu buka lagi untuk melihat stok berkurang, tampilan siang dan malam.

## Diserahkan ke pembuat

- Pembagian file, satu atau beberapa scene, dan bentuk animasinya.
- Rincian tata letak kartu dan sheet selama mengikuti design system.
- Tanda pembangkit mati, dan posisi area ketuk mengikuti gambar akhir.

## Keputusan Claude yang sudah disetujui pengguna

- Kelima SDA langsung terbuka, level ditunda.
- Mengisi pembangkit dilakukan manual oleh pemain, bukan otomatis setelah menangkap (mengikuti PRD).
- Unit yang tidak muat ditolak, bukan dipotong.
- Nama desa bisa diubah kapan saja.
- Pemandu di layar lain ditunda ke tahap berikutnya.
