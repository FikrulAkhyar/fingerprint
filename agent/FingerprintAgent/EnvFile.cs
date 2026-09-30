using System.Collections.Generic;
using System.IO;

namespace FingerprintAgent
{
    // Baca file `.env` sederhana (format KEY=VALUE, satu per baris, baris
    // diawali `#` dianggap komentar) — dipakai supaya konfigurasi Agent
    // (mis. alamat backend) gampang diubah tanpa build ulang, konsisten
    // dengan pola `.env` yang sudah dipakai di web/ (Laravel).
    public static class EnvFile
    {
        public static Dictionary<string, string> Load(string path)
        {
            var values = new Dictionary<string, string>();
            if (!File.Exists(path))
                return values;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                int idx = line.IndexOf('=');
                if (idx <= 0)
                    continue;

                string key = line.Substring(0, idx).Trim();
                string value = line.Substring(idx + 1).Trim();
                values[key] = value;
            }

            return values;
        }
    }
}
