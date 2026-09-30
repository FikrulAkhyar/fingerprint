using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FingerprintAgent
{
    // Helper JSON super minimal — sengaja tanpa library eksternal (NuGet)
    // supaya Agent tidak butuh koneksi internet untuk restore package saat
    // build/deploy di jaringan bank yang mungkin dibatasi. Cuma menangani
    // bentuk data flat yang dipakai di sini (object rata / satu field
    // string, atau array of flat object untuk /identify), bukan JSON umum.
    public static class MiniJson
    {
        public static string WriteObject(params (string key, object value)[] fields)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(fields[i].key).Append("\":").Append(FormatValue(fields[i].value));
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "null";
            if (value is bool b) return b ? "true" : "false";
            if (value is int || value is long || value is double) return value.ToString();
            return "\"" + Escape(value.ToString()) + "\"";
        }

        public static string ExtractString(string json, string key)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return match.Success ? Unescape(match.Groups[1].Value) : null;
        }

        // Pecah array JSON flat (tanpa object bersarang) jadi daftar string
        // object mentah, tiap elemen lalu bisa dibaca lagi pakai ExtractString.
        public static List<string> ExtractObjects(string json)
        {
            var results = new List<string>();
            foreach (Match m in Regex.Matches(json, "\\{[^{}]*\\}"))
            {
                results.Add(m.Value);
            }
            return results;
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        // "/" wajib dihandle — base64 sering mengandung karakter ini, dan
        // json_encode PHP (dipakai Laravel) meng-escape-nya jadi "\/" secara
        // default. Tanpa ini, template yang diterima jadi rusak/invalid.
        private static string Unescape(string s) => s.Replace("\\/", "/").Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}
