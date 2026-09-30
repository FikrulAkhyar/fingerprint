@extends('layouts.app')

@section('title', 'Enroll')

@section('content')
  <div id="agentStatus" class="agent-status">Mengecek koneksi ke Agent...</div>

  <div class="card">
    <h2>Daftar Sidik Jari</h2>
    <label for="nama">Nama</label>
    <input type="text" id="nama" placeholder="contoh: Budi Santoso">
    <button class="full" id="btnEnroll">Mulai Enroll</button>
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

const btnEnroll = document.getElementById('btnEnroll');
const namaInput = document.getElementById('nama');
const csrfToken = document.querySelector('meta[name="csrf-token"]').content;

btnEnroll.addEventListener('click', async () => {
  const nama = namaInput.value.trim();
  if (!nama) {
    alert('Isi Nama dulu.');
    return;
  }

  btnEnroll.disabled = true;
  ScanModal.open('Daftar Sidik Jari — ' + nama, 3);

  try {
    const startRes = await fetch(AGENT_URL + '/enroll/start', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nama }),
    });
    const startData = await startRes.json();
    if (!startRes.ok) throw new Error(startData.error || 'Gagal memulai enroll');

    const template = await pollEnrollStatus(startData.session_id);

    ScanModal.setStatus('Menyimpan ke database...');
    const saveRes = await fetch('{{ route('fingerprints.store') }}', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': csrfToken,
      },
      body: JSON.stringify({ nama, template }),
    });
    if (!saveRes.ok) throw new Error('Gagal menyimpan ke database');

    ScanModal.success('Enroll berhasil untuk "' + nama + '".', () => {
      namaInput.value = '';
    });
  } catch (err) {
    ScanModal.error(err.message, 'Coba Lagi');
  } finally {
    btnEnroll.disabled = false;
  }
});

function pollEnrollStatus(sessionId) {
  return new Promise((resolve, reject) => {
    const interval = setInterval(async () => {
      try {
        const res = await fetch(AGENT_URL + '/enroll/status?session_id=' + sessionId);
        const data = await res.json();

        if (!data.done) {
          const step = data.step || 0;
          ScanModal.setStep(step);
          ScanModal.setStatus(step > 0
            ? 'Scan ke-' + step + ' dari 3 berhasil, lanjutkan...'
            : 'Tempel jari (1 dari 3)...');
          return;
        }

        clearInterval(interval);
        if (data.success) {
          ScanModal.setStep(3);
          resolve(data.template);
        } else {
          reject(new Error(data.error || 'Enroll gagal.'));
        }
      } catch (err) {
        clearInterval(interval);
        reject(err);
      }
    }, 500);
  });
}
</script>
@endsection
