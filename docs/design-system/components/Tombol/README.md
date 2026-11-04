Tombol tebal bergaris `tinta` dengan bibir bawah `bayang-tombol`; satu-satunya bentuk tombol teks di EnerGo.

**Struktur**: label huruf kapital `label-tombol` (Bricolage 800), ikon opsional 32 unit di kiri label, tinggi minimal `tombol-tinggi`, `radius-md`, garis `garis-tinta`.

**Varian**
| Varian | Isian | Label | Kapan |
|---|---|---|---|
| utama | `oranye` | `tinta` | Satu aksi terpenting per layar: BERBURU!, TANGKAP, LANJUT, MULAI, SELESAI |
| ok | `hutan` | `kertas-100` | Tombol BENAR di kuis |
| bahaya | `stempel` | `kertas-100` | Tombol SALAH, konfirmasi hapus |
| info | `laut` | `kertas-100` | LATIHAN (TANPA GPS), COBA LAGI |
| kertas | `kertas-100` | `tinta` | Aksi sekunder: TUTUP, LEWATI, kembali |
| nonaktif | `kertas-300`, garis putus | `tinta-pudar` | DEKATI DULU (N m), SEGERA HADIR, BACA DULU... |

**Ukuran**: besar (80 unit, `garis-tebal`) hanya untuk BERBURU! di Lobby. Kecil (44 unit) untuk tombol kembali dan LEWATI; area sentuhnya tetap diperluas sampai `sentuh-min`.

**State ditekan**: geser 3 unit ke bawah, bayangan diganti `bayang-ditekan`. Nonaktif tidak punya bibir dan tampak "tertekan" permanen, jadi bisa dibedakan dari tombol aktif tanpa melihat warna.

**Tombol terkunci sementara** (BACA DULU... 2 detik): pakai varian nonaktif dan isi bar tipis di dasar tombol (`.eg-btn__isi`) yang bertambah sampai penuh, lalu tombol berubah ke utama.

Jangan: dua tombol utama berdampingan; label lebih dari 4 kata; ikon tanpa label kecuali di dock.
