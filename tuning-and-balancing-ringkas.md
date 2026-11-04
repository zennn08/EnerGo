# Tuning dan Balancing (Versi Ringkas)

> Draft untuk dipilih. Setiap bagian diberi kode [A]-[H]. Tandai bagian mana yang dimasukkan ke Word.
> Perkiraan panjang di Word: semua bagian ±4-5 halaman. Tanpa [G] dan [H] ±3 halaman.

---

## [A] Pengertian

"Tuning" adalah menyetel nilai parameter (angka, durasi, toleransi) pada mekanik yang sudah ada sampai terasa pas. Mekaniknya tidak berubah, hanya angkanya. "Balancing" adalah menyeimbangkan hubungan antar-elemen (SDA, pembangkit, hadiah) supaya permainan adil dan tidak ada satu pilihan yang selalu paling unggul.

| | Tuning | Balancing |
|---|---|---|
| Fokus | Satu mekanik | Hubungan antar-elemen |
| Tujuan | Rasa bermain dan kesulitan yang pas | Adil dan beragam |
| Contoh di EnerGo | Kecepatan gerak matahari 0,22 → 0,18 | Durasi per unit uranium dibuat jauh lebih lama dari matahari karena 10× lebih langka |

---

## [B] Proses Tuning

Tuning dilakukan berulang dengan siklus berikut, dipimpin oleh Quality Research bersama programmer:

1. Tetapkan target. Contoh: "satu penangkapan selesai dalam 10-30 detik".
2. Ubah satu parameter saja, supaya jelas perubahan mana yang berpengaruh.
3. Playtest di HP Android asli dengan minimal 3 pemain di luar tim, karena sensor tiap HP berbeda.
4. Ukur dan catat nilai sebelum dan sesudah di Log Iterasi.
5. Ulangi sampai target tercapai.

---

## [C] Parameter yang Di-tuning

Setiap fitur di EnerGo dikendalikan oleh beberapa angka, misalnya jarak, durasi, dan toleransi gerakan. Tabel berikut hanya memuat parameter yang paling berpengaruh pada pengalaman bermain. Kolom "Sekarang" adalah nilai di build saat ini, "Rencana" adalah nilai yang akan dicoba, dan "Pemicu" adalah kondisi saat playtest yang membuat perubahan itu dijalankan. Kalau pemicunya tidak terjadi, nilai sekarang dipertahankan.

| Fitur | Parameter | Sekarang | Rencana | Pemicu |
|---|---|---|---|---|
| Radar GPS | Peluang jumlah node per petak (±55 m) | Kosong 60%, 1 node 30%, 2 node 10% | Kosong 50%, 1 node 35%, 2 node 15% | Pemain jalan > 5 menit tanpa menemukan node |
| Radar GPS | Jangkauan interaksi | 20 m | 25 m | Gagal menangkap karena GPS meleset > 5% |
| Batu Bara (Find and Break) | Ketukan per bongkahan | 2-5 | 2-4 | Durasi > 30 detik |
| Emas (Panning for Gold) | Toleransi guncangan | 0,5 | 0,6 | Emas tumpah tanpa sebab di HP tertentu |
| Matahari (Catch and Hold) | Kecepatan gerak matahari | 0,22 | 0,18 | Pemain baru sering gagal |
| Minyak Bumi (Bor & Pompa) | Toleransi ketukan "Perfect" | ±0,2 dtk | ±0,25 dtk | Ketukan Perfect < 40% |
| Uranium (Geiger & Penjepit) | Kecepatan dosis radiasi | Penuh dalam 40 dtk | Penuh dalam 22 dtk | Dinilai tim terlalu mudah, overdosis hampir tidak pernah terjadi |
| Uranium (Geiger & Penjepit) | Dosis saat mencari bijih | Belum berjalan | Berjalan pelan, 1/4 kecepatan normal | Fase mencari tidak punya tekanan sama sekali |

### Radar GPS

Peta dunia nyata dibagi menjadi petak-petak berukuran ±55 m. Setiap 15 menit, setiap petak mengundi berapa node SDA yang muncul di dalamnya. Dengan angka sekarang, sekitar 11 node terlihat di radar berjangkauan 150 m. Peluang ini adalah parameter terpenting di seluruh game: kalau pemain terlalu lama berjalan tanpa menemukan apa pun, mereka akan berhenti bermain sebelum sempat mencoba mekanik AR. Jangkauan interaksi juga perlu disetel karena GPS di HP sering meleset beberapa meter, apalagi di sekitar gedung.

### Mekanik AR

Setiap SDA punya satu parameter yang paling menentukan tingkat kesulitannya. Pada Emas dan Uranium, toleransi gerakan sangat bergantung pada kualitas sensor HP. Akibatnya, angka yang terasa pas di satu HP bisa terasa terlalu sulit di HP lain, sehingga keduanya wajib diuji di beberapa perangkat. Pada Matahari dan Minyak Bumi, yang disetel adalah kecepatan dan ketepatan waktu, supaya pemain baru tetap bisa berhasil tanpa membuat pemain mahir bosan.

### Uranium

Uranium dinilai tim masih terlalu mudah untuk SDA yang paling langka. Fase mencari bijih tidak punya tekanan sama sekali, dan dosis radiasi baru penuh setelah 40 detik, padahal pemain biasanya sudah mengumpulkan ketiga serpihan dalam waktu sekitar 12 detik dengan dosis yang baru terisi 20-25%. Akibatnya, gagal karena overdosis hampir tidak pernah terjadi.

Karena itu ada dua perubahan yang direncanakan. Pertama, dosis dipercepat sehingga penuh dalam 22 detik. Pemain yang mengambil serpihan dengan tempo normal akan selesai dengan dosis sekitar setengah, sedangkan pemain yang ragu-ragu akan overdosis. Kedua, dosis mulai berjalan pelan sejak fase mencari, sehingga pemain yang lama mencari bijih punya sisa waktu lebih sedikit untuk mengambil serpihan. Perubahan kedua ini sekaligus mengajarkan prinsip keselamatan radiasi "waktu": makin lama berada di dekat sumber radiasi, makin besar dosis yang diterima. Targetnya, tingkat gagal uranium berada di kisaran 20-30%.

### Kuis

Waktu menjawab 15 detik per soal sudah sesuai berdasarkan uji coba tim, sehingga tidak termasuk parameter yang diubah.

---

## [D] Balancing Antar-SDA

Semua SDA hasil tangkapan dipakai sebagai bahan bakar untuk satu pembangkit listrik di Desa Teknik. Setiap unit SDA menambah "durasi menyala" pembangkit. Kelima SDA diseimbangkan dengan pola "biaya vs manfaat". "Biaya" bagi pemain adalah usaha untuk mendapatkan SDA, yaitu seberapa jauh harus berjalan (kelangkaan) dan seberapa sulit mekanik AR-nya. "Manfaat" adalah berapa lama pembangkit menyala dari satu unit SDA tersebut.

| SDA | Peluang muncul | Kesulitan | Durasi per unit (rencana) | Sumbangan rata-rata |
|---|---|---|---|---|
| Batu Bara | 30% | Mudah | 20 menit | 6 menit |
| Emas | 20% | Mudah | 30 menit | 6 menit |
| Matahari | 35% (siang saja) | Sedang | 20 menit | 7 menit |
| Minyak Bumi | 12% | Sulit | 45 menit | 5,4 menit |
| Uranium | 3% | Paling sulit | 3 jam | 5,4 menit |

Keterangan: sumbangan rata-rata = peluang muncul × durasi per unit, yaitu kontribusi setiap SDA terhadap pembangkit dalam jangka panjang.

Durasi per unit disusun supaya sumbangan rata-rata kelima SDA hampir sama (5-7 menit). Artinya, dalam jangka panjang tidak ada SDA yang menjadi satu-satunya andalan, dan setiap node yang muncul di radar layak didatangi. Batu bara dan matahari sering muncul, jadi satu unitnya cukup menyalakan pembangkit selama 20 menit. Uranium sangat jarang muncul dan mekaniknya paling sulit (mencari bijih lewat bunyi Geiger sambil mengejar dosis radiasi), jadi satu unitnya menyalakan pembangkit selama 3 jam. Hasilnya, menemukan uranium terasa istimewa tanpa membuat SDA lain jadi tidak berguna.

### Masalah yang ditemukan

Di build sekarang semua SDA memberi hasil yang sama (1-6 unit), sehingga uranium yang paling langka tidak terasa istimewa. Pemain yang beruntung menemukan uranium dan berhasil melewati mekanik tersulit hanya mendapat hasil yang sama seperti menangkap batu bara. Masalah ini diselesaikan lewat durasi per unit di atas, tanpa mengubah rumus hasil tangkapan.

### Rumus hasil tangkapan

1 (dasar) + jawaban benar (0-3) + bonus mekanik AR (0-2). Dari hasil maksimum 6 unit, kuis menyumbang porsi terbesar (50%), mekanik AR 33%, dan dasar 17%. Kuis sengaja diberi porsi terbesar karena tujuan utama EnerGo adalah edukasi energi. Pemain yang menjawab ketiga soal dengan benar (4 unit) mendapat lebih banyak dibanding pemain yang hanya jago mekanik AR tetapi salah semua di kuis (3 unit). Jawaban yang salah tetap memberi hasil dasar, supaya pemain tidak merasa dihukum karena belum tahu.

Durasi yang masuk ke pembangkit = jumlah unit × durasi per unit. Contoh: menangkap uranium dengan hasil 4 unit menambah 4 × 3 jam = 12 jam, sedangkan batu bara dengan hasil 4 unit menambah 4 × 20 menit = 1 jam 20 menit.

### Level awal

Sesuai GDD, Level 1-3 hanya memunculkan emas dan batu bara. Keduanya punya mekanik paling sederhana, sehingga pemain baru bisa memahami alur tangkap → kuis → hasil lebih dulu sebelum bertemu SDA yang lebih rumit.

---

## [E] Balancing Pembangkit Listrik

Desa Teknik hanya punya satu pembangkit listrik. Pembangkit menyimpan stok durasi menyala yang diisi dari SDA hasil tangkapan. Stok ini berkurang mengikuti waktu nyata, termasuk saat aplikasi ditutup. Selama stok masih ada, Desa Teknik terang. Begitu stok habis, desa padam sampai pemain menangkap SDA lagi.

| Parameter | Rencana | Alasan |
|---|---|---|
| Kapasitas maksimal stok | 24 jam | Mencegah pemain menimbun stok berhari-hari, sehingga tetap ada alasan untuk bermain setiap hari |
| Durasi dari satu sesi bermain (±5 penangkapan) | ±8-10 jam | Satu sampai dua sesi sehari cukup untuk menjaga desa tetap terang |
| Peringatan stok menipis | Sisa < 3 jam | Ditampilkan di Laporan Singkat, supaya pemain sempat keluar mencari SDA sebelum desa padam |

### Perhitungannya

Rata-rata satu unit SDA bernilai ±30 menit (gabungan sumbangan rata-rata di bagian sebelumnya), dan satu penangkapan rata-rata menghasilkan 3-4 unit. Jadi satu penangkapan menambah sekitar 1,5-2 jam, dan satu sesi berisi ±5 penangkapan menyalakan desa selama ±8-10 jam. Pemain yang bermain sekali sehari akan melihat desanya padam sekitar setengah hari, sedangkan pemain yang bermain dua kali sehari bisa menjaga desa terang hampir sepanjang hari.

Angka-angka ini menjadi kenop utama balancing pembangkit:
- Kalau desa terlalu cepat padam walaupun pemain sudah rajin bermain, durasi per unit dinaikkan.
- Kalau stok selalu penuh sehingga pemain tidak merasa perlu keluar mencari SDA, durasi per unit diturunkan atau kapasitas maksimal dikurangi.
- Kapasitas maksimal juga menjaga pemain yang absen beberapa hari tidak dihukum terlalu berat. Desa hanya padam sampai pemain kembali, tanpa kerugian lain.

Saat playtest, keseimbangan ini diuji dengan mencatat berapa jam desa padam per hari. Targetnya, pemain yang bermain dua kali sehari mengalami desa padam kurang dari 6 jam per hari.
---

## [F] Ukur dengan Data

| Metrik | Target |
|---|---|
| Waktu jalan sampai menemukan node | 2-5 menit |
| Durasi satu mekanik AR | 10-30 detik |
| Tingkat gagal per SDA | 5-15% (mudah) sampai 20-30% (uranium) |
| Jawaban kuis benar | 60-80% |
| Desa padam per hari (pemain yang bermain dua kali sehari) | < 6 jam |

---

## [G] AI, Legal, dan Etika

- AI: Lapisan Makro dan DDA di bab Level Desain membantu menyesuaikan pasokan SDA dan kesulitan secara otomatis, tetapi angka akhirnya tetap diputuskan tim lewat playtest. Asisten coding AI dipakai untuk membantu pengembangan, dan setiap hasilnya diperiksa tim.
- Privasi: lokasi GPS dan save data hanya diproses di HP pemain. Tidak ada data yang dikirim ke server.
- Lisensi: efek suara dari MyInstants dan musik dari YouTube perlu dicek lisensinya, atau diganti dengan aset bebas lisensi.
- Etika monetisasi: iklan tidak pernah memengaruhi mekanik AR, kuis, kemunculan SDA, atau durasi pembangkit (bukan pay-to-win). Tidak ada loot box.

---

## [H] Log Iterasi

| Tanggal | Parameter | Sebelum | Masalah | Sesudah | Hasil |
|---|---|---|---|---|---|
| | | | | | |
| | | | | | |
