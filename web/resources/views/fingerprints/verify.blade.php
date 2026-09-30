@extends('layouts.app')

@section('title', 'Verifikasi')

@section('content')
  <div id="agentStatus" class="agent-status">Mengecek koneksi ke Agent...</div>

  <div class="card">
    <h2>Verifikasi Sidik Jari</h2>
    <label for="nama">Nama</label>
    <input type="text" id="nama" placeholder="contoh: Budi Santoso">
    <button class="full" id="btnVerify">Verifikasi</button>
  </div>
@endsection

@section('scripts')
<script>
async function checkAgent() {
  const el = document.getElementById('agentStatus');
  try {
    await fetch(AGENT_URL + '/verify', { method: 'OPTIONS' });
    el.textContent = 'Agent terhubung di ' + AGENT_URL;
    el.className = 'agent-status ok';
  } catch (e) {
    el.textContent = 'Agent tidak terjangkau di ' + AGENT_URL + ' — pastikan FingerprintAgent sedang berjalan.';
    el.className = 'agent-status down';
  }
}
checkAgent();

const btnVerify = document.getElementById('btnVerify');
const namaInput = document.getElementById('nama');

btnVerify.addEventListener('click', async () => {
  const nama = namaInput.value.trim();
  if (!nama) {
    alert('Isi Nama dulu.');
    return;
  }

  btnVerify.disabled = true;
  ScanModal.open('Verifikasi — ' + nama, 1);

  try {
    // Agent yang mengambil template tersimpan dari Laravel (server-to-server)
    // dan melakukan pencocokan — template tidak lewat browser sama sekali.
    const res = await fetch(AGENT_URL + '/verify', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nama }),
    });
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || 'Verifikasi gagal');

    if (data.match) {
      ScanModal.success('Cocok! Skor: ' + data.score);
    } else {
      ScanModal.error('Tidak cocok. Skor: ' + data.score, 'Coba Lagi');
    }
  } catch (err) {
    ScanModal.error(err.message, 'Coba Lagi');
  } finally {
    btnVerify.disabled = false;
  }
});
</script>
@endsection
