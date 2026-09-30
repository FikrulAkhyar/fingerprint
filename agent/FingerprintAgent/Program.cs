using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace FingerprintAgent
{
    static class Program
    {
        private const string DefaultBackendBaseUrl = "http://127.0.0.1:8000";

        [STAThread]
        static void Main()
        {
            Logger.Info("Agent starting...");

            string backendBaseUrl = LoadBackendBaseUrl();
            Logger.Info("Backend base URL: " + backendBaseUrl);

            var zk = new ZkFingerService();
            try
            {
                zk.Start();
            }
            catch (Exception ex)
            {
                Logger.Error("Gagal inisialisasi device: " + ex.Message);
                MessageBox.Show(
                    "Alat sidik jari tidak bisa diaktifkan:\n\n" + ex.Message +
                    "\n\nPastikan alat sudah dicolok dan driver sudah terpasang (driver/setup.exe).",
                    "Fingerprint Agent - Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Logger.Info("Device siap.");

            var store = new BackendTemplateStore(backendBaseUrl);
            var enrollManager = new EnrollSessionManager(zk);
            var api = new HttpApi("http://127.0.0.1:9001/", zk, enrollManager, store);

            var httpThread = new Thread(() =>
            {
                try
                {
                    api.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error("HTTP API berhenti: " + ex.Message);
                }
            });
            httpThread.IsBackground = true;
            httpThread.Start();

            Logger.Info("Agent siap di http://127.0.0.1:9001/");

            using (var trayContext = new TrayApplicationContext(zk))
            {
                Application.Run(trayContext);
            }
        }

        private static string LoadBackendBaseUrl()
        {
            string path = FindOrCreateEnvFile();
            var env = EnvFile.Load(path);
            return env.TryGetValue("BACKEND_URL", out var url) && !string.IsNullOrEmpty(url)
                ? url
                : DefaultBackendBaseUrl;
        }

        // .env dicari mulai dari folder .exe (bin\<Config>\net48\), naik ke
        // folder induk satu-satu, sampai ketemu — supaya bisa ditaruh di
        // `agent/.env` (gampang ditemukan, tidak ke-reset kalau folder bin
        // dihapus/dibersihkan), bukan cuma di sebelah .exe. Kalau belum ada
        // sama sekali, dibuatkan baru persis di `agent/.env`.
        private static string FindOrCreateEnvFile()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                string candidate = Path.Combine(dir.FullName, ".env");
                if (File.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }

            // agent/FingerprintAgent/bin/<Config>/net48/ -> naik 4 level -> agent/
            string agentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)
                .Parent?.Parent?.Parent?.Parent?.FullName
                ?? AppDomain.CurrentDomain.BaseDirectory;

            string newPath = Path.Combine(agentDir, ".env");
            File.WriteAllText(newPath, "BACKEND_URL=" + DefaultBackendBaseUrl);
            return newPath;
        }
    }
}
