# HOPE PD SkyNews

Sistem otomasi siaran cepat berbasis overlay transparan untuk kepolisian di platform FiveM. Aplikasi ini dirancang secara khusus untuk memfasilitasi pengiriman pesan peringatan darurat dan status siaga kota secara instan menggunakan integrasi tombol pintas tingkat perangkat keras (Hardware Scan Code).

## Deskripsi Sistem

HOPE PD SkyNews menyediakan antarmuka minimalis bergaya ReShade yang mengapung di atas klien permainan FiveM tanpa mengganggu fokus visual atau interaksi *gameplay*. Dengan memanfaatkan pengalihan input tingkat rendah, personel kepolisian dapat mengirimkan teks siaran panjang secara konsisten dan aman di tengah dinamika penegakan hukum virtual.

## Fitur Utama

- **Overlay Transparan & Interaktif** — Menampilkan indikator status siaga dan hitung mundur jeda pengiriman tanpa menghalangi pandangan.
- **Mode Transparansi Pasif (Click-Through)** — Menjamin klik *mouse* diteruskan langsung ke dalam permainan saat antarmuka disembunyikan.
- **Injeksi Scan Code Perangkat Keras** — Menerjemahkan ketikan secara langsung ke *Hardware Scan Code* untuk menjamin kompatibilitas penuh dengan DirectInput dan mesin permainan FiveM.
- **Manajemen Tombol Pintas Dinamis** — Mendaftarkan dan melepas kaitan input secara *real-time* tanpa memerlukan memori residu atau proses *restart*.
- **Keamanan Anti-Spam (Rate Limiter)** — Mengimplementasikan jeda antar-pesan terkelola untuk mencegah pengiriman berulang yang tidak disengaja.

## Informasi Keamanan & Kompatibilitas Anti-Cheat

Aplikasi ini dikembangkan dengan mematuhi standar integritas eksekusi pada lingkungan permainan multipemain:
1. **Bukan Perangkat Lunak Curang (Non-Cheat)**: Sistem ini **tidak** membaca, memodifikasi, atau menginjeksi memori internal proses FiveM (`gta-core.dll` atau sejenisnya). Operasi murni sebatas emulasi masukan papan ketik standar Windows API.
2. **Bebas Malware/Virus**: Binari dikompilasi secara mandiri menggunakan rantai alat resmi Microsoft .NET 6.0. Seluruh kode sumber bersifat terbuka dan dapat diaudit secara transparan oleh komunitas maupun pengelola peladen.
3. **Penyimpanan Konfigurasi Standar**: Pengaturan disimpan secara lokal pada direktori standar Windows (`%APPDATA%\HopePDSkyNews\config.json`), menghindari penulisan berkas mencurigakan pada direktori sistem.

## Panduan Instalasi

### Menggunakan Installer (Rekomendasi)
1. Unduh berkas instalasi terbaru `HopePDSkyNews_Setup_v1.0.0.exe` dari halaman rilis resmi.
2. Jalankan berkas penyiapan dan ikuti arahan petunjuk pada layar.
3. Aplikasi akan secara otomatis mendaftarkan pintasan program dan entri mulai otomatis (opsional) pada sistem operasi Windows Anda.

### Menjalankan Binari Mandiri
1. Ekstrak arsip rilis ke direktori pilihan Anda.
2. Jalankan berkas eksekusi `HopePDSkyNews.exe`.

## Panduan Penggunaan

### Konfigurasi Awal
1. Saat aplikasi dijalankan, sebuah ikon akan muncul pada *System Tray* Windows.
2. Buka permainan FiveM; aplikasi akan mendeteksi proses secara otomatis dan menampilkan lencana status di sudut kiri atas layar.
3. Tekan tombol **F10** untuk membuka atau menutup panel interaktif utama.

### Menyesuaikan Tombol Pintas
1. Buka panel antarmuka dengan menekan **F10** atau melalui menu klik kanan pada ikon *System Tray*.
2. Navigasikan ke bilah **Tombol Pintas**.
3. Klik tombol penambahan, masukkan kombinasi masukan yang diinginkan, dan tautkan dengan templat siaran yang tersedia.
4. Klik **Simpan** untuk menerapkan pemetaan secara instan.

### Mode Operasi
Sistem mendukung dua mode sasaran keluaran pesan:
- **Mode Produksi**: Menggunakan perintah `/info` untuk penyiaran publik ke seluruh penjuru kota.
- **Mode Pengujian**: Menggunakan perintah `/me` untuk memverifikasi format teks secara lokal tanpa mengganggu saluran publik.

Mode ini dapat dialihkan secara langsung melalui bilah **Pengaturan Umum**.

## Kompilasi Mandiri

Untuk membangun aplikasi dari kode sumber, pastikan Anda telah memasang .NET 6.0 SDK:

```powershell
# Mengunduh repositori
git clone https://github.com/Gustyx-Power/HPD-Tools.git
cd HPD-Tools/FiveMPoliceOverlay

# Membangun solusi
dotnet build -c Release

# Menerbitkan binari teroptimasi
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## Lisensi & Kontribusi

HOPE PD SkyNews didistribusikan di bawah lisensi resmi dan terbuka untuk penyempurnaan dari para pengembang komunitas penegak hukum virtual.
Silakan merujuk pada berkas `CONTRIBUTING.md` dan `SECURITY.md` untuk informasi tata cara berkontribusi dan pelaporan isu keamanan.

---
**Pengembang Utama**: Gustyx-Power  
**Versi Distribusi**: 1.0.0  
**Hak Cipta © 2026 Xtra Manager Softwares Community**
