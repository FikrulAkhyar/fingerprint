using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FingerprintAgent
{
    // Tempat menyimpan template supaya bisa diambil lagi saat /verify.
    //
    // PENTING: JsonFileTemplateStore ini cuma untuk testing standalone
    // (Tahap 2) di laptop ini, TANPA CBS. Di produksi nanti, sesuai
    // design.md §5.2, Agent akan panggil REST API CBS untuk ambil/simpan
    // template — bukan file lokal ini. Tinggal ganti implementasi
    // ITemplateStore-nya, endpoint HTTP Agent tidak perlu berubah.
    public interface ITemplateStore
    {
        byte[] Get(string nama);
        void Save(string nama, byte[] template);
        List<EnrolledEntry> List();
    }

    public class EnrolledEntry
    {
        public string Nama;
        public string EnrolledAt;
    }

    public class JsonFileTemplateStore : ITemplateStore
    {
        private readonly string _filePath;
        private readonly object _fileLock = new object();

        public JsonFileTemplateStore(string filePath)
        {
            _filePath = filePath;
        }

        public byte[] Get(string nama)
        {
            lock (_fileLock)
            {
                var data = Load();
                if (!data.TryGetValue(nama, out var raw))
                    return null;
                return Convert.FromBase64String(SplitTemplate(raw));
            }
        }

        public void Save(string nama, byte[] template)
        {
            lock (_fileLock)
            {
                var data = Load();
                string enrolledAt = DateTime.Now.ToString("o");
                data[nama] = enrolledAt + "|" + Convert.ToBase64String(template);
                Persist(data);
            }
        }

        public List<EnrolledEntry> List()
        {
            lock (_fileLock)
            {
                var data = Load();
                var result = new List<EnrolledEntry>();
                foreach (var kv in data)
                {
                    result.Add(new EnrolledEntry
                    {
                        Nama = kv.Key,
                        EnrolledAt = SplitTimestamp(kv.Value),
                    });
                }
                return result;
            }
        }

        // Nilai yang disimpan berbentuk "<timestamp>|<base64 template>".
        private static string SplitTimestamp(string raw)
        {
            int idx = raw.IndexOf('|');
            return idx >= 0 ? raw.Substring(0, idx) : "";
        }

        private static string SplitTemplate(string raw)
        {
            int idx = raw.IndexOf('|');
            return idx >= 0 ? raw.Substring(idx + 1) : raw;
        }

        private Dictionary<string, string> Load()
        {
            if (!File.Exists(_filePath))
                return new Dictionary<string, string>();

            string json = File.ReadAllText(_filePath, Encoding.UTF8);
            return MiniJson.ParseFlatStringDictionary(json);
        }

        private void Persist(Dictionary<string, string> data)
        {
            File.WriteAllText(_filePath, MiniJson.WriteFlatStringDictionary(data), Encoding.UTF8);
        }
    }
}
