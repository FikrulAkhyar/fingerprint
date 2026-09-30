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

- `/` — daftar semua yang sudah terdaftar (nama + waktu daftar), dari tabel `fingerprints`.
- `/enroll` — form daftar sidik jari. Browser memanggil Agent untuk capture (3x scan),
  lalu POST hasil template ke `POST /api/fingerprints` (route di sini) untuk disimpan.
- `/verify` — form verifikasi. Browser cuma memanggil Agent; Agent sendiri yang
  mengambil template tersimpan lewat `GET /api/fingerprints/{nama}` (server-to-server,
  lihat `../agent/FingerprintAgent/TemplateStore.cs`) dan melakukan pencocokan —
  template tidak pernah dikirim ke browser saat verifikasi.

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

## Kenapa Laravel, bukan PHP Slim seperti CBS asli?

Untuk kebutuhan testing standalone ini, Laravel dipilih karena lebih cepat
di-scaffold (migration, Eloquent, routing bawaan). Kontrak API-nya
(`POST /api/fingerprints`, `GET /api/fingerprints/{nama}`) dibuat mengikuti
draft di `../design.md` §5.2 apa adanya, supaya nanti gampang dipetakan ulang
ke controller/route Slim di CBS asli — cuma pindah bahasa/framework, bukan
ubah kontrak.
