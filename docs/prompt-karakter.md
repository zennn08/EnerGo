# EnerGo – Prompt Karakter Pemandu

Prompt generator gambar untuk karakter pemandu (ahli geologi). Gaya: chibi, mengikuti gambar acuan dari pengguna.

## Cara pakai

1. Lampirkan gambar acuan sebagai `image_1.png` di setiap prompt.
2. Buat lembar acuan dulu, lalu pakai hasilnya sebagai acuan tambahan kalau generator menerima lebih dari satu gambar.
3. Crop karakternya, lalu simpan sebagai PNG transparan dengan nama file yang tertulis. Hapus hanya putih yang tersambung ke tepi gambar (mode contiguous), supaya kemeja dan garis pantul yang putih tidak ikut bolong.
4. Pose menunjuk dan pose lain bisa dibalik kiri-kanan di game, karena tidak ada tulisan di gambar.

Latar putih polos dipakai, tanpa rumput dan bintang seperti gambar acuan. Garis tepi utuh dan jarak kosong di sekeliling karakter membuat gambarnya mudah di-crop dan ditaruh di layar mana pun.

## Lembar acuan

`pemandu_acuan`

```text
Character reference sheet of the explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game art with bold dark brown outlines and soft cel-shading. Three full body views side by side at the same height: front, side, and back. Standing straight in a neutral friendly pose with arms slightly away from the body, holding nothing, with a wide empty gap between the views so each can be cropped separately. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

## Pose wajib

### Melambai menyapa

`pemandu_melambai` · Perkenalan prolog, Lobby

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: waving hello with one arm raised high and the other hand holding the clipboard against her chest, body turned slightly, weight on one leg. Expression: wide joyful open-mouth smile. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Menjelaskan

`pemandu_menjelaskan` · Panduan, tutorial, dialog umum

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: explaining something with one hand raised open at shoulder height, palm up, the other arm holding the clipboard against her chest. Expression: friendly, mouth open as if talking, eyebrows lifted. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Menunjuk ke samping

`pemandu_menunjuk` · Mengarahkan ke tombol atau pembangkit

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: arm stretched out to one side pointing at something beside her, head turned toward the same side, the other hand on her hip, leaning slightly toward where she points. Expression: bright, encouraging smile. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Berpikir

`pemandu_berpikir` · Pembuka kuis, memuat data

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: one hand on her chin, the other holding the clipboard up and looking at it, head tilted, one foot slightly forward. Expression: curious and thoughtful, eyes looking sideways, small closed-mouth smile. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Bersorak

`pemandu_bersorak` · Hasil "Sempurna", desa menyala

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: jumping for joy with both feet off the ground and both arms thrown up, the rock hammer held high in one hand. Expression: eyes happily closed in curved arcs, huge open-mouth smile. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Menyemangati

`pemandu_menyemangati` · Hasil "Coba Lagi", gagal mini-game

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: both hands in small fists in front of her chest in a "you can do it" gesture, leaning slightly forward, feet together. Expression: warm, kind smile with slightly raised inner eyebrows. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Memperingatkan

`pemandu_memperingatkan` · Peringatan keselamatan, bahaya uranium dan minyak

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: one arm extended straight forward with an open palm toward the viewer in a stop gesture, the other hand on her hip, feet planted firmly. Expression: serious and focused, eyebrows angled down, mouth a small flat line. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

## Pose tambahan

### Khawatir memegang senter

`pemandu_khawatir` · Desa padam, stok listrik di bawah 3 jam

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: holding a small flashlight in both hands close to her chest with a soft small glow at its lens, shoulders raised, knees slightly together, looking to one side. Expression: worried, eyebrows raised at the center, small wavy mouth. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Mengamati batu

`pemandu_mengamati` · Panduan SDA, penjelasan kuis

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: leaning forward and examining a small grey rock held up in one hand through a small round magnifying glass in the other hand. Rock hammer tucked in her belt. Expression: fascinated, eyes wide, small open "ooh" mouth. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Mengisi pembangkit

`pemandu_mengisi` · Saat memasukkan unit SDA

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: side three-quarter view, stepping forward and holding out a solid golden energy crystal with both hands, the crystal has no glow and no light rays, as if placing it into a machine beside her. Clipboard and hammer not visible. Expression: focused, happy, tongue slightly out. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Duduk santai

`pemandu_santai` · Layar Desa saat terang

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Pose: sitting on a small wooden stool with legs swinging a little, holding a warm cup with both hands, hat tilted slightly back. Expression: content and relaxed, eyes gently closed, soft smile. Full body from hat to boots, centered. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

## Ekspresi untuk dialog

Semua dari dada ke atas, supaya wajah bisa ditukar tanpa geser posisi di kotak dialog.

### Netral ramah

`pemandu_wajah_netral`

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Expression: friendly neutral face, gentle closed-mouth smile, relaxed eyebrows, eyes looking at the viewer. Dialogue portrait from the chest up, facing the viewer, centered, same zoom and head position as the other portraits. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Senang

`pemandu_wajah_senang`

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Expression: very happy face, wide open-mouth smile, eyes bright and sparkling, cheeks extra rosy. Dialogue portrait from the chest up, facing the viewer, centered, same zoom and head position as the other portraits. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Kaget

`pemandu_wajah_kaget`

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Expression: surprised face, eyes wide with small pupils, eyebrows high, small round open mouth. Dialogue portrait from the chest up, facing the viewer, centered, same zoom and head position as the other portraits. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Khawatir

`pemandu_wajah_khawatir`

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Expression: worried face, inner eyebrows raised, eyes looking to the side, small wavy mouth, one small sweat drop by her temple. Dialogue portrait from the chest up, facing the viewer, centered, same zoom and head position as the other portraits. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Berpikir

`pemandu_wajah_berpikir`

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Expression: thoughtful face, one eyebrow raised, eyes looking up to the side, mouth pushed to one side. Dialogue portrait from the chest up, facing the viewer, centered, same zoom and head position as the other portraits. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

### Sedih ringan

`pemandu_wajah_sedih`

```text
The same explorer woman from image_1.png, keeping her exact face, hat, hair and outfit. 2D chibi game character in a highly cute, vibrant chibi game art style with bold dark brown outlines and soft cel-shading: large head, big expressive brown eyes with highlights, rosy cheeks, beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes on the chest and sleeves, white collared shirt, round compass pendant, brown utility belt with a small pouch, loose brown pants, chunky dark brown adventure boots. Simple rounded chibi hands. Expression: gently sad face, eyebrows slanted, eyes slightly lowered, small downturned mouth, no tears. Dialogue portrait from the chest up, facing the viewer, centered, same zoom and head position as the other portraits. Isolated sticker-like cutout: a clean, unbroken bold outline around her whole silhouette so she can be cropped out easily, and every part of her and her props fully inside the frame with a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat. No ground, no grass, no cast shadow, no sparkles, no stars, no particles, no effects, no text or letters anywhere.
```

## Panel prolog

Lampirkan satu gambar pose karakter sebagai `image_1.png`, supaya gaya dan wajahnya sama. Teks cerita ditaruh game di kartu kertas, jadi gambarnya harus tanpa tulisan.

### Radar menangkap sinyal

`prolog_1_perjalanan`

```text
First-person view at night on a forest path in the Indonesian mountains. Two small chibi hands in the lower middle hold a round handheld field radar with a softly glowing green screen and one bright blip. Tall dark trees, a starry deep blue sky, a moon behind thin clouds. Do not show the person's face or body beyond the hands. Keep the hands above the lower third. Keep the lower third of the image calm and simple, because a text card will cover it. 2D chibi game background art in exactly the same style as image_1.png: bold dark brown outlines, soft cel-shading, rich friendly colours, clean rounded shapes. Vertical 9:16 portrait image, full bleed. Keep important things away from the left and right edges. No text, no letters, no logos, no user interface, no watermark.
```

### Desa gelap gulita

`prolog_2_desa_gelap`

```text
Wide view at night of a small traditional Indonesian mountain village with no electric light at all: wooden houses with dark windows, a dirt road leading in, rice fields, palm and banana trees, dark mountains behind, a pale moon. Quiet, a little lonely but still cute, not scary. No people. Keep the lower third of the image calm and simple, because a text card will cover it. 2D chibi game background art in exactly the same style as image_1.png: bold dark brown outlines, soft cel-shading, rich friendly colours, clean rounded shapes. Vertical 9:16 portrait image, full bleed. Keep important things away from the left and right edges. No text, no letters, no logos, no user interface, no watermark.
```

### Pemandu muncul

`prolog_3_pemandu`

```text
The explorer woman from image_1.png, keeping her exact face, hat, hair and outfit: beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes, compass pendant, brown belt with a pouch, loose brown pants, dark brown boots. She stands in the middle of a dark village dirt road at night, full body, holding a small flashlight whose warm beam lights up the ground in front of her, looking at the viewer with a worried but kind face and one hand raised in a small greeting. Dark wooden houses behind her. Place her in the upper two thirds. Keep the lower third of the image calm and simple, because a text card will cover it. 2D chibi game background art in exactly the same style as image_1.png: bold dark brown outlines, soft cel-shading, rich friendly colours, clean rounded shapes. Vertical 9:16 portrait image, full bleed. Keep important things away from the left and right edges. No text, no letters, no logos, no user interface, no watermark.
```

### Memberi nama desa

`prolog_4_nama`

```text
The explorer woman from image_1.png, keeping her exact face, hat, hair and outfit: beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes, compass pendant, brown belt with a pouch, loose brown pants, dark brown boots. Close view from the waist up at night, lit warmly by a small lantern beside her. She smiles and holds out toward the viewer an open blank paper map with hand-drawn contour lines and a small empty label box. The map has no writing. Dark village softly visible behind her. Place her in the upper two thirds. Keep the lower third of the image calm and simple, because a text card will cover it. 2D chibi game background art in exactly the same style as image_1.png: bold dark brown outlines, soft cel-shading, rich friendly colours, clean rounded shapes. Vertical 9:16 portrait image, full bleed. Keep important things away from the left and right edges. No text, no letters, no logos, no user interface, no watermark.
```

### Ajakan mencari SDA

`prolog_5_ajakan`

```text
The explorer woman from image_1.png, keeping her exact face, hat, hair and outfit: beige safari hat with a dark brown band, dark brown bob hair, yellow outdoor jacket with reflective white stripes, compass pendant, brown belt with a pouch, loose brown pants, dark brown boots. Seen from behind and slightly to the side at the very first light of dawn, standing on a small hill at the edge of the village, full body, one arm pointing far away with energy in her pose. The horizon glows soft orange over the mountains, and the dark village lies below. Five tiny glowing points of light are scattered across the far landscape, in yellow, grey, gold, brown and green. Place her in the upper two thirds. Keep the lower third of the image calm and simple, because a text card will cover it. 2D chibi game background art in exactly the same style as image_1.png: bold dark brown outlines, soft cel-shading, rich friendly colours, clean rounded shapes. Vertical 9:16 portrait image, full bleed. Keep important things away from the left and right edges. No text, no letters, no logos, no user interface, no watermark.
```

## Latar desa

Buat `desa_siang` dulu dan pastikan kamu suka, karena dua gambar malam diedit dari gambar itu. Lampirkan satu gambar karakter sebagai acuan gaya untuk `desa_siang`, dan lampirkan `desa_siang` sebagai `image_1.png` untuk dua gambar malam.

Setelah dibuat, tumpuk ketiganya dan cek rumah serta pembangkit tidak bergeser. Kalau bergeser, kirim `desa_siang` saja, karena versi malamnya bisa diturunkan lewat pengolahan warna.

### Siang

`desa_siang`

```text
A peaceful traditional Indonesian mountain village, seen from a slightly raised angle. Wooden traditional houses with thatched or dark wooden roofs and glass windows, a small wooden power house with a metal roof and a short chimney clearly visible right of centre, with thin wooden electric poles and wires running from it to the houses and small lamps along the road. Terraced green rice fields, leafy trees and banana trees, a winding dirt road from the bottom of the image into the village, lush grass, big mountains in the background. Open sky fills the top fifth of the image, and the bottom fifth is only plain grass and road, because interface cards will cover those areas. No people and no animals. Bright sunny day, blue sky with a few soft clouds. 2D chibi game background art in exactly the same style as image_1.png: bold dark brown outlines, soft cel-shading, rich friendly colours, clean rounded shapes. Vertical 9:16 portrait image, full bleed. Keep important things away from the left and right edges. No text, no letters, no logos, no user interface, no watermark.
```

### Malam menyala

`desa_malam_terang`

```text
Edit image_1.png, this exact village picture. Keep the camera, composition, and the position, shape and size of every house, pole, lamp, tree, field, road and mountain exactly the same. Change only the time of day and the lighting. It is now a clear night with a deep blue sky, stars and a moon. All windows of the houses and the power house glow warm yellow, the road lamps are on with soft pools of warm light, and the power house shows a small warm light at its door. Cosy and alive. No text, no letters, no people, no animals, no watermark.
```

### Malam padam

`desa_malam_padam`

```text
Edit image_1.png, this exact village picture. Keep the camera, composition, and the position, shape and size of every house, pole, lamp, tree, field, road and mountain exactly the same. Change only the time of day and the lighting. It is now a clear night with a deep blue sky, stars and a pale moon. Every window is dark and every lamp is off, there is no warm light anywhere, and the village is only faintly lit by cold moonlight. Quiet and a little lonely but still cute, not scary. No text, no letters, no people, no animals, no watermark.
```

## Benda yang digerakkan di game

Dipisah dari latar supaya bisa bergerak sendiri.

### Kerbau

`desa_kerbau`

```text
A cute 2D chibi water buffalo grazing, side view, full body, dark grey skin, curved horns, in the same style as image_1.png: bold dark brown outlines and soft cel-shading. Isolated with a clean, unbroken outline and a wide empty margin on all sides. Pure white background (#FFFFFF), completely flat, no ground, no shadow, no text.
```

### Awan

`desa_awan`. Latarnya biru karena awan putih tidak bisa dipotong dari latar putih.

```text
Three separate fluffy white cartoon clouds of different sizes in a row with wide gaps between them, in the same style as image_1.png: soft cel-shading with pale blue shadows at the bottom and no dark outline. Plain solid flat sky blue background (#5FA8E0), no other objects, no text.
```
