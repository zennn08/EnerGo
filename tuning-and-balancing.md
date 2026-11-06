# **Tuning dan Balancing**

> Catatan 2026-11-06: rencana pembangkit, Gulden, dan distrik di dokumen ini digantikan oleh `tuning-and-balancing-ringkas.md` dan `docs/PRD-EnerGo.md` 3.13.


Bab ini menjelaskan proses tuning dan balancing EnerGo, mengikuti alur Markas → Penangkapan → Markas → My Village.

**Tuning** adalah proses menyesuaikan nilai parameter (angka, kurva, durasi) pada mekanik yang sudah ada sampai terasa dan berjalan sesuai tujuan desain. Mekaniknya tidak berubah, hanya nilainya yang disetel. **Balancing** mengatur hubungan antar-elemen (SDA, pembangkit, sumber daya, level) supaya tantangan terasa adil, beragam, dan tidak ada satu pilihan yang mendominasi.

Keduanya saling terkait: tuning menghasilkan angka, lalu balancing menguji angka itu terhadap angka-angka lain.

| | Tuning | Balancing |
|---|---|---|
| Fokus | Satu mekanik | Hubungan antar-elemen |
| Tujuan | Game feel dan kesulitan yang pas | Adil dan beragam |
| Contoh di EnerGo | Kecepatan gerak matahari di Catch and Hold 0,22 → 0,18 | Kurs uranium dibuat lebih tinggi dari matahari karena 10× lebih langka |

Cara membaca tabel di bab ini:

- **Sekarang**: nilai yang dipakai di build saat ini.
- **Rencana**: nilai baru atau arah perubahan yang akan diuji.
- **Pemicu**: hasil playtest yang membuat perubahan dijalankan. Kalau pemicunya tidak terjadi, nilai sekarang dipertahankan.

Semua nilai di kolom Rencana masih berupa hipotesis dan baru dipastikan lewat playtesting pada tahap Beta.

---

## **1. Praktik Tuning: Siklus Iterasi**

Penanggung jawab: Rehandra (Quality Research), sesuai tugas *balancing parameter gameplay* di bagian Tim Inti. Perubahan nilai di kode dikerjakan bersama programmer.

| Langkah | Penerapan di EnerGo |
|---|---|
| 1. Tetapkan target | Tentukan rasa yang dituju untuk setiap fitur. Contoh: "Catch and Hold selesai dalam 10–15 detik dan jarang gagal". Target lengkap ada di bagian 4 |
| 2. Ubah 1 parameter | Hanya satu nilai yang diubah per iterasi, supaya jelas perubahan mana yang berpengaruh |
| 3. Playtest | Dimainkan di perangkat Android asli (bukan editor), di area kampus |
| 4. Ukur & catat | Nilai sebelum dan sesudah dicatat di Log Iterasi (bagian 8) |
| 5. Ulangi | Sampai target di bagian 4 tercapai |

**Aturan emas:** setiap perubahan dicatat, dan pengujian dilakukan oleh pemain yang berbeda, bukan hanya tim sendiri. Rencananya minimal 3 pemain di luar tim dengan perangkat yang berbeda-beda, termasuk perangkat kelas bawah, karena mekanik EnerGo bergantung pada sensor (GPS, kompas, giroskop, akselerometer) yang kualitasnya berbeda di tiap perangkat. Masukan pengunjung dan dosen di Pameran Inno Electrica dipakai sebagai putaran playtest terakhir.

**Teknis penyimpanan parameter:**

| Aspek | Sekarang | Rencana |
|---|---|---|
| Lokasi parameter | Konstanta bernama di bagian atas setiap script C#. Rumus utama (skor, spawn, fisika bor, dosis uranium) dipisah ke script khusus dan punya unit test | Pindahkan parameter yang paling sering di-tune ke file data (ScriptableObject atau JSON), supaya bisa diubah tanpa menyentuh kode |
| Mengubah nilai saat bermain | Belum bisa, harus build ulang APK | Panel debug tersembunyi di Pengaturan untuk mengubah nilai langsung di perangkat saat playtest |

---

## **2. Tuning per Mekanik**

Pada bagian ini mekaniknya tidak diubah. Yang disetel hanya angka, jeda, dan durasinya.

### **2.1 Eksplorasi dan Radar GPS**

**Cara node SDA dimunculkan.** Peta dunia nyata dibagi menjadi petak-petak berukuran ±55 m × 55 m, seperti kotak-kotak kertas grafik yang dibentangkan di atas peta. Setiap 15 menit, setiap petak mengundi berapa node yang muncul di dalamnya: 60% peluang kosong, 30% peluang 1 node, dan 10% peluang 2 node. Kalau ada node, posisinya diacak di dalam petak dan jenis SDA-nya diundi berdasarkan bobot kelangkaan (bagian 3.2).

Dengan angka sekarang, rata-rata ada 0,5 node per petak, atau kira-kira satu node setiap dua petak (±110 m). Radar berjangkauan 150 m mencakup sekitar 23 petak, sehingga biasanya ±11 node terlihat sekaligus.

Undian ini dihitung dari koordinat petak dan waktu, bukan dari angka acak di perangkat. Akibatnya, semua pemain di lokasi yang sama melihat node yang sama di posisi yang sama. Node yang sudah ditangkap hanya hilang dari radar pemain yang menangkapnya. Pergantian siang dan malam hanya mengubah jenis SDA, tidak mengubah posisi node.

Peluang jumlah node per petak adalah parameter utama untuk mengatur kepadatan node. Kalau pemain terlalu lama berjalan sebelum menemukan node, parameter ini diubah lebih dulu, bukan ukuran petaknya.

| Parameter | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| Ukuran petak spawn | ±55 m × 55 m | Tetap | — |
| Peluang jumlah node per petak (±55 m) | 0 node 60%, 1 node 30%, 2 node 10% (rata-rata 0,5 node per petak, ±11 node di radar) | 0 node 50%, 1 node 35%, 2 node 15% (rata-rata 0,65 node per petak, ±15 node di radar) | Waktu jalan rata-rata sampai menemukan node > 5 menit |
| Pergantian node | Setiap 15 menit | Setiap 10 menit | Pemain merasa node di peta tidak pernah berubah dalam satu sesi |
| Jangkauan interaksi | 20 m | 25 m | Penangkapan gagal karena GPS meleset > 5% |
| Toleransi saat GPS buruk | Jangkauan bertambah maksimal 15 m | Tetap | — |
| Jangkauan radar | 150 m | 200 m | Radar sering kosong di area kampus |
| Peringatan GPS lemah | Akurasi > 30 m | Tetap | — |
| Petunjuk arah uranium | Sektor kasar ±45° | ±30° | Pemain tidak menemukan uranium |

### **2.2 Batu Bara — Find and Break**

Pemain mencari bongkahan batu bara di sekitarnya, lalu memecahkannya dengan pickaxe.

| Parameter | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| Jumlah bongkahan | 3–5 | Tetap | — |
| Ketukan per bongkahan | 2–5 | 2–4 | Durasi rata-rata > 30 detik |
| Sebaran bongkahan | ±75° di sekitar pemain | ±60° | Pemain tidak menyadari ada bongkahan di belakangnya |
| Waktu target | 0,45 dtk per ketukan + 1,5 dtk per bongkahan | 0,5 dtk + 1,8 dtk | Grade "Hebat" < 15% |

### **2.3 Emas — Panning for Gold**

Perangkat dipegang datar seperti dulang, lalu dimiringkan memutar (giroskop). Gerakan yang terlalu kasar membuat emas tumpah.

| Parameter | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| Lama mendulang | ±10 detik | Tetap | — |
| Kecepatan putar yang dihitung | 40–420 °/dtk | 30–480 °/dtk | Pemain sudah memutar tetapi tidak terdeteksi |
| Toleransi guncangan | 0,5 | 0,6 | Emas tumpah tanpa sebab di perangkat tertentu |
| Jumlah kepingan emas | 18 | Tetap | — |
| Gagal jika sisa emas | < 40% | < 30% | Tingkat gagal > 20% |
| Syarat posisi datar | ≤ 12° selama 1 detik | ≤ 15° | Hitung mundur sulit dimulai |

### **2.4 Matahari — Catch and Hold**

Pemain menjaga matahari yang bergerak tetap di dalam bidikan sampai bar terisi penuh.

| Parameter | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| Lama mengisi bar | ±8 detik | Tetap | — |
| Bar berkurang saat lepas | ½ kecepatan isi | ⅓ kecepatan isi | Tingkat gagal > 20% |
| Kecepatan gerak matahari | 0,22 | 0,18 | Pemain baru sering gagal di percobaan pertama |
| Ukuran bidikan | 56 | 64 | Gagal di perangkat berlayar kecil |
| Batas waktu | Acak 15–25 detik | Tetap | — |
| Syarat berhasil | Bar ≥ 50% saat waktu habis | Tetap | — |

### **2.5 Minyak Bumi — Bor & Pompa**

Tahap 1: bor ditahan untuk menembus lapisan tanah, batuan, dan gas tanpa ledakan tekanan (*blowout*), lalu dihentikan tepat di lapisan minyak. Tahap 2: pompa ditekan mengikuti irama (swipe berirama).

| Parameter | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| Kecepatan bor | 0,22 per detik | Tetap | — |
| Daya rem bor | 0,30 per detik | 0,40 per detik | Bor sering kebablasan ke lapisan air asin |
| Kenaikan tekanan gas | 9 | 7 | Blowout > 30% percobaan |
| Irama pompa | 1,2 detik per ketukan | Tetap | — |
| Toleransi ketukan "Perfect" | ±0,2 detik | ±0,25 detik | Rasio Perfect < 40% (layar sentuh perangkat murah lambat) |
| Waktu target | 25 detik | 30 detik | Grade "Hebat" < 15% |

### **2.6 Uranium — Geiger & Penjepit**

Pemain mencari bijih tersembunyi dengan bantuan bunyi Geiger dan getaran, lalu menjepit setiap fragmen dan memasukkannya ke kotak timbal sebelum dosis radiasi penuh.

| Parameter | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| Bunyi Geiger | 2 klik/dtk (jauh) → 20 klik/dtk (dekat) | 1 → 25 klik/dtk | Pemain tidak bisa membedakan jauh dan dekat |
| Jumlah fragmen | 3 | Tetap | — |
| Waktu menjepit | 1 detik | Tetap | — |
| Toleransi tangan goyang | 0,35 | 0,45 | Fragmen sering lepas tanpa disengaja |
| Kenaikan dosis radiasi | 1/40 per detik per fragmen | 1/50 per detik | Overdosis > 30% percobaan |
| Waktu target | 20 detik | Tetap | — |

### **2.7 Evaluasi (Kuis)**

| Parameter | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| Jumlah soal per penangkapan | 3 | Tetap | — |
| Waktu per soal | 15 detik (habis = salah) | 20 detik | Soal habis waktu > 10% |
| Format | Benar/salah lewat swipe | Tetap | — |
| Jumlah soal di bank | Matahari 33, Batu Bara 37, Emas 35, Minyak 30, Uranium 31 | ≥ 40 per SDA | Pemain melaporkan soal sering berulang |
| Komposisi jawaban | Belum dihitung | Jawaban "Benar" dan "Salah" seimbang, ±50:50 | Pemain bisa menang dengan selalu menjawab "Benar" |

---

## **3. Balancing**

Balancing EnerGo dibagi menjadi empat jenis: numerik, posisional, ekonomi, dan progresi.

### **3.1 Balancing Numerik: Antar-SDA**

Kelima SDA diseimbangkan dengan pola **biaya vs manfaat**: SDA yang lebih langka dan lebih sulit ditangkap harus memberi hasil yang lebih bernilai.

| SDA | Peluang muncul (siang) | Kesulitan mekanik | Target tingkat gagal | Kurs Gulden per unit (rencana) | Kelemahan |
|---|---|---|---|---|---|
| Batu Bara | 30% | Paling mudah | 5–15% | 8 | Nilai rendah, bahan bakar PLTU berpolusi |
| Emas | 20% | Mudah | 10–20% | 25 | Bukan bahan bakar, hanya untuk Gulden |
| Matahari | 35% | Sedang | 10–20% | 10 | Hanya muncul siang hari |
| Minyak Bumi | 12% | Sulit | 15–25% | 20 | Bisa gagal karena blowout, berpolusi |
| Uranium | 3% | Paling sulit | 20–30% | 60 | Paling langka, ada risiko overdosis |

**Masalah balancing yang ditemukan di build sekarang:** semua SDA memberi rentang hasil yang sama (1–6 unit), padahal uranium 10× lebih langka dan jauh lebih sulit daripada matahari. Menangkap uranium belum terasa istimewa. Penyelesaiannya ada di kurs Gulden (bagian 3.3), bukan di jumlah unit.

**Rumus hasil tangkapan sekarang:**

```
Hasil = 1 (dasar) + jumlah jawaban benar (0–3) + bonus mekanik AR (0–2)   →  1–6 unit SDA
```

| Skor mekanik AR | Grade | Bonus |
|---|---|---|
| ≥ 0,75 | Hebat | +2 |
| ≥ 0,40 | Bagus | +1 |
| < 0,40 | Cukup | +0 |

Jawaban yang salah tetap memberi hasil dasar dan penjelasan singkat. Kalau ketiga jawaban benar, SDA tercatat sebagai SDA **Murni**. Porsi hasil maksimum: kuis 50%, mekanik AR 33%, dasar 17%. Kuis sengaja diberi porsi terbesar karena tujuan utama EnerGo adalah edukasi energi.

| Aspek | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| Bonus mekanik AR maksimum | +2 | +3 | Pemain yang mahir mekanik AR merasa kurang dihargai |
| Ambang grade "Hebat" | 0,75 untuk semua SDA | Disetel per SDA supaya "Hebat" tercapai ±30% | Distribusi grade antar-SDA timpang |

### **3.2 Balancing Posisional: Lokasi dan Jarak**

Pada game berbasis lokasi, keunggulan posisi adalah lokasi pemain di dunia nyata. Balancing posisional menjaga supaya pemain di area mana pun dan di jam berapa pun tetap punya kesempatan yang adil.

| Aspek | Sekarang | Rencana | Pemicu |
|---|---|---|---|
| SDA di Level 1–3 (Lapisan Statis) | Semua SDA muncul sejak awal: Matahari 35%, Batu Bara 30%, Emas 20%, Minyak 12%, Uranium 3% | Hanya Emas 50% dan Batu Bara 50%, sesuai GDD | Diterapkan setelah sistem level dibangun |
| SDA di Level 4 ke atas | — | Bobot sekarang dipakai lagi: 35 / 30 / 20 / 12 / 3 | Uranium tidak pernah ditemui dalam 3 sesi, maka bobot uranium dinaikkan |
| Siang vs malam | Matahari tidak muncul pukul 18.00–06.00. Peluangnya dibagi ke SDA lain: Batu Bara 46%, Emas 31%, Minyak 18%, Uranium 4,6% | Tetap. Laporan Singkat memberi info "berburu malam: uranium lebih sering muncul" | Pemain yang hanya bisa bermain malam merasa tertinggal |
| Kepadatan node | Sama di semua tempat | Tetap sama di semua tempat | Pemain di area padat gedung kesulitan karena GPS melompat. Kalau terjadi, jangkauan interaksi dinaikkan |
| Node bantuan saat krisis (Lapisan Makro) | Belum ada | 1 node berdaya tinggi muncul 30–60 m dari pemain saat cadangan listrik Desa Teknik < 20%, maksimal 1 kali per 2 jam | Pemain sengaja membiarkan krisis demi uranium gratis, maka batas waktunya diperpanjang |

Catatan teknis: urutan daftar SDA di kode tidak boleh diubah karena akan menggeser semua node di dunia. Yang boleh diubah hanya angka bobotnya.

### **3.3 Balancing Ekonomi: Gulden dan Pembangkit**

**Sekarang:** sistem Gulden, Markas, dan pembangkit belum diimplementasikan. Hasil tangkapan baru tersimpan di inventori sebagai stok biasa dan Murni. Tabel konversi SDA ke pembangkit di GDD juga masih kosong.

**Pemasukan (rencana):**

| Parameter | Rencana | Alasan |
|---|---|---|
| Kurs SDA → Gulden di Markas | Batu Bara 8, Matahari 10, Minyak Bumi 20, Emas 25, Uranium 60 per unit | Mengikuti kelangkaan dan kesulitan (bagian 3.1) |
| Bonus SDA Murni | ×1,5 Gulden | Memberi alasan untuk menjawab kuis dengan serius |
| Rewarded video | Gandakan Gulden dari Laporan Singkat, maksimal 1 kali per hari | Sesuai GDD |
| Batas bonus iklan | Gulden dari iklan ≤ 20% dari total Gulden harian rata-rata | Pemain yang tidak menonton iklan tidak tertinggal jauh |

**Pembangkit (usulan tabel konversi, 3 jenis sesuai target Beta).** Tidak ada pembangkit yang paling unggul di semua aspek. Setiap pembangkit punya kelebihan yang dibayar dengan kekurangan:

| Pembangkit | Bahan bakar | Energi per unit SDA | Kelebihan | Kekurangan |
|---|---|---|---|---|
| PLTS (Surya) | Matahari | 1 | Bersih, tanpa polusi, SDA paling sering muncul | Hanya menghasilkan listrik siang hari |
| PLTU (Uap) | Batu Bara / Minyak Bumi | 2 / 3 | Output besar, menyala siang dan malam | Ada polusi, cadangan energi konvensional kota terus menurun |
| PLTN (Nuklir) | Uranium | 10 | Output sangat besar, tanpa polusi udara | SDA paling langka, biaya bangun paling mahal |

Emas tidak menjadi bahan bakar dan hanya ditukar menjadi Gulden.

Sesuai dilema di bagian Cerita, energi konvensional harus terasa **cepat tapi berisiko dalam jangka panjang**, sedangkan energi berkelanjutan terasa **butuh usaha lebih besar di awal tapi lebih stabil**. Pesan ini disampaikan lewat angka di Laporan Singkat, bukan lewat ceramah.

| Parameter | Rencana | Pemicu penyesuaian |
|---|---|---|
| Kebutuhan listrik Desa Teknik | Naik bertahap mengikuti level pemain | Desa selalu terang tanpa perlu eksplorasi baru, berarti kebutuhan terlalu rendah |
| Konsumsi bahan bakar | Stok penuh dari satu sesi cukup untuk ±1 hari | Pemain absen sehari dan desa langsung padam total, berarti konsumsi terlalu cepat |
| Simulasi offline | Dihitung maksimal 8 jam | Pemain merasa dihukum karena tidak membuka aplikasi |

### **3.4 Balancing Progresi: XP, Level, dan Alat Penangkap**

**Sekarang:** belum diimplementasikan.

| Parameter | Rencana | Alasan |
|---|---|---|
| XP per penangkapan | 10 XP + 5 XP per jawaban benar, dikali tingkat SDA (dasar ×1, berdaya tinggi ×2) | |
| Kurva level | XP ke level berikutnya = 100 × level | Naik level setiap 1–2 sesi di awal, melambat di level tinggi |
| Biaya upgrade Alat Penangkap | Naik ±1,5× setiap tingkat | Upgrade pertama bisa dibeli di sesi pertama |
| Efek upgrade Alat Penangkap | Jangkauan radar +25 m per tingkat, peluang node langka naik | Sesuai GDD: alat yang lebih baik membuat pemain menjelajah lebih jauh dan menemukan SDA langka |
| Batas efek upgrade | Bonus yang menyentuh mekanik AR (misalnya bidikan lebih besar) maksimal +10% | Alat Penangkap tidak boleh membuat mekanik AR otomatis berhasil |

**Dynamic Difficulty Adjustment (Lapisan Mikro).** DDA hanya boleh menggeser parameter mekanik AR **di dalam batas** yang sudah ditentukan, supaya game tidak menjadi terlalu mudah atau mustahil.

| Masukan DDA | Diukur dari |
|---|---|
| Kestabilan gestur | Guncangan akselerometer selama mekanik AR |
| Kecepatan respons | Waktu selesai dibanding waktu target |
| Akurasi kuis | Rasio jawaban benar dari 5 penangkapan terakhir |

| Parameter yang disetel DDA | Batas bawah (lebih mudah) | Nilai normal | Batas atas (lebih sulit) |
|---|---|---|---|
| Kecepatan gerak matahari | 0,15 | 0,22 | 0,30 |
| Ukuran bidikan | 70 | 56 | 46 |
| Toleransi guncangan emas | 0,65 | 0,5 | 0,4 |
| Toleransi ketukan "Perfect" minyak | ±0,3 dtk | ±0,2 dtk | ±0,15 dtk |
| Kenaikan dosis uranium | 1/55 | 1/40 | 1/30 |

| Aturan DDA | Rencana |
|---|---|
| Turunkan kesulitan | Setelah 2 kali gagal berturut-turut pada SDA yang sama |
| Naikkan kesulitan | Setelah 3 kali berturut-turut mendapat grade "Hebat" |
| Besar perubahan | 1 langkah (±20% dari jarak normal ke batas) per penangkapan |
| Transparansi | Pemain tidak diberi tahu, tetapi hasil tangkapan tidak dikurangi saat kesulitan diturunkan |

---

## **4. Praktik Balancing: Ukur dengan Data**

EnerGo bukan game kompetitif, jadi ukuran seperti win rate, pick rate, dan time-to-kill diterjemahkan ke padanannya:

| Ukuran umum | Padanan di EnerGo | Target |
|---|---|---|
| Win rate sehat | Tingkat berhasil menangkap per SDA | Sesuai target tingkat gagal di bagian 3.1 |
| Pick rate | Persentase pemain memilih tiap jenis pembangkit | Tidak ada pembangkit yang dipilih > 60% atau < 10% |
| Time-to-kill | Durasi satu mekanik AR | 10–30 detik |

**Metrik lengkap untuk playtesting.** Sekarang metrik belum dicatat otomatis, jadi pencatatan dilakukan manual di spreadsheet.

| Metrik | Target | Parameter yang disetel jika meleset |
|---|---|---|
| Waktu jalan sampai menemukan node | 2–5 menit | Peluang jumlah node per petak, jangkauan radar |
| Durasi mekanik AR | 10–30 detik | Lama isi bar, jumlah objek, waktu target |
| Tingkat gagal per SDA | Sesuai tabel di bagian 3.1 | Parameter kesulitan setiap mekanik |
| Distribusi grade | ±30% Cukup, 40% Bagus, 30% Hebat | Ambang grade, waktu target |
| Jawaban kuis benar | 60–80% | Tingkat kesulitan soal, waktu per soal |
| Penangkapan Murni | 30–50% | Jumlah soal per penangkapan |
| Gagal karena GPS/kompas | < 5% | Jangkauan interaksi, toleransi GPS |
| Sesi sampai upgrade pertama | 1 sesi | Kurs Gulden, biaya upgrade |
| Desa padam setelah absen 1 hari | Sebagian distrik, bukan seluruh desa | Konsumsi bahan bakar, batas simulasi offline |
| Pick rate pembangkit | 10–60% per jenis | Energi per unit, biaya bangun, polusi |

---

## **5. AI untuk Tuning dan Balancing**

| Langkah | Penerapan di EnerGo |
|---|---|
| Simulasi bot | Script simulasi yang menjalankan ribuan penangkapan dan hari simulasi kota dengan berbagai gaya bermain (rajin, jarang main, hanya malam), tanpa perlu berjalan di lapangan |
| Kumpulkan data | Gulden per hari, kondisi Desa Teknik, pembangkit yang paling menguntungkan |
| Deteksi anomali | Contoh: strategi "hanya PLTU" ternyata selalu paling untung, atau krisis bisa disengaja untuk mendapat node uranium |
| Putusan manusia | Tim tetap menyetel angka akhir berdasarkan hasil simulasi dan playtest |

**Penggunaan AI di proyek ini:**
- **Kode:** asisten coding AI dipakai untuk membantu menulis, men-debug, dan mendokumentasikan kode, termasuk mengumpulkan parameter tuning untuk bab ini. Setiap perubahan tetap diperiksa dan diuji oleh tim.
- **Level Design:** Lapisan Makro (Decision Tree) dan Lapisan Mikro (DDA probabilistik) di bab Game Level Desain adalah penerapan AI di dalam game untuk menjaga kesulitan dan pasokan SDA.

**Batasan:** simulasi bisa menemukan angka yang *seimbang*, tetapi tidak bisa menilai apakah game itu *menyenangkan*. Karena itu playtest dengan pemain nyata tetap wajib, dan simulasi dipakai hanya untuk menyaring angka yang jelas tidak seimbang sebelum playtest.

---

## **6. Legal dan Etika**

### **6.1 Aspek Legal**

| Aspek | Kondisi EnerGo | Tindakan |
|---|---|---|
| Privasi data (UU PDP No. 27/2022) | Lokasi GPS hanya diproses di perangkat. Save data disimpan lokal di perangkat. Tidak ada server dan tidak ada telemetri yang dikirim | Tampilkan penjelasan izin lokasi dan kamera saat pertama kali dibuka. Kalau nanti ada telemetri untuk balancing, minta izin dan jelaskan tujuannya |
| Lisensi aset audio | Efek suara diambil dari MyInstants dan musik Markas dari YouTube | Cek lisensi setiap file. Kalau tidak jelas, ganti dengan aset berlisensi bebas (misalnya CC0) sebelum dirilis atau dipamerkan |
| Lisensi alat | Unity, AR Foundation, dan ARCore | Gunakan sesuai syarat lisensi versi gratis/edukasi |
| Hak cipta output AI | Sebagian kode dibuat dengan bantuan AI | Peran manusia (desain, review, pengujian) didokumentasikan |
| Aturan platform | Target rilis Google Play | Ikuti kebijakan iklan, izin lokasi, dan pemain anak di Google Play |
| Keselamatan pemain | Game mengajak pemain berjalan di dunia nyata | Peringatan keselamatan di radar sudah ada. Node tidak boleh ditempatkan di area berbahaya |

*Catatan umum, bukan nasihat hukum.*

### **6.2 Etika Tuning dan Monetisasi**

| Prinsip | Penerapan di EnerGo |
|---|---|
| Hindari dark pattern pemicu kecanduan | Tidak ada hukuman berat karena tidak bermain: simulasi offline dibatasi 8 jam dan desa hanya padam sebagian. Tidak ada login streak yang hilang |
| Umumkan peluang drop | Tidak ada loot box. Peluang kemunculan SDA bisa ditampilkan di Energipedia |
| Lindungi pemain anak | Iklan hanya banner di Markas dan rewarded video opsional. Tidak ada pembelian dalam aplikasi |
| Jangan merusak balance lewat pay-to-win | Iklan hanya menggandakan Gulden dari Laporan Singkat, 1 kali per hari, dan dibatasi ≤ 20% Gulden harian. Iklan tidak pernah memengaruhi mekanik AR, kuis, atau kemunculan SDA |
| Transparan soal penggunaan AI | Penggunaan AI dicantumkan di GDD (bagian 5) dan di halaman kredit |
| Konten edukasi akurat | Setiap soal kuis punya sumber tepercaya dan sudah ditinjau oleh Quality Research |

---

## **7. Checklist Praktik Terbaik**

| Praktik | Status sekarang | Rencana |
|---|---|---|
| Target dulu | Target ditulis di bagian 4 | Disepakati tim sebelum Beta |
| Data, bukan kode | Parameter masih berupa konstanta di script | Pindah ke ScriptableObject/JSON + panel debug |
| Playtest nyata | Baru uji internal tim | Minimal 3 pemain luar tim + Pameran Inno Electrica |
| Changelog | Log Iterasi (bagian 8) sudah disiapkan | Diisi setiap perubahan angka |
| AI sebagai asisten | AI membantu kode dan dokumentasi | Keputusan akhir tetap di tim |
| Legal & etika | Lisensi audio belum dicek | Cek lisensi audio, cantumkan penggunaan AI di kredit |

---

## **8. Log Iterasi**

Setiap perubahan dari kolom Rencana yang benar-benar dijalankan dicatat di sini.

| Tanggal | Parameter | Sebelum | Masalah saat playtest | Sesudah | Hasil |
|---|---|---|---|---|---|
| Okt 2026 | Mekanik Uranium (v1 → v3) | *(isi)* | *(isi)* | *(isi)* | *(isi)* |
| | | | | | |
| | | | | | |

---

## **9. Bagian yang Paling Perlu Di-tuning Lebih Dulu**

1. **Kepadatan node (peluang jumlah node per petak).** Ini menentukan apakah pemain menemukan sesuatu dalam beberapa menit pertama. Kalau pemain berjalan lama tanpa menemukan node, mereka berhenti sebelum sempat mencoba mekanik AR.
2. **Mekanik yang bergantung pada sensor (Panning for Gold dan Geiger & Penjepit).** Toleransi guncangan sangat dipengaruhi kualitas sensor perangkat, jadi harus diuji di beberapa perangkat sebelum angka lain disetel.
3. **Kurs Gulden per SDA.** Setelah sistem ekonomi dibangun, kurs ini yang menyelesaikan masalah utama balancing saat ini: uranium yang langka belum terasa bernilai.
