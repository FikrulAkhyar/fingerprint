using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace FingerprintAgent
{
    // Local HTTP API yang dipanggil browser (localhost only). Kontraknya
    // mengikuti draft di design.md §5.1: POST /enroll/start, GET
    // /enroll/status, POST /verify.
    public class HttpApi
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly ZkFingerService _zk;
        private readonly EnrollSessionManager _enrollManager;
        private readonly ITemplateStore _store;

        public HttpApi(string prefix, ZkFingerService zk, EnrollSessionManager enrollManager, ITemplateStore store)
        {
            _listener.Prefixes.Add(prefix);
            _zk = zk;
            _enrollManager = enrollManager;
            _store = store;
        }

        public void Start()
        {
            _listener.Start();
            while (_listener.IsListening)
            {
                var context = _listener.GetContext();
                ThreadPool.QueueUserWorkItem(_ => Handle(context));
            }
        }

        private void Handle(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            res.Headers.Add("Access-Control-Allow-Methods", "GET,POST,OPTIONS");

            try
            {
                if (req.HttpMethod == "OPTIONS")
                {
                    res.StatusCode = 204;
                    res.Close();
                    return;
                }

                if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/enroll/start")
                    HandleEnrollStart(req, res);
                else if (req.HttpMethod == "GET" && req.Url.AbsolutePath == "/enroll/status")
                    HandleEnrollStatus(req, res);
                else if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/verify")
                    HandleVerify(req, res);
                else if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/identify")
                    HandleIdentify(res);
                else
                    WriteJson(res, 404, MiniJson.WriteObject(("error", "Not found")));
            }
            catch (Exception ex)
            {
                WriteJson(res, 500, MiniJson.WriteObject(("error", ex.Message)));
            }
        }

        private void HandleEnrollStart(HttpListenerRequest req, HttpListenerResponse res)
        {
            string nama = MiniJson.ExtractString(ReadBody(req), "nama");
            if (string.IsNullOrEmpty(nama))
            {
                WriteJson(res, 422, MiniJson.WriteObject(("error", "nama wajib diisi")));
                return;
            }

            var session = _enrollManager.Start(nama);
            WriteJson(res, 200, MiniJson.WriteObject(("session_id", session.Id)));
        }

        private void HandleEnrollStatus(HttpListenerRequest req, HttpListenerResponse res)
        {
            string sessionId = req.QueryString["session_id"];
            var session = _enrollManager.Get(sessionId);
            if (session == null)
            {
                WriteJson(res, 404, MiniJson.WriteObject(("error", "Session tidak ditemukan")));
                return;
            }

            if (!session.Done)
            {
                WriteJson(res, 200, MiniJson.WriteObject(("step", session.Step), ("done", false)));
                return;
            }

            WriteJson(res, 200, session.Success
                ? MiniJson.WriteObject(("done", true), ("success", true), ("template", session.TemplateBase64), ("merge_fallback", session.UsedMergeFallback))
                : MiniJson.WriteObject(("done", true), ("success", false), ("error", session.ErrorMessage)));
        }

        private void HandleVerify(HttpListenerRequest req, HttpListenerResponse res)
        {
            string nama = MiniJson.ExtractString(ReadBody(req), "nama");
            if (string.IsNullOrEmpty(nama))
            {
                WriteJson(res, 422, MiniJson.WriteObject(("error", "nama wajib diisi")));
                return;
            }

            byte[] stored = _store.Get(nama);
            if (stored == null)
            {
                WriteJson(res, 404, MiniJson.WriteObject(("error", "Belum ada sidik jari terdaftar untuk nama ini")));
                return;
            }

            byte[] captured = _zk.CaptureOnce();
            if (captured == null)
            {
                WriteJson(res, 408, MiniJson.WriteObject(("error", "Waktu habis, tidak ada jari terdeteksi")));
                return;
            }

            int score = _zk.Match(captured, stored);
            WriteJson(res, 200, MiniJson.WriteObject(("match", score > 0), ("score", score)));
        }

        // 1:N — cari ke semua template tersimpan, tanpa perlu tahu nama
        // duluan. Testing convenience, di luar kontrak produksi §5.1
        // design.md (yang aslinya cuma 1:1, karena no. rekening/nama selalu
        // sudah diketahui dari alur teller sebenarnya).
        private void HandleIdentify(HttpListenerResponse res)
        {
            var candidates = _store.List();
            if (candidates.Count == 0)
            {
                WriteJson(res, 404, MiniJson.WriteObject(("error", "Belum ada sidik jari yang terdaftar")));
                return;
            }

            byte[] captured = _zk.CaptureOnce();
            if (captured == null)
            {
                WriteJson(res, 408, MiniJson.WriteObject(("error", "Waktu habis, tidak ada jari terdeteksi")));
                return;
            }

            string bestNama = null;
            int bestScore = 0;
            foreach (var (nama, template) in candidates)
            {
                int score = _zk.Match(captured, template);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestNama = nama;
                }
            }

            WriteJson(res, 200, bestNama != null
                ? MiniJson.WriteObject(("match", true), ("nama", bestNama), ("score", bestScore))
                : MiniJson.WriteObject(("match", false), ("score", 0)));
        }

        private static string ReadBody(HttpListenerRequest req)
        {
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void WriteJson(HttpListenerResponse res, int statusCode, string json)
        {
            res.StatusCode = statusCode;
            res.ContentType = "application/json";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.OutputStream.Close();
        }
    }
}
