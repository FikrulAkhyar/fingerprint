@extends('layouts.app')

@section('title', 'Test Fingerprint')

@section('content')
  <div id="agentStatus" class="agent-status">Mengecek koneksi ke Agent...</div>

  <div class="grid">
    <div class="card">
      <h2>Daftar Sidik Jari</h2>
      <label for="nama">Nama</label>
      <input type="text" id="nama" placeholder="contoh: Budi Santoso">
      <button class="full" id="btnEnroll">Mulai Enroll</button>
    </div>

    <div class="card">
      <h2>Cek Sidik Jari</h2>
      <p class="sub" style="margin-bottom: 14px;">Tempel jari langsung, sistem yang mencari cocok dengan siapa di database — tidak perlu isi nama.</p>
      <button class="full" id="btnIdentify">Scan & Cek</button>
    </div>
  </div>

  <div class="card">
    <h2>Daftar Terdaftar</h2>
    <table>
      <thead>
        <tr><th>Nama</th><th>Terdaftar pada</th><th></th></tr>
      </thead>
      <tbody id="fingerprintsBody">
        @forelse ($fingerprints as $fingerprint)
          <tr>
            <td>{{ $fingerprint->nama }}</td>
            <td>{{ $fingerprint->created_at->format('d M Y H:i') }}</td>
            <td style="text-align: right;">
              <button class="btn-secondary btn-sm btn-delete" data-nama="{{ $fingerprint->nama }}">Hapus</button>
            </td>
          </tr>
        @empty
          <tr class="empty-row"><td colspan="3">Belum ada yang terdaftar.</td></tr>
        @endforelse
      </tbody>
    </table>
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

const csrfToken = document.querySelector('meta[name="csrf-token"]').content;

// --- Enroll ---
const btnEnroll = document.getElementById('btnEnroll');
const namaInput = document.getElementById('nama');

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
      location.reload();
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

// --- Cek Sidik Jari (identifikasi 1:N, tanpa isi nama) ---
const btnIdentify = document.getElementById('btnIdentify');

btnIdentify.addEventListener('click', async () => {
  btnIdentify.disabled = true;
  ScanModal.open('Cek Sidik Jari', 1);

  try {
    const res = await fetch(AGENT_URL + '/identify', { method: 'POST' });
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || 'Gagal mengecek sidik jari');

    if (data.match) {
      ScanModal.success('Cocok dengan "' + data.nama + '". Skor: ' + data.score);
    } else {
      ScanModal.error('Sidik jari tidak dikenali. Skor: ' + data.score, 'Coba Lagi');
    }
  } catch (err) {
    ScanModal.error(err.message, 'Coba Lagi');
  } finally {
    btnIdentify.disabled = false;
  }
});

// --- Hapus data terdaftar ---
document.querySelectorAll('.btn-delete').forEach((btn) => {
  btn.addEventListener('click', async () => {
    const nama = btn.dataset.nama;
    if (!confirm('Yakin mau hapus data sidik jari "' + nama + '"?')) return;

    btn.disabled = true;
    try {
      const res = await fetch('/api/fingerprints/' + encodeURIComponent(nama), {
        method: 'DELETE',
        headers: { 'X-CSRF-TOKEN': csrfToken },
      });
      if (!res.ok) throw new Error('Gagal menghapus data');
      location.reload();
    } catch (err) {
      alert(err.message);
      btn.disabled = false;
    }
  });
});
</script>
@endsection
