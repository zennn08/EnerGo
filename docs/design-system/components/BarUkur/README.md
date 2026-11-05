Progress bar seperti alat ukur: track kraft dengan garis skala setiap 10%, isian berwarna dengan ujung bergaris tinta, dan baris label di atasnya.

**Struktur**: track `kertas-300`, garis `garis-tinta`, `radius-pil`. Isian diberi warna lewat `--isi` (bawaan `oranye`). Label kiri adalah nama ukuran (huruf kapital), label kanan adalah nilainya, keduanya `data`.

**Tebal**: biasa 18 unit; `--tipis` 12 unit untuk timer kuis dan loading splash; `--tebal` 26 unit untuk drum minyak dan meter utama.

**Warna isian**
- Warna SDA untuk progres mini-game: cahaya (`sda-matahari`), drum (`sda-minyak`), RAD (`sda-uranium`).
- `oranye` untuk waktu dan loading.
- Ukuran bahaya (dosis, tekanan gas, tekanan pompa) mulai `oker`, lalu `stempel` saat masuk zona merah (dosis > 75%, tekanan > 60%). Saat merah, label kiri ikut berubah (DOSIS · BAHAYA) supaya tidak hanya warna yang memberi tahu.
- Timer kuis berubah `stempel` di 5 detik terakhir.

Untuk bar vertikal (pengukur gas di penampang minyak) pakai komponen yang sama diputar 90°.
