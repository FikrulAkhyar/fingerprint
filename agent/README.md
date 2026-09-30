# Fingerprint Agent — Tahap 2

Agent yang membungkus ZKFinger SDK jadi local HTTP API, sesuai kontrak di
`design.md` §5.1. Ditulis pakai project format modern (SDK-style `.csproj`)
supaya bisa diedit di editor apa pun (termasuk di Mac), tapi **build & run
tetap wajib di Windows** karena `libzkfpcsharp.dll` dan device-nya cuma jalan
di situ.

**Tidak ada terminal/console yang perlu dibuka sehari-hari** — Agent jalan
sebagai icon kecil di system tray (pojok kanan bawah Windows), tanpa jendela.
Build lewat command line itu cuma dilakukan **sekali** oleh yang setup
(develop/IT); user sehari-hari cukup double-click `.exe`-nya (atau biarkan
auto-start, lihat di bawah).

## Yang perlu disiapkan di laptop Windows (sekali saja)

1. Pastikan sudah pernah jalankan `driver/setup.exe` (di folder ini) untuk
   pasang driver reader — ini sudah dilakukan di Tahap 1. Kalau pindah ke
   laptop lain yang baru, jalankan lagi dari sini.
2. Install **.NET SDK** (bukan Visual Studio, cukup SDK-nya saja, ~200MB).
   Paling gampang lewat command line (PowerShell):
   ```
   winget install Microsoft.DotNet.SDK.8
   ```
   Atau download installer dari halaman resmi .NET di situs Microsoft
   (dotnet.microsoft.com) kalau `winget` tidak tersedia.
3. Kalau nanti muncul error soal "targeting pack .NET Framework 4.8" saat
   build (karena project ini target `net48`, sama seperti Demo2 yang sudah
   terbukti jalan di Tahap 1) — ikuti link yang diberikan error tersebut untuk
   install "Microsoft .NET Framework 4.8 Targeting Pack" (installer kecil).

## Setup awal (sekali saja, dilakukan yang develop/IT)

1. Copy folder `fingerprint/agent` ini via USB ke laptop Windows.
2. Colok alat ZKTeco Live20R.
3. Buka Command Prompt / PowerShell, masuk ke folder, lalu build (Release,
   bukan `dotnet run`, supaya hasil `.exe`-nya bisa dipakai berulang tanpa
   command line lagi):
   ```
   cd FingerprintAgent
   dotnet build -c Release
   ```
4. Hasil `.exe`-nya ada di `FingerprintAgent\bin\Release\net48\FingerprintAgent.exe`
   — double-click file ini untuk jalanin Agent. **Tidak ada jendela yang
   muncul**; cek icon baru di system tray (pojok kanan bawah) bertuliskan
   "Fingerprint Agent — Aktif". Klik kanan icon itu untuk keluar/matikan.
5. (Opsional, disarankan) supaya Agent otomatis nyala tiap kali Windows
   login — tidak perlu double-click manual tiap hari — klik kanan
   `install-autostart.ps1` > **Run with PowerShell** (sekali saja).
6. Buka file `test-web/index.html` (bisa dipindah bareng lewat USB juga)
   langsung di browser — halaman ini otomatis cek koneksi ke Agent, lalu
   bisa dipakai untuk coba Enroll & Verifikasi.

## Pemakaian sehari-hari (untuk user non-IT)

Kalau autostart sudah di-setup (langkah 5 di atas), user tidak perlu
melakukan apa-apa — Agent otomatis aktif begitu Windows nyala. Cukup pastikan
ada icon "Fingerprint Agent" di system tray sebelum mulai transaksi. Kalau
icon-nya tidak ada, double-click `FingerprintAgent.exe` di
`FingerprintAgent\bin\Release\net48\` untuk jalanin manual.

## Yang perlu diketahui

- **Penyimpanan template masih sementara** (`templates.json`, dibuat otomatis
  di folder yang sama saat run) — ini cuma untuk testing standalone tanpa
  CBS. Di produksi nanti diganti panggilan ke REST API CBS (lihat
  `TemplateStore.cs` dan `design.md` §5.2).
- Enrollment butuh **scan jari yang sama 3x** — ini persyaratan SDK
  (`DBMerge`), bukan bug.
- Kalau device gagal diinisialisasi (alat belum dicolok, driver belum
  terpasang, atau masih dipakai aplikasi lain seperti Demo2 dari Tahap 1),
  akan muncul **popup error** saat Agent dijalankan (bukan tersembunyi) —
  pesannya menjelaskan apa yang salah.
- Semua log (start, error, aktivitas) ditulis ke `agent.log` di folder yang
  sama dengan `.exe`-nya — cek file ini kalau perlu troubleshoot lebih detail
  (karena tidak ada console yang menampilkan log secara langsung).
- File `FingerprintAgent/lib/libzkfpcsharp.dll` adalah **versi x64** (dipakai
  secara default). Kalau ternyata laptop Windows-nya 32-bit (jarang di laptop
  modern), pakai `FingerprintAgent/lib/x86/libzkfpcsharp.dll` (sudah
  disertakan juga) sebagai gantinya — ubah `HintPath` di
  `FingerprintAgent.csproj` ke `lib\x86\libzkfpcsharp.dll` dan
  `<PlatformTarget>` dari `x64` ke `x86`.
- Dokumentasi resmi API SDK (referensi fungsi `zkfp2.*`, kode error, kode
  parameter) ada di `docs/ZKFinger Reader SDK C#_en_V2.pdf`, kalau nanti perlu
  extend Agent (mis. pakai `DBIdentify` atau parameter LED/DPI).

## Struktur

```
agent/
├── README.md                     (file ini)
├── install-autostart.ps1          (sekali klik: auto-start Agent tiap login Windows)
├── driver/setup.exe               (installer driver reader ZKTeco)
├── docs/ZKFinger Reader SDK C#_en_V2.pdf   (referensi resmi API SDK)
└── FingerprintAgent/
    ├── FingerprintAgent.csproj
    ├── Program.cs                 (entry point, WinExe — tanpa jendela console)
    ├── TrayApplicationContext.cs  (icon system tray + menu Keluar)
    ├── Logger.cs                  (tulis log ke agent.log, pengganti Console)
    ├── ZkFingerService.cs         (wrapper SDK: Init/Capture/Merge/Match)
    ├── EnrollSession.cs           (state machine 3x-scan + polling progres)
    ├── TemplateStore.cs           (penyimpanan sementara, lihat catatan di atas)
    ├── HttpApi.cs                 (routing HTTP: /enroll/start, /enroll/status, /verify)
    ├── MiniJson.cs                (helper JSON minimal, sengaja tanpa NuGet)
    └── lib/
        ├── libzkfpcsharp.dll       (x64, dipakai default)
        └── x86/libzkfpcsharp.dll   (cadangan kalau Windows-nya 32-bit)
```

Catatan: folder SDK asli (`ZKFinger Standard SDK 5.3.0.33`) sudah dihapus
setelah file-file yang dibutuhkan (DLL, driver, dokumentasi) dipindah ke sini
— tidak perlu lagi disimpan terpisah.
