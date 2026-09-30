using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FingerprintAgent
{
    // Helper JSON super minimal — sengaja tanpa library eksternal (NuGet)
    // supaya Agent tidak butuh koneksi internet untuk restore package saat
    // build/deploy di jaringan bank yang mungkin dibatasi. Cuma menangani
    // bentuk data flat yang dipakai endpoint-endpoint di sini, bukan JSON
    // umum (nested object/array tidak didukung).
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

        public static string WriteArray(IEnumerable<string> items)
        {
            return "[" + string.Join(",", items) + "]";
        }

        public static string ExtractString(string json, string key)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return match.Success ? Unescape(match.Groups[1].Value) : null;
        }

        public static Dictionary<string, string> ParseFlatStringDictionary(string json)
        {
            var result = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(json, "\"((?:[^\"\\\\]|\\\\.)*)\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
            {
                result[Unescape(m.Groups[1].Value)] = Unescape(m.Groups[2].Value);
            }
            return result;
        }

        public static string WriteFlatStringDictionary(Dictionary<string, string> data)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (var kv in data)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(Escape(kv.Key)).Append("\":\"").Append(Escape(kv.Value)).Append('"');
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static string Unescape(string s) => s.Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}
