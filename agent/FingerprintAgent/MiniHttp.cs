using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FingerprintAgent
{
    // Parser & writer HTTP/1.1 minimal di atas TcpClient mentah — sengaja
    // tanpa System.Net.HttpListener. HttpListener jalan di atas http.sys,
    // yang butuh reservasi ACL (`netsh http add urlacl`) sebelum proses
    // non-Administrator boleh bind ke suatu alamat, walau cuma 127.0.0.1.
    // TcpListener polos tidak lewat jalur http.sys itu sama sekali, jadi
    // laptop client tidak perlu command apa pun sebelum Agent bisa jalan —
    // cukup copy folder & double-click .exe.
    //
    // Cuma menangani bentuk request sederhana yang dipakai di sini: satu
    // baris request-line, header flat, body ditentukan dari Content-Length
    // — cukup buat API kecil ini (3 route, GET/POST/OPTIONS, body JSON
    // flat), bukan implementasi HTTP/1.1 umum.
    public class MiniHttpRequest
    {
        public string Method;
        public string Path;
        public Dictionary<string, string> Query = new Dictionary<string, string>();
        public string Body;

        public static MiniHttpRequest Parse(Stream stream)
        {
            string requestLine = ReadLine(stream);
            if (string.IsNullOrEmpty(requestLine)) return null;

            string[] parts = requestLine.Split(' ');
            if (parts.Length < 2) return null;

            var request = new MiniHttpRequest { Method = parts[0] };

            string rawPath = parts[1];
            int qIndex = rawPath.IndexOf('?');
            if (qIndex >= 0)
            {
                request.Path = rawPath.Substring(0, qIndex);
                ParseQuery(rawPath.Substring(qIndex + 1), request.Query);
            }
            else
            {
                request.Path = rawPath;
            }

            int contentLength = 0;
            string line;
            while (!string.IsNullOrEmpty(line = ReadLine(stream)))
            {
                int colon = line.IndexOf(':');
                if (colon < 0) continue;
                string name = line.Substring(0, colon).Trim();
                string value = line.Substring(colon + 1).Trim();
                if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(value, out contentLength);
            }

            if (contentLength > 0)
            {
                var buffer = new byte[contentLength];
                int read = 0;
                while (read < contentLength)
                {
                    int n = stream.Read(buffer, read, contentLength - read);
                    if (n <= 0) break; // koneksi putus di tengah body
                    read += n;
                }
                request.Body = Encoding.UTF8.GetString(buffer, 0, read);
            }

            return request;
        }

        // statusCode 204/dkk boleh kirim json = null (tanpa body/Content-Type).
        public static void WriteResponse(Stream stream, int statusCode, string json)
        {
            byte[] body = json != null ? Encoding.UTF8.GetBytes(json) : Array.Empty<byte>();

            var header = new StringBuilder();
            header.Append("HTTP/1.1 ").Append(statusCode).Append(' ').Append(StatusText(statusCode)).Append("\r\n");
            if (json != null) header.Append("Content-Type: application/json\r\n");
            header.Append("Content-Length: ").Append(body.Length).Append("\r\n");
            header.Append("Access-Control-Allow-Origin: *\r\n");
            header.Append("Access-Control-Allow-Headers: Content-Type\r\n");
            header.Append("Access-Control-Allow-Methods: GET,POST,OPTIONS\r\n");
            header.Append("Connection: close\r\n");
            header.Append("\r\n");

            byte[] headerBytes = Encoding.ASCII.GetBytes(header.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        private static string StatusText(int code)
        {
            switch (code)
            {
                case 200: return "OK";
                case 204: return "No Content";
                case 404: return "Not Found";
                case 408: return "Request Timeout";
                case 422: return "Unprocessable Entity";
                case 500: return "Internal Server Error";
                default: return "OK";
            }
        }

        private static void ParseQuery(string query, Dictionary<string, string> target)
        {
            foreach (string pair in query.Split('&'))
            {
                if (string.IsNullOrEmpty(pair)) continue;
                int eq = pair.IndexOf('=');
                string key = eq >= 0 ? pair.Substring(0, eq) : pair;
                string value = eq >= 0 ? pair.Substring(eq + 1) : "";
                target[Uri.UnescapeDataString(key)] = Uri.UnescapeDataString(value);
            }
        }

        // Baca satu baris mentah (sampai "\n", "\r" di depannya kalau ada
        // dibuang) byte-by-byte — sengaja tidak pakai StreamReader supaya
        // tidak "makan" buffer lebih dari yang diminta (body dibaca lagi
        // dari Stream mentah yang sama persis sesudah header selesai).
        private static string ReadLine(Stream stream)
        {
            var sb = new StringBuilder();
            int b;
            while ((b = stream.ReadByte()) != -1)
            {
                if (b == '\n')
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
                        sb.Length--;
                    return sb.ToString();
                }
                sb.Append((char)b);
            }
            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}
