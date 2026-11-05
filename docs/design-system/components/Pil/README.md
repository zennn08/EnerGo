Label kecil untuk bacaan dan status: pil (bulat, info yang terus tampil), chip (sudut kecil, rincian dan hadiah), badge (jumlah unit di sudut tile).

**Pil**: tinggi 40 unit, `radius-pil`, garis `garis-tinta`, teks `status` huruf kapital. Varian `--data` dan `--angka` memakai `data` (Space Mono) untuk bacaan alat: SPAWN BARU mm:dd, GPS ±N m, N unit, SOAL n/3. `--laut` untuk mode kamera, `--oker` untuk hal yang berhubungan dengan energi/hadiah.

Pakai di: total unit Lobby, label seksi KOLEKSI ENERGIMU, header layar tangkap (mode kamera dan nama SDA), info radar.

**Chip**: tinggi 34 unit, `radius-sm`, garis `garis-tipis`, teks `isi-tebal` 15 unit tidak kapital. Varian `--benar` dan `--salah` selalu memuat ikon centang/silang dan kata BENAR/SALAH. `--oker` untuk hadiah (+1 unit).

Pakai di: rincian hadiah (Dasar +1, Kuis +n, [mini-game] [nilai] +b), chip pernyataan di umpan balik.

**Badge**: lingkaran `stempel` dengan angka `kertas-100` dalam Space Mono, di pojok kanan atas tile. Di atas 99 tulis 99+.
