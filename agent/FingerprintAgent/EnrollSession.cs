using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FingerprintAgent
{
    // State satu proses enrollment (3x scan + merge), supaya browser bisa
    // polling progresnya lewat GET /enroll/status sementara proses jalan
    // di background thread (scan jari butuh beberapa detik per langkah).
    public class EnrollSession
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public string Nama { get; set; }
        public int Step { get; set; }
        public bool Done { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string TemplateBase64 { get; set; }
        public bool UsedMergeFallback { get; set; }
    }

    public class EnrollSessionManager
    {
        private const int RequiredScans = 3;

        private readonly ConcurrentDictionary<string, EnrollSession> _sessions =
            new ConcurrentDictionary<string, EnrollSession>();
        private readonly ZkFingerService _zk;

        public EnrollSessionManager(ZkFingerService zk)
        {
            _zk = zk;
        }

        public EnrollSession Start(string nama)
        {
            var session = new EnrollSession { Nama = nama };
            _sessions[session.Id] = session;
            Task.Run(() => Run(session));
            return session;
        }

        public EnrollSession Get(string sessionId)
        {
            _sessions.TryGetValue(sessionId ?? string.Empty, out var session);
            return session;
        }

        private void Run(EnrollSession session)
        {
            try
            {
                var scans = new byte[RequiredScans][];
                for (int i = 0; i < RequiredScans; i++)
                {
                    byte[] captured = _zk.CaptureOnce();
                    if (captured == null)
                    {
                        session.ErrorMessage = "Waktu habis menunggu jari ke-" + (i + 1) + ".";
                        session.Done = true;
                        return;
                    }
                    scans[i] = captured;
                    session.Step = i + 1;
                }

                byte[] finalTemplate;
                try
                {
                    finalTemplate = _zk.MergeTemplates(scans[0], scans[1], scans[2]);
                }
                catch (InvalidOperationException ex)
                {
                    // SDK menolak menggabungkan 3 capture jadi satu (lihat
                    // catatan di ZkFingerService.MergeTemplates). Daripada
                    // bikin user harus ulang dari awal, fallback: pakai
                    // capture terakhir langsung sebagai template tunggal.
                    // Trade-off: sedikit lebih rentan false-reject saat
                    // verifikasi dibanding hasil merge SDK, tapi enrollment
                    // tetap bisa selesai.
                    Logger.Error("Merge gagal, fallback ke single capture: " + ex.Message);
                    finalTemplate = scans[RequiredScans - 1];
                    session.UsedMergeFallback = true;
                }

                // Template dikembalikan ke browser saja — browser yang POST
                // ke Laravel (lihat resources/views/fingerprints/enroll.blade.php
                // di web/). Agent tidak menyimpan apa pun secara permanen.
                session.TemplateBase64 = Convert.ToBase64String(finalTemplate);
                session.Success = true;
                session.Done = true;
            }
            catch (Exception ex)
            {
                session.ErrorMessage = ex.Message;
                session.Done = true;
            }
        }
    }
}
