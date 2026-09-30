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

    // Dipanggil Agent (server-to-server) saat /verify (1:1, by nama) butuh
    // template tersimpan.
    //
    // NB: JSON_UNESCAPED_SLASHES wajib ada — base64 sering mengandung "/",
    // dan json_encode bawaan PHP meng-escape-nya jadi "\/". Parser JSON
    // minimal di Agent (MiniJson.cs) tidak menghandle escape itu, jadi
    // tanpa flag ini template yang diterima Agent jadi rusak/invalid.
    public function show(string $nama)
    {
        $fingerprint = Fingerprint::where('nama', $nama)->first();

        if (! $fingerprint) {
            return response()->json(['error' => 'Belum terdaftar'], 404, [], JSON_UNESCAPED_SLASHES);
        }

        return response()->json(['template' => $fingerprint->template], 200, [], JSON_UNESCAPED_SLASHES);
    }

    // Dipanggil Agent (server-to-server) saat /identify (1:N, tanpa nama)
    // butuh semua template buat dicocokkan satu-satu.
    public function list()
    {
        $fingerprints = Fingerprint::all(['nama', 'template']);

        return response()->json($fingerprints, 200, [], JSON_UNESCAPED_SLASHES);
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
