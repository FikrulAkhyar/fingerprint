<?php

namespace App\Http\Controllers;

use App\Models\Fingerprint;
use Illuminate\Http\Request;

class FingerprintController extends Controller
{
    public function index()
    {
        $fingerprints = Fingerprint::orderByDesc('created_at')->get(['nama', 'created_at']);

        return view('fingerprints.index', compact('fingerprints'));
    }

    public function enrollForm()
    {
        return view('fingerprints.enroll');
    }

    public function verifyForm()
    {
        return view('fingerprints.verify');
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

    // Dipanggil Agent (server-to-server) saat /verify butuh template tersimpan.
    public function show(string $nama)
    {
        $fingerprint = Fingerprint::where('nama', $nama)->first();

        if (! $fingerprint) {
            return response()->json(['error' => 'Belum terdaftar'], 404);
        }

        return response()->json(['template' => $fingerprint->template]);
    }
}
