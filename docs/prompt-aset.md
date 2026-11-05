# EnerGo – Prompt Aset 3D & Audio

Nov 3, 2026 · @Daffandri Faiz

## Asumsi dan cara pakai

Semua prompt di dokumen ini memakai gaya **Jurnal Ekspedisi**, mengikuti design system EnerGo: warna dari token yang sama, bentuk low-poly sederhana dengan flat shading, dan garis tepi tinta tebal di sekeliling objek supaya model 3D terlihat seperti sketsa di buku lapangan. Bentuknya tetap geometris, jadi cocok dikerjakan lewat Blender MCP.

Cara pakai:

1. Kerjakan bagian Blender secara berurutan: file gaya dulu, baru aset satu per satu. Setiap blok prompt dikirim sebagai satu pesan ke Claude yang tersambung ke Blender.
2. Simpan file Blender (Ctrl+S) sebelum setiap prompt. Kalau hasilnya salah, buka lagi versi terakhir.
3. Untuk audio, salin kolom Prompt ke ElevenLabs, atur durasi dan loop sesuai tabel, lalu simpan dengan nama di kolom File.

Palet warna yang dipakai di semua prompt:

| Material | Hex | Token design system | Dipakai untuk |
| --- | --- | --- | --- |
| mat\_tinta | #1F2A36 | tinta | Garis tepi (outline), logam gelap, alas |
| mat\_kertas | #FBF6EA | kertas-100 | Dinding, bagian terang |
| mat\_kraft | #E7D9BA | kertas-300 | Pasir, kayu muda |
| mat\_kraft\_tua | #CDB68A | kraft-400 | Kayu, dulang |
| mat\_oranye | #F07F3C | oranye | Aksen utama, motor, atap |
| mat\_oker | #E2A72E | oker | Sinar matahari, lampu, jendela menyala |
| mat\_laut | #2F6E8F | laut | Rangka logam, pipa |
| mat\_hutan | #2E7A47 | hutan | Tanaman |
| mat\_peta\_hijau | #A9C79A | peta-hijau | Tanah berumput |
| mat\_air | #9CC3D3 | peta-air | Air dulang (transparan) |
| mat\_batubara | #4B4F58 | sda-batubara | Bongkahan batu bara |
| mat\_batubara\_terang | #7A808A | (turunan) | Kilau batu bara, batuan |
| mat\_emas | #C99A2E | sda-emas | Keping dan bongkah emas |
| mat\_minyak | #5B4BB5 | sda-minyak | Lengan pompa |
| mat\_uranium | #4FA83A | sda-uranium | Badan kristal uranium |
| mat\_uranium\_nyala | #39FF14 | uranium-nyala | Emisi kristal dan serpihan uranium |
| mat\_matahari | #F2B33D | sda-matahari | Bola matahari (beremisi) |

Pengecualian (3 Nov 2026): di kamera AR, batu bara dengan warna token terlihat abu-abu kebiruan, jadi materialnya digelapkan dan dibuat lebih licin. `mat_batubara` memakai #121418 (Unity smoothness 0,85, metallic 0,15) dan `mat_batubara_terang` memakai #3E4450 (smoothness 0,95, metallic 0,2), setelah uji HP pertama dinilai kurang hitam dan kurang mengilap, di Blender dan di `Assets/EnerGo/Materials/`. Tile dan ikon UI tetap memakai token `sda-batubara`.

Kalau token warna di design system berubah, ubah tabel ini dan prompt setup dengan nilai yang sama. Struktur prompt per aset tidak perlu diubah.

## Blender MCP: file gaya

Buat satu file `energo_gaya.blend` yang berisi semua material dan aturan, lalu bangun setiap aset di file terpisah yang menyalin material dari file ini. Dengan begitu semua aset memakai warna yang sama persis.

Aturan yang berlaku untuk semua aset:

| Aturan | Nilai |
| --- | --- |
| Skala | 1 unit Blender = 1 meter |
| Origin | Di tengah dasar objek (titik yang menyentuh tanah) |
| Batas poligon | Kecil 300–800 segitiga, sedang 800–2.000, besar maks 3.000 (belum termasuk outline) |
| Shading | Flat (Shade Flat), tanpa subdivision, tanpa tekstur |
| Material | Principled BSDF warna polos, Roughness 0,8, Metallic 0 (emas 0,3) |
| Outline tinta | Objek terpisah `<nama>_outline`: salinan mesh dengan face dibalik, didorong keluar sekitar 1,5% ukuran objek lewat modifier Displace (arah Normal), material `mat_tinta`. Jangan pakai Solidify: Solidify menyimpan lapisan asli yang menempel di permukaan objek (z-fighting, segitiga dua kali lipat) dan membuat duri di segitiga tipis |
| Penamaan objek | `sda_<nama>_<bagian>`, misalnya `sda_pompa_lengan` |

Prompt setup (kirim sekali di file kosong):

```text
Kita akan membuat aset 3D untuk game mobile AR bergaya sketsa buku lapangan: bentuk low-poly sederhana, flat shading, warna polos, dan garis tepi tinta tebal di sekeliling objek. Siapkan file ini sebagai file gaya:
1. Hapus semua objek bawaan (kubus, kamera, lampu).
2. Atur unit scene ke Metric, unit scale 1.0.
3. Buat material Principled BSDF berikut, Roughness 0.8, Metallic 0, nama persis seperti ini:
   mat_tinta #1F2A36, mat_kertas #FBF6EA, mat_kraft #E7D9BA, mat_kraft_tua #CDB68A, mat_oranye #F07F3C, mat_oker #E2A72E, mat_laut #2F6E8F, mat_hutan #2E7A47, mat_peta_hijau #A9C79A, mat_batubara #4B4F58, mat_batubara_terang #7A808A, mat_emas #C99A2E (Metallic 0.3), mat_minyak #5B4BB5, mat_uranium #4FA83A.
4. Buat material mat_air: Base Color #9CC3D3, Alpha 0.6, Blend Mode Alpha Blend.
5. Buat material mat_uranium_nyala: Base Color #39FF14, Emission #39FF14 kekuatan 4.
6. Buat material mat_matahari: Base Color #F2B33D, Emission #F2B33D kekuatan 3.
7. Beri setiap material fake user supaya tidak hilang saat tidak dipakai.
8. Buat fungsi Python kecil tambah_outline(obj, tebal) yang: menduplikasi obj menjadi <nama>_outline, membalik semua face salinan itu (normal menghadap ke dalam), memberi modifier Displace arah Normal dengan mid level 0 dan strength -tebal (karena normal sudah dibalik, nilai negatif mendorong ke luar), mengganti semua material duplikat menjadi mat_tinta, lalu menjadikannya child dari obj. Simpan fungsi ini sebagai text block bernama energo_outline.py supaya bisa dipakai lagi.
9. Tambahkan satu plane 2 x 2 meter bernama ref_tanah dengan mat_peta_hijau, sebagai acuan ukuran.
Setelah selesai, tampilkan daftar material dan isi energo_outline.py.
```

Setelah itu simpan file. Untuk setiap aset baru: buat file baru, lalu minta Claude menautkan (append) material dari `energo_gaya.blend`, atau kerjakan semua aset di file ini dalam koleksi terpisah per aset.

## Blender MCP: prompt per aset

Ada 12 file model untuk build sekarang ditambah 2 untuk Desa Teknik. Kirim satu blok per pesan, lalu minta screenshot viewport sebelum lanjut ke aset berikutnya.

| Aset | File | Ukuran | Segitiga |
| --- | --- | --- | --- |
| Matahari | `sda_matahari.fbx` | 1 m | ≤ 600 |
| Batu bara tahap 1–4 | `sda_batubara_1..4.fbx` | 0,5 m → 0,15 m | ≤ 400 per tahap |
| Endapan emas | `sda_emas_endapan.fbx` | 0,6 m | ≤ 800 |
| Dulang | `sda_dulang.fbx` | 0,5 m | ≤ 1.000 |
| Keping emas besar dan kecil | `sda_emas_keping_besar.fbx`, `sda_emas_keping_kecil.fbx` | 3 cm, 1,5 cm | ≤ 60 |
| Pompa angguk | `sda_pompa.fbx` | 3,5 × 3 m | ≤ 2.500 |
| Bijih uranium | `sda_uranium_bijih.fbx` | 0,4 m | ≤ 600 |
| Serpihan uranium | `sda_uranium_serpihan.fbx` | 0,12 m | ≤ 150 |
| Rumah desa (direncanakan) | `desa_rumah.fbx` | 3 m | ≤ 1.500 |
| Pembangkit (direncanakan) | `desa_pembangkit.fbx` | 6 m | ≤ 3.000 |

### Matahari

```text
Buat objek matahari low-poly bernama sda_matahari: icosphere subdivisions 2, diameter 1 meter, flat shading, material mat_matahari. Tambahkan 8 sinar berbentuk prisma segitiga pipih (panjang 0.35 m) yang menempel di permukaan dan menghadap keluar secara merata, material mat_oker. Gabungkan semua menjadi satu objek, origin di tengah bola (bukan di dasar, karena objek ini melayang). Jalankan tambah_outline(sda_matahari, 0.02). Tampilkan screenshot viewport.
```

### Batu bara (4 tahap pecah)

```text
Buat 4 objek bongkahan batu bara low-poly yang menunjukkan tahap pecah, berjajar di sumbu X dengan jarak 0.8 m, flat shading:
- sda_batubara_1: bongkahan utuh, sekitar 0.5 m, bentuk tak beraturan dari icosphere subdivisions 1 yang vertex-nya digeser acak (seed tetap), material mat_batubara dengan beberapa face mat_batubara_terang sebagai kilau.
- sda_batubara_2: bongkahan yang sama tapi retak, dipecah menjadi 3 pecahan besar yang sedikit terpisah (celah 2 cm).
- sda_batubara_3: 6 sampai 8 butiran kecil (5 sampai 10 cm) yang menumpuk rendah.
- sda_batubara_4: lapisan serbuk tipis berupa 15 butiran sangat kecil (2 cm) di atas lingkaran datar diameter 0.4 m.
Origin setiap objek di tengah dasarnya. Jalankan tambah_outline dengan tebal 0.008 untuk tahap 1 dan 2, 0.004 untuk tahap 3, dan tanpa outline untuk tahap 4. Tampilkan screenshot viewport.
```

### Endapan emas

```text
Buat objek sda_emas_endapan: gundukan pasir low-poly diameter 0.6 m, tinggi 0.15 m, dari grid yang ditarik ke atas di tengah, material mat_kraft, flat shading. Sisipkan 5 keping emas kecil (bentuk icosphere subdivisions 0 yang dipipihkan, 2 sampai 4 cm, material mat_emas) yang setengah terbenam di permukaan. Gabungkan menjadi satu objek, origin di tengah dasar. Jalankan tambah_outline(sda_emas_endapan, 0.008). Tampilkan screenshot viewport.
```

### Dulang

```text
Buat objek dulang pendulang emas tradisional dari kayu bernama sda_dulang: mangkuk lebar dan dangkal berbentuk kerucut terbalik, diameter 0.5 m, kedalaman 0.08 m, 16 segmen keliling, tebal dinding 1 cm, material mat_kraft_tua di bagian luar dan mat_kraft di bagian dalam. Buat objek terpisah sda_dulang_air: piringan datar diameter 0.36 m di dalam mangkuk, 1 cm di atas dasar, material mat_air. Buat objek terpisah sda_dulang_pasir: lapisan tipis tidak rata diameter 0.32 m, material mat_kraft. Ketiganya berbagi origin di tengah dasar mangkuk. Jalankan tambah_outline(sda_dulang, 0.006) saja (air dan pasir tanpa outline). Tampilkan screenshot viewport.
```

### Keping emas

```text
Buat 2 objek keping emas low-poly dengan flat shading dan material mat_emas:
- sda_emas_keping_besar: bongkah bersudut sekitar 3 cm, dari icosphere subdivisions 0 yang dipipihkan 40 persen dan vertex-nya digeser sedikit.
- sda_emas_keping_kecil: serpih pipih sekitar 1.5 cm, dari cylinder 5 sisi yang sangat tipis.
Origin di tengah masing-masing objek. Jalankan tambah_outline dengan tebal 0.002 untuk keping besar saja. Tampilkan screenshot viewport dengan zoom dekat.
```

### Pompa angguk

Model akhir sudah dibuat di `Art/energo_gaya.blend` (koleksi `sda_pompa`) mengikuti referensi pengguna, jadi prompt di bawah hanya titik awal.

```text
Buat pompa angguk minyak (pump jack) low-poly, tinggi total sekitar 2.5 m, flat shading, sebagai beberapa objek terpisah dalam satu koleksi sda_pompa:
- sda_pompa_alas: alas balok 2.4 x 0.6 x 0.15 m, material mat_tinta.
- sda_pompa_tiang: rangka segitiga (A-frame) dari 2 balok miring setinggi 1.6 m, material mat_laut.
- sda_pompa_lengan: balok mendatar 2.2 x 0.15 x 0.15 m di atas tiang, material mat_minyak. Di ujung depannya ada kepala kuda melengkung (bentuk setengah lingkaran pipih 0.5 m), material mat_minyak. Letakkan ORIGIN objek ini tepat di titik poros di puncak tiang, supaya bisa diputar naik turun dari Unity.
- sda_pompa_pemberat: balok 0.4 x 0.4 x 0.3 m di ujung belakang, material mat_tinta, jadikan child dari sda_pompa_lengan.
- sda_pompa_motor: kotak 0.5 x 0.4 x 0.4 m di belakang alas, material mat_oranye.
- sda_pompa_sumur: silinder 8 sisi diameter 0.15 m setinggi 0.4 m di bawah kepala kuda, material mat_tinta.
Origin seluruh rakitan (objek induk kosong sda_pompa) di tengah dasar alas. Jalankan tambah_outline dengan tebal 0.02 untuk tiang, lengan, dan motor (outline lengan otomatis ikut bergerak karena menjadi child-nya). Tampilkan screenshot viewport dari samping.
```

### Bijih dan serpihan uranium

```text
Buat 2 objek kristal uranium low-poly dengan flat shading:
- sda_uranium_bijih: batuan bersudut sekitar 0.4 m dari icosphere subdivisions 1 yang vertex-nya digeser acak (seed tetap), material mat_batubara_terang. Tancapkan 5 kristal prisma heksagonal (tinggi 8 sampai 15 cm, ujung meruncing) yang mencuat miring dari permukaan, material mat_uranium_nyala. Origin di tengah dasar. Jalankan tambah_outline(sda_uranium_bijih, 0.006).
- sda_uranium_serpihan: satu kristal prisma heksagonal tunggal tinggi 0.12 m dengan kedua ujung meruncing, material mat_uranium_nyala, origin di tengah objek, tanpa outline supaya cahayanya tetap menonjol.
Tampilkan screenshot viewport.
```

### Rumah desa (direncanakan)

```text
Buat rumah desa low-poly bernama desa_rumah dengan flat shading: badan rumah 3 x 2.5 x 2 m material mat_kertas, atap pelana material mat_oranye, satu pintu material mat_tinta, dan dua jendela sebagai objek terpisah desa_rumah_jendela dengan material mat_oker (supaya di Unity bisa diganti antara menyala dan padam). Origin di tengah dasar. Jalankan tambah_outline(desa_rumah, 0.03). Tampilkan screenshot viewport.
```

### Pembangkit listrik (direncanakan)

```text
Buat bangunan pembangkit listrik low-poly bernama desa_pembangkit dengan flat shading: gedung utama 5 x 4 x 3.5 m dengan atap gergaji (3 gigi) material mat_kertas dan mat_laut, cerobong silinder 8 sisi setinggi 6 m material mat_tinta, dan satu panel lambang petir di dinding depan sebagai objek terpisah desa_pembangkit_lampu dengan material mat_oker. Origin di tengah dasar. Jalankan tambah_outline(desa_pembangkit, 0.04). Tampilkan screenshot viewport.
```

## Blender MCP: ekspor ke Unity

Ekspor setiap aset sebagai FBX terpisah dengan transform yang sudah di-apply, lalu atur materialnya di Unity ke URP. Prompt ekspor yang sama dipakai untuk semua aset, cukup ganti nama koleksi dan file.

```text
Siapkan koleksi sda_pompa untuk diekspor ke Unity:
1. Apply rotation dan scale pada semua objek di koleksi ini (location jangan di-apply untuk objek yang origin-nya dipakai sebagai poros).
2. Pastikan semua face menghadap keluar (recalculate normals outside).
3. Ekspor hanya objek di koleksi ini sebagai FBX ke //export/sda_pompa.fbx dengan pengaturan: Apply Scalings = FBX All, Forward = -Z Forward, Up = Y Up, Apply Unit, Apply Transform aktif, hanya Mesh dan Empty, Apply Modifiers aktif, tanpa kamera dan lampu, tanpa animasi.
4. Laporkan jumlah segitiga per objek.
```

Pengaturan di Unity setelah file masuk ke `Assets/Models/`:

| Bagian | Pengaturan |
| --- | --- |
| Model | Scale Factor 1, Convert Units aktif, Read/Write mati, Mesh Compression Low |
| Material | Extract Materials, lalu ganti shader ke URP/Lit (Smoothness 0,2) |
| Outline (`*_outline`) | Satu material bersama `mat_tinta`: URP/Unlit, warna #1F2A36, Render Face = Front (bawaan). Face outline sudah dibalik di Blender, jadi Front hanya menggambar sisi belakang cangkang; Back justru membuat outline menutupi objeknya. Teruji di Unity dengan batu bara, 3 Nov 2026 |
| Matahari dan uranium | Shader URP/Unlit, warna emisi sesuai palet, supaya tetap terang di kamera gelap |
| Air dulang | URP/Lit, Surface Type Transparent |
| Collider | Tambahkan Sphere atau Box Collider di prefab untuk objek yang diketuk (bongkahan batu bara, endapan emas, serpihan uranium) |
| Prefab | Satu prefab per SDA di `Assets/Resources/` mengikuti nama yang sekarang dipanggil kode |

Catatan untuk aset bergerak:

- **Pompa angguk**: gerak dihitung `PompaPose` dari sudut engkol, jadi semua bagian sinkron: engkol berputar, batang belakang (`sda_pompa_pitman`) panjangnya tetap, lengan berayun pada sumbu Z model, batang pompa (`sda_pompa_tali`) naik-turun, kabel (`sda_pompa_kabel`) memanjang lewat skala Y. Tiap tekan dan lepas jari memutar engkol setengah putaran. Kepala kuda ada di sisi −X model di Unity. Saat mengekspor: objek induk `sda_pompa` dilepas dulu dari bagian-bagiannya, atau outline ikut terputar 90° di Unity.
- **Batu bara**: empat tahap jadi empat child di satu prefab; script mengaktifkan satu child sesuai jumlah ketukan.
- **Keping emas**: 4 besar dan 14 kecil diletakkan oleh script di atas `sda_dulang_pasir`, jadi cukup dua prefab keping.
- **Jendela desa dan lampu pembangkit**: ganti material antara menyala (`mat_oker` beremisi) dan padam (`mat_tinta`) dari script.
- **Outline saat objek jauh**: kalau outline terlihat terlalu tipis atau terlalu tebal di kamera AR, ubah tebal Displace-nya di Blender (jalankan ulang `tambah_outline` dengan tebal baru) lalu ekspor ulang, jangan atur skala objek `*_outline` di prefab karena tebalnya jadi tidak rata.

## ElevenLabs: aturan umum

Semua suara dibuat dengan fitur Sound Effects model `eleven_text_to_sound_v3`, durasi selalu diatur manual, dan prompt ditulis dalam bahasa Inggris supaya lebih akurat. Batasan dari [dokumentasi ElevenLabs](https://elevenlabs.io/docs/overview/capabilities/sound-effects):

| Hal | Nilai |
| --- | --- |
| Durasi per generate | 0,1–30 detik |
| Loop mulus | Ada, untuk suara latar seperti sungai |
| Format | MP3 untuk semua; WAV 48 kHz hanya untuk yang tidak di-loop |
| Biaya | 40 kredit per detik kalau durasi diatur |
| Prompt influence | Tinggi = mengikuti prompt persis; rendah = lebih bervariasi |
| Lisensi komersial | Tidak dijelaskan di halaman itu; cek syarat paketmu sebelum rilis |

Karakter suara Jurnal Ekspedisi: suara UI berasal dari perlengkapan penjelajah, yaitu kertas, pensil, cap karet, kompas kuningan, dan kotak kayu. Semuanya pendek, kering, dan nyata. Hadiah dan jawaban benar diberi denting kecil glockenspiel atau kalimba supaya terasa menyenangkan. Suara mini-game memakai bunyi alam dan benda nyata (batu, air, logam). Hindari suara elektronik atau sci-fi.

Cara kerja per suara:

1. Salin prompt, atur durasi, prompt influence sekitar 0,6, lalu generate 3–4 variasi.
2. Pilih yang paling pas, potong hening di awal dan akhir.
3. Samakan kerasnya: semua suara UI kira-kira sama keras, suara gagal sedikit lebih menonjol, suara latar paling pelan.
4. Simpan dengan nama di kolom File supaya cocok dengan nama yang dipanggil kode sekarang.

Total durasi semua suara di tabel berikut sekitar 54 detik, jadi satu putaran generate (1 variasi per suara) butuh sekitar 2.150 kredit; 3 variasi sekitar 6.500 kredit.

## ElevenLabs: prompt per suara

Ada 21 suara: 13 mengisi nama file atau peran yang sudah ada di kode, 8 baru (ditandai "baru"). Klik Geiger tetap dibuat dari kode, dan musik lobby tidak ada di sini karena fitur Sound Effects hanya sampai 30 detik.

| File | Kapan | Durasi | Loop | Prompt |
| --- | --- | --- | --- | --- |
| `sfx_splash` | Splash | 2,5 dtk | Tidak | A leather field notebook opening, quick page flutter, then a soft bright glockenspiel chime, cheerful adventure start |
| `sfx_button_click` | Semua tombol | 0,3 dtk | Tidak | Single short pencil tap on a hardcover notebook, dry UI click, no reverb |
| `sfx_button_back` | Tombol kembali, tutup sheet | 0,3 dtk | Tidak | Single quick paper page slide and soft flip, UI back sound, dry, no reverb |
| `sfx_radar_ping` (baru) | SDA masuk jarak tangkap | 0,8 dtk | Tidak | Small brass compass bell ding, single clear note, short decay, friendly notification |
| `sfx_hitung_mundur` (baru) | Hitung mundur 3-2-1 dulang | 0,4 dtk | Tidak | Single mechanical stopwatch tick, crisp and dry |
| `sfx_matahari` | Matahari, di-pan ke arah cahaya | 10 dtk | Ya | Warm airy shimmering hum with soft high chimes, sunny energy ambience, continuous, no birds, no music |
| `sfx_ekstrak_matahari` | Bar cahaya terisi | 3 dtk | Tidak | Rising warm sparkle hum, energy slowly gathering, soft and bright |
| `sfx_bara_pukul` | Ketukan bongkahan (diputar saat ujung pickaxe kena, nada diacak ±8%) | 0,4 dtk | Tidak | Small rock hammer hitting a lump of coal, dry stone crack, short and punchy |
| `sfx_bara_hancur` | Bongkahan hancur | 1 dtk | Tidak | Lump of coal shattering into small pieces, gravel scattering, short |
| `sfx_sungai` | Sepanjang scene emas | 15 dtk | Ya | Gentle shallow river flowing over pebbles, calm and steady, continuous, no birds |
| `sfx_aliran_air` | Mendulang, volume ikut putaran | 6 dtk | Ya | Water and sand swirling in a shallow wooden pan, gold panning, steady sloshing, continuous |
| `sfx_dulang_emas` | Keping emas tumpah | 0,5 dtk | Tidak | Tiny gold flake bouncing out of a wooden pan onto stone, small bright metallic tink |
| `sfx_bor` (baru) | Menahan bor minyak | 5 dtk | Ya | Steady low drilling rumble into earth, mechanical grinding, continuous |
| `sfx_pompa_detak` | Pompa tepat irama | 0,6 dtk | Tidak | Single heavy mechanical pump stroke, metal clunk with short oily hiss |
| `sfx_blowout` (baru) | Blowout, kena air, overpressure | 1,5 dtk | Tidak | Sudden gas blowout burst, loud pressurized hiss and whoosh, short |
| `sfx_jepit` | Mulai menjepit serpihan (baru) | 0,4 dtk | Tidak | Metal field tongs clamping shut, small precise mechanical click |
| `sfx_serpihan_masuk` | Serpihan masuk wadah timbal (baru) | 0,7 dtk | Tidak | Small crystal dropped into a heavy lead container, muffled thud then lid clank |
| `sfx_quiz_benar` | Jawaban benar, cap BENAR | 0,8 dtk | Tidak | Firm rubber stamp pressed onto paper, followed by two quick ascending glockenspiel notes, happy |
| `sfx_quiz_salah` | Jawaban salah, waktu habis, cap SALAH | 0,8 dtk | Tidak | Firm rubber stamp pressed onto paper, followed by one low soft wooden knock, gentle wrong answer |
| `sfx_gagal` | Gagal mini-game (baru, menggantikan sfx\_quiz\_salah di sini) | 1,2 dtk | Tidak | Sheet of paper crumpled quickly, then a short descending kalimba phrase, gentle, not scary |
| `sfx_hasil_sempurna` (baru) | Hasil Sempurna, cap MURNI | 2,5 dtk | Tidak | Three rubber stamps in quick rhythm on paper, then a bright glockenspiel flourish, rewarding |

`sfx_pickaxe` sudah dihapus (3 Nov 2026). `sfx_batu_jatuh` masih dipakai sebagai detak pompa sampai `sfx_pompa_detak` ada.

Catatan percobaan 3 Nov 2026: generate otomatis lewat API (model `eleven_text_to_sound_v3`, prompt di tabel ini, variasi dipilih dari analisis sinyal tanpa didengar) dinilai jelek oleh tim, untuk suara batu bara maupun klik dan kuis, jadi semuanya dibatalkan. Suara harus dipilih dengan didengar langsung. Durasi di bawah 1 dtk juga sering menghasilkan file hampir hening; generate 1 dtk lalu potong. Suara loop dibuat dengan opsi loop dan diekspor MP3; selain itu WAV.

## Import audio di Unity dan checklist

Atur import per jenis suara supaya hemat memori dan tidak ada jeda saat diputar. Simpan semua di `Assets/Audio/` (atau folder Resources yang sekarang dipakai kode).

| Jenis | Load Type | Format | Lainnya |
| --- | --- | --- | --- |
| One-shot UI dan mini-game | Decompress On Load | Vorbis, Quality 70 | Force To Mono aktif |
| Loop pendek (matahari, aliran air, bor) | Compressed In Memory | Vorbis, Quality 60 | Force To Mono aktif (matahari wajib mono karena di-pan) |
| Loop panjang (sungai) | Streaming | Vorbis, Quality 50 | Force To Mono aktif |

Setelah import, putar setiap loop beberapa kali di Unity untuk memastikan sambungannya tidak terdengar klik.

Checklist:

- [ ] Kunci gaya final: Jurnal Ekspedisi
- [ ] Buat `energo_gaya.blend` dengan semua material
- [ ] Matahari
- [ ] Batu bara tahap 1–4
- [ ] Endapan emas, dulang, keping besar dan kecil
- [ ] Pompa angguk (cek poros lengan di Unity)
- [ ] Bijih dan serpihan uranium
- [ ] Ekspor semua FBX dan buat prefab di Unity
- [ ] Generate 21 suara di ElevenLabs dan pilih variasi terbaik
- [ ] Samakan kerasnya dan import dengan pengaturan di atas
- [ ] Ganti placeholder detak pompa dan hapus `sfx_batu_jatuh`, `sfx_pickaxe`
- [ ] Cek lisensi komersial paket ElevenLabs
- [ ] (Nanti) Rumah desa dan pembangkit untuk Desa Teknik
