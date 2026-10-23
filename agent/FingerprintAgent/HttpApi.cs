using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FingerprintAgent
{
    // Local HTTP API yang dipanggil browser CBS (localhost only): POST
    // /enroll/start, GET /enroll/status, POST /verify. Identifier nasabah
    // dikirim sebagai field generik "key" (di sisi ARB+ nilainya cf_mast_id),
    // supaya kontrak Agent tidak terikat istilah ARB+.
    //
    // Dibangun di atas TcpListener + parser manual (MiniHttp.cs), bukan
    // System.Net.HttpListener — supaya laptop client tidak perlu izin
    // `netsh http add urlacl` sama sekali buat bind ke 127.0.0.1.
    public class HttpApi
    {
        private readonly TcpListener _listener;
        private readonly ZkFingerService _zk;
        private readonly EnrollSessionManager _enrollManager;
        private readonly ITemplateStore _store;

        public HttpApi(int port, ZkFingerService zk, EnrollSessionManager enrollManager, ITemplateStore store)
        {
            _listener = new TcpListener(IPAddress.Loopback, port);
            _zk = zk;
            _enrollManager = enrollManager;
            _store = store;
        }

        public void Start()
        {
            _listener.Start();
            while (true)
            {
                var client = _listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => Handle(client));
            }
        }

        private void Handle(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                stream.ReadTimeout = 10000;
                stream.WriteTimeout = 10000;

                try
                {
                    var req = MiniHttpRequest.Parse(stream);
                    if (req == null) return; // koneksi kosong/putus di awal, abaikan

                    if (req.Method == "OPTIONS")
                    {
                        MiniHttpRequest.WriteResponse(stream, 204, null);
                        return;
                    }

                    if (req.Method == "POST" && req.Path == "/enroll/start")
                        HandleEnrollStart(req, stream);
                    else if (req.Method == "GET" && req.Path == "/enroll/status")
                        HandleEnrollStatus(req, stream);
                    else if (req.Method == "POST" && req.Path == "/verify")
                        HandleVerify(req, stream);
                    else
                        MiniHttpRequest.WriteResponse(stream, 404, MiniJson.WriteObject(("error", "Not found")));
                }
                catch (Exception ex)
                {
                    try { MiniHttpRequest.WriteResponse(stream, 500, MiniJson.WriteObject(("error", ex.Message))); }
                    catch { /* koneksi sudah keburu putus, tidak ada yang bisa dilakukan */ }
                }
            }
        }

        private void HandleEnrollStart(MiniHttpRequest req, Stream stream)
        {
            string key = MiniJson.ExtractString(req.Body, "key");
            if (string.IsNullOrEmpty(key))
            {
                MiniHttpRequest.WriteResponse(stream, 422, MiniJson.WriteObject(("error", "key wajib diisi")));
                return;
            }

            var session = _enrollManager.Start(key);
            MiniHttpRequest.WriteResponse(stream, 200, MiniJson.WriteObject(("session_id", session.Id)));
        }

        private void HandleEnrollStatus(MiniHttpRequest req, Stream stream)
        {
            req.Query.TryGetValue("session_id", out var sessionId);
            var session = _enrollManager.Get(sessionId);
            if (session == null)
            {
                MiniHttpRequest.WriteResponse(stream, 404, MiniJson.WriteObject(("error", "Session tidak ditemukan")));
                return;
            }

            if (!session.Done)
            {
                MiniHttpRequest.WriteResponse(stream, 200, MiniJson.WriteObject(("step", session.Step), ("done", false)));
                return;
            }

            MiniHttpRequest.WriteResponse(stream, 200, session.Success
                ? MiniJson.WriteObject(("done", true), ("success", true), ("template", session.TemplateBase64), ("merge_fallback", session.UsedMergeFallback))
                : MiniJson.WriteObject(("done", true), ("success", false), ("error", session.ErrorMessage)));
        }

        private void HandleVerify(MiniHttpRequest req, Stream stream)
        {
            string key = MiniJson.ExtractString(req.Body, "key");
            if (string.IsNullOrEmpty(key))
            {
                MiniHttpRequest.WriteResponse(stream, 422, MiniJson.WriteObject(("error", "key wajib diisi")));
                return;
            }

            byte[] stored = _store.Get(key);
            if (stored == null)
            {
                MiniHttpRequest.WriteResponse(stream, 404, MiniJson.WriteObject(("error", "Belum ada sidik jari terdaftar untuk key ini")));
                return;
            }

            byte[] captured = _zk.CaptureOnce();
            if (captured == null)
            {
                MiniHttpRequest.WriteResponse(stream, 408, MiniJson.WriteObject(("error", "Waktu habis, tidak ada jari terdeteksi")));
                return;
            }

            int score = _zk.Match(captured, stored);
            MiniHttpRequest.WriteResponse(stream, 200, MiniJson.WriteObject(("match", score > 0), ("score", score)));
        }
    }
}
