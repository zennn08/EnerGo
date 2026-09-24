# ⚡ EnerGo

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-blue?logo=unity&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Android%20%7C%20iOS%20%7C%20Editor-green)
![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-orange)
![License](https://img.shields.io/badge/License-MIT-lightgrey)

**Game Edukasi Augmented Reality (AR) Interaktif untuk Eksplorasi Sumber Daya & Energi**

*Jelajahi Sumber Daya · Temukan Energi · Pelajari Sainsnya*

</div>

---

## 📖 Tentang EnerGo

**EnerGo** adalah game mobile berbasis **Augmented Reality (AR)** dan **Sensor 360°** yang menggabungkan keseruan berburu sumber daya (seperti Energi Surya, Batu Bara, dan Emas) dengan kuis edukatif interaktif. 

Pemain diajak untuk mendeteksi target mineral di dunia nyata, mengumpulkannya lewat interaksi tap spasial, lalu menganalisis fakta ilmiahnya melalui sistem **Swipeable Card Quiz** modern (gaya Tinder-swipe).

---

## ✨ Fitur Utama

- 🌐 **Dual Tracking Mode**:
  - **ARCore / AR Spasial**: Deteksi permukaan dunia nyata untuk menempatkan target 3D secara presisi.
  - **Gyro 360° & Manual Fallback**: Tetap bisa dimainkan di perangkat tanpa dukungan ARCore melalui giroskop perangkat atau drag manual layar.
- 🎯 **Radar & Direction HUD**:
  - Mini radar dan indikator arah 360° yang membantu pemain menemukan target mineral di sekitarnya secara intuitif.
- 🃏 **Modern Swipeable Card Quiz**:
  - Interaksi kuis interaktif berbasis gesture swipe:
    - 👈 **Swipe Kiri** $\rightarrow$ **BENAR** (Indikator Hijau)
    - 👉 **Swipe Kanan** $\rightarrow$ **SALAH** (Indikator Merah)
  - Dilengkapi fisika rotasi dinamis, visual feedback realtime, dan animasi flick/snap-back.
- 💎 **Variasi Sumber Daya & Edukasi Sains**:
  - **Energi Surya (Sun)**
  - **Batu Bara (Coal)**
  - **Emas (Gold)**
  - Pertanyaan diverifikasi dari sumber terpercaya (EIA, USGS).
- 🎨 **Modern Minimalist UI**:
  - Tampilan lobby futuristik yang rapi, transisi halus, dan menu pengaturan/debug tersembunyi yang fleksibel.

---

## 🛠️ Tech Stack & Kebutuhan Sistem

- **Game Engine**: Unity 2022.3 LTS (atau versi yang lebih baru)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **AR Framework**: AR Foundation & Google ARCore XR Plugin
- **Input System**: Unity New Input System
- **Bahasa**: C# (.NET Standard 2.1)

---

## 📂 Struktur Direktori Proyek

```text
EnerGo/
├── Assets/
│   ├── EnerGo/
│   │   ├── Resources/          # Texture, Icon, UI Assets (bg_lobby, play button, logo)
│   │   ├── Scripts/            # Core Gameplay Logic
│   │   │   ├── ARCapturePrototype.cs   # AR, Gyro 360, Radar HUD, & Swipe Card Quiz
│   │   │   ├── LobbyController.cs      # Flow Lobby, Splash Screen, & Debug Menu
│   │   │   ├── EnerGoProgress.cs       # State Manager, Inventory & Session Data
│   │   │   └── QuizBank.cs             # Bank Data Soal & Fakta Edukatif
│   │   ├── Textures/           # Asset Material & Shaders
│   │   ├── ARPrototype.unity   # Scene Utama AR & Gameplay
│   │   ├── LobbyScene.unity    # Scene Menu Utama
│   │   └── Sensor360Scene.unity# Fallback Scene Sensor 360
│   └── Settings/               # Konfigurasi URP & Graphics
└── ProjectSettings/            # Konfigurasi Build & Input System
```

---

## 🚀 Cara Menjalankan Proyek

### 1. Clone Repository
```bash
git clone https://github.com/zennn08/EnerGo.git
cd EnerGo
```

### 2. Buka di Unity Editor
1. Buka **Unity Hub**.
2. Klik **Add** dan pilih folder proyek `EnerGo`.
3. Gunakan editor Unity dengan modul **Android Build Support** (termasuk OpenJDK & Android SDK/NDK).

### 3. Konfigurasi Scene
Pastikan urutan scene pada **Build Settings** (`File` > `Build Settings`):
1. `Assets/EnerGo/LobbyScene.unity` (Index 0)
2. `Assets/EnerGo/ARPrototype.unity` (Index 1)

### 4. Menjalankan di Editor
- Buka `LobbyScene.unity` lalu tekan tombol **Play** di Unity Editor.
- Anda dapat menguji gameplay, simulasi putaran pandangan menggunakan drag mouse, dan menguji swipe kuis menggunakan klik-kiri mouse.

---

## 🎮 Kontrol & Cara Bermain

1. **Di Lobby**:
   - Tekan **PLAY** untuk memulai petualangan.
   - Gunakan tombol **DEBUG / SETTINGS** untuk memilih resource spesifik atau mengganti mode tracking.
2. **Saat Berburu (AR / 360°)**:
   - Gerakkan ponsel di sekitar Anda atau gunakan drag layar untuk mencari target mineral.
   - Ikuti petunjuk panah pada **Radar HUD** jika target berada di luar bidang pandang.
   - Tap mineral sebanyak 5 kali untuk memulai ekstraksi energi.
3. **Menganalisis (Swipe Card Quiz)**:
   - Baca pernyataan ilmiah pada kartu.
   - Tarik kartu ke **kiri** jika pernyataan **BENAR**.
   - Tarik kartu ke **kanan** jika pernyataan **SALAH**.
   - Dapatkan unit energi tambahan dan pelajari penjelasannya!

---

## 📄 Lisensi

Proyek ini dirilis di bawah lisensi [MIT License](LICENSE).

---

<div align="center">
Dibuat dengan ❤️ untuk pembelajaran sains & energi interaktif.
</div>
