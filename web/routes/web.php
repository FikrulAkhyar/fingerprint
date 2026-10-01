<?php

use App\Http\Controllers\FingerprintController;
use Illuminate\Support\Facades\Route;

// Satu-satunya halaman: enroll, cek sidik jari, dan daftar terdaftar.
Route::get('/', [FingerprintController::class, 'index'])->name('fingerprints.index');

// Dipanggil browser (JS) setelah Agent selesai capture — simpan ke DB.
Route::post('/api/fingerprints', [FingerprintController::class, 'store'])->name('fingerprints.store');

// Dipanggil browser (JS) untuk hapus data dari tabel "Daftar Terdaftar".
Route::delete('/api/fingerprints/{nama}', [FingerprintController::class, 'destroy'])->name('fingerprints.destroy');
