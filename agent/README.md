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
6. Jalankan aplikasi web-nya (folder `web/` di project ini, Laravel) —
   `cd web && php artisan serve`, lalu buka `http://127.0.0.1:8000` di
   browser. Halaman ini yang dipakai untuk Enroll, Verifikasi, dan lihat
   daftar terdaftar (datanya di database, bukan lagi file lokal).

## Menjalankan Agent & backend di laptop berbeda (1 jaringan WiFi)

Agent tidak harus di laptop yang sama dengan backend-nya — bisa dipisah, asal
kedua laptop tersambung ke **WiFi/jaringan yang sama** dan tidak ada
"client/AP isolation" yang memblokir laptop saling akses (beberapa WiFi
publik/kantor mengaktifkan ini — kalau tidak bisa connect padahal sudah satu
jaringan, ini kemungkinan penyebabnya).

Contoh: alat & Agent di **laptop A** (Windows), backend (Laravel, untuk saat
ini) di **laptop B** (mis. `192.168.10.62`):

1. Di laptop B, jalankan Laravel supaya bisa diakses dari luar (`--host=0.0.0.0`,
   bukan `php artisan serve` biasa yang cuma bisa diakses dari laptop itu sendiri):
   ```
   cd web
   php artisan serve --host=0.0.0.0 --port=8000
   ```
   Cari IP laptop B: Windows `ipconfig`, Mac/Linux `ifconfig` atau
   System Settings > Wi-Fi > Details.
2. Di laptop A, edit `agent/.env` (dibuat otomatis setelah Agent pernah
   dijalankan sekali) isinya jadi:
   ```
   BACKEND_URL=http://192.168.10.62:8000
   ```
   Lalu jalankan ulang Agent-nya (tidak perlu build ulang — ini cuma file teks).
3. Buka browser **di laptop A** (karena di situ ada alat & Agent-nya) ke
   `http://192.168.10.62:8000` — bukan `127.0.0.1`, supaya bisa muat halaman
   dari laptop B.

Yang **tidak berubah** untuk skenario di atas: browser tetap harus dibuka di
laptop yang sama dengan Agent (Agent cuma bisa diakses dari mesin tempat dia
jalan, tidak lewat jaringan) — kecuali Anda mengaktifkan akses jarak jauh ke
Agent seperti di bawah ini.

## Mengakses Agent dari laptop lain (mis. testing/debug dari Mac)

Secara default Agent bind ke `127.0.0.1` (localhost) — sengaja, ini yang
paling aman (lihat `design.md` §7: Agent tidak boleh diakses dari luar
localhost, supaya halaman web sembarangan tidak bisa diam-diam memanggilnya).
Tapi untuk kebutuhan testing/debug langsung dari laptop lain (mis. curl dari
Mac ke Agent yang jalan di Windows), ini bisa diaktifkan:

1. **Windows (laptop tempat Agent jalan)** — sekali saja, buka Command
   Prompt / PowerShell **as Administrator**, jalankan:
   ```
   netsh http add urlacl url=http://+:9001/ user=Everyone
   netsh advfirewall firewall add rule name="FingerprintAgent" dir=in action=allow protocol=TCP localport=9001
   ```
   Baris pertama mengizinkan Agent bind ke semua network interface tanpa
   perlu run-as-admin setiap kali jalan; baris kedua membuka portnya di
   Windows Firewall.
2. Edit `agent/.env`, ubah:
   ```
   AGENT_BIND_HOST=+
   ```
3. Jalankan ulang `FingerprintAgent.exe` (tidak perlu build ulang — ini cuma
   file `.env`). Cek `agent.log`, harus muncul `Agent bind host: +`.
4. Cari IP laptop Windows-nya (`ipconfig`, lihat IPv4 Address di adapter
   Wi-Fi/Ethernet yang aktif), misal `192.168.10.50`.
5. Dari laptop lain (Mac dkk.), akses lewat IP itu, bukan `127.0.0.1`:
   ```
   curl http://192.168.10.50:9001/enroll/start -X POST -H "Content-Type: application/json" -d "{\"nama\":\"test\"}"
   ```
   Kalau mau diakses lewat halaman web (bukan curl), edit `AGENT_URL` di
   `web/.env` jadi `http://192.168.10.50:9001` (lihat `web/README.md`) —
   tidak perlu ubah kode/blade manapun, sama seperti `BACKEND_URL` di sisi
   Agent.

**Catatan keamanan**: dengan `AGENT_BIND_HOST=+`, siapa pun di jaringan yang
sama bisa memanggil Agent (termasuk memicu capture sidik jari). Ini oke untuk
jaringan testing pribadi/kantor yang tepercaya, tapi jangan dipakai dengan
setting ini di jaringan publik atau di laptop produksi — kembalikan ke
`AGENT_BIND_HOST=127.0.0.1` (atau hapus baris itu dari `.env`) begitu selesai
testing.

## Pemakaian sehari-hari (untuk user non-IT)

Kalau autostart sudah di-setup (langkah 5 di atas), user tidak perlu
melakukan apa-apa — Agent otomatis aktif begitu Windows nyala. Cukup pastikan
ada icon "Fingerprint Agent" di system tray sebelum mulai transaksi. Kalau
icon-nya tidak ada, double-click `FingerprintAgent.exe` di
`FingerprintAgent\bin\Release\net48\` untuk jalanin manual.

## Yang perlu diketahui

- **Penyimpanan template sekarang di database lewat backend**, bukan file
  lokal lagi. Kode Agent-nya (`BackendTemplateStore`, `.env` / `BACKEND_URL`)
  sengaja dibuat generic — tidak nge-hardcode "Laravel" — karena backend ini
  cuma pengganti sementara untuk testing, nanti diarahkan ke **CBS asli**
  tanpa perlu ubah kode Agent, cukup ganti isi `agent/.env`. Saat ini
  backend-nya adalah aplikasi Laravel di `web/`. Alurnya: saat enroll,
  browser yang POST template ke backend (`web/routes/web.php`) setelah Agent
  selesai capture+merge; saat verifikasi, Agent sendiri yang `GET` template
  dari backend (server-to-server, lihat `TemplateStore.cs`) lalu `Match()`
  lokal — ini mendekati arsitektur produksi asli di `design.md` §5.2 (backend
  testing ini berperan seperti CBS, walau CBS asli nanti pakai PHP Slim,
  bukan Laravel). **Backend-nya harus sudah jalan** (`php artisan serve` untuk
  saat ini) sebelum Agent dites, kalau tidak `/verify`/`/identify` akan
  gagal karena tidak bisa ambil template.
- **`POST /identify`** — endpoint tambahan (testing convenience, di luar
  kontrak resmi §5.1 `design.md`) buat cek sidik jari **tanpa isi nama**:
  Agent ambil semua template dari backend (`GET /api/fingerprints`), lalu
  `Match()` satu-satu, kembalikan yang skornya tertinggi. Ini yang dipakai
  halaman web sekarang untuk "Cek Sidik Jari". `POST /verify` (by nama, 1:1)
  tetap ada di kode untuk kebutuhan nanti kalau CBS asli mau pakai pola
  1:1 sesuai proses bisnis yang sudah dikonfirmasi di `design.md`.
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
├── .env.example                    (contoh isi .env — copy jadi .env kalau mau ubah manual)
├── .env                            (dibuat otomatis saat run pertama, isinya BACKEND_URL — di-gitignore)
├── driver/setup.exe               (installer driver reader ZKTeco)
├── docs/ZKFinger Reader SDK C#_en_V2.pdf   (referensi resmi API SDK)
└── FingerprintAgent/
    ├── FingerprintAgent.csproj
    ├── icon.ico                   (icon fingerprint — dipakai .exe & system tray)
    ├── Program.cs                 (entry point, WinExe — tanpa jendela console; cari .env naik ke agent/)
    ├── TrayApplicationContext.cs  (icon system tray + menu Keluar)
    ├── Logger.cs                  (tulis log ke agent.log, pengganti Console)
    ├── ZkFingerService.cs         (wrapper SDK: Init/Capture/Merge/Match)
    ├── EnrollSession.cs           (state machine 3x-scan + polling progres)
    ├── TemplateStore.cs           (BackendTemplateStore: ambil template dari backend, lihat catatan di atas)
    ├── HttpApi.cs                 (routing HTTP: /enroll/start, /enroll/status, /verify, /identify)
    ├── MiniJson.cs                (helper JSON minimal, sengaja tanpa NuGet)
    ├── EnvFile.cs                 (baca file .env sederhana, format KEY=VALUE)
    └── lib/
        ├── libzkfpcsharp.dll       (x64, dipakai default)
        └── x86/libzkfpcsharp.dll   (cadangan kalau Windows-nya 32-bit)
```

Catatan: folder SDK asli (`ZKFinger Standard SDK 5.3.0.33`) sudah dihapus
setelah file-file yang dibutuhkan (DLL, driver, dokumentasi) dipindah ke sini
— tidak perlu lagi disimpan terpisah.
