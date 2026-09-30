using System;
using System.IO;

namespace FingerprintAgent
{
    // Karena Agent jalan tanpa jendela terminal (system tray only), semua
    // log ditulis ke file supaya masih bisa dicek kalau ada masalah.
    public static class Logger
    {
        private static readonly string LogPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent.log");
        private static readonly object Lock = new object();

        public static void Info(string message) => Write("INFO", message);
        public static void Error(string message) => Write("ERROR", message);

        private static void Write(string level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            lock (Lock)
            {
                try
                {
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
                catch
                {
                    // Kalau gagal tulis log (mis. folder read-only), jangan sampai
                    // bikin Agent crash cuma gara-gara logging.
                }
            }
        }
    }
}
