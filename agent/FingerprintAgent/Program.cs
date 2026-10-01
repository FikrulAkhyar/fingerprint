using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace FingerprintAgent
{
    static class Program
    {
        private const string DefaultArbUrl = "http://127.0.0.1:8000";
        private const string DefaultAgentPort = "9001";

        [STAThread]
        static void Main()
        {
            Logger.Info("Agent starting...");

            string envPath = FindOrCreateEnvFile();
            var env = EnvFile.Load(envPath);

            string arbUrl;
            if (env.TryGetValue("ARB_URL", out var existing) && !string.IsNullOrWhiteSpace(existing))
            {
                // Sudah ada isinya — langsung pakai, tanpa dialog, supaya
                // start sehari-hari (termasuk auto-start) tetap diam/silent.
                arbUrl = existing;
            }
            else
            {
                // .env belum ada atau ARB_URL-nya masih kosong — baru di
                // sini minta diisi. Sekali diisi, tidak akan ditanya lagi.
                string input = Interaction.InputBox(
                    "Alamat backend (ARB_URL) — tempat Agent simpan/ambil data sidik jari:",
                    "Fingerprint Agent - Konfigurasi Awal",
                    DefaultArbUrl);
                arbUrl = string.IsNullOrWhiteSpace(input) ? DefaultArbUrl : input.Trim();
                EnvFile.Set(envPath, "ARB_URL", arbUrl);
            }

            Logger.Info("ARB_URL: " + arbUrl);

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

            var store = new BackendTemplateStore(arbUrl);
            var enrollManager = new EnrollSessionManager(zk);

            // Bind ke 127.0.0.1 saja — browser yang memicu scan selalu di
            // laptop yang sama dengan Agent (JS CBS jalan di browser teller,
            // bukan di server CBS, jadi localhost selalu cukup apa pun
            // hosting CBS-nya). Tidak perlu diakses dari jaringan luar sama
            // sekali di produksi; lihat design.md §7 kenapa ini defaultnya.
            string port = env.TryGetValue("AGENT_PORT", out var p) && !string.IsNullOrWhiteSpace(p)
                ? p.Trim()
                : DefaultAgentPort;
            string prefix = "http://127.0.0.1:" + port + "/";
            var api = new HttpApi(prefix, zk, enrollManager, store);

            var httpThread = new Thread(() =>
            {
                try
                {
                    api.Start();
                }
                catch (Exception ex)
                {
                    Logger.Error("HTTP API berhenti: " + ex.Message);
                    MessageBox.Show(
                        "Agent gagal membuka port " + port + ":\n\n" + ex.Message +
                        "\n\nKalau pesannya \"Access is denied\": Windows butuh izin eksplisit untuk " +
                        "bind ke alamat ini walau cuma localhost. Jalankan SEKALI di Command Prompt " +
                        "sebagai Administrator (ini TIDAK membuka akses ke jaringan luar, cuma izin " +
                        "internal Windows):\n\n" +
                        "netsh http add urlacl url=http://127.0.0.1:" + port + "/ user=Everyone\n\n" +
                        "Lalu jalankan ulang Agent. Kalau masih gagal, cek juga apa port-nya sudah " +
                        "dipakai proses lain (netstat -ano | findstr :" + port + ") atau masuk " +
                        "excluded port range (netsh int ipv4 show excludedportrange protocol=tcp) " +
                        "— kalau begitu, ganti AGENT_PORT di agent/.env ke angka lain.",
                        "Fingerprint Agent - Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            });
            httpThread.IsBackground = true;
            httpThread.Start();

            Logger.Info("Agent siap di " + prefix);

            using (var trayContext = new TrayApplicationContext(zk, prefix, envPath, store, arbUrl))
            {
                Application.Run(trayContext);
            }
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

            // ARB_URL sengaja dibuat kosong (bukan diisi DefaultArbUrl) supaya
            // Main() tahu ini instalasi baru dan perlu munculkan dialog
            // pengisian. AGENT_PORT dikomentari sebagai contoh saja — kalau
            // tidak diisi, dipakai DefaultAgentPort.
            string newPath = Path.Combine(agentDir, ".env");
            File.WriteAllText(newPath,
                "ARB_URL=" + Environment.NewLine +
                "# AGENT_PORT=" + DefaultAgentPort + " (ganti kalau port ini bentrok, lihat README)" + Environment.NewLine);
            return newPath;
        }
    }
}
