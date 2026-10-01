using System;
using System.IO;
using System.Net;
using System.Text;

namespace FingerprintAgent
{
    // Login/logout ke ARB+ pakai 1 akun sistem (ARB_USERNAME/ARB_PASSWORD di
    // .env) — dipakai Agent buat manggil progId MADC0005 server-to-server,
    // tanpa ada user beneran yang login. Pola & alasannya sama seperti
    // ArbSystemAuthService di m-approval-v2 (akun sistem global, bukan akun
    // user asli), tapi ditulis ulang independen di sini — bukan numpang
    // m-approval-v2, karena itu aplikasi lain dan logicnya cukup sederhana.
    //
    // Kenapa perlu logout eksplisit: ARB+ nyimpen flag "akun sedang login" di
    // DB yang CUMA ke-clear lewat panggilan logout eksplisit (bukan otomatis
    // kalau token basi/expired). Jadi:
    // (1) RefreshToken() logout token lama dulu sebelum login ulang,
    // (2) Logout() dipanggil pas Agent keluar normal (lihat TrayApplicationContext),
    // (3) konstruktor baca sisa token dari file sesi lokal (kalau proses
    //     sebelumnya mati gak wajar / crash) dan logout duluan — ini jaring
    //     pengaman kalau (2) gak sempat kejadian.
    public class ArbAuthService
    {
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly string _sessionFilePath;
        private readonly object _lock = new object();
        private string _token;

        public ArbAuthService(string baseUrl, string username, string password, string sessionFilePath)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _username = username;
            _password = password;
            _sessionFilePath = sessionFilePath;

            string stale = ReadSessionFile();
            if (!string.IsNullOrEmpty(stale))
            {
                Logger.Info("Ditemukan sesi ARB+ peninggalan proses sebelumnya — logout duluan.");
                TryLogout(stale);
                ClearSessionFile();
            }
        }

        public string GetToken()
        {
            lock (_lock)
            {
                return _token ?? (_token = Login());
            }
        }

        public string RefreshToken()
        {
            lock (_lock)
            {
                if (_token != null)
                    TryLogout(_token);
                _token = Login();
                return _token;
            }
        }

        public void Logout()
        {
            lock (_lock)
            {
                if (_token != null)
                    TryLogout(_token);
                _token = null;
                ClearSessionFile();
            }
        }

        private string Login()
        {
            if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrWhiteSpace(_password))
                throw new InvalidOperationException("ARB_USERNAME/ARB_PASSWORD belum diisi di .env");

            string url = _baseUrl + "/CorePublic/api/public/webarb/login/login";
            string body = MiniJson.WriteObject(("username", _username), ("password", _password));

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = 10000;

            byte[] data = Encoding.UTF8.GetBytes(body);
            using (var stream = request.GetRequestStream())
                stream.Write(data, 0, data.Length);

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string responseBody = reader.ReadToEnd();
                string token = MiniJson.ExtractString(responseBody, "token");
                if (token == null)
                    throw new InvalidOperationException("Login ARB+ gagal: tidak ada token di response.");

                WriteSessionFile(token);
                Logger.Info("Login ARB+ berhasil (akun sistem).");
                return token;
            }
        }

        private void TryLogout(string token)
        {
            try
            {
                string url = _baseUrl + "/CoreMain/api/public/webarb/login/logout";
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("Authorization", token);
                request.Timeout = 5000;
                using (request.GetResponse()) { }
            }
            catch (Exception ex)
            {
                // Best-effort — kegagalan logout tidak boleh menghalangi Agent
                // tetap jalan/keluar normal.
                Logger.Error("Gagal logout ARB+ (diabaikan, best-effort): " + ex.Message);
            }
        }

        private void WriteSessionFile(string token)
        {
            try { File.WriteAllText(_sessionFilePath, token); }
            catch { /* best-effort, bukan hal fatal kalau gagal tulis */ }
        }

        private string ReadSessionFile()
        {
            try { return File.Exists(_sessionFilePath) ? File.ReadAllText(_sessionFilePath).Trim() : null; }
            catch { return null; }
        }

        private void ClearSessionFile()
        {
            try { if (File.Exists(_sessionFilePath)) File.Delete(_sessionFilePath); }
            catch { /* best-effort */ }
        }
    }
}
