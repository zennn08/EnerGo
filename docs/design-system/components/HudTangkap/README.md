Header dan kotak status yang menempel di atas feed kamera selama mini-game; keduanya kertas terang supaya terbaca di kamera siang maupun gelap.

**Header** (baris paling atas, celah `space-2`):
- kiri: tombol kecil kertas "‹ RADAR" (dari radar) atau "‹ LOBBY" (latihan). Hilang saat kuis dimulai.
- tengah: pil `--laut` mode kamera (ARCORE, SENSOR 360, DRAG 360). Di scene ARCore pil ini bisa diketuk untuk berpindah mode; di Sensor 360 teksnya "TANPA ARCORE".
- kanan: pil nama SDA dengan ikon dan jumlah unit.

**Kotak status**: `kertas-100`, garis `garis-tinta`, `radius-md`, bibir 3 unit. Baris atas = tag + instruksi (`status`, huruf kapital, maksimal 2 baris). Di bawahnya bar ukur.

Tag menggantikan garis tepi hijau/oranye versi lama, karena kata lebih jelas dari warna:
| Tag | Warna | Kapan |
|---|---|---|
| AMAN | `hutan` | instruksi normal, progres berjalan |
| AWAS | `oker` | TERLALU KENCANG, KANTONG GAS, TERLALU CEPAT, JEPITAN LEPAS, dosis/tekanan di zona merah |
| GAGAL | `stempel` + muka `stempel-muda` | BLOWOUT!, KENA AIR!, OVERPRESSURE!, DOSIS BERLEBIH!, CAHAYA TERLALU SEDIKIT (1,5 detik sebelum target baru) |
| INFO | `laut` | pesan sistem: inisialisasi sensor, BERALIH KE SENSOR 360, MENUNGGU TARGET |

Tombol "MUNCULKAN ULANG TARGET" di dasar layar memakai Tombol kertas ukuran kecil, lebar penuh.
