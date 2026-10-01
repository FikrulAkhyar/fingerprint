using System;
using System.Collections.Generic;
using System.Net;

namespace FingerprintAgent
{
    // Sumber template tersimpan, dipakai saat /verify (1:1, by nama) dan
    // /identify (1:N, cari ke semua data — testing convenience, di luar
    // kontrak produksi §5.1/§5.2 design.md yang aslinya cuma 1:1).
    //
    // Enrollment (menyimpan template baru) TIDAK lewat sini lagi — browser
    // yang langsung POST ke backend setelah Agent selesai capture+merge
    // (lihat design.md §4: template naik lewat browser cuma sekali, saat
    // enrollment). Di sini Agent cuma perlu GET template lewat backend
    // server-to-server untuk keperluan Match(), sesuai §5.2.
    //
    // BackendTemplateStore ini memanggil aplikasi Laravel di web/ (yang saat
    // ini berperan sebagai pengganti CBS untuk testing) lewat REST API biasa
    // — begitu diarahkan ke CBS asli nanti, cukup ganti ARB_URL di `.env`
    // (lihat Program.cs/EnvFile.cs), endpoint /api/fingerprints/{nama}
    // tinggal dibuatkan yang serupa di CBS.
    public interface ITemplateStore
    {
        byte[] Get(string nama);
        List<(string Nama, byte[] Template)> List();
    }

    public class BackendTemplateStore : ITemplateStore
    {
        // Bukan readonly — bisa diganti saat runtime lewat UpdateBaseUrl(),
        // dipakai menu "Ubah ARB_URL..." di system tray (TrayApplicationContext).
        private string _baseUrl;

        public BackendTemplateStore(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public void UpdateBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public byte[] Get(string nama)
        {
            string url = _baseUrl + "/api/fingerprints/" + Uri.EscapeDataString(nama);

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json";
            request.Timeout = 5000;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    string body = reader.ReadToEnd();
                    string base64 = MiniJson.ExtractString(body, "template");
                    return base64 != null ? Convert.FromBase64String(base64) : null;
                }
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse resp && resp.StatusCode == HttpStatusCode.NotFound)
            {
                return null; // belum terdaftar
            }
        }

        public List<(string Nama, byte[] Template)> List()
        {
            string url = _baseUrl + "/api/fingerprints";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = "application/json";
            request.Timeout = 5000;

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
            {
                string body = reader.ReadToEnd();
                var result = new List<(string, byte[])>();

                foreach (string obj in MiniJson.ExtractObjects(body))
                {
                    string nama = MiniJson.ExtractString(obj, "nama");
                    string base64 = MiniJson.ExtractString(obj, "template");
                    if (nama != null && base64 != null)
                        result.Add((nama, Convert.FromBase64String(base64)));
                }

                return result;
            }
        }
    }
}
