using System;
using System.IO;
using System.Net;

namespace FingerprintAgent
{
    // Sumber template tersimpan, dipakai saat /verify (1:1, by "key" —
    // identifier generik, nilainya terserah pemanggil; ARB+ kirim cf_mast_id,
    // bukan nama asli). Tidak ada lagi dukungan 1:N — progId MADC0005 di ARB+
    // cuma punya lookup by cf_mast_id, sesuai proses bisnis yang memang 1:1.
    //
    // Enrollment (menyimpan template baru) TIDAK lewat sini — itu urusan
    // CBS/browser yang manggil progId CFMA0031 (method save_fingerprint)
    // langsung, pakai sesi login teller sendiri (bukan Agent). Agent cuma baca.
    public interface ITemplateStore
    {
        byte[] Get(string key);
    }

    // Manggil progId MADC0005 di ARB+ (kodenya ada di codebase ARB+ sendiri,
    // di luar project ini). Pola dispatch & format response sama seperti progId ARB+ lain: GET
    // .../CoreMain/api/public/MADC0005/read?methods=<nama_method>, response
    // sukses = JSON langsung, response error = {"error": "..."} atau
    // {"error": {"message": "...", ...}}.
    public class BackendTemplateStore : ITemplateStore
    {
        private const string ProgId = "MADC0005";

        // Bukan readonly — bisa diganti saat runtime lewat UpdateBaseUrl(),
        // dipakai menu "Ubah ARB_URL..." di system tray (TrayApplicationContext).
        private string _baseUrl;
        private readonly ArbAuthService _auth;

        private static readonly string[] SessionExpiredCodes =
            { "SY0002", "SY0003", "SY0005", "SY0006", "SY0007" };

        public BackendTemplateStore(string baseUrl, ArbAuthService auth)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _auth = auth;
        }

        public void UpdateBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        // "key" diteruskan ke progId sebagai parameter "cf_mast_id" — nama
        // parameter ARB+-nya, bukan nama kontrak HTTP Agent (sengaja beda,
        // supaya Agent tetap generic & tidak terikat istilah ARB+).
        public byte[] Get(string key)
        {
            string url = _baseUrl + "/CoreMain/api/public/" + ProgId +
                "/read?methods=get_template&cf_mast_id=" + Uri.EscapeDataString(key);

            string body = CallWithAuth(url);
            if (body == null) return null;

            string base64 = MiniJson.ExtractString(body, "template");
            return base64 != null ? Convert.FromBase64String(base64) : null;
        }

        // Login dulu kalau belum ada token; kalau ARB+ bilang sesi expired,
        // refresh token sekali lalu ulangi. Response error lain (mis. data
        // tidak ditemukan) dianggap "tidak ada" — dicatat ke log, return null.
        private string CallWithAuth(string url)
        {
            string token = _auth.GetToken();
            string body = DoGet(url, token);

            if (IsSessionExpired(body))
            {
                token = _auth.RefreshToken();
                body = DoGet(url, token);
            }

            if (IsErrorResponse(body))
            {
                Logger.Error("ARB+ merespons error (" + url + "): " + (MiniJson.ExtractString(body, "message") ?? body));
                return null;
            }

            return body;
        }

        private static string DoGet(string url, string token)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json";
            request.Headers.Add("Authorization", token);
            request.Timeout = 10000;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                    return reader.ReadToEnd();
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse resp)
            {
                using (var reader = new StreamReader(resp.GetResponseStream()))
                    return reader.ReadToEnd();
            }
        }

        private static bool IsSessionExpired(string body)
        {
            if (body == null) return false;
            string message = MiniJson.ExtractString(body, "message") ?? body;
            foreach (string code in SessionExpiredCodes)
                if (message.Contains("[" + code + "]"))
                    return true;
            return false;
        }

        private static bool IsErrorResponse(string body)
        {
            return body != null && body.Contains("\"error\"");
        }
    }
}
