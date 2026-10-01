# Fingerprint Agent — Tahap 2

Agent yang membungkus ZKFinger SDK jadi local HTTP API, sesuai kontrak di
`design.md` §5.1. Ditulis pakai project format modern (SDK-style `.csproj`)
supaya bisa diedit di editor apa pun (termasuk di Mac), tapi **build & run
tetap wajib di Windows** karena `libzkfpcsharp.dll` dan device-nya cuma jalan
di situ.

**Tidak ada terminal/console sama sekali** — Agent jalan sebagai icon kecil
di system tray (pojok kanan bawah Windows). Build lewat command line itu
cuma dilakukan **sekali** oleh yang setup (develop/IT); user sehari-hari
cukup double-click `.exe`-nya (atau biarkan auto-start, lihat di bawah).

**Dialog pengisian `ARB_URL` cuma muncul sekali** — pas pertama kali Agent
dijalankan dan `agent/.env` belum ada/masih kosong. Begitu terisi, start
berikutnya (termasuk auto-start saat Windows login) **langsung jalan tanpa
dialog apa pun**, benar-benar silent. Kalau nanti perlu ganti alamat backend,
tidak perlu edit `.env` manual atau restart Agent — klik kanan icon di tray,
pilih **"Ubah ARB_URL..."**, isi alamat baru, langsung berlaku saat itu juga
(otomatis tersimpan ke `.env` juga).

**Agent cuma bisa diakses dari laptop itu sendiri** (bind ke `127.0.0.1`) —
ini sengaja dan memang cukup: JS di halaman CBS jalan di browser teller
(laptop yang sama dengan Agent), bukan di server CBS, jadi `127.0.0.1`
selalu bisa diakses apa pun bentuk hosting CBS-nya. Tidak ada rencana
expose Agent ke jaringan luar sama sekali — lihat `design.md` §7.

Di beberapa Windows (tergantung konfigurasi/kebijakan sistemnya), bind ke
`127.0.0.1` saja pun tetap butuh izin eksplisit dari Windows — lihat langkah
4 di bawah.

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
4. Buka Command Prompt / PowerShell **as Administrator**, jalankan sekali:
   ```
   netsh http add urlacl url=http://127.0.0.1:9001/ user=Everyone
   ```
   Ini **bukan** membuka akses jaringan (beda dari `http://+:9001/` yang
   berarti semua interface) — ini murni izin internal Windows supaya proses
   biasa (bukan Administrator) boleh bind ke alamat localhost ini. Tanpa
   langkah ini, di sebagian konfigurasi Windows, Agent gagal start dengan
   error **"Access is denied"** walau sudah bind ke `127.0.0.1` saja.
   Kalau nanti `AGENT_PORT` diganti dari default `9001` (lihat "Yang perlu
   diketahui" di bawah), ulangi perintah ini dengan port yang baru.

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
6. Pastikan backend-nya (lihat `ARB_URL`, bagian "Yang perlu diketahui" di
   bawah) sudah jalan sebelum dipakai — tanpa itu `/enroll` dan `/verify`
   tidak bisa menyimpan/mengambil template.

## Menjalankan Agent, backend, & browser di laptop berbeda (1 jaringan WiFi)

**Browser yang memicu scan wajib dibuka di laptop yang sama dengan Agent**
(Agent cuma bind ke `127.0.0.1`, tidak bisa diakses lewat jaringan — lihat
catatan keamanan di atas). Yang **boleh** di laptop/server lain cuma
backend-nya, karena itu Agent sendiri yang manggil keluar (server-to-server),
bukan sebaliknya.

Kalau backend dijalankan di laptop/server terpisah (mis. `192.168.10.62`):

1. Pastikan backend-nya bisa diakses dari luar mesin itu sendiri (bukan cuma
   `localhost`) — caranya tergantung backend-nya (web server apa yang dipakai).
2. Di laptop Agent, edit `agent/.env` (dibuat otomatis setelah Agent pernah
   dijalankan sekali), atau pakai menu **"Ubah ARB_URL..."** di tray:
   ```
   ARB_URL=http://192.168.10.62:8000
   ```
3. Buka browser **di laptop yang sama dengan Agent** (bukan di laptop
   backend) untuk memicu scan — Agent-nya sendiri yang akan manggil ke
   `ARB_URL` itu di belakang layar.

Agent dan backend perlu **satu jaringan WiFi/LAN yang sama** dan tidak ada
"client/AP isolation" yang memblokir perangkat saling akses (beberapa WiFi
publik/kantor mengaktifkan ini — kalau tidak bisa connect padahal sudah satu
jaringan, ini kemungkinan penyebabnya).

## Pemakaian sehari-hari (untuk user non-IT)

Kalau autostart sudah di-setup (langkah 5 di atas), user tidak perlu
melakukan apa-apa — Agent otomatis aktif begitu Windows nyala. Cukup pastikan
ada icon "Fingerprint Agent" di system tray sebelum mulai transaksi. Kalau
icon-nya tidak ada, double-click `FingerprintAgent.exe` di
`FingerprintAgent\bin\Release\net48\` untuk jalanin manual.

## Yang perlu diketahui

- **Cek status & ubah ARB_URL dari tray**: klik kanan icon di system tray —
  ada baris info (tidak bisa diklik) nunjukkin alamat Agent sendiri
  (`Listen: ...`) dan alamat backend yang lagi dipakai (`ARB_URL: ...`), plus
  menu **"Ubah ARB_URL..."** buat ganti alamat backend kapan saja tanpa
  restart Agent (langsung berlaku & otomatis tersimpan ke `.env`).
- **Migrasi dari versi lama**: kalau `agent/.env` di laptop Anda masih punya
  `BACKEND_URL=...` dan/atau `AGENT_BIND_HOST=...` dari setup sebelumnya,
  itu **tidak terbaca lagi** — ganti nama key `BACKEND_URL` jadi `ARB_URL`,
  dan hapus baris `AGENT_BIND_HOST` kalau ada (Agent sekarang selalu bind ke
  `127.0.0.1` saja, tidak ada opsi lain).
- **Agent tidak menyimpan template apa pun secara permanen** — itu tugas
  **ARB+** (CBS), lewat progId **`MADC0005`** (kodenya ada di codebase ARB+
  sendiri, di luar project ini — bukan bagian dari Agent). Agent login ke
  ARB+ pakai akun sistem (`ARB_USERNAME`/`ARB_PASSWORD` di `.env`, lihat
  `ArbAuthService.cs`) — bukan akun user asli — lalu manggil progId itu pakai
  token hasil login. `ARB_URL` sekarang isinya **host ARB+**, bukan sekadar
  backend generik. **ARB+ harus bisa diakses & akun sistemnya sudah
  disiapkan** sebelum dipakai, kalau tidak `/verify` akan gagal.
- **Field `key`, bukan `nama`** — `POST /enroll/start` dan `POST /verify`
  menerima body `{"key": "..."}`, identifier generik yang nilainya terserah
  pemanggil (CBS). Dari sisi ARB+, nilai yang dikirim adalah **`cf_mast_id`**
  nasabah (lihat `CFMA0031.js`), bukan nama asli — Agent sendiri tidak tahu
  dan tidak perlu tahu itu `cf_mast_id`, cuma meneruskannya sebagai parameter
  `cf_mast_id` saat manggil progId `MADC0005` (`methods=get_template`).
- **1:1 saja, tidak ada 1:N** — proses bisnis yang dikonfirmasi di
  `design.md` §5.1 memang cuma 1:1 (identifier nasabah sudah diketahui dari
  alur teller). Endpoint `POST /identify` (cek sidik jari tanpa tahu
  identifier dulu) sempat ada sebagai testing convenience tapi **sudah
  dihapus** karena progId MADC0005 yang sebenarnya memang tidak punya
  dukungan ambil-semua-template.
- Enrollment butuh **scan jari yang sama 3x** — ini persyaratan SDK
  (`DBMerge`), bukan bug.
- **"Gagal menggabungkan hasil scan" / `DBMerge` kode `-22` (`ZKFP_ERR_MERGE`)**
  — SDK butuh area yang cukup overlap antar 3 capture buat menemukan titik
  referensi (minutiae) yang sama. **Variasi posisi antar scan itu wajar &
  memang tujuannya** (makanya diminta 3x, bukan 1x) — bukan berarti jari
  harus ditempel persis di titik yang sama. Penyebab paling umum justru:
  jari yang dipakai berbeda-beda di tengah proses (mis. orang lain ikut
  coba), atau salah satu capture kualitasnya jelek (kurang mantap/tertarik
  saat scan, jari basah/kering/kotor). Bukan bug — user tinggal enroll ulang
  pakai jari yang sama & tempelkan dengan mantap tiap kali. Kode error SDK
  lain ada di `docs/ZKFinger Reader SDK C#_en_V2.pdf` §6.2.
- Kalau device gagal diinisialisasi (alat belum dicolok, driver belum
  terpasang, atau masih dipakai aplikasi lain seperti Demo2 dari Tahap 1),
  akan muncul **popup error** saat Agent dijalankan (bukan tersembunyi) —
  pesannya menjelaskan apa yang salah.
- **"Agent gagal membuka port 9001: Access is denied"** — tray icon tetap
  muncul (karena itu bagian lain dari kode), tapi API-nya tidak aktif
  (browser tidak bisa connect). Urutan troubleshoot:
  1. **Paling umum**: belum jalankan izin `netsh` di langkah 4 "Yang perlu
     disiapkan" di atas. Jalankan itu dulu (as Administrator), lalu coba lagi.
  2. Kalau sudah dilakukan tapi masih gagal, cek apa port-nya bentrok dengan
     proses lain atau masuk *excluded port range* Windows (sering kejadian
     kalau Hyper-V/WSL2/Docker Desktop aktif):
     ```
     netsh int ipv4 show excludedportrange protocol=tcp
     netstat -ano | findstr :9001
     ```
     Kalau `9001` muncul di salah satunya, ganti port lewat `agent/.env`:
     ```
     AGENT_PORT=9101
     ```
     (angka bebas), ulangi perintah `netsh urlacl` dengan port baru itu, lalu
     jalankan ulang Agent — tidak perlu build ulang. Jangan lupa sesuaikan
     juga alamat yang dipakai browser untuk memanggil Agent supaya cocok
     dengan port baru.
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
├── .env                            (dibuat otomatis saat run pertama, isinya ARB_URL dkk — di-gitignore)
├── .arb-session                    (dibuat otomatis pas login ARB+, dihapus pas logout normal — di-gitignore)
├── driver/setup.exe               (installer driver reader ZKTeco)
├── docs/ZKFinger Reader SDK C#_en_V2.pdf   (referensi resmi API SDK)
└── FingerprintAgent/
    ├── FingerprintAgent.csproj
    ├── icon.ico                   (icon fingerprint — dipakai .exe & system tray)
    ├── Program.cs                 (entry point; dialog ARB_URL di awal; cari .env naik ke agent/)
    ├── TrayApplicationContext.cs  (icon system tray + menu Keluar, logout ARB+ pas keluar)
    ├── Logger.cs                  (tulis log ke agent.log, pengganti Console)
    ├── ZkFingerService.cs         (wrapper SDK: Init/Capture/Merge/Match)
    ├── EnrollSession.cs           (state machine 3x-scan + polling progres)
    ├── ArbAuthService.cs          (login/logout akun sistem ke ARB+, cache token)
    ├── TemplateStore.cs           (BackendTemplateStore: manggil progId MADC0005 di ARB+, lihat catatan di atas)
    ├── HttpApi.cs                 (routing HTTP: /enroll/start, /enroll/status, /verify)
    ├── MiniJson.cs                (helper JSON minimal, sengaja tanpa NuGet)
    ├── EnvFile.cs                 (baca file .env sederhana, format KEY=VALUE)
    └── lib/
        ├── libzkfpcsharp.dll       (x64, dipakai default)
        └── x86/libzkfpcsharp.dll   (cadangan kalau Windows-nya 32-bit)
```

Catatan: folder SDK asli (`ZKFinger Standard SDK 5.3.0.33`) sudah dihapus
setelah file-file yang dibutuhkan (DLL, driver, dokumentasi) dipindah ke sini
— tidak perlu lagi disimpan terpisah.
