<?php

namespace App\Http\Controllers;

use App\Models\Fingerprint;
use Illuminate\Http\Request;

class FingerprintController extends Controller
{
    // Satu-satunya halaman: enroll, cek sidik jari, dan daftar terdaftar.
    public function index()
    {
        $fingerprints = Fingerprint::orderByDesc('created_at')->get(['nama', 'created_at']);

        return view('fingerprints.index', compact('fingerprints'));
    }

    // Dipanggil browser setelah Agent selesai capture + merge 3x scan.
    public function store(Request $request)
    {
        $data = $request->validate([
            'nama' => 'required|string|max:255',
            'template' => 'required|string',
        ]);

        Fingerprint::updateOrCreate(
            ['nama' => $data['nama']],
            ['template' => $data['template']]
        );

        return response()->json(['success' => true]);
    }

    // Dipanggil browser untuk hapus data terdaftar dari tabel "Daftar Terdaftar".
    public function destroy(string $nama)
    {
        $deleted = Fingerprint::where('nama', $nama)->delete();

        if (! $deleted) {
            return response()->json(['error' => 'Data tidak ditemukan'], 404);
        }

        return response()->json(['success' => true]);
    }
}
