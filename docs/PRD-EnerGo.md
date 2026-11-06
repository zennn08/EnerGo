# PRD EnerGo

Product Requirements Document untuk redesign tampilan EnerGo. Dokumen ini mencatat semua layar, aktivitas, mekanisme, teks, dan kebutuhan teknis yang ada di build sekarang (branch `feat/ui-arcade-cerah`), ditambah fitur yang sudah direncanakan di GDD dan dokumen tuning. Setiap fitur diberi label:

- "Ada": sudah berjalan di build sekarang.
- "Direncanakan": sudah diputuskan di GDD atau dokumen tuning, belum dibuat.

Semua angka di dokumen ini diambil langsung dari kode, jadi bisa dipakai sebagai acuan desain tanpa menebak.

---

## 1. Ringkasan Produk

| Item | Isi |
|---|---|
| Nama | EnerGo |
| Tagline | "Jelajahi sumber daya, temukan energinya." |
| Genre | Game edukasi berbasis lokasi (GPS) dengan mini-game AR |
| Platform | Android, orientasi portrait |
| Target pemain | Pelajar SMP/SMA di Indonesia |
| Bahasa | Bahasa Indonesia |
| Tujuan edukasi | Mengenalkan 5 sumber daya energi (SDA): Matahari, Batu Bara, Emas, Minyak Bumi, Uranium, beserta cara mendapatkannya dan dampaknya |
| Inti permainan | Berjalan di dunia nyata, temukan SDA di radar, tangkap lewat mini-game AR, jawab 3 soal benar/salah, kumpulkan unit |

### Loop utama

1. Pemain membuka radar dan berjalan di dunia nyata.
2. SDA muncul di sekitar pemain (radius 150 m). Pemain mendekat sampai jarak tangkap (20 m).
3. Pemain menekan TANGKAP dan memainkan mini-game AR khusus SDA tersebut.
4. Setelah mini-game berhasil, pemain menjawab 3 soal benar/salah tentang SDA itu.
5. Pemain mendapat unit SDA (1-6 unit). SDA itu hilang dari radar pemain.
6. Kembali ke radar dan berburu lagi.
7. Unit SDA dipakai untuk menyalakan pembangkit listrik di desa pemain.

### Mode bermain

| Mode | Cara masuk | GPS | Hasil |
|---|---|---|---|
| Berburu (utama) | Desa, tombol BERBURU! | Wajib | Unit masuk koleksi, SDA hilang dari radar |
| Latihan | Desa, dock Latihan; atau Pengaturan; atau radar saat GPS gagal | Tidak perlu | Hanya 1 unit per ronde, tidak terkait node radar |

---

## 2. Peta Layar dan Alur Navigasi

### Daftar scene

| Scene | Isi |
|---|---|
| LobbyScene | Splash, prolog, Desa (home), sheet Pengaturan, sheet Latihan, sheet Panduan, transisi |
| ExploreScene | Radar GPS |
| ARPrototype | Layar tangkap memakai ARCore (kamera + tracking ARCore) |
| Sensor360Scene | Layar tangkap memakai Sensor 360 (giroskop + feed kamera biasa) |

Layar tangkap (mini-game, kuis, hasil) memakai satu tampilan yang sama di kedua scene AR. Scene yang dipakai ditentukan oleh pengaturan "Mode AR (ARCore)".

### Alur

```
Splash
  -> Prolog (sekali)
  -> Desa (home)
       -> [BERBURU!] -> transisi "Menyalakan radar..." -> Radar
             -> [TANGKAP] -> Layar Tangkap (dari radar)
                   -> Panduan (jika belum dimatikan) -> Mini-game -> Kuis 3 soal -> Hasil
                   -> [LANJUT BERBURU] -> Radar
             -> [< RADAR] (batal) -> Radar, SDA tetap ada
             -> GPS gagal -> [MAIN MODE LATIHAN] -> Desa, sheet Latihan langsung terbuka
             -> [< DESA] -> Desa
       -> [Panduan] -> sheet Panduan -> kartu panduan per SDA
       -> [Latihan] -> sheet Latihan -> [MULAI LATIHAN] -> transisi "Menyiapkan kamera..." -> Layar Tangkap
                   -> ... -> Hasil -> [LANJUT KE DESA] -> Desa
       -> [Pengaturan] -> sheet Pengaturan
```

### Aturan navigasi khusus

- Splash hanya tampil sekali per sesi aplikasi. Kembali ke LobbyScene setelah itu langsung membuka Desa.
- Berburu sampai kuis adalah satu rangkaian. Jika aplikasi ditutup di tengah jalan, saat dibuka lagi pemain kembali ke Desa dan rangkaian itu dibuang. Kuis yang belum selesai tidak memberi unit dan SDA-nya tetap ada di radar. Unit dibayar pada jawaban terakhir, jadi yang sudah sampai sana tetap tersimpan.
- Tombol kembali di Layar Tangkap hanya muncul sebelum kuis dimulai. Selama kuis dan hasil, pemain tidak bisa keluar.
- Semua sheet di Desa bisa ditutup dengan mengetuk area di luar kartu.
- Setelah setiap ketukan ada jeda input 0,15-0,25 detik supaya satu ketukan tidak menekan dua tombol.
- Layar HP tidak pernah meredup selama aplikasi terbuka (mini-game sering dimainkan tanpa menyentuh layar).
- Frame rate target 60 fps.

---

## 3. Layar dan Aktivitas

### 3.1 Splash (Ada)

| Elemen | Detail |
|---|---|
| Latar | Dunia kartun (lihat 6.3) |
| Logo | Simbol kristal + wordmark "EnerGo" di atas pil kertas. Simbol masuk dari skala 88% ke 100% dalam 0,6 detik |
| Tagline | "Jelajahi sumber daya, temukan energinya." |
| Loading bar | Gradasi oranye, terisi selama durasi splash |
| Ajakan | "SENTUH LAYAR UNTUK MULAI", berkedip, muncul setelah 0,35 detik |
| Durasi | 2,4 detik (fade in 0,5 detik, fade out 0,4 detik). Ketuk layar untuk melewati |
| Suara | SFX splash, lalu musik lobby mulai saat masuk Desa |

### 3.2 Home: Desa (Ada)

Layar Lobby lama sudah dihapus. Home adalah layar Desa, rinciannya di 3.13. Dari atas ke bawah:

| Elemen | Isi |
|---|---|
| Kartu stok | Nama desa (ketuk untuk mengganti), sisa listrik, bar stok, cap PADAM atau tanda MENIPIS |
| Gambar desa | Latar mengikuti jam HP, Sya, kerbau, dan pembangkit yang bisa diketuk |
| Kartu dialog | Potret dan kalimat Sya sesuai keadaan desa |
| Aksi | BERBURU! sebagai tombol utama, ISI PEMBANGKIT sebagai tombol kedua |
| Dock | Panduan, Latihan, Pengaturan, di atas kartu kertas |

Jumlah unit per SDA tampil di sheet ISI PEMBANGKIT.

### 3.3 Sheet Pengaturan (Ada)

Sheet kartu kertas dari bawah, pita judul "PENGATURAN".

| Baris | Tipe | Teks | Perilaku |
|---|---|---|---|
| Mode AR | Toggle | "Mode AR (ARCore)". Sub: nyala "Butuh Google Play Services for AR", mati "Mati: pakai Sensor 360 (giroskop)" | Disimpan. Menentukan scene tangkap |
| Volume | Slider | "Volume" + persen | Geser di bar. Disimpan dan diputar contoh klik saat dilepas |
| Getaran | Toggle | "Getaran". Sub: nyala "HP bergetar saat main", mati "Mati: tanpa getaran" | Disimpan. HP bergetar sekali saat dinyalakan |
| Latihan | Tombol biru | "LATIHAN (TANPA GPS)" | Membuka sheet Latihan |
| Reset | Tombol | "RESET PROGRES" (teks merah). Ketuk pertama berubah jadi tombol merah "TEKAN LAGI UNTUK MENGHAPUS" selama 3,5 detik | Ketuk kedua dalam 3,5 detik menghapus semua progres |
| Selesai | Tombol oranye | "SELESAI" | Menutup sheet |

### 3.4 Sheet Latihan (Ada)

Pita judul "LATIHAN". Sub: "Pilih sumber daya, main tanpa GPS".

- Grid 6 tile: Matahari, Batu Bara, Emas, Minyak Bumi, Uranium, Acak (ikon dadu, gradasi pink).
- Tile terpilih diberi cincin oranye. Pilihan disimpan untuk sesi berikutnya.
- Tile SDA menampilkan badge jumlah unit, tile Acak tidak.
- Tombol "MULAI LATIHAN": Acak memilih SDA secara acak tiap kali mulai.
- Arah SDA di layar tangkap diacak (tidak ada arah dari radar).

### 3.5 Sheet Panduan (Ada)

Pita judul "PANDUAN". Sub: "Ketuk untuk membaca cara mainnya".

- Grid 5 tile SDA. Ketuk tile untuk membuka kartu panduan SDA itu (mode baca, tombol TUTUP).
- Tombol "TAMPILKAN SEMUA PANDUAN LAGI": menghapus semua pilihan "Jangan ingatkan lagi". Muncul teks konfirmasi "Panduan akan muncul lagi" selama 2 detik.

### 3.6 Kartu Panduan (Ada)

Dipakai di dua tempat: sebelum mini-game (mode main) dan dari sheet Panduan (mode baca).

| Elemen | Detail |
|---|---|
| Latar | Layar digelapkan 55% |
| Kartu | Pita judul nama SDA, "LANGKAH n/N", gambar ilustrasi, judul langkah, isi langkah, titik halaman |
| Pindah halaman | Ketuk sepertiga kiri/kanan kartu, atau tombol utama |
| Mode main | Kotak centang "Jangan ingatkan lagi", tombol utama "LANJUT" lalu "MULAI" di halaman terakhir, tombol kecil "LEWATI" |
| Mode baca | Tombol utama "LANJUT" lalu "TUTUP", tombol kecil "TUTUP" |

Mini-game (termasuk timer matahari) baru mulai setelah pemain menekan MULAI atau LEWATI.

Isi kartu per SDA:

| SDA | Langkah | Judul | Isi | Ilustrasi |
|---|---|---|---|---|
| Matahari | 1 | Kejar cahaya | Arahkan lingkaran di tengah layar ke cahaya matahari yang bergerak. | Lingkaran bidik + matahari |
| | 2 | Kumpulkan sebelum waktu habis | Minimal 50% cahaya. Makin penuh barnya, makin banyak bonus. | Lingkaran bidik |
| Batu Bara | 1 | Ikuti kompas | Hadap ke titik oranye di pita kompas sampai SDA terlihat di kamera. | Pita kompas |
| | 2 | Pecahkan bongkahan | Ketuk tiap bongkahan batu bara berkali-kali sampai hancur. | Ketukan jari |
| Emas | 1 | Ikuti kompas | (sama seperti di atas) | Pita kompas |
| | 2 | Letakkan HP datar | Setelah endapan diketuk, letakkan HP datar sampai hitungan mulai. | HP mendatar + gelembung |
| | 3 | Putar pelan seperti mendulang | Putar HP perlahan. Terlalu kencang, emasnya tumpah. | HP berputar |
| Minyak Bumi | 1 | Tahan untuk mengebor | Tahan layar untuk mengebor ke bawah. Bor tidak langsung berhenti saat dilepas. | Mata bor |
| | 2 | Lepas di lapisan minyak | Pelan-pelan di kantong gas, lalu lepas tepat di lapisan minyak. | Lapisan tanah |
| | 3 | Pompa ikuti lingkaran | Tekan saat lingkaran mengecil pas di target. Terlalu cepat, tekanan naik. | Cincin pompa |
| Uranium | 1 | Ikuti sinyal RAD | Hadap ke sektor hijau di kompas, lalu putar pelan sampai angka RAD paling tinggi. | Meter RAD |
| | 2 | Cari & jepit serpihan | Bijih pecah jadi 3 serpihan tak terlihat di sekelilingmu. Ikuti RAD sampai serpihan berpendar, lalu tahan JEPIT. Penjepit menjaga jarak tanganmu dari radiasi. | Penjepit |
| | 3 | Tutup wadah timbal | Wadah terbuka membuat RAD bising dan dosis naik cepat. Tekan TUTUP tiap serpihan masuk, karena timbal adalah perisai. HP terguncang, jepitan lepas. | HP bergetar |

Ilustrasi saat ini masih bentuk sederhana (placeholder) dan perlu digambar ulang.

### 3.7 Transisi (Ada)

Layar menggelap 0,45 detik dengan simbol logo dan teks "Menyalakan radar..." (ke radar) atau "Menyiapkan kamera..." (ke layar tangkap).

### 3.8 Radar (Ada)

| Elemen | Detail |
|---|---|
| Tombol kiri atas | "< LOBBY" |
| Pil kanan atas | "SPAWN BARU mm:dd", hitung mundur ke pergantian SDA berikutnya (setiap 15 menit) |
| Radar | Lingkaran kertas, cincin jarak 50 m dan 100 m dengan label, garis sapuan berputar (90 derajat/detik), huruf arah U/T/S/B di tepi (U oranye) yang ikut berputar |
| Pemain | Titik oranye di tengah dengan sorot kipas ±35 derajat ke atas. Radar heading-up: atas selalu arah HP menghadap, dunia yang berputar |
| Node SDA | Ikon SDA. Di luar jangkauan 22 px, dalam jangkauan 30 px + cincin oranye. Node terpilih diberi cincin gelap. Uranium diberi cahaya berdenyut karena langka. Node terkunci diberi gembok kecil |
| Pil info | Baris 1: "Kompas: [sumber] · N°" atau "Kompas tidak tersedia, utara di atas" (oranye). Baris 2: "GPS ±N m", ditambah " · Sinyal lemah, coba ke tempat terbuka" (oranye) jika akurasi lebih buruk dari 30 m |
| Kartu SDA terpilih | Ikon, nama SDA, "N m · Arah" (8 arah: Utara, Timur Laut, Timur, Tenggara, Selatan, Barat Daya, Barat, Barat Laut), panah yang menunjuk arah jalan relatif terhadap HP |
| Tombol kartu | Dalam jangkauan: "TANGKAP" (oranye). Di luar: "DEKATI DULU (N m)" (abu-abu, nonaktif). Terkunci: "SEGERA HADIR" (abu-abu) |
| Kosong | "Belum ada SDA di sekitar. Coba berjalan ke area lain." |

Perilaku:
- Node dipilih dengan mengetuknya di radar. Default memilih yang terdekat.
- Daftar SDA diperbarui setiap 0,5 detik.
- HP bergetar sekali saat ada SDA baru masuk jarak tangkap (tidak bergetar ulang selama 10 detik untuk SDA yang sama, supaya tidak berisik karena GPS goyang).

State GPS:

| State | Judul (pita merah) | Isi | Tombol |
|---|---|---|---|
| Mencari | (tanpa pita) | "Mencari sinyal GPS..." | - |
| Izin ditolak | IZIN LOKASI DITOLAK | Radar butuh lokasimu untuk menemukan SDA di sekitar. | COBA LAGI, MAIN MODE LATIHAN |
| Lokasi mati | LOKASI HP MATI | Nyalakan Lokasi di pengaturan HP, lalu coba lagi. | COBA LAGI, MAIN MODE LATIHAN |
| GPS gagal | GPS TIDAK MERESPONS | Coba pindah ke tempat terbuka, lalu coba lagi. | COBA LAGI, MAIN MODE LATIHAN |

GPS dianggap gagal jika tidak ada posisi dalam 20 detik.

Peringatan keselamatan (sekali seumur instalasi, sebelum radar bisa dipakai):
- Pita: "HATI-HATI DI JALAN"
- Isi: "Perhatikan jalan dan sekitarmu saat berburu. Jangan masuk ke area berbahaya atau milik orang lain."
- Tombol: "SAYA MENGERTI"

### 3.9 Layar Tangkap: HUD umum (Ada)

Tampil di atas feed kamera selama mini-game.

| Elemen | Detail |
|---|---|
| Kiri atas | "< RADAR" (jika dari radar) atau "< LOBBY" (jika latihan). Keluar tanpa menangkap, SDA tetap ada di radar |
| Tengah atas | Mode kamera: "ARCORE", "SENSOR 360", atau "DRAG 360". Di scene ARCore ini tombol untuk berpindah mode. Di scene Sensor 360 berupa pil "MODE TANPA ARCORE" |
| Kanan atas | Pil nama SDA + "N UNIT" |
| Kotak status | Kotak gelap di bawah header berisi instruksi 1-2 baris dan progress bar. Garis tepi hijau (aman) atau oranye (peringatan) |
| Pita kompas | Strip kompas ±60 derajat: garis tiap 15 derajat, label U/TL/T/TG/S/BD/B/BL tiap 45 derajat, garis kuning di tengah (arah pandang), titik oranye (posisi SDA). Jika SDA di luar strip, tampil penanda di tepi "< 75°" atau "75° >". Tanpa kompas: "KOMPAS TIDAK TERSEDIA" |
| Bawah | Tombol "MUNCULKAN ULANG TARGET": memunculkan ulang SDA dan mereset arah kamera |
| Efek ketukan | Cincin oranye yang membesar dan memudar di titik ketukan (0,35 detik) |

Pesan status umum (muncul saat belum ada target):

| Pesan | Kapan |
|---|---|
| Memulai inisialisasi sensor spasial... | Awal |
| MODE DRAG 360 AKTIF (EDITOR) / MODE SENSOR 360 AKTIF / MEMERIKSA SENSOR ARCORE... | Saat memilih mode |
| SENSOR 360 AKTIF (ARCORE TIDAK TERSEDIA) | ARCore tidak siap dalam 2 detik, otomatis pindah |
| BERALIH KE SENSOR 360 / BERALIH KE ARCORE | Setelah tombol mode ditekan |
| CARI CAHAYA DI DEPAN / IKUTI PITA KOMPAS | Setelah munculkan ulang |
| MENUNGGU TARGET TERDETEKSI... | Ketukan saat target belum ada |
| TARGET DI LUAR JANGKAUAN // IKUTI ARAH RADAR | Ketukan saat target di luar layar |
| Aset model ... belum tersedia di Resources. / MODEL MINERAL TIDAK MEMILIKI MESH VALID | Error aset |
| ANALISIS SIAP | Mini-game selesai, kuis dibuka |

### 3.10 Mini-game per SDA (Ada)

Kelima mini-game punya pola yang sama: SDA muncul di arah sebenarnya (sesuai bearing dari radar, dikunci ke utara lewat kompas), pemain memainkan mekaniknya, lalu skor 0-1 dihitung dan diubah menjadi bonus unit (lihat 4.3).

#### a. Matahari: "Ekstraksi" (mekanik Lensa)

- Matahari muncul di depan pemain, sedikit di atas cakrawala (12 derajat), lalu melayang acak: ±38 derajat kiri-kanan, ±14 derajat atas-bawah. Gerakannya mulai pelan dalam 1,5 detik pertama.
- Lingkaran bidik tetap di tengah layar (titik-titik putih, menyala kuning saat cahaya masuk).
- Cahaya di dalam lingkaran: bar terisi (penuh dalam 8 detik). Di luar: bar berkurang setengah kecepatan.
- Batas waktu acak 15-25 detik per matahari.
- Berhasil: bar penuh (skor 1), atau waktu habis dengan bar minimal 50% (skor 0 di 50%, naik sampai 1 di 100%).
- Gagal: waktu habis dengan bar di bawah 50%. Pesan "CAHAYA TERLALU SEDIKIT", matahari baru muncul setelah 1,5 detik.
- Kotak status: "‹ PUTAR KE KIRI N°" / "PUTAR KE KANAN N° ›" + "SISA N DTK" jika matahari lebih dari 28 derajat di samping; selain itu "CAHAYA TERKUNCI · SISA N DTK" atau "ARAHKAN KE CAHAYA · SISA N DTK", dengan bar persen.
- Umpan balik: getaran halus tiap 0,25 detik selama terkunci, suara "whoosh" yang di-pan ke arah matahari (keras saat lepas, pelan saat terkunci), suara ekstraksi yang posisinya mengikuti persentase bar.
- Matahari hanya muncul siang hari (06.00-18.00 waktu lokal) di radar. Di mode latihan bisa kapan saja.

#### b. Batu Bara: "Tambang" (mekanik Pemicu)

- 3-5 bongkahan muncul dalam busur ±75 derajat di arah SDA, jarak 1,2-2 m, tinggi bervariasi, antar-bongkahan minimal 20 derajat.
- Tiap bongkahan butuh 2-5 ketukan (bongkahan besar butuh lebih banyak). Ukuran bongkahan menunjukkan jumlah ketukan.
- Bongkahan melewati 4 tahap model (utuh, pecah sedang, butiran, serbuk), lalu hancur dengan serpihan berhamburan.
- Di atas tiap bongkahan ada deretan kotak kecil (pip), terisi oranye sesuai ketukan yang sudah masuk.
- Kotak status: "PECAHKAN BATU BARA · KETUK BONGKAHNYA", bar progres ketukan, label "hancur/total".
- Titik oranye di pita kompas menunjuk bongkahan terdekat dari arah pandang.
- Selesai: "SEMUA BATU BARA TERKUMPUL". Skor dari kecepatan: target waktu = 0,45 detik per ketukan + 1,5 detik per bongkahan. Di bawah target = 1, 2,5 kali target atau lebih = 0.
- Tidak ada kondisi gagal.
- Umpan balik: suara pukul + getar 20 ms per ketukan, suara hancur + getar 50 ms, getar 120 ms saat semua hancur, bongkahan mengecil sebentar saat dipukul.

#### c. Emas: "Dulang" (mekanik Stabilizer)

Fase:
1. Cari: endapan emas berputar pelan di arah SDA. Kotak status "ENDAPAN EMAS DITEMUKAN" + "Ketuk endapannya untuk menyendok pasir". Suara sungai diputar sepanjang scene emas.
2. Mendatar: dulang muncul di layar. Pemain meletakkan HP datar (layar menghadap ke atas, toleransi 12 derajat) selama 1 detik. Ada gelembung waterpass di tengah dulang; cincin dan gelembung jadi hijau saat datar. Kotak status "LETAKKAN HP MENDATAR" dengan bar lama menahan.
3. Hitung mundur: kartu "SIAP?" dengan angka 3-2-1, lalu tulisan "MULAI!".
4. Mendulang: pemain memutar kemiringan HP pelan membentuk lingkaran; titik di sekeliling dulang menunjukkan arah miring dan lingkaran yang harus diikuti. Air mengalir ke sisi rendah, butir pasir berputar dan tumpah di bibir dulang, gundukan pasir menipis seiring progres, memperlihatkan emas di bawahnya.
   - Terisi jika arah miring berputar minimal 40 derajat/detik dan miring cukup (0,08-0,6 g). Penuh dalam 10 detik.
   - Terlalu kasar selama 0,25 detik atau lebih (kemiringan bergerak lebih dari 2,2 g/detik, miring lebih dari 0,6 g, atau guncangan lebih dari 0,5 g): bar turun, dan setiap 0,35 detik satu keping emas terlempar keluar dari dulang (animasi melengkung lalu hilang). Kotak status "TERLALU KENCANG! EMAS BISA TUMPAH".
   - Diam saja: bar tidak bergerak.
   - Pil di atas dulang: "EMAS n/18" (18 keping: 4 bongkah besar, 14 serpih kecil).
5. Selesai: "EMAS TERPISAH DARI PASIR". Skor = sisa emas, dari 40% (skor 0) sampai 100% (skor 1).
6. Gagal: sisa emas di bawah 40%. Kartu merah "EMAS TERBUANG!" dengan "Gerakanmu terlalu kencang, emasnya tumpah dari dulang." dan tombol "CARI ENDAPAN BARU". Endapan baru muncul 25-50 derajat ke samping.

Umpan balik: suara aliran air (volume naik sesuai kecepatan putar), getar halus saat kasar, getar 120 ms tiap keping tumpah.

Tanpa akselerometer (editor), seret jari di sekitar dulang berlaku sebagai memiringkan.

#### d. Minyak Bumi: "Bor & Pompa"

Model pompa angguk (pump jack) muncul di arah SDA.

Fase Bor:
- Penampang tanah ditampilkan sebagai kartu dengan 6 lapisan berwarna dan berlabel: Tanah (coklat), Batuan (abu), Batuan Penutup (abu gelap), Kantong Gas (ungu), Minyak (hitam kecoklatan, lapisan tipis), Air Asin (biru). Ketebalan tiap lapisan diacak setiap sumur.
- Tahan layar (di mana saja kecuali bar atas dan bawah) untuk mengebor. Bor punya momentum: tidak langsung berhenti saat dilepas.
- Kecepatan per lapisan: Tanah ×1, Batuan ×0,7, Batuan Penutup ×0,35, Kantong Gas ×1,2, Minyak ×0,8.
- Pengukur tekanan gas vertikal di samping penampang. Tekanan naik jika mengebor cepat di kantong gas, turun perlahan jika pelan atau di luar gas.
- Berhasil: bor berhenti di lapisan minyak, lanjut ke fase pompa.
- Gagal: tekanan gas penuh ("BLOWOUT!") atau tembus ke air asin ("KENA AIR!").
- Kotak status: "TAHAN UNTUK MENGEBOR · [LAPISAN]", "KANTONG GAS! PELAN-PELAN", "LAPISAN MINYAK! LEPAS SEKARANG", dengan bar kedalaman dan label meter (0-2000 m).
- Getar halus tiap 0,25 detik selama mengebor.

Fase Pompa:
- Lingkaran target di bawah layar. Cincin mengecil dari 2,6 kali ukuran target ke ukuran target sesuai irama 1,2 detik (lebih cepat 15% setelah drum 70%). Ketukan pertama hanya memulai irama ("TEKAN UNTUK MULAI MEMOMPA").
- Tekan saat cincin pas (toleransi ±0,2 detik): drum +10%. Terlalu cepat: tekanan pompa +25%. Terlambat: tidak dapat apa-apa.
- Tidak memompa lebih dari 2 detik: drum berkurang pelan.
- Bar "TEKANAN" di bawah, merah di atas 60%.
- Lengan pompa angguk bergerak naik-turun mengikuti tekanan jari.
- Kotak status: "TEKAN SAAT LINGKARAN PAS", "PAS! TERUS POMPA", "TERLALU CEPAT! TEKANAN NAIK", "TERLAMBAT · IKUTI IRAMA", bar drum dalam persen.
- Berhasil: drum 100% ("DRUM MINYAK PENUH"). Skor = setengah dari kecepatan (target 25 detik total) + setengah dari persentase tekanan tepat.
- Gagal: tekanan pompa penuh ("OVERPRESSURE!").

Setelah gagal (bor atau pompa), pesan tampil 1,5 detik lalu sumur baru dengan lapisan baru muncul ("CARI TITIK BOR BARU").

#### e. Uranium: "Pemburu Geiger"

Fase Cari:
- Bijih uranium tersembunyi (tidak terlihat) di arah SDA, sedikit di atas atau bawah mata (-5 sampai 15 derajat).
- Pita kompas hanya menunjukkan sektor hijau kasar ±45 derajat, bukan titik pasti.
- Meter "RAD 0-100" besar di bawah layar, makin tinggi makin dekat arah pandang ke bijih. Label tren "SINYAL MENGUAT" / "SINYAL MELEMAH".
- Bunyi Geiger: klik teratur dari 2 klik/detik (jauh) sampai 20 klik/detik (tepat), dengan getar (maksimal 12 per detik).
- Lingkaran bidik di tengah menyala hijau saat dekat.
- Bijih ditemukan jika arah pandang dalam 20 derajat selama 0,8 detik.
- Kotak status: "IKUTI SEKTOR HIJAU · CARI RAD TERTINGGI" atau "SINYAL KUAT! TAHAN ARAH HP", bar RAD.

Fase Ambil:
- Bijih pecah menjadi 3 serpihan hijau menyala (#39FF14): satu di tempat bijih, dua lainnya di sekeliling pemain (minimal 80 derajat terpisah). "BIJIH PECAH! AMBIL SERPIHANNYA".
- Serpihan tidak terlihat. Serpihan mulai berpendar saat arah pandang kurang dari 28 derajat dan tampak penuh pada 20 derajat, lalu hilang lagi kalau arah pandang menjauh.
- Meter RAD sekarang menunjuk serpihan terdekat.
- Tombol bulat "JEPIT" di kanan layar setinggi ibu jari (area sentuh dilebarkan). Tahan JEPIT sambil membidik serpihan (dalam 20 derajat) selama 1 detik. Lingkaran dalam tombol membesar sesuai progres jepit.
- HP terguncang (lebih dari 0,35 g): jepitan lepas, "JEPITAN LEPAS! PEGANG HP STABIL".
- Serpihan yang terjepit terbang ke wadah timbal di kiri bawah dan hilang. "SERPIHAN MASUK WADAH TIMBAL". Tutup wadah terlepas.
- Tombol bulat "TUTUP" muncul di atas wadah setelah serpihan mendarat. Selama wadah terbuka: serpihan di dalamnya ikut terbaca sehingga meter RAD minimal 85 dan Geiger tetap cepat, serpihan di wadah tidak melindungi sehingga dosis naik secepat saat semua serpihan di luar, dan kotak status "TUTUP WADAH! RADIASI BOCOR".
- Bar "DOSIS" di bawah terus naik (penuh dalam 60 detik jika 3 serpihan masih di luar). Tiap serpihan di wadah yang tertutup memperlambat dosis, tetapi tidak pernah berhenti (minimal 25%). Merah di atas 75%.
- Saat menahan JEPIT, layar tidak bisa digeser.
- Kotak status: "CARI SERPIHAN · IKUTI RAD", "TAHAN TOMBOL JEPIT", "MENJEPIT... TAHAN", "JEPITAN LEPAS! PEGANG HP STABIL", "TUTUP WADAH! RADIASI BOCOR", bar "n/3".
- Berhasil: 3 serpihan masuk dan wadah ditutup ("URANIUM AMAN DI WADAH TIMBAL"). Skor = setengah dari kecepatan (target 35 detik) + setengah dari sisa dosis.
- Gagal: dosis penuh ("DOSIS BERLEBIH!"). Setelah 1,5 detik, bijih baru disembunyikan ("CARI BIJIH URANIUM BARU").

Edukasi yang dibawa: tiga prinsip keselamatan radiasi (waktu, jarak, perisai).

Catatan tuning (Direncanakan, dari dokumen tuning): dosis dipercepat menjadi penuh dalam 22 detik dan dosis mulai berjalan pelan (1/4 kecepatan) sejak fase cari.

### 3.11 Kuis (Ada)

Dibuka otomatis 0,6 detik setelah mini-game berhasil. Latar dunia kartun digelapkan.

| Elemen | Detail |
|---|---|
| Penanda progres | 3 bulatan di kiri atas: kuning (soal sekarang), hijau dengan centang (benar), merah dengan silang (salah), gelap (belum) |
| Pil kanan atas | "SOAL n/3" |
| Judul | "BENAR" putih + "ATAU" oranye + "SALAH?" putih |
| Kartu soal | Kartu kertas: ikon jam + bar waktu + angka detik di atas, ikon SDA, teks pernyataan. Kartu berikutnya mengintip miring 3 derajat di belakang |
| Timer | 15 detik per soal. Bar oranye, jadi merah di 5 detik terakhir |
| Menjawab | Geser kartu ke kanan = BENAR, ke kiri = SALAH (ambang 90 px). Kartu miring saat digeser (maks 18 derajat), tepi layar berwarna hijau/merah, dan muncul cap "BENAR"/"SALAH". Bisa juga dengan tombol "SALAH" (merah, ikon silang, kiri) dan "BENAR" (hijau, ikon centang, kanan) |
| Waktu habis | Dihitung salah |
| Umpan balik | Benar: SFX benar + getar 30 ms. Salah: SFX salah + getar 200 ms |

Kartu umpan balik setelah tiap jawaban:

| Elemen | Detail |
|---|---|
| Pita | "BENAR!" (hijau), "SALAH" (merah), atau "WAKTU HABIS" (merah) |
| Chip | "Pernyataan ini BENAR" (hijau) atau "Pernyataan ini SALAH" (merah) |
| Isi | Ikon SDA dan penjelasan singkat |
| Hadiah | Chip "+1 unit" dengan ikon SDA, hanya jika benar |
| Tombol | "SOAL BERIKUTNYA" atau "LIHAT HASIL". Jika salah, tombol terkunci 2 detik bertuliskan "BACA DULU..." dengan bar pengisi, supaya penjelasan dibaca |

### 3.12 Hasil (Ada)

| Elemen | Detail |
|---|---|
| Judul besar | "SEMPURNA!" (3 benar), "BAGUS!" (1-2 benar), "COBA LAGI" (0 benar) |
| Kartu | Pita "HASIL ANALISIS" |
| Bintang | 3 bintang (tengah lebih besar), terisi sesuai jumlah benar |
| Skor | "n/3 BENAR" |
| Hadiah | Ikon SDA + "+N" besar + chip kualitas "Murni" (semua benar) atau "Normal" |
| Rincian | Chip "Dasar +1", "Kuis +n", "[Nama mini-game] [Hebat/Bagus/Cukup] +b". Mode Latihan: satu chip "Mode Latihan +1" |
| Pesan | Semua benar: "Sempurna! Semua jawaban tepat." 0 benar: "Belum ada yang tepat. Coba lagi di tangkapan berikutnya." Lainnya: "Bagus! Jawab semua dengan tepat untuk hasil Murni." |
| Tombol | "LANJUT BERBURU" (dari radar) atau "LANJUT KE LOBBY" (latihan) |

Nama mini-game di chip: Ekstraksi (Matahari), Tambang (Batu Bara), Dulang (Emas), Bor & Pompa (Minyak), Pemburu Geiger (Uranium).

Pesan error simpan (jika ada) tampil di bawah kartu kuis, umpan balik, dan hasil.

### 3.13 Desa, Prolog, dan Pemandu Sya (Ada)

Rancangan: `docs/superpowers/specs/2026-11-06-desa-teknik-design.md`.

**Prolog.** Muncul sekali setelah Splash pada pembukaan pertama, 5 panel ilustrasi dengan teks di kartu kertas. LEWATI melompat ke panel nama. Panel 4 meminta nama desa (1-16 karakter, kosong menjadi "Desa Teknik"). Tangkapan yang terputus karena aplikasi ditutup dibuang saat aplikasi dibuka lagi.

**Pemandu.** Sya, ahli geologi muda, 2D chibi. Tampil di prolog dan layar Desa. Pose dan potret: `docs/prompt-karakter.md`.

**Pembangkit.** Di home, pemain memasukkan unit SDA sendiri lewat sheet ISI PEMBANGKIT. Setiap unit menambah durasi menyala:

| SDA | Durasi per unit | Peran |
|---|---|---|
| Batu Bara | 20 menit | Bahan bakar pembangkit |
| Matahari | 20 menit | Dialirkan dari panel surya |
| Emas | 30 menit | Memperbaiki kabel dan sambungan, bukan bahan bakar |
| Minyak Bumi | 45 menit | Bahan bakar pembangkit |
| Uranium | 3 jam | Bahan bakar reaktor |

- Stok disimpan sebagai satu waktu habis (UTC) dan berkurang mengikuti waktu nyata, termasuk saat aplikasi ditutup.
- Kapasitas maksimal 24 jam. Unit yang membuat stok melebihi kapasitas ditolak dan tidak berkurang.
- Unit Normal dipakai lebih dulu, baru Murni.
- Menipis: sisa kurang dari 3 jam, tampil di kartu stok.
- Padam: sisa 0. Desa gelap sampai diisi lagi, tanpa hukuman lain.

**Layar Desa.** Latar mengikuti jam HP: siang `desa_siang`, malam `desa_malam_terang` atau `desa_malam_padam`. Pengisian pertama selalu memutar adegan malam dari gelap ke terang, jam berapa pun, sekali saja.

Belum ada: level, desa yang tumbuh, Gulden (dibatalkan), notifikasi HP. Pemandu di layar lain menyusul.

---

## 4. Aturan Game

### 4.1 Sumber Daya (SDA)

| ID | Nama | Peluang muncul | Waktu muncul | Mini-game | Jumlah soal di bank |
|---|---|---|---|---|---|
| sun | Matahari | 35% | Siang saja (06.00-18.00) | Ekstraksi | 33 |
| coal | Batu Bara | 30% | Kapan saja | Tambang | 37 |
| gold | Emas | 20% | Kapan saja | Dulang | 35 |
| oil | Minyak Bumi | 12% | Kapan saja | Bor & Pompa | 30 |
| uranium | Uranium | 3% | Kapan saja | Pemburu Geiger | 31 |

Malam hari, Matahari tidak muncul dan peluang yang lain dinormalkan: Batu Bara 46%, Emas 31%, Minyak 18%, Uranium 4,6%.

Warna gradasi tile per SDA (atas ke bawah):

| SDA | Atas | Bawah |
|---|---|---|
| Matahari | #FFF0A0 | #FFC93C |
| Batu Bara | #B9BFCC | #6B7180 |
| Emas | #FFE9A0 | #F0B52A |
| Minyak Bumi | #8A7563 | #3A2F27 |
| Uranium | #D6FF9C | #58C23A |
| Acak | #FFB3E0 | #D1409F |

### 4.2 Kemunculan SDA di dunia

- Dunia dibagi menjadi petak 0,0005 derajat (sekitar 55 m).
- Tiap petak berisi 0 SDA (60%), 1 SDA (30%), atau 2 SDA (10%) di titik acak dalam petak.
- SDA berganti setiap 15 menit (jendela waktu UTC). Hitung mundur ditampilkan di radar.
- Kemunculan deterministik: semua HP di tempat dan waktu yang sama melihat SDA yang sama, tanpa server.
- Siang/malam hanya mengubah jenis SDA, bukan posisinya.
- Radar menampilkan SDA dalam radius 150 m, diurutkan dari yang terdekat.
- SDA yang sudah ditangkap hilang dari radar pemain itu (dicatat untuk jendela sekarang dan sebelumnya).
- Jarak tangkap 20 m, ditambah bonus akurasi GPS maksimal 15 m (jadi paling jauh 35 m saat GPS buruk).

### 4.3 Hadiah

```
Unit = 1 (dasar) + jumlah jawaban benar (0-3) + bonus mini-game (0-2)
```

| Skor mini-game (0-1) | Nilai | Bonus |
|---|---|---|
| 0,75 ke atas | Hebat | +2 |
| 0,40 sampai di bawah 0,75 | Bagus | +1 |
| Di bawah 0,40 | Cukup | +0 |

- Minimal 1 unit, maksimal 6 unit per tangkapan dari radar. Mode Latihan selalu 1 unit, supaya berburu di luar tetap lebih menguntungkan.
- Kualitas "Murni" jika ketiga jawaban benar, selain itu "Normal". Koleksi menyimpan jumlah Murni dan Normal secara terpisah (tampilan saat ini menjumlahkan keduanya).
- Unit dan penghapusan SDA dari radar disimpan bersamaan saat jawaban terakhir masuk.

### 4.4 Ringkasan skor per mini-game

| Mini-game | Kondisi gagal | Rumus skor |
|---|---|---|
| Matahari | Waktu habis, bar < 50% | Bar penuh = 1. Waktu habis = (bar - 50%) / 50% |
| Batu Bara | Tidak ada | Kecepatan terhadap target waktu |
| Emas | Sisa emas < 40% | Sisa emas, 40% = 0 sampai 100% = 1 |
| Minyak | Blowout, kena air, overpressure | 50% kecepatan (target 25 dtk) + 50% rasio tekanan tepat |
| Uranium | Dosis penuh | 50% kecepatan (target 35 dtk) + 50% sisa dosis |

Skor kecepatan: selesai pada atau di bawah target = 1, selesai pada 2,5 kali target atau lebih = 0, linier di antaranya.

### 4.5 Bank soal

- Total 166 soal benar/salah: Matahari 33, Batu Bara 37, Emas 35, Minyak 30, Uranium 31.
- Tiap soal punya pernyataan, jawaban, penjelasan, dan sumber (EIA, NASA, USGS, RSC, WHO, IEA, World Nuclear Association, CDC).
- Tiap tangkapan mengambil 3 soal acak tanpa pengulangan dari bank SDA tersebut.
- Hanya soal yang sudah ditandai "reviewed" yang dipakai.

---

## 5. Kebutuhan Teknis

### 5.1 Platform

| Item | Nilai |
|---|---|
| Engine | Unity 6000.3.24f1 |
| Render pipeline | URP 17.3.0 |
| AR | AR Foundation 6.3.5 + ARCore 6.3.5 |
| Input | Input System 1.20.0 (Enhanced Touch) |
| Android minimum | API 30 (Android 11) |
| Versi aplikasi | 0.1.0 |
| Orientasi | Portrait |
| UI | UI Toolkit (UXML dan USS di `Assets/UI`), skala dasar 480 × 800 unit, memperhatikan safe area (notch) |

### 5.2 Sensor dan izin

| Sensor / izin | Dipakai untuk | Jika tidak ada |
|---|---|---|
| Lokasi (FINE_LOCATION) | Radar | Layar error + tombol Mode Latihan |
| Kamera | Feed kamera di belakang SDA | Sensor 360 memakai latar gelap polos |
| ARCore | Tracking AR di scene ARCore | Otomatis pindah ke Sensor 360 setelah 2 detik |
| Sensor rotasi (kompas fusi) | Arah radar, kompas di layar tangkap | Radar memakai utara di atas, pita kompas "KOMPAS TIDAK TERSEDIA" |
| Giroskop / attitude | Arah kamera di Sensor 360 | Geser layar untuk melihat sekeliling ("DRAG 360") |
| Akselerometer | Dulang emas, guncangan uranium | Editor: seret jari menggantikan kemiringan |
| Getar (VIBRATE) | Umpan balik | Bisa dimatikan di Pengaturan |

### 5.3 Mode kamera

| Mode | Cara kerja |
|---|---|
| ARCore | Kamera dan tracking dari ARCore. Hanya untuk HP yang mendukung ARCore |
| Sensor 360 | Kamera belakang ditampilkan sebagai latar, arah pandang dari giroskop. Bisa ditambah geser jari (pitch dibatasi ±60 derajat) |
| Drag 360 | Tanpa sensor: geser jari untuk melihat sekeliling |

SDA ditempatkan sesuai arah kompas sebenarnya. Kompas dikalibrasi saat mulai, lalu dikunci begitu SDA muncul, supaya objek tidak bergeser karena kompas goyang. Kalibrasi hanya dilakukan saat HP tidak terlalu menengadah atau menunduk (maks 60 derajat).

### 5.4 GPS

- Update setiap pindah 2 m, akurasi yang diminta 5 m.
- Filter lompatan: posisi yang berarti bergerak lebih cepat dari 50 m/detik ditahan sekali. Jika posisi berikutnya setuju, lompatan dianggap nyata.
- Akurasi lebih buruk dari 30 m dianggap sinyal lemah.
- Kompas dianggap hilang jika tidak ada pembacaan baru selama 2 detik. Kompas dihaluskan supaya tidak melompat.
- GPS dan kompas hanya aktif di layar radar (hemat baterai).

### 5.5 Penyimpanan

| Data | Tempat |
|---|---|
| Progres (koleksi, kuis yang sedang berjalan, SDA yang sudah ditangkap). Kuis yang terputus karena aplikasi ditutup direset saat dibuka lagi: unit dibayar di jawaban terakhir, jadi kuis yang belum selesai tidak memberi unit dan SDA-nya tetap ada di radar | File JSON `energo-demo-v1.json` di penyimpanan aplikasi, dengan cadangan `.bak` |
| Pengaturan (mode AR, volume, getaran, SDA latihan terpilih) | PlayerPrefs |
| Peringatan keselamatan sudah dibaca | PlayerPrefs |
| "Jangan ingatkan lagi" per SDA | PlayerPrefs |

- Simpan aman: tulis ke file sementara, validasi, lalu ganti file utama dan simpan cadangan.
- File rusak: pulihkan dari cadangan ("Save utama rusak. Cadangan berhasil dipulihkan."). Jika keduanya rusak, data tidak dihapus dan reset harus dikonfirmasi ("Save tidak dapat dibaca. Data lama tidak dihapus; periksa berkas sebelum reset.").
- Pesan error lain: "Gagal membuat save. Periksa ruang penyimpanan.", "Data progres tidak valid.", "Gagal menyimpan progres: ...", "Save rusak. Reset terkonfirmasi diperlukan sebelum menyimpan lagi.", "Gagal membuka analisis. Coba lagi."
- Progres disimpan saat aplikasi masuk background.
- Save lama otomatis ditambah Minyak dan Uranium dengan 0 unit. Emas dari prototipe lama diimpor sekali.
- Tidak ada akun, server, atau jaringan. Semua offline.

### 5.6 Audio

| Suara | Kapan |
|---|---|
| sfx_splash | Splash |
| music_lobby | Desa (berhenti saat masuk radar/tangkap) |
| sfx_button_click / sfx_button_back | Semua tombol / tombol kembali dan menutup sheet |
| sfx_matahari (loop) | Matahari, di-pan ke arah cahaya |
| sfx_ekstrak_matahari | Matahari, posisi diputar sesuai bar |
| sfx_bara_pukul / sfx_bara_hancur | Ketukan / bongkahan hancur |
| sfx_sungai (loop) | Sepanjang scene emas |
| sfx_aliran_air (loop) | Mendulang, volume mengikuti kecepatan putar |
| Klik Geiger (dibuat dari kode) | Uranium |
| Detak pompa (sfx_batu_jatuh pitch rendah, placeholder) | Pompa minyak |
| sfx_quiz_benar / sfx_quiz_salah | Jawaban kuis, juga saat gagal minyak/uranium |
| sfx_dulang_emas, sfx_batu_jatuh, sfx_pickaxe | Sudah dimuat, belum dipakai |

Volume utama diatur di Pengaturan dan disimpan.

### 5.7 Getaran

| Durasi | Kapan |
|---|---|
| 8-15 ms | Tick halus: matahari terkunci, mengebor, menjepit, Geiger, pompa biasa |
| 20-30 ms | Ketukan batu bara, jawaban benar, pompa tepat |
| 40-80 ms | HP datar, jepitan lepas, serpihan masuk, berhasil |
| 120 ms | Emas tumpah per keping, semua batu bara hancur |
| 200-300 ms | Jawaban salah, gagal mini-game |
| 60 ms | SDA masuk jarak tangkap di radar, getaran dinyalakan |

---

## 6. Sistem Visual Sekarang ("Arcade Cerah")

Bagian ini mencatat tampilan saat ini sebagai titik awal redesign. Boleh diubah, tetapi semua komponen di bawah harus punya padanan di desain baru.

### 6.1 Warna

| Token | Hex | Fungsi |
|---|---|---|
| Paper | #FFFAF0 | Muka kartu |
| Ink | #14213A | Garis tepi, outline teks |
| Muted | #3A4256 | Teks sekunder |
| Line | #E6DCC0 | Garis tipis di kertas |
| Orange | #FF6B2C | Aksi utama |
| Sun | #FFC93C | Hadiah, energi, label |
| Success | #45B83A | Benar, aman |
| Danger | #E8304E | Salah, bahaya |
| Berry | #FF3B5C | Badge jumlah |
| TitleOrange | #FF9A1F | "GO!", "ATAU" |
| Uranium glow | #39FF14 | Bijih dan serpihan uranium |
| Langit | #155D78, #1E8FA6, #36B8A8 | Latar dunia |
| Rumput | #7BD36A, #4FB35A | Latar dunia |

Jenis tombol (gradasi atas ke bawah): Orange (#FFC04D ke #FF6B2C), Ok (#C6F58A ke #45B83A), Bad (#FF9AA8 ke #E8304E), Info (#8FF3FF ke #1AA4FF), Sun (#FFF07A ke #FFBE1A), Paper (#FFFAF0 ke #FFE9C2), Off (#C9CCD6 ke #8D93A3), Dark (#34466B ke #22304D).

### 6.2 Font

- Lilita One: judul, label tombol, angka besar.
- Fredoka SemiBold dan Bold: teks isi.

### 6.3 Komponen

| Komponen | Dipakai di |
|---|---|
| Latar dunia (langit gradasi, bukit rumput, grid isometrik, pohon) | Splash, Radar, Kuis |
| Tombol tebal dengan bibir bawah dan state ditekan | Semua tombol |
| Kartu kertas dengan outline | Sheet, kartu radar, kuis, hasil, panduan |
| Pita judul (ribbon) | Atas kartu |
| Pil gelap transparan | Info radar, header layar tangkap |
| Tile SDA dengan badge jumlah dan state terkunci | Latihan, Panduan |
| Toggle | Pengaturan |
| Slider | Volume |
| Progress bar gradasi | Splash, HUD, timer kuis, dosis, tekanan |
| Kotak HUD gelap dengan aksen hijau/oranye | Layar tangkap |
| Pita kompas | Layar tangkap |
| Chip putih | Kuis dan hasil |
| Bintang terisi/kosong | Hasil |
| Teks dengan outline tebal | Judul, angka |

### 6.4 Ikon

arrow, bolt, book, check, clock, coal, compass, cross, dice, gear, gold, oil, star, star_empty, sun, target, tree, uranium.

### 6.5 Aset 3D

| Aset | Status |
|---|---|
| Emas (GoldModel + tekstur) | Ada |
| Batu bara 4 tahap pecah (bara_1 sampai bara_4) | Ada |
| Matahari (sun) | Ada, digambar tanpa cahaya (unlit) |
| Pompa angguk minyak | Placeholder dari kubus dan silinder |
| Bijih dan serpihan uranium | Placeholder dari bola hijau menyala |

Rencana: semua aset 3D dibuat ulang dalam satu tema yang sama.

---

## 7. Daftar Periksa Redesign

Semua layar dan state yang harus punya desain:

- [ ] Splash
- [x] Home Desa
- [ ] Sheet Pengaturan (toggle nyala/mati, slider, reset normal dan konfirmasi)
- [ ] Sheet Latihan (tile terpilih, tile Acak)
- [ ] Sheet Panduan (dengan konfirmasi reset panduan)
- [ ] Kartu Panduan mode main dan mode baca, 13 ilustrasi langkah
- [ ] Transisi ke radar dan ke kamera
- [ ] Radar: normal, kosong, SDA dalam jangkauan, di luar jangkauan, terkunci, uranium, kompas tidak ada, GPS lemah
- [ ] Radar: mencari GPS, izin ditolak, lokasi mati, GPS gagal
- [ ] Peringatan keselamatan
- [ ] HUD layar tangkap: header, kotak status (aman/peringatan), pita kompas (titik, sektor, penanda tepi, tanpa kompas)
- [ ] Matahari: lingkaran bidik (biasa/terkunci), petunjuk putar, gagal
- [ ] Batu bara: pip bongkahan, selesai
- [ ] Emas: dulang, waterpass, hitung mundur, MULAI!, mendulang, terlalu kencang, emas tumpah, gagal
- [ ] Minyak: penampang 6 lapisan, pengukur gas, pompa dan cincin irama, bar tekanan, blowout, kena air, overpressure
- [ ] Uranium: meter RAD, tren sinyal, lingkaran bidik, tombol JEPIT (diam/ditekan/progres), bar dosis, jepitan lepas, dosis berlebih
- [ ] Kuis: kartu soal, timer (biasa/terburu), geser kanan/kiri dengan cap, tombol BENAR/SALAH, penanda 3 soal
- [ ] Umpan balik: benar, salah, waktu habis, tombol terkunci "BACA DULU..."
- [ ] Hasil: Sempurna, Bagus, Coba Lagi, Murni/Normal, rincian bonus
- [ ] Pesan error simpan
- [x] Desa menyala dan padam, pembangkit dengan stok durasi, memasukkan SDA, peringatan stok, prolog
- [ ] (Direncanakan) Level
