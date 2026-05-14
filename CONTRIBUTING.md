# Panduan Kontribusi (Contributing Guidelines)

Kami sangat menyambut antusiasme dan kontribusi dari para pengembang, desainer antarmuka, serta personel kepolisian virtual untuk bersama-sama mengembangkan **HOPE PD SkyNews**.

Panduan ini bertujuan untuk menyelaraskan proses pengajuan perubahan agar basis kode tetap terstruktur, aman, dan berkinerja tinggi.

## Cara Berkontribusi

### 1. Pelaporan Isu (Bug Reports)
Jika Anda menemukan anomali seperti antarmuka yang terpotong, pengiriman pesan yang terputus, atau ketidaksesuaian fungsi tombol pintas:
- Pastikan isu tersebut belum pernah dilaporkan sebelumnya di halaman *Issues*.
- Buat laporan baru dengan menyertakan versi sistem operasi Windows, resolusi layar permainan FiveM, serta langkah-langkah presisi untuk mereproduksi *bug* tersebut.

### 2. Pengajuan Fitur Baru
Sebelum menghabiskan waktu menulis kode fungsionalitas baru yang masif:
- Diskusikan ide Anda terlebih dahulu dengan membuka *Issue* berlabel **Feature Request**.
- Jelaskan nilai tambah fitur tersebut bagi efisiensi operasional siaran kepolisian di dalam kota.

### 3. Alur Pengajuan Kode (Pull Requests)
1. **Fork** repositori ini ke akun GitHub pribadi Anda.
2. Buat *branch* baru dari *branch* utama (`main` atau `master`) dengan penamaan deskriptif (contoh: `fix/input-timing-delay` atau `feat/custom-opacity-slider`).
3. Lakukan modifikasi kode dengan tetap mematuhi prinsip arsitektur yang bersih (KISS dan DRY) serta pastikan penulisan berbasis C# 10 / .NET 6.0 standar.
4. Uji perubahan Anda secara lokal di dalam klien FiveM untuk memastikan tidak ada penurunan *frame rate* atau penutupan paksa (*crash*).
5. Ajukan **Pull Request (PR)** dengan menyertakan tangkapan layar (jika mengubah UI) serta rangkuman jelas mengenai modifikasi yang dilakukan.

## Standar Penulisan Kode
- **Pemisahan Logika**: Pastikan logika manipulasi *low-level* (seperti P/Invoke Windows API) terisolasi di dalam lapisan `Infrastructure` atau `Services`, bukan bercampur di dalam *code-behind* antarmuka XAML.
- **Komentar & Dokumentasi**: Berikan penjelasan singkat pada bagian blok kode yang kompleks, khususnya yang menangani manipulasi *pointer* atau emulasi *scan code*.

Terima kasih atas dedikasi Anda dalam menyempurnakan HOPE PD SkyNews!
