using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace FingerprintAgent
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Logger.Info("Agent starting...");

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

            string dataDir = AppDomain.CurrentDomain.BaseDirectory;
            var store = new JsonFileTemplateStore(Path.Combine(dataDir, "templates.json"));
            var enrollManager = new EnrollSessionManager(zk, store);
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
    }
}
