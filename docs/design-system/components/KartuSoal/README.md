Kartu pernyataan benar/salah di kuis, dijawab dengan menggeser kartu atau menekan tombol BENAR/SALAH di bawahnya.

**Struktur**: Kartu kertas 360 unit, `radius-lg`. Baris atas: ikon jam, bar waktu tipis (15 detik, `oranye`, menjadi `stempel` di 5 detik terakhir bersama angkanya), angka detik `data`. Ikon SDA 56 unit di tengah. Pernyataan dalam `soal`, rata tengah. Kartu soal berikutnya mengintip di belakang, miring 3°, muka `kertas-300`.

**Menggeser**: ke kanan = BENAR, ke kiri = SALAH, ambang 90 unit. Kartu ikut miring maksimal 18°. Begitu lewat ambang: cap BENAR (`hutan`) atau SALAH (`stempel`) muncul di kartu, cincin warna yang sama mengelilingi kartu, dan tepi layar berpendar warna itu. Cap memastikan jawaban terbaca tanpa bergantung pada hijau/merah.

**Penanda soal** (kiri atas layar): 3 bulatan `.eg-penanda` — kini `oker`, benar `hutan` + ✓, salah `stempel` + ✕, belum `kertas-300`. Pil "SOAL n/3" di kanan atas.

Waktu habis dihitung salah.
