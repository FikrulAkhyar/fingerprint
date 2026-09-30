<?php

use App\Http\Controllers\FingerprintController;
use Illuminate\Support\Facades\Route;

Route::get('/', [FingerprintController::class, 'index'])->name('fingerprints.index');
Route::get('/enroll', [FingerprintController::class, 'enrollForm'])->name('fingerprints.enroll');
Route::get('/verify', [FingerprintController::class, 'verifyForm'])->name('fingerprints.verify');

// Dipanggil browser (JS) setelah Agent selesai capture — simpan ke DB.
Route::post('/api/fingerprints', [FingerprintController::class, 'store'])->name('fingerprints.store');

// Dipanggil Agent (server-to-server) saat /verify butuh template tersimpan.
Route::get('/api/fingerprints/{nama}', [FingerprintController::class, 'show'])->name('fingerprints.show');
