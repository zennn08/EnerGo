# **EnergiGo**

### Jelajah Sumber Daya Alam, Nyalakan Kotamu

**Tim Pengembang**

- Akhlaqul Muhammad Fadwa
- Muhammad Abidillah
- M. Sohibbal
- Rehandra

---

## Daftar Isi

1. Ringkasan
2. Cerita dan Gameplay
3. Mekanik Gameplay Inti (Detail)
4. Gaya Grafis
5. Aset yang Dibutuhkan
6. Jadwal dan Milestone
7. Rencana Promosi
8. Lampiran: Fitur di Luar Cakupan

---

# **1. Ringkasan**

## **Elevator Pitch**

EnergiGo adalah game AR berbasis lokasi. Pemain berjalan di dunia nyata untuk menangkap sumber daya alam Indonesia, lalu memakainya untuk menyalakan kembali kota yang padam, sambil memilih antara energi bersih yang menantang atau energi fosil yang praktis.

## **Deskripsi Proyek**

EnergiGo adalah game mobile single player yang memakai GPS dan Augmented Reality (AR). Pemain berperan sebagai Penjaga Energi yang direkrut Dewan Energi untuk memulihkan sebuah kota yang kehilangan pasokan listrik. Semua proses game berjalan di perangkat pemain, tanpa server.

Di layar peta, node Sumber Daya Alam (SDA) muncul secara acak di sekitar posisi pemain. SDA terbagi dua kelas. SDA Energi (Matahari, Angin, Air, Geotermal, Batu Bara, Minyak Bumi, Gas Alam, Uranium) menjadi bahan pembangkit listrik. SDA Mineral (Silika, Tembaga, Nikel, Emas) menjadi bahan bangunan dan bahan upgrade alat. Setiap node punya tingkat Bintang ★1 sampai ★5 yang menentukan kesulitan dan hasilnya.

Saat pemain mendekati node, kamera AR terbuka dan objek 3D SDA muncul di lingkungan nyata. Setiap jenis SDA punya mini-game tangkap sendiri: Matahari ditangkap dengan mengarahkan HP ke langit, Angin dengan swipe searah putaran, Uranium dengan menahan HP tetap stabil. Setelah tangkapan berhasil, pemain menjalani Analisis, yaitu kuis benar/salah dengan swipe. Jawaban benar memberi kualitas tangkapan Murni dengan bonus hasil. Jawaban salah tetap memberi hasil normal dan menampilkan penjelasan singkat. Setiap fakta yang muncul masuk ke Energipedia.

Hasil tangkapan dibawa ke Markas. Di sana pemain menjual SDA untuk mendapat Gulden, meng-upgrade lima modul Alat Penangkap, dan membangun pembangkit di kota. Kota punya kebutuhan listrik yang berubah sepanjang hari, dengan puncak di malam hari. PLTS mati saat gelap, PLTB naik-turun, PLTU stabil tapi berpolusi. Pemain harus menyusun kombinasi pembangkit dan baterai agar distrik tetap menyala dan langit kota tetap bersih.

Kota terus berjalan saat game ditutup. Ketika pemain membuka game, Laporan Semalam menunjukkan distrik mana yang padam, berapa Gulden yang terkumpul, dan bagaimana kondisi langit. Pelajaran energi paling nyata muncul di layar ini: pemain melihat sendiri kotanya gelap karena hanya mengandalkan tenaga surya.

Cerita berjalan per distrik, dari Distrik Rumah sampai Pusat Kota. Di awal setiap distrik, Pak Darto, kontraktor PLTU, datang dengan tawaran yang menggiurkan, sementara Bu Sari dari Dewan Energi mendorong energi bersih. Pilihan pemain sepanjang game menentukan kondisi langit kota dan ending yang ia dapat.

## **Tema / Setting / Genre**

- **Tema:** Eksplorasi dan pengelolaan energi, dengan edukasi tentang SDA dan energi Indonesia.
- **Setting:** Dunia nyata pemain (kampus, rumah, lingkungan sekitar) yang dipadukan dengan kota fiksi melalui AR dan layar Markas. Latar waktunya masa depan dekat, saat krisis energi membuat kota-kota padam.
- **Genre:** Location-based AR, koleksi, dan simulasi pengelolaan energi ringan.
- **Mode:** Single player, offline (tanpa server game).

## **Mekanik Gameplay Inti (Ringkas)**

1. **Eksplorasi berbasis lokasi:** node SDA muncul acak di sekitar posisi GPS pemain.
2. **Tangkap AR:** mini-game berbeda untuk setiap jenis SDA, dengan kesulitan sesuai Bintang node.
3. **Analisis:** kuis swipe benar/salah yang menentukan kualitas tangkapan dan mengisi Energipedia.
4. **Alat Penangkap modular:** lima modul yang di-upgrade memakai Gulden dan mineral untuk membuka node yang lebih sulit.
5. **Kota dan jaringan listrik:** membangun pembangkit, menyeimbangkan kebutuhan siang dan malam, menjaga langit kota tetap bersih.

## **Platform yang Dituju**

- **Android (utama):** mudah diuji tim, basis pengguna luas di Indonesia, mendukung ARCore.
- **iOS (opsional):** memungkinkan karena memakai AR Foundation (ARKit), tetapi bukan prioritas karena butuh perangkat Mac dan akun Apple Developer.
- **Engine:** Unity dengan AR Foundation.
- **Syarat perangkat:** HP Android yang mendukung ARCore, dengan GPS dan gyroscope.

## **Model Monetisasi**

EnergiGo gratis dimainkan dengan iklan opsional (AdMob, butuh koneksi internet tetapi tidak butuh server milik tim).

- **Rewarded video:** pemain menonton iklan untuk menggandakan Gulden dari satu Laporan Semalam, maksimal sekali sehari.
- **Banner:** hanya muncul di layar Markas, tidak pernah di mode AR, peta, atau Analisis.

Prinsip monetisasi:

- Tidak ada fitur berbayar yang memberi keuntungan besar. Semua level alat dan pembangkit bisa diraih lewat bermain normal.
- Iklan tidak boleh membuka jawaban Analisis, agar fungsi edukasi tetap terjaga.
- Untuk pengumpulan tugas dan pameran, iklan boleh dinonaktifkan.

## **Cakupan Proyek**

- **Bentuk akhir:** vertical slice yang memuat seluruh loop inti, lima distrik, dan dua belas jenis SDA.
- **Area uji:** lingkungan kampus dan sekitarnya. Spawn tetap berjalan di lokasi mana pun.
- **Biaya pengembangan:** Rp0 (memakai perangkat lunak gratis).
- **Jumlah anggota tim:** 4 orang.

### Tim Inti

**Akhlaqul Muhammad Fadwa, Lead Programmer**

- Memimpin pengembangan teknis dan arsitektur game.
- Merancang integrasi AR, GPS, dan simulasi kota.
- Mengatur standar coding dan workflow Git.
- Menangani code review dan bug teknis.

**Muhammad Abidillah, Programmer**

- Mengerjakan mini-game tangkap per SDA, sistem Analisis, dan UI.
- Mengerjakan sistem inventori, upgrade alat, dan save data.

**M. Sohibbal, Asset Creator**

- Membuat model 3D low-poly SDA, pembangkit, kota, dan Alat Penangkap.
- Membuat ikon, HUD, dan elemen UI 2D.

**Rehandra, Quality Research**

- Menyusun bank soal Analisis dan isi Energipedia berdasarkan sumber tepercaya.
- Menyusun skenario playtest dan mencatat hasil pengujian.
- Membantu balancing angka (harga, drop rate, output pembangkit).

### Lisensi / Hardware / Biaya Lain

| Kebutuhan | Pilihan | Biaya |
|---|---|---|
| Game engine | Unity Personal + AR Foundation | Gratis |
| Model 3D | Blender | Gratis |
| Aset 2D | Krita / Figma | Gratis |
| Audio | Audacity + aset CC0 (Freesound, Kenney) | Gratis |
| Peta | Mapbox Unity SDK (kuota gratis) atau peta stilisasi buatan sendiri | Gratis |
| Iklan | Google AdMob | Gratis |
| Version control | Git + GitHub | Gratis |
| Perangkat uji | HP Android anggota tim yang mendukung ARCore | Sudah dimiliki |
| Publikasi (opsional) | Google Play Console | USD 25, sekali bayar |

## **Pengaruh (Ringkas)**

**Pengaruh #1: Pokémon GO**

- **Media:** Game
- Pokémon GO membuktikan bahwa berjalan di dunia nyata untuk menangkap sesuatu terasa menyenangkan. EnergiGo mengambil loop peta, mendekati node, dan menangkap lewat AR, lalu mengganti monster dengan SDA dan menambah tujuan jangka panjang berupa kota.

**Pengaruh #2: Tinder**

- **Media:** Aplikasi
- Swipe kanan dan kiri adalah gestur yang langsung dipahami tanpa tutorial. EnergiGo memakai gestur ini untuk Analisis (kanan untuk Benar, kiri untuk Salah), sehingga kuis terasa cepat dan ringan, bahkan saat pemain sedang berjalan.

**Pengaruh #3: WarioWare**

- **Media:** Game
- WarioWare menyajikan puluhan mini-game singkat dengan instruksi yang langsung terbaca. Pendekatan ini menjadi dasar mini-game tangkap EnergiGo: setiap SDA punya satu interaksi pendek yang unik dan memakai sensor HP.

**Pengaruh #4: SimCity dan Cities: Skylines**

- **Media:** Game
- Kedua game ini memperlihatkan bahwa jaringan listrik bisa menjadi teka-teki yang menarik: kapasitas, kebutuhan, dan dampak polusi. EnergiGo menyederhanakan ide ini menjadi satu kota kecil dengan kurva kebutuhan siang-malam.

### Apa yang Membedakan EnergiGo dari Game Lain

- **Tema energi dan SDA Indonesia**, termasuk peran nikel dan tambang dalam energi terbarukan, jarang diangkat di genre location-based AR.
- **Setiap SDA punya cara tangkap berbeda**, tidak hanya satu mekanik lempar atau bidik untuk semua objek.
- **Edukasi lewat konsekuensi:** pemain belajar bahwa PLTS tidak menyala di malam hari karena melihat kotanya sendiri padam, bukan karena membaca teks.
- **Kuis sebagai hadiah:** jawaban salah tidak menggagalkan tangkapan, dan setiap fakta menjadi kartu koleksi Energipedia.
- **Dilema tanpa penjahat:** Pak Darto menawarkan solusi cepat yang masuk akal, sehingga pilihan energi terasa seperti keputusan nyata.
- **Berjalan tanpa server**, sehingga game tetap bisa dimainkan dan didemokan di mana saja.

---

# **2. Cerita dan Gameplay**

## **Cerita (Ringkas)**

Di masa depan dekat, krisis energi membuat Kota Lentera padam. Dewan Energi merekrut pemain sebagai Penjaga Energi dengan alat khusus yang bisa menangkap esensi SDA di dunia nyata. Pemain menyalakan kota distrik demi distrik, sambil memilih mendengarkan Bu Sari yang memperjuangkan energi bersih atau Pak Darto yang menawarkan listrik cepat dari batu bara. Kondisi langit kota di akhir game menentukan ending.

## **Cerita (Detail)**

### Latar

Cadangan energi dunia menipis dan jaringan listrik antarkota runtuh. Kota Lentera, kota kecil fiksi di Indonesia, menjadi salah satu yang padam total. Para peneliti Dewan Energi menemukan bahwa krisis ini membuat esensi SDA "bocor" ke permukaan dan tersebar di sekitar tempat tinggal warga. Esensi ini tidak terlihat mata, tetapi bisa dideteksi dan ditangkap dengan Alat Penangkap berbasis AR.

Dewan Energi tidak punya cukup petugas, jadi mereka merekrut warga biasa sebagai Penjaga Energi. Pemain adalah salah satunya.

### Tokoh

- **Penjaga Energi (pemain):** warga biasa yang menerima Alat Penangkap pertama. Tidak punya nama atau wajah tetap agar pemain bisa memproyeksikan dirinya.
- **Bu Sari:** peneliti senior Dewan Energi. Sabar, idealis, dan berpikir jangka panjang. Ia mengajari pemain cara kerja energi terbarukan dan memperingatkan dampak polusi. Ia juga jujur mengakui kelemahan energi bersih: mahal di awal dan butuh perencanaan.
- **Pak Darto:** kontraktor PLTU yang ramah dan pragmatis. Ia ingin kota cepat menyala dan tidak peduli dengan cara. Argumennya masuk akal: warga butuh listrik malam ini, bukan sepuluh tahun lagi. Ia bukan penjahat, melainkan wakil dari dilema energi yang nyata.

### Struktur Bab

Cerita terbagi menjadi lima bab. Setiap bab terbuka saat pemain mencapai Level Penjaga tertentu dan berakhir saat distrik tersebut menyala penuh selama 24 jam simulasi.

| Bab | Distrik | Level | Konflik utama |
|---|---|---|---|
| 1 | Distrik Rumah | 1 | Tutorial. Pemain menyalakan lampu pertama dengan PLTS dan belajar dasar tangkap. |
| 2 | Distrik Pasar | 5 | Pasar ramai di malam hari. PLTS saja tidak cukup, dan Pak Darto menawarkan PLTU. |
| 3 | Distrik Industri | 12 | Pabrik butuh daya besar dan stabil. Baterai dan mineral seperti nikel mulai penting. |
| 4 | Distrik Rumah Sakit | 20 | Listrik tidak boleh padam sama sekali. Pemain butuh sumber stabil seperti PLTP atau PLTA. |
| 5 | Pusat Kota | 25 | PLTN terbuka. Pemain menentukan wajah akhir Kota Lentera. |

### Tawaran Pak Darto

Di awal setiap bab, Pak Darto menawarkan satu kesepakatan, lalu Bu Sari memberi tanggapan. Pemain memilih **Terima** atau **Tolak**.

| Bab | Tawaran Pak Darto | Jika diterima | Jika ditolak |
|---|---|---|---|
| 1 | Generator diesel gratis untuk satu malam | Distrik Rumah menyala malam ini, polusi kecil | Bu Sari memberi 1 Baterai kecil |
| 2 | PLTU langsung jadi tanpa biaya | Gratis 1 PLTU, polusi naik | Bu Sari memberi 300 Gulden untuk PLTS |
| 3 | Kontrak pasokan batu bara murah | 50 Batu Bara ke gudang kota | Bu Sari membuka riset Baterai lebih cepat |
| 4 | Upgrade PLTU gratis ke kapasitas besar | Output PLTU naik 50%, polusi ikut naik | Bu Sari memberi diskon 30% untuk PLTP |
| 5 | Pusat Kota ditenagai PLTU raksasa | Pusat Kota langsung menyala, Langit turun drastis | Pemain harus menyalakan Pusat Kota dengan usaha sendiri |

Setiap tawaran bisa diterima tanpa membuat game kalah. Pilihan ini menggeser indikator Langit, yang kemudian menentukan ending.

### Ending

Ending ditentukan oleh rata-rata nilai Langit selama Bab 5 dan kondisi saat Pusat Kota menyala penuh.

- **Langit Biru (Langit rata-rata ≥ 70):** Kota Lentera menjadi contoh kota energi bersih. Bu Sari menjadikan pemain Kepala Penjaga Energi. Pak Darto akhirnya membuka usaha instalasi panel surya.
- **Langit Senja (40–69):** kota menyala dengan campuran energi. Warga hidup nyaman, tetapi Bu Sari mengingatkan bahwa pekerjaan belum selesai.
- **Langit Kelabu (< 40):** kota terang tetapi tertutup asap. Listrik stabil, warga mulai sakit, dan Dewan Energi mengirim pemain untuk memulai pemulihan dari awal di kota lain.

Setelah ending, game berlanjut dalam mode bebas. Pemain bisa terus mengoleksi, meningkatkan Penguasaan SDA, melengkapi Energipedia, dan mengubah susunan pembangkit untuk memperbaiki langit kota.

## **Gameplay (Ringkas)**

Pemain membuka peta, mendekati node SDA acak, menyelesaikan mini-game tangkap di mode AR, lalu menjawab Analisis. Hasil tangkapan dijual untuk Gulden, dipakai meng-upgrade Alat Penangkap, dan dipakai membangun pembangkit. Kota berjalan terus saat game ditutup, dan Laporan Semalam memberi tahu pemain apa yang harus diperbaiki.

## **Gameplay (Detail)**

### Tiga Lapis Loop

**Loop tangkapan (sekitar 30 detik)**

1. Pemain melihat node di peta dan berjalan mendekat.
2. Saat berada dalam radius tangkap (30 meter), pemain mengetuk node.
3. Kamera AR terbuka dan objek 3D SDA muncul.
4. Pemain menyelesaikan mini-game tangkap.
5. Pemain menjawab satu soal Analisis dengan swipe.
6. SDA masuk inventori dengan kualitas Normal atau Murni, dan XP bertambah.

**Loop sesi (sekitar 10–20 menit)**

1. Pemain membuka game dan membaca Laporan Semalam.
2. Pemain melihat kebutuhan kota: pembangkit yang ingin dibangun, bahan bakar yang hampir habis, modul yang ingin di-upgrade.
3. Pemain berjalan dan menangkap beberapa SDA sampai Wadah penuh atau target tercapai.
4. Pemain kembali ke Markas untuk menjual, meng-upgrade, dan membangun.

**Loop jangka panjang (harian sampai mingguan)**

1. Pemain naik Level Penjaga dan membuka SDA, pembangkit, serta distrik baru.
2. Pemain menyelesaikan bab cerita dan mengambil keputusan atas tawaran Pak Darto.
3. Pemain melengkapi Energipedia dan Penguasaan SDA.

### Alur Layar

```
Laporan Semalam → Peta → Mode AR (Tangkap → Analisis) → Peta
                    ↕
                  Markas (Jual · Alat · Kota · Energipedia · Profil)
```

### Contoh Sesi Pemain Level 6

Pemain membuka game pukul 07.00. Laporan Semalam menunjukkan Distrik Pasar padam dari pukul 19.00 sampai 23.00 karena kota hanya punya dua PLTS dan satu PLTU kecil. Bu Sari menyarankan Baterai, tetapi Baterai belum terbuka (butuh Level 8). Pemain memutuskan menambah satu PLTB untuk malam hari.

PLTB butuh 20 Esensi Angin dan 15 Tembaga. Pemain berjalan ke kampus, menangkap tiga node Angin ★1–★2 dan satu Tembaga ★2. Ia menemukan Batu Bara ★3, tetapi Pemicu miliknya masih Lv1, sehingga ia hanya bisa mencoba tangkapan nekat dengan kesulitan ekstra, dan gagal. Pemain pulang ke Markas, menjual Silika berlebih, lalu meng-upgrade Pemicu ke Lv2. Sore harinya ia keluar lagi untuk menyelesaikan bahan PLTB.

---

# **3. Mekanik Gameplay Inti (Detail)**

## **3.1 Eksplorasi dan Spawn Random**

### Detail

Layar peta menampilkan posisi pemain dan node SDA di sekitarnya. Node muncul secara acak dari kolam SDA yang sudah terbuka sesuai Level Penjaga. Setiap node menampilkan ikon SDA dan, jika modul Radar cukup tinggi, jumlah Bintangnya. Node yang Bintangnya belum bisa ditangkap tetap terlihat dengan ikon gembok.

Untuk MVP, peta memakai tampilan stilisasi: latar grid sederhana dengan posisi pemain di tengah dan node di sekelilingnya, tanpa data jalan. Pada tahap Alpha, tim bisa mengganti latar ini dengan peta jalan nyata dari Mapbox Unity SDK jika kuota dan waktu memungkinkan.

### Cara Kerja

**Spawn berbasis grid dan seed.** Game membagi dunia menjadi sel berukuran sekitar 100 × 100 meter berdasarkan koordinat GPS. Untuk setiap sel, game membuat angka acak dengan seed dari gabungan ID sel dan jendela waktu 30 menit. Selama jendela yang sama, sel yang sama selalu menghasilkan node yang sama, sehingga pemain tidak bisa mengulang spawn dengan menutup dan membuka game. Setiap sel berisi 0 sampai 3 node. Node yang sudah ditangkap disimpan di save data dan tidak muncul lagi pada jendela tersebut.

**Pemilihan node.** Game mengacak SDA dari kolam yang terbuka berdasarkan bobot kelangkaan, lalu mengacak Bintang berdasarkan Level Penjaga.

| Kelangkaan | SDA | Bobot dasar |
|---|---|---|
| Umum | Matahari, Angin, Batu Bara, Silika | 55% |
| Tidak umum | Air, Minyak Bumi, Tembaga | 28% |
| Langka | Nikel, Emas, Gas Alam | 13% |
| Epik | Geotermal, Uranium | 4% |

Jika sebuah SDA belum terbuka, bobotnya dibagi rata ke SDA lain dalam kelas kelangkaan yang sama, atau ke kelas di bawahnya jika kelas itu kosong.

| Level Penjaga | ★1 | ★2 | ★3 | ★4 | ★5 |
|---|---|---|---|---|---|
| 1–4 | 70% | 30% | 0% | 0% | 0% |
| 5–9 | 45% | 35% | 20% | 0% | 0% |
| 10–15 | 25% | 35% | 30% | 10% | 0% |
| 16–24 | 15% | 25% | 30% | 22% | 8% |
| 25–30 | 10% | 20% | 30% | 25% | 15% |

**Bad luck protection.** Game menghitung jumlah node yang ditemui berturut-turut tanpa SDA Langka atau Epik. Jika hitungan mencapai 20, node berikutnya dijamin minimal Langka, lalu hitungan kembali ke nol.

**Mode debug.** Build pengembangan punya joystick virtual untuk menggeser posisi GPS palsu, sehingga tim bisa menguji spawn tanpa berjalan kaki.

## **3.2 Tangkap AR per SDA**

### Detail

Setiap SDA punya satu mini-game tangkap berdurasi 5–15 detik yang memakai kamera, gyroscope, atau sentuhan. Setiap mini-game termasuk salah satu dari tiga gaya, dan setiap gaya dibantu oleh satu modul Alat Penangkap.

| Modul kunci | SDA | Mini-game tangkap |
|---|---|---|
| **Lensa** | Matahari | Arahkan HP ke langit dan tahan bola cahaya di dalam reticle sampai panel terisi. |
| | Air | Ikuti aliran partikel air dengan menggerakkan kamera. |
| | Silika | Pantulkan berkas cahaya ke kristal dengan memiringkan HP. |
| **Stabilizer** | Uranium | Tahan HP benar-benar diam selama beberapa detik. Guncangan membuat bar turun. |
| | Gas Alam | Tahan HP stabil untuk menutup kebocoran sambil bar tekanan naik. |
| | Emas | Miringkan HP perlahan untuk mengayak pasir dan memisahkan butiran emas. |
| **Pemicu** | Angin | Swipe searah putaran pusaran angin untuk memutar turbin. |
| | Geotermal | Ketuk tepat saat uap menyembur dari retakan. |
| | Batu Bara | Ketuk berulang pada titik retak untuk menambang. |
| | Nikel | Ketuk titik retak yang berpindah-pindah. |
| | Minyak Bumi | Tekan dan lepas mengikuti irama pompa. |
| | Tembaga | Tarik kabel tembaga dengan swipe panjang tanpa terputus. |

### Cara Kerja

Game menempatkan model 3D SDA di depan kamera memakai AR Foundation (plane detection jika tersedia, atau penempatan di depan kamera jika tidak). Bintang node mengatur parameter mini-game:

| Bintang | Kesulitan | Hasil dasar | XP |
|---|---|---|---|
| ★1 | Lambat, window lebar, tanpa gangguan | ×1 | 10 |
| ★2 | Sedikit lebih cepat | ×1,5 | 20 |
| ★3 | Cepat, ada gangguan kecil (objek bergeser, arah berubah) | ×2 | 35 |
| ★4 | Window sempit, gangguan lebih sering | ×3 | 55 |
| ★5 | Sangat cepat, gangguan beruntun | ×4 + peluang mineral bonus | 80 |

Modul kunci meringankan parameter mini-game sesuai levelnya, misalnya memperlebar window atau memperlambat objek. Jika mini-game gagal, node tetap berada di peta dan pemain boleh mencoba sekali lagi. Setelah dua kali gagal, node menghilang dari sesi tersebut.

Contoh skala kesulitan untuk Angin: pada ★1 pemain cukup swipe searah putaran. Pada ★5 arah putaran berbalik di tengah permainan, sehingga pemain harus bereaksi cepat.

## **3.3 Analisis dan Energipedia**

### Detail

Setelah mini-game berhasil, layar Analisis menampilkan satu pernyataan tentang SDA yang baru ditangkap. Pemain swipe ke kanan untuk **Benar** atau ke kiri untuk **Salah**. Analisis tidak pernah menggagalkan tangkapan. Tujuannya memberi bonus bagi pemain yang tahu, dan mengajari pemain yang belum tahu.

Setiap pernyataan yang pernah muncul, beserta penjelasannya, masuk ke **Energipedia** sebagai kartu. Energipedia dikelompokkan per SDA dan menampilkan progres koleksi, misalnya "Uranium 7/15".

### Cara Kerja

| Hasil | Kualitas | Hasil tangkapan | XP tambahan |
|---|---|---|---|
| Benar | Murni | +50% | +50% XP tangkap |
| Salah | Normal | Hasil dasar | +10% XP tangkap |

Jika jawaban salah, penjelasan satu kalimat muncul selama minimal 3 detik sebelum bisa ditutup. Jika jawaban benar, penjelasan tetap tersedia lewat tombol "Kenapa?".

Bintang node menentukan tingkat soal:

| Bintang | Tingkat soal | Contoh |
|---|---|---|
| ★1–★2 | Fakta dasar | "PLTS menghasilkan listrik dari sinar matahari." (Benar) |
| ★3 | Sebab-akibat | "PLTS tetap menghasilkan listrik penuh di malam hari." (Salah) |
| ★4–★5 | Perbandingan dan data | "Indonesia termasuk negara dengan cadangan nikel terbesar di dunia." (Benar) |

Bank soal disimpan dalam file JSON dengan target minimal 15 soal per SDA (sekitar 180 soal). Game tidak mengulang soal yang sama sampai semua soal di tingkat tersebut sudah muncul. Setiap soal wajib mencantumkan sumber di data internal agar tim bisa memverifikasi kebenarannya.

## **3.4 Progresi: Level Penjaga, Alat Penangkap, Penguasaan SDA**

### Level Penjaga

Level Penjaga (1–30) naik dari XP dan membuka konten baru. XP untuk naik ke level berikutnya dihitung dengan rumus `100 × level^1,5` (dibulatkan).

**Sumber XP**

| Aktivitas | XP |
|---|---|
| Tangkap berhasil | 10–80, sesuai Bintang |
| Analisis benar | +50% XP tangkap |
| Kartu Energipedia baru | 15 |
| Membangun jenis pembangkit untuk pertama kali | 100 |
| Distrik menyala penuh 24 jam simulasi | 300 |
| Menyelesaikan bab | 500 |

**Jalur pembukaan konten**

| Level | Yang terbuka |
|---|---|
| 1 | Matahari, Batu Bara, Silika · PLTS, PLTU · modul Lensa, Pemicu, Wadah · Distrik Rumah |
| 3 | Modul Radar |
| 5 | Angin, Tembaga · PLTB · Distrik Pasar |
| 8 | Air, Minyak Bumi · PLTD, Baterai |
| 12 | Nikel, Emas · PLTA · modul Stabilizer · Distrik Industri |
| 16 | Gas Alam · PLTG |
| 20 | Geotermal · PLTP · Distrik Rumah Sakit |
| 25 | Uranium · PLTN · Pusat Kota |
| 30 | Gelar Kepala Penjaga · mode bebas penuh |

### Alat Penangkap Modular

Alat Penangkap terdiri dari lima modul. Setiap modul bisa naik sampai Lv5, dan semua modul pada akhirnya bisa dimaksimalkan. Keputusan pemain terletak pada urutan upgrade, karena biaya naik tajam dan mineral langka terbatas.

**Modul tangkap** (memengaruhi mini-game dan mengunci Bintang node)

| Modul | Efek per level |
|---|---|
| Lensa | Reticle +10% dan kecepatan objek −8% per level |
| Stabilizer | Toleransi guncangan +15% per level |
| Pemicu | Window timing +12% dan jumlah ketukan yang dibutuhkan −10% per level |

**Modul pendukung** (tidak mengunci node)

| Modul | Lv1 | Lv2 | Lv3 | Lv4 | Lv5 |
|---|---|---|---|---|---|
| Radar (radius deteksi) | 60 m | 90 m | 120 m + Bintang terlihat | 160 m | 200 m + notifikasi SDA Epik |
| Wadah (slot inventori) | 20 | 30 | 40 | 50 | 60 |

**Aturan penguncian.** Node ★N butuh modul kuncinya minimal Lv(N−1). Node ★1 bisa ditangkap siapa saja. Uranium ★4 butuh Stabilizer Lv3.

**Tangkapan nekat.** Pemain boleh mencoba node satu Bintang di atas kemampuan alatnya. Mini-game mendapat kesulitan tambahan 30%, dan pemain hanya punya satu kesempatan.

**Biaya upgrade (sama untuk semua modul)**

| Naik ke | Gulden | Mineral |
|---|---|---|
| Lv2 | 200 | 10 Silika |
| Lv3 | 600 | 15 Tembaga |
| Lv4 | 1.500 | 20 Nikel + 10 Tembaga |
| Lv5 | 4.000 | 25 Nikel + 5 Emas |

**Tampilan alat.** Setiap level modul menambah detail visual pada model Alat Penangkap di mode AR, misalnya lensa tambahan, antena yang lebih panjang, atau lampu Stabilizer yang menyala.

### Penguasaan SDA

Setiap SDA punya bar Penguasaan terpisah yang naik setiap kali pemain menjawab Analisis SDA tersebut dengan benar.

| Level | Nama | Syarat (jawaban benar) | Efek |
|---|---|---|---|
| 1 | Pengenal | 1 | Kartu Energipedia pertama terbuka |
| 2 | Pengamat | 5 | Harga jual SDA ini +10% |
| 3 | Peneliti | 12 | Hasil tangkap SDA ini +10% |
| 4 | Analis | 25 | Soal tingkat lanjut muncul lebih sering |
| 5 | Ahli | 40 | Pembangkit berbahan SDA ini +10% output, kartu Energipedia langka terbuka |

Untuk SDA Mineral, efek Lv5 diganti menjadi biaya mineral tersebut −10% untuk semua upgrade dan pembangunan.

## **3.5 Ekonomi, Kota, dan Jaringan Listrik**

### Detail

Markas menampilkan Kota Lentera dari sudut pandang isometrik. Kota terdiri dari lima distrik. Setiap distrik punya slot pembangkit dan kebutuhan listrik sendiri. Pemain membangun pembangkit, mengisi bahan bakar, dan memantau dua indikator:

- **Pasokan:** persentase kebutuhan listrik yang terpenuhi pada jam ini.
- **Langit (0–100):** tingkat kebersihan udara. Nilai 100 berarti langit biru bersih.

### Cara Kerja: Ekonomi

**Gulden** diperoleh dari dua sumber:

1. **Menjual SDA** di Markas. Harga dasar per unit: Umum 5, Tidak umum 12, Langka 30, Epik 80 Gulden. Kualitas Murni menaikkan harga 20%.
2. **Distrik yang menyala.** Setiap jam distrik mendapat listrik penuh, distrik menghasilkan Gulden sesuai tabel di bawah, dikalikan pengali Langit.

**Gudang Kota** menyimpan SDA Energi yang disetor sebagai Esensi (untuk membangun pembangkit terbarukan) dan Bahan Bakar (untuk pembangkit tak terbarukan). Gudang terpisah dari Wadah, sehingga pemain bisa mengosongkan Wadah setiap pulang.

### Cara Kerja: Pembangkit

| Pembangkit | Syarat level | Biaya bangun | Output | Bahan bakar per jam | Polusi per jam |
|---|---|---|---|---|---|
| PLTS (Surya) | 1 | 150 G, 20 Esensi Matahari, 10 Silika | 0–10 MW, hanya 06.00–18.00, puncak 11.00–13.00 | Tidak ada | 0 |
| PLTU (Batu Bara) | 1 | 100 G | 12 MW stabil | 2 Batu Bara | +3 |
| PLTB (Angin) | 5 | 250 G, 20 Esensi Angin, 15 Tembaga | 2–12 MW, acak per jam | Tidak ada | 0 |
| PLTD (Diesel) | 8 | 150 G | 8 MW stabil | 2 Minyak Bumi | +2 |
| Baterai | 8 | 400 G, 20 Nikel, 10 Tembaga | Simpan 30 MWh, lepas maks 10 MW | Tidak ada | 0 |
| PLTA (Air) | 12 | 800 G, 40 Esensi Air, 20 Tembaga | 15 MW stabil, maks 1 per distrik | Tidak ada | 0 |
| PLTG (Gas) | 16 | 500 G | 18 MW stabil | 1 Gas Alam | +1,5 |
| PLTP (Geotermal) | 20 | 1.500 G, 10 Esensi Geotermal, 20 Tembaga | 20 MW stabil | Tidak ada | 0 |
| PLTN (Nuklir) | 25 | 5.000 G, 30 Nikel, 10 Emas | 50 MW stabil | 1 Uranium per 4 jam | 0 |

Catatan: Level 8 mengharuskan Nikel untuk Baterai, sementara Nikel baru terbuka di Level 12. Untuk mengatasinya, Bu Sari memberikan 20 Nikel sebagai hadiah saat Bab 2 selesai. Tim perlu menguji dan menyesuaikan angka ini saat playtest.

Pembangkit bisa di-upgrade sampai Lv3. Setiap level menaikkan output 25% dengan biaya 2× dan 4× biaya bangun awal.

### Cara Kerja: Kebutuhan Distrik

| Distrik | Kebutuhan dasar | Kebutuhan puncak (18.00–22.00) | Gulden per jam menyala | Prioritas padam |
|---|---|---|---|---|
| Rumah | 5 MW | 10 MW | 10 | 3 |
| Pasar | 6 MW | 16 MW | 20 | 1 (padam pertama) |
| Industri | 20 MW | 25 MW | 45 | 2 |
| Rumah Sakit | 15 MW | 18 MW | 35 | 5 (padam terakhir) |
| Pusat Kota | 25 MW | 40 MW | 80 | 4 |

Kebutuhan di jam 00.00–06.00 turun menjadi 60% dari kebutuhan dasar.

### Cara Kerja: Simulasi per Jam

Simulasi kota berjalan dalam langkah satu jam berdasarkan jam HP asli. Setiap langkah:

1. **Produksi.** Game menjumlahkan output semua pembangkit aktif pada jam tersebut. Pembangkit tak terbarukan tanpa bahan bakar di Gudang tidak menghasilkan listrik.
2. **Kebutuhan.** Game menjumlahkan kebutuhan semua distrik yang sudah terbuka.
3. **Penyeimbangan.**
   - Jika produksi lebih besar dari kebutuhan, surplus mengisi Baterai.
   - Jika produksi kurang, Baterai melepas cadangan.
   - Jika masih kurang, distrik padam satu per satu sesuai prioritas sampai sisa kebutuhan terpenuhi.
4. **Gulden.** Distrik yang menyala menghasilkan Gulden × pengali Langit.
5. **Polusi.** Langit turun sebesar total polusi, lalu naik 1 poin secara alami (maksimal 100).

**Pengali Langit**

| Langit | Status | Pengali Gulden | Visual kota |
|---|---|---|---|
| 70–100 | Biru | ×1,2 | Cerah, burung, pohon hijau |
| 40–69 | Senja | ×1,0 | Kekuningan, sedikit kabut |
| 0–39 | Kelabu | ×0,7 | Abu-abu, asap, warga memakai masker |

### Progres Offline dan Laporan Semalam

Saat pemain membuka game, sistem menghitung selisih waktu sejak game terakhir ditutup, membatasinya maksimal **8 jam**, lalu menjalankan simulasi per jam sebanyak selisih tersebut. Saat game terbuka, simulasi berjalan terus secara real-time.

Setelah perhitungan, game menampilkan **Laporan Semalam**:

> Selama 7 jam terakhir:
> Distrik Pasar padam 4 jam (19.00–23.00). Baterai habis pukul 19.00.
> Gulden terkumpul: 340. Bahan bakar terpakai: 14 Batu Bara.
> Langit: 72 → 65 (Senja).
>
> **Bu Sari:** "Surplus siang harimu terbuang. Baterai bisa menyimpannya untuk malam."

Saran di bawah laporan dipilih dari kondisi kota: Bu Sari memberi saran energi bersih, Pak Darto memberi saran cepat ketika banyak distrik padam.

**Mode debug.** Build pengembangan dan build pameran punya tombol "Lompat Waktu" (+1 jam, +8 jam, set ke 20.00) agar tim dan pengunjung bisa melihat efek malam hari tanpa menunggu.

## **3.6 Save Data**

Semua data tersimpan lokal di perangkat dalam file JSON terenkripsi ringan:

- Profil: Level, XP, Gulden, bab, pilihan tawaran Pak Darto.
- Alat: level lima modul.
- Inventori (Wadah) dan Gudang Kota.
- Kota: pembangkit, level, isi Baterai, nilai Langit, waktu terakhir simulasi.
- Penguasaan SDA dan kartu Energipedia yang terbuka.
- Node yang sudah ditangkap per sel dan jendela waktu, serta hitungan bad luck protection.

Game menyimpan otomatis setiap selesai tangkapan, transaksi di Markas, dan saat aplikasi masuk ke background.

---

# **4. Gaya Grafis**

EnergiGo memakai gaya **low-poly dengan warna flat dan saturasi tinggi**. Gaya ini menjaga objek AR tetap jelas di atas feed kamera dengan pencahayaan yang berubah-ubah, dan menjaga ukuran aset tetap ringan untuk HP kelas menengah.

**Kode warna SDA**

| SDA | Warna |
|---|---|
| Matahari | Kuning |
| Angin | Putih kebiruan |
| Air | Biru muda |
| Geotermal | Oranye |
| Batu Bara | Abu-abu gelap |
| Minyak Bumi | Hitam kecokelatan |
| Gas Alam | Ungu muda |
| Uranium | Hijau neon |
| Silika | Bening keperakan |
| Tembaga | Oranye tembaga |
| Nikel | Abu-abu metalik |
| Emas | Emas |

**Arah visual per layar**

- **Peta:** latar terang dengan ikon node besar dan bingkai warna sesuai kelangkaan (abu-abu, hijau, biru, ungu).
- **Mode AR:** objek SDA memancarkan glow tipis agar tetap terlihat di bawah sinar matahari. Alat Penangkap tampil di bagian bawah layar.
- **Analisis:** kartu besar di tengah layar, dengan panduan swipe hijau (Benar) di kanan dan merah (Salah) di kiri.
- **Markas:** kota isometrik kecil yang berubah warna mengikuti status Langit, dan distrik padam tampil gelap.
- **UI:** ikon sederhana, tipografi tebal, dan kontras tinggi agar terbaca di luar ruangan.

---

# **5. Aset yang Dibutuhkan**

## **2D**

- **Ikon SDA:** 12 ikon, ditambah 4 bingkai kelangkaan dan ikon Bintang.
- **Ikon pembangkit:** 9 ikon.
- **Ikon modul:** 5 ikon, dengan variasi Lv1–Lv5.
- **Logo:** logo EnergiGo dan logo Dewan Energi (fiksi).
- **HUD peta:** indikator Level dan XP, Gulden, kapasitas Wadah, lingkaran Radar, ikon gembok node.
- **HUD AR:** reticle, bar progres tangkap, indikator guncangan, tombol keluar.
- **UI Analisis:** kartu soal, panduan swipe, panel penjelasan.
- **UI Markas:** panel Jual, Alat, Kota, Energipedia, Profil, indikator Pasokan dan Langit, grafik produksi vs kebutuhan 24 jam.
- **Laporan Semalam:** panel ringkasan dan panel saran NPC.
- **Potret NPC:** Bu Sari dan Pak Darto, masing-masing 3 ekspresi.
- **Kartu Energipedia:** template kartu per SDA.
- **Latar peta stilisasi:** tile grid.

## **3D**

**Model SDA (12)**

- SDA Energi: Matahari, Angin, Air, Geotermal, Batu Bara, Minyak Bumi, Gas Alam, Uranium.
- SDA Mineral: Silika, Tembaga, Nikel, Emas.

**Alat Penangkap**

- 1 model dasar dengan variasi komponen per modul (Lensa, Stabilizer, Pemicu, Radar, Wadah).

**Kota (Markas)**

- 9 model pembangkit, masing-masing dengan 3 tingkat upgrade.
- 5 set bangunan distrik (Rumah, Pasar, Industri, Rumah Sakit, Pusat Kota), masing-masing versi menyala dan padam.
- Dekorasi: pohon, jalan, lampu jalan, warga sederhana.
- Efek lingkungan: asap, kabut, langit tiga status.

## **Suara**

**Ambient**

- Peta (luar ruangan): angin ringan, suasana kota.
- Mode AR: dengungan alat, bervariasi per jenis SDA.
- Markas: suasana kota ramai (Langit Biru), kota berkabut dan sunyi (Langit Kelabu), kota padam.

**Tangkap**

- Suara interaksi mini-game per SDA (12): kilau cahaya, desir angin, gemericik air, ketukan tambang, desis gas, dan sebagainya.
- Tangkapan berhasil, tangkapan gagal, tangkapan nekat.

**Analisis**

- Swipe, jawaban benar, jawaban salah, kartu Energipedia baru.

**UI dan progresi**

- Tombol, notifikasi, naik level, upgrade modul, pembangkit selesai dibangun, distrik menyala, distrik padam.

**Musik**

- Musik menu dan peta, musik Markas, musik singkat per ending.

## **Kode**

**Sistem inti**

- `GameManager`: alur antar layar dan state game.
- `SaveSystem`: simpan dan muat data lokal.
- `TimeService`: jam asli, progres offline, Lompat Waktu (debug).

**Eksplorasi**

- `LocationService`: pembacaan GPS dan GPS palsu (debug).
- `SpawnGenerator`: grid, seed, bobot kelangkaan, distribusi Bintang, bad luck protection.
- `MapView`: tampilan peta stilisasi atau Mapbox.

**Tangkap dan Analisis**

- `ARCaptureController`: penempatan objek AR dan alur tangkap.
- `CaptureMinigame` (kelas dasar) dengan 12 turunan per SDA.
- `QuizManager`: memuat bank soal JSON, memilih soal sesuai Bintang, menghindari pengulangan.
- `SwipeInput`: deteksi swipe kanan dan kiri.

**Progresi**

- `PlayerProgress`: Level, XP, pembukaan konten.
- `GearSystem`: level modul, efek, aturan penguncian.
- `MasterySystem`: Penguasaan SDA.
- `Encyclopedia`: kartu Energipedia.
- `Inventory` dan `CityStorage`: Wadah dan Gudang Kota.

**Kota**

- `CitySimulation`: langkah simulasi per jam.
- `PowerPlant` (kelas dasar) dengan turunan per jenis pembangkit.
- `DistrictManager`: kebutuhan, prioritas padam, produksi Gulden.
- `SkySystem`: nilai Langit dan pengali.
- `OvernightReport`: Laporan Semalam dan saran NPC.

**Cerita dan lainnya**

- `DialogueSystem`: dialog bab dan tawaran Pak Darto.
- `EndingManager`: penentuan ending.
- `AdsManager`: rewarded video dan banner (opsional).

**Data (ScriptableObject dan JSON)**

- Data SDA, data pembangkit, data distrik, tabel upgrade modul, tabel XP, bank soal, naskah dialog.

## **Animasi**

**Lingkungan dan objek**

- Animasi idle setiap model SDA (berputar, berdenyut, memancar).
- Animasi mini-game per SDA: objek bergerak, berubah arah, menyembur, retak.
- Animasi berhasil (objek terserap ke alat) dan gagal (objek memudar).
- Animasi pembangkit bekerja: panel berkilau, turbin berputar, cerobong berasap.
- Transisi distrik menyala dan padam.
- Transisi langit Biru, Senja, Kelabu.

**Alat Penangkap**

- Animasi alat saat membidik dan menyerap.
- Animasi komponen baru muncul saat modul naik level.

**UI**

- Transisi layar: Peta ke Mode AR ke Analisis ke Peta, serta Peta ke Markas.
- Kartu Analisis bergeser mengikuti jari.
- Bar XP terisi dan efek naik level.
- Laporan Semalam muncul baris per baris.

**Karakter**

- Pergantian ekspresi potret Bu Sari dan Pak Darto saat dialog.

---

# **6. Jadwal dan Milestone**

Estimasi total sekitar 14 minggu. Tim bisa memadatkan atau memperpanjang setiap tahap sesuai kalender semester.

## **Objektif 1: Pra-produksi**

- **Waktu:** 2 minggu
- **Milestone:**
  - GDD versi ini disepakati seluruh tim.
  - Referensi visual dan palet warna terkumpul.
  - Struktur proyek Unity, repository Git, dan workflow branch siap.
  - Rehandra mulai menyusun bank soal (target 5 soal per SDA).

## **Objektif 2: Prototype / Minimum Viable Product (MVP)**

- **Waktu:** 3 minggu
- **Milestone:**
  - Peta stilisasi dengan GPS asli dan GPS palsu.
  - Spawn grid dan seed untuk 3 SDA awal (Matahari, Batu Bara, Silika).
  - Tangkap AR untuk 3 SDA tersebut, dengan Bintang ★1–★2.
  - Analisis swipe dengan bank soal awal.
  - Inventori, jual SDA, dan Gulden.
  - Kota dengan Distrik Rumah, PLTS, PLTU, dan simulasi per jam.
  - Save data lokal.

## **Objektif 3: Alpha**

- **Waktu:** 4 minggu
- **Milestone:**
  - Seluruh 12 SDA beserta mini-game tangkapnya.
  - Bintang ★1–★5 dan aturan penguncian modul.
  - Lima modul Alat Penangkap beserta upgrade.
  - Level Penjaga, jalur pembukaan konten, dan Penguasaan SDA.
  - Seluruh 9 pembangkit, 5 distrik, Baterai, indikator Langit.
  - Progres offline dan Laporan Semalam.
  - Energipedia dengan minimal 10 soal per SDA.

## **Objektif 4: Beta**

- **Waktu:** 3 minggu
- **Milestone:**
  - Lima bab cerita, dialog Bu Sari dan Pak Darto, tawaran per bab, dan tiga ending.
  - Seluruh aset final, suara, dan musik masuk.
  - Bank soal lengkap (15 soal per SDA).
  - Iklan terintegrasi (opsional).
  - Playtest eksternal pertama dengan mahasiswa di lingkungan kampus.
  - Balancing angka ekonomi, drop rate, dan output pembangkit berdasarkan hasil playtest.

## **Objektif 5: Rilis / Pengumpulan Tugas**

- **Waktu:** 2 minggu
- **Milestone:**
  - Perbaikan bug dari hasil playtest.
  - Build pameran dengan tombol Lompat Waktu dan GPS palsu untuk demo di dalam ruangan.
  - Build final siap dikumpulkan dan, jika memungkinkan, diunggah ke Google Play.
  - Materi presentasi dan video gameplay untuk pameran.

---

# **7. Rencana Promosi**

- **Demo langsung di kampus:** sesi coba-main di area kampus, karena pemain bisa langsung mencoba spawn di lokasi nyata.
- **Instagram:** reels cuplikan tangkap AR dan konten edukasi ringan tentang energi dan SDA Indonesia, misalnya satu kartu Energipedia per unggahan.
- **TikTok:** video pendek momen tangkapan sulit, seperti Uranium ★5 atau Angin yang berbalik arah, serta perbandingan kota Langit Biru dan Langit Kelabu.
- **Pameran Inno Electrica:** demo memakai build pameran, sekaligus mengumpulkan masukan dari pengunjung dan dosen.

---

# **8. Lampiran: Fitur di Luar Cakupan**

Fitur berikut sengaja tidak dimasukkan dalam versi ini. Tim bisa mempertimbangkannya untuk pengembangan lanjutan.

| Fitur | Alasan tidak dimasukkan |
|---|---|
| Stasiun Energi dan tantangan komunitas | Butuh server dan data gabungan antarpemain |
| Anomali Energi (node boss multi-tahap) | Beban aset dan balancing tinggi; fungsinya terwakili node ★5 |
| Kontrak Dewan (misi harian) | Menambah sistem baru; dilema NPC sudah hadir lewat tawaran per bab |
| Kapasitas Inti (batas total level modul) | Tidak relevan tanpa kontrak dan fitur sosial |
| Spawn berdasarkan waktu, cuaca, atau lokasi geografis | Spawn sengaja dibuat sepenuhnya acak |
| Multiplayer, leaderboard, dan akun online | Game dirancang single player dan offline |
