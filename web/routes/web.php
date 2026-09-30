<?php

use App\Http\Controllers\FingerprintController;
use Illuminate\Support\Facades\Route;

// Satu-satunya halaman: enroll, cek sidik jari, dan daftar terdaftar.
Route::get('/', [FingerprintController::class, 'index'])->name('fingerprints.index');

// Dipanggil browser (JS) setelah Agent selesai capture — simpan ke DB.
Route::post('/api/fingerprints', [FingerprintController::class, 'store'])->name('fingerprints.store');

// Dipanggil Agent (server-to-server) saat /identify (1:N) butuh semua
// template buat dicocokkan satu-satu.
Route::get('/api/fingerprints', [FingerprintController::class, 'list'])->name('fingerprints.list');

// Dipanggil Agent (server-to-server) saat /verify (1:1, by nama) butuh
// template tersimpan.
Route::get('/api/fingerprints/{nama}', [FingerprintController::class, 'show'])->name('fingerprints.show');

// Dipanggil browser (JS) untuk hapus data dari tabel "Daftar Terdaftar".
Route::delete('/api/fingerprints/{nama}', [FingerprintController::class, 'destroy'])->name('fingerprints.destroy');
