<!doctype html>
<html lang="id">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="csrf-token" content="{{ csrf_token() }}">
<title>@yield('title', 'Test Fingerprint') — ZKTeco Live20R</title>
<style>
  :root {
    --primary: #2563eb;
    --primary-dark: #1d4ed8;
    --bg: #f4f6f9;
    --card: #ffffff;
    --border: #e2e8f0;
    --text: #0f172a;
    --muted: #64748b;
    --success: #15803d;
    --error: #b91c1c;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0;
    font-family: -apple-system, "Segoe UI", Roboto, Arial, sans-serif;
    background: var(--bg);
    color: var(--text);
  }
  header {
    background: #fff;
    border-bottom: 1px solid var(--border);
    padding: 14px 24px;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  header a { color: var(--text); text-decoration: none; font-weight: 600; font-size: 15px; }
  header nav a { margin-left: 18px; font-weight: 500; color: var(--muted); font-size: 14px; }
  header nav a.active, header nav a:hover { color: var(--primary); }
  main { max-width: 880px; margin: 32px auto; padding: 0 16px; }
  .sub { color: var(--muted); font-size: 14px; margin-bottom: 20px; }
  .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 20px; }
  @media (max-width: 720px) { .grid { grid-template-columns: 1fr; } }
  .card { background: var(--card); border: 1px solid var(--border); border-radius: 12px; padding: 22px; margin-bottom: 20px; }
  .card h2 { font-size: 16px; margin: 0 0 16px; }
  label { display: block; font-size: 13px; color: var(--muted); margin-bottom: 4px; }
  input[type=text] {
    width: 100%; padding: 10px 12px; border: 1px solid var(--border);
    border-radius: 8px; font-size: 14px; margin-bottom: 14px;
  }
  button {
    padding: 11px 16px; border: none; border-radius: 8px;
    background: var(--primary); color: #fff; font-size: 14px; font-weight: 600; cursor: pointer;
  }
  button.full { width: 100%; }
  button:hover { background: var(--primary-dark); }
  button:disabled { background: #94a3b8; cursor: not-allowed; }
  .btn-secondary { background: #eef2f7; color: var(--text); }
  .btn-secondary:hover { background: #e2e8f0; }
  .agent-status { font-size: 12px; color: var(--muted); margin-bottom: 20px; }
  .agent-status.ok { color: var(--success); }
  .agent-status.down { color: var(--error); }
  code { background: #eef2f7; padding: 1px 5px; border-radius: 4px; font-size: 12px; }
  table { width: 100%; border-collapse: collapse; }
  th, td { text-align: left; padding: 8px 10px; border-bottom: 1px solid var(--border); font-size: 14px; }
  th { color: var(--muted); font-weight: 600; font-size: 12px; text-transform: uppercase; }
  .empty-row td { color: var(--muted); text-align: center; padding: 20px; }

  /* Modal scan sidik jari */
  .scan-modal-overlay {
    position: fixed; inset: 0; background: rgba(15, 23, 42, .55);
    display: none; align-items: center; justify-content: center;
    padding: 16px; z-index: 1000;
  }
  .scan-modal-overlay.show { display: flex; }
  .scan-modal {
    background: #fff; border-radius: 16px; padding: 32px 28px;
    width: 100%; max-width: 320px; text-align: center;
    box-shadow: 0 20px 50px rgba(0,0,0,.25);
  }
  .scan-modal h3 { margin: 0 0 18px; font-size: 15px; color: var(--muted); font-weight: 600; }

  .scan-icon-wrap {
    position: relative; width: 120px; height: 120px; margin: 0 auto 20px;
  }
  .scan-icon-wrap .scan-ring {
    position: absolute; inset: 0; border-radius: 50%;
    border: 3px solid var(--primary); opacity: 0;
    animation: scan-pulse 1.8s ease-out infinite;
  }
  .scan-icon-wrap .scan-ring.delay-1 { animation-delay: .6s; }
  .scan-icon-wrap .scan-ring.delay-2 { animation-delay: 1.2s; }
  .scan-icon-wrap.done .scan-ring { animation: none; opacity: 0; }
  @keyframes scan-pulse {
    0% { transform: scale(.75); opacity: .55; }
    100% { transform: scale(1.5); opacity: 0; }
  }

  /* Icon fingerprint: 2 lapis SVG identik ditumpuk — lapis bawah abu-abu
     (selalu penuh), lapis atas berwarna gradient tapi di-clip dari bawah
     sesuai progres (1/4 → 3/4 → penuh saat scan ke-1/2/3 berhasil). */
  .fp-layer {
    position: absolute; inset: 0;
    display: flex; align-items: center; justify-content: center;
  }
  .fp-layer svg { width: 88px; height: 88px; }
  .fp-outline svg { color: #cbd5e1; }
  .fp-fill {
    clip-path: inset(100% 0 0 0);
    transition: clip-path .6s ease;
  }
  .scan-icon-wrap.hide-fp .fp-layer,
  .scan-icon-wrap.hide-fp .scan-ring { display: none; }

  .scan-result-icon {
    position: absolute; inset: 0; display: none;
    align-items: center; justify-content: center;
  }
  .scan-result-icon.show { display: flex; }
  .scan-result-icon .badge {
    width: 72px; height: 72px; border-radius: 50%; color: #fff;
    display: flex; align-items: center; justify-content: center; font-size: 32px;
  }
  .scan-result-icon .badge.error { background: var(--error); }

  .scan-status { font-size: 14px; color: var(--text); margin-bottom: 18px; min-height: 20px; }
  .scan-actions { display: none; }
  .scan-actions.show { display: block; }
  .scan-actions button { width: 100%; }
</style>
</head>
<body>
<header>
  <a href="{{ route('fingerprints.index') }}">Test Fingerprint (Laravel)</a>
</header>
<main>
  @yield('content')
</main>

<div class="scan-modal-overlay" id="scanModalOverlay">
  <div class="scan-modal">
    <h3 id="scanModalTitle">Scan Sidik Jari</h3>

    <div class="scan-icon-wrap" id="scanIconWrap">
      <div class="scan-ring"></div>
      <div class="scan-ring delay-1"></div>
      <div class="scan-ring delay-2"></div>

      <div class="fp-layer fp-outline">
        <svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg" fill="none" stroke="currentColor" stroke-width="7" stroke-linecap="round">
          <circle cx="100" cy="98" r="86" stroke-dasharray="26 9 14 11" stroke-dashoffset="4"/>
          <circle cx="100" cy="98" r="74" stroke-dasharray="20 8 16 9" stroke-dashoffset="30"/>
          <circle cx="100" cy="98" r="62" stroke-dasharray="24 7 12 10" stroke-dashoffset="12"/>
          <circle cx="100" cy="98" r="50" stroke-dasharray="18 6 14 8" stroke-dashoffset="40"/>
          <circle cx="100" cy="98" r="38" stroke-dasharray="16 6 10 7" stroke-dashoffset="6"/>
          <circle cx="100" cy="98" r="26" stroke-dasharray="14 5 8 6" stroke-dashoffset="20"/>
          <circle cx="100" cy="98" r="14" stroke-dasharray="10 5"/>
          <path d="M100 150 q-6 8 0 16 q6 8 0 16"/>
        </svg>
      </div>

      <div class="fp-layer fp-fill" id="fpFillLayer">
        <svg viewBox="0 0 200 200" xmlns="http://www.w3.org/2000/svg" fill="none" stroke-width="7" stroke-linecap="round">
          <defs>
            <linearGradient id="fpGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0%" stop-color="#a855f7"/>
              <stop offset="55%" stop-color="#ec4899"/>
              <stop offset="100%" stop-color="#f97316"/>
            </linearGradient>
          </defs>
          <g stroke="url(#fpGradient)">
            <circle cx="100" cy="98" r="86" stroke-dasharray="26 9 14 11" stroke-dashoffset="4"/>
            <circle cx="100" cy="98" r="74" stroke-dasharray="20 8 16 9" stroke-dashoffset="30"/>
            <circle cx="100" cy="98" r="62" stroke-dasharray="24 7 12 10" stroke-dashoffset="12"/>
            <circle cx="100" cy="98" r="50" stroke-dasharray="18 6 14 8" stroke-dashoffset="40"/>
            <circle cx="100" cy="98" r="38" stroke-dasharray="16 6 10 7" stroke-dashoffset="6"/>
            <circle cx="100" cy="98" r="26" stroke-dasharray="14 5 8 6" stroke-dashoffset="20"/>
            <circle cx="100" cy="98" r="14" stroke-dasharray="10 5"/>
            <path d="M100 150 q-6 8 0 16 q6 8 0 16"/>
          </g>
        </svg>
      </div>

      <div class="scan-result-icon" id="scanResultIcon"></div>
    </div>

    <div class="scan-status" id="scanStatusText"></div>
    <div class="scan-actions" id="scanActions"></div>
  </div>
</div>

<script>
  const AGENT_URL = 'http://127.0.0.1:9001';

  const ScanModal = (() => {
    const overlay = document.getElementById('scanModalOverlay');
    const titleEl = document.getElementById('scanModalTitle');
    const iconWrap = document.getElementById('scanIconWrap');
    const fillLayer = document.getElementById('fpFillLayer');
    const resultIcon = document.getElementById('scanResultIcon');
    const statusEl = document.getElementById('scanStatusText');
    const actionsEl = document.getElementById('scanActions');

    let totalSteps = 1;

    // Bukan linear (1/3, 2/3, 3/3) — sengaja 1/4 lalu 3/4 lalu penuh,
    // supaya lompatan terakhir terasa lebih "klik" sebagai tanda selesai.
    function fractionForStep(step, total) {
      if (total === 3) {
        return [0.25, 0.75, 1][step - 1] ?? 1;
      }
      return Math.min(step / total, 1);
    }

    function setFill(fraction) {
      const topInset = Math.round((1 - fraction) * 100);
      fillLayer.style.clipPath = `inset(${topInset}% 0 0 0)`;
    }

    function open(title, steps) {
      totalSteps = steps;
      titleEl.textContent = title;
      iconWrap.className = 'scan-icon-wrap';
      resultIcon.classList.remove('show');
      resultIcon.innerHTML = '';
      setFill(0);
      actionsEl.classList.remove('show');
      actionsEl.innerHTML = '';
      setStatus('Tempel jari pada alat...');
      overlay.classList.add('show');
    }

    function setStep(step) {
      setFill(fractionForStep(step, totalSteps));
    }

    function setStatus(text) {
      statusEl.textContent = text;
    }

    function success(message, onClose) {
      iconWrap.classList.add('done');
      setFill(1);
      setStatus(message);
      showAction('Tutup', onClose);
    }

    function error(message, actionLabel, onAction) {
      iconWrap.classList.add('done', 'hide-fp');
      resultIcon.innerHTML = '<div class="badge error">&#10005;</div>';
      resultIcon.classList.add('show');
      setStatus(message);
      showAction(actionLabel || 'Tutup', onAction);
    }

    function showAction(label, handler) {
      actionsEl.innerHTML = '';
      const btn = document.createElement('button');
      btn.className = 'btn-secondary';
      btn.textContent = label;
      btn.addEventListener('click', () => {
        close();
        if (handler) handler();
      });
      actionsEl.appendChild(btn);
      actionsEl.classList.add('show');
    }

    function close() {
      overlay.classList.remove('show');
    }

    return { open, setStep, setStatus, success, error, close };
  })();
</script>
@yield('scripts')
</body>
</html>
