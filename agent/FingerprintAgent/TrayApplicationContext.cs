using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace FingerprintAgent
{
    // Setelah dialog ARB_URL di awal (lihat Program.cs, cuma muncul kalau
    // .env belum ada/kosong) selesai, satu-satunya hal yang terlihat user
    // sehari-hari adalah icon kecil di system tray. Klik kanan buat lihat
    // status/alamat, ubah ARB_URL, atau keluar.
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ZkFingerService _zk;
        private readonly BackendTemplateStore _store;
        private readonly string _envPath;
        private readonly ToolStripItem _arbUrlMenuItem;

        public TrayApplicationContext(ZkFingerService zk, string listenAddress, string envPath, BackendTemplateStore store, string arbUrl)
        {
            _zk = zk;
            _store = store;
            _envPath = envPath;

            var menu = new ContextMenuStrip();
            menu.Items.Add("Fingerprint Agent — Aktif").Enabled = false;
            menu.Items.Add("Listen: " + listenAddress).Enabled = false;
            _arbUrlMenuItem = menu.Items.Add("ARB_URL: " + arbUrl);
            _arbUrlMenuItem.Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Ubah ARB_URL...", null, OnChangeArbUrl);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Keluar", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Text = "Fingerprint Agent — Aktif",
                ContextMenuStrip = menu,
                Visible = true,
            };

            _trayIcon.ShowBalloonTip(3000, "Fingerprint Agent", "Agent aktif dan siap dipakai.", ToolTipIcon.Info);
        }

        private void OnChangeArbUrl(object sender, EventArgs e)
        {
            string current = _arbUrlMenuItem.Text.Substring("ARB_URL: ".Length);
            string input = Interaction.InputBox(
                "Alamat backend (ARB_URL) baru:",
                "Fingerprint Agent - Ubah ARB_URL",
                current);

            if (string.IsNullOrWhiteSpace(input))
                return; // dibatalkan, tidak ada perubahan

            string newUrl = input.Trim();
            _store.UpdateBaseUrl(newUrl);
            EnvFile.Set(_envPath, "ARB_URL", newUrl);
            _arbUrlMenuItem.Text = "ARB_URL: " + newUrl;

            Logger.Info("ARB_URL diubah lewat tray: " + newUrl);
            _trayIcon.ShowBalloonTip(2000, "Fingerprint Agent", "ARB_URL diperbarui.", ToolTipIcon.Info);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Logger.Info("Agent dihentikan lewat menu tray.");
            _trayIcon.Visible = false;
            _zk.Dispose();
            Application.Exit();
        }
    }
}
