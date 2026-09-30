using System.Text;
using System.Text.RegularExpressions;

namespace FingerprintAgent
{
    // Helper JSON super minimal — sengaja tanpa library eksternal (NuGet)
    // supaya Agent tidak butuh koneksi internet untuk restore package saat
    // build/deploy di jaringan bank yang mungkin dibatasi. Cuma menangani
    // bentuk data flat yang dipakai di sini (object rata / satu field
    // string), bukan JSON umum.
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

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static string Unescape(string s) => s.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}
