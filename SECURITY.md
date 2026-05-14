# Kebijakan Keamanan (Security Policy)

Keamanan dan integritas operasional adalah prioritas mendasar dalam pengembangan **HOPE PD SkyNews**. Dokumen ini menguraikan versi yang didukung, jaminan kepatuhan terhadap sistem *anti-cheat*, serta prosedur pelaporan kerentanan keamanan.

## Versi yang Didukung

Kami secara aktif memelihara dan menyediakan pembaruan keamanan untuk versi perangkat lunak berikut:

| Versi | Status Dukungan | Keterangan |
|-------|-----------------|------------|
| **1.0.x** | Didukung Penuh | Rilis stabil saat ini untuk arsitektur 64-bit Windows. |
| **< 1.0.0** | Tidak Didukung | Versi prarilis/beta lama; disarankan untuk segera memperbarui. |

## Pernyataan Kepatuhan Anti-Cheat & Larangan Eksploitasi

Aplikasi ini dirancang untuk beroperasi di ranah ekosistem permainan FiveM dengan batasan operasional yang sangat ketat:
- **Nihil Injeksi Memori**: Sistem ini tidak menyuntikkan kode (DLL injection) atau memodifikasi ruang alamat memori klien FiveM.
- **Transparansi API**: Interaksi masukan murni menggunakan fungsi subsistem antarmuka pengguna Windows standar (`SendInput` dengan penanda `KEYEVENTF_SCANCODE` serta `SetWindowsHookEx` untuk penyadapan *keyboard* global).
- **Auditabilitas Sempurna**: Seluruh kode sumber didistribusikan secara terbuka untuk memungkinkan pengawas peladen (*server admin*) atau analis keamanan memverifikasi ketiadaan fungsi berbahaya (*keylogger* eksternal, pencurian kredensial, dsb.).

## Pelaporan Kerentanan Keamanan

Jika Anda menemukan kerentanan keamanan atau celah logika yang berpotensi disalahgunakan, kami memohon agar Anda **tidak** memublikasikannya secara terbuka di saluran umum atau *Issues* publik.

Silakan laporkan temuan Anda secara rahasia melalui langkah berikut:
1. Kirimkan laporan surel terperinci ke tim pengelola melalui repositori GitHub utama atau hubungi langsung **Gustyx-Power**.
2. Sertakan deskripsi teknis mengenai langkah-langkah untuk mereproduksi celah keamanan tersebut.
3. Kami berkomitmen untuk merespons laporan Anda dalam waktu maksimal 48 jam dan menyediakan rilis tambalan (*patch*) sesegera mungkin.

Terima kasih atas kontribusi Anda dalam menjaga keamanan ekosistem HOPE PD SkyNews.
