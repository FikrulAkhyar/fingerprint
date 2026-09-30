# Fingerprint Test Web (Laravel)

Aplikasi web untuk uji coba enrollment & verifikasi sidik jari ZKTeco
Live20R. Ini **bukan CBS asli** — cuma pengganti sementara yang berperan
seperti CBS di `../design.md` (simpan template ke database, dipanggil oleh
Agent), supaya alurnya bisa dites end-to-end sebelum diintegrasikan ke
aplikasi CBS PHP/Slim yang sesungguhnya.

## Menjalankan

```
cd web
php artisan serve
```

Buka `http://127.0.0.1:8000`. Pastikan **Fingerprint Agent** (folder `../agent/`)
juga sedang berjalan di laptop Windows yang sama (lihat `../agent/README.md`).

## Halaman

Cuma **satu halaman** (`/`), berisi tiga bagian:

- **Daftar Sidik Jari** (enroll) — isi nama, browser memanggil Agent untuk
  capture (3x scan), lalu POST hasil template ke `POST /api/fingerprints`
  (route di sini) untuk disimpan.
- **Cek Sidik Jari** — tanpa isi nama sama sekali. Browser cuma memanggil
  Agent (`POST /identify`); Agent sendiri yang ambil **semua** template lewat
  `GET /api/fingerprints` (server-to-server, lihat
  `../agent/FingerprintAgent/TemplateStore.cs`) dan mencocokkan satu-satu
  (1:N) untuk cari siapa pemilik jari itu — template tidak pernah dikirim ke
  browser.
- **Daftar Terdaftar** — tabel nama + waktu daftar, dari tabel `fingerprints`.

Endpoint `GET /api/fingerprints/{nama}` (1:1, by nama) tetap ada untuk
kebutuhan Agent `/verify` di masa depan (sesuai kontrak §5.1/§5.2
`design.md`), walau UI di halaman ini sekarang pakai jalur 1:N
(`/identify`) demi kemudahan testing.

## Database

Pakai **PostgreSQL lokal** (lihat `DB_*` di `.env`). Setup yang sudah
dilakukan di mesin ini:

```
createdb fingerprint          # database
php artisan migrate           # bikin tabelnya
```

Tabel `fingerprints`:

```
id, nama (unique), template (text, base64), created_at, updated_at
```

Kalau mau ganti host/kredensial (mis. di laptop lain), tinggal ubah `DB_*`
di `.env` lalu `php artisan migrate` ulang — tidak ada kode lain yang perlu
diubah karena lewat Eloquent.

## Alamat Fingerprint Agent (`AGENT_URL`)

JS di halaman ini manggil Agent lewat `AGENT_URL`, diatur dari `.env`
(`config/services.php`), bukan hardcode di blade:

```
AGENT_URL=http://127.0.0.1:9001
```

- **Kasus normal** (browser & Agent di laptop yang sama): biarkan default
  `127.0.0.1`.
- **Browser dibuka dari laptop lain**, Agent-nya di laptop Windows terpisah:
  ganti jadi IP laptop Windows itu (mis. `http://192.168.10.50:9001`) — lihat
  `../agent/README.md` bagian "Mengakses Agent dari laptop lain" untuk setup
  tambahan yang dibutuhkan di sisi Windows (izin bind + firewall).

Setelah ubah `.env`, jalankan `php artisan config:clear` kalau config sempat
di-cache (`config:cache`); untuk `php artisan serve` biasa di local biasanya
tidak perlu.

## Catatan teknis: JSON_UNESCAPED_SLASHES

Semua response JSON yang dibaca Agent (`show()`, `list()` di
`FingerprintController`) wajib pakai flag `JSON_UNESCAPED_SLASHES`. Tanpa
ini, `json_encode` bawaan PHP meng-escape karakter `/` (yang sering muncul
di base64) jadi `\/` — parser JSON minimal di Agent (`MiniJson.cs`, sengaja
tanpa library eksternal) tidak menghandle escape itu, sehingga template yang
diterima Agent jadi rusak/invalid ("input is not a valid base-64 string").
Kalau nanti nambah endpoint baru yang responsnya dibaca Agent, jangan lupa
flag ini juga.

## Kenapa Laravel, bukan PHP Slim seperti CBS asli?

Untuk kebutuhan testing standalone ini, Laravel dipilih karena lebih cepat
di-scaffold (migration, Eloquent, routing bawaan). Kontrak API-nya
(`POST /api/fingerprints`, `GET /api/fingerprints/{nama}`) dibuat mengikuti
draft di `../design.md` §5.2 apa adanya, supaya nanti gampang dipetakan ulang
ke controller/route Slim di CBS asli — cuma pindah bahasa/framework, bukan
ubah kontrak.
