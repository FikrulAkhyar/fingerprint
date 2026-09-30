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
    }

    public class EnrollSessionManager
    {
        private const int RequiredScans = 3;

        private readonly ConcurrentDictionary<string, EnrollSession> _sessions =
            new ConcurrentDictionary<string, EnrollSession>();
        private readonly ZkFingerService _zk;
        private readonly ITemplateStore _store;

        public EnrollSessionManager(ZkFingerService zk, ITemplateStore store)
        {
            _zk = zk;
            _store = store;
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

                byte[] merged = _zk.MergeTemplates(scans[0], scans[1], scans[2]);
                _store.Save(session.Nama, merged);

                session.TemplateBase64 = Convert.ToBase64String(merged);
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
