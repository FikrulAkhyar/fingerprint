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
        private readonly ArbAuthService _auth;
        private readonly ToolStripItem _arbUrlMenuItem;
        private readonly ToolStripItem _arbUsernameMenuItem;

        public TrayApplicationContext(ZkFingerService zk, string listenAddress, string envPath, BackendTemplateStore store, string arbUrl, string arbUsername, ArbAuthService auth)
        {
            _zk = zk;
            _store = store;
            _envPath = envPath;
            _auth = auth;

            var menu = new ContextMenuStrip();
            menu.Items.Add("Fingerprint Agent — Aktif").Enabled = false;
            menu.Items.Add("Listen: " + listenAddress).Enabled = false;
            _arbUrlMenuItem = menu.Items.Add("ARB_URL: " + arbUrl);
            _arbUrlMenuItem.Enabled = false;
            // Password sengaja TIDAK ditampilkan di menu (walau cuma akun
            // sistem, bukan akun pribadi) — beda dari URL/username yang
            // aman dilihat sekilas di layar.
            _arbUsernameMenuItem = menu.Items.Add("ARB_USERNAME: " + (arbUsername ?? "(belum diisi)"));
            _arbUsernameMenuItem.Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Ubah ARB_URL...", null, OnChangeArbUrl);
            menu.Items.Add("Ubah ARB_USERNAME...", null, OnChangeArbUsername);
            menu.Items.Add("Ubah ARB_PASSWORD...", null, OnChangeArbPassword);
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

        private void OnChangeArbUsername(object sender, EventArgs e)
        {
            string current = _arbUsernameMenuItem.Text.Substring("ARB_USERNAME: ".Length);
            string input = Interaction.InputBox(
                "Username akun sistem ARB+ (dipakai Agent login ke progId MADC0005):",
                "Fingerprint Agent - Ubah ARB_USERNAME",
                current == "(belum diisi)" ? "" : current);

            if (string.IsNullOrWhiteSpace(input))
                return; // dibatalkan, tidak ada perubahan

            string newUsername = input.Trim();
            EnvFile.Set(_envPath, "ARB_USERNAME", newUsername);
            _auth.UpdateUsername(newUsername);
            _arbUsernameMenuItem.Text = "ARB_USERNAME: " + newUsername;

            Logger.Info("ARB_USERNAME diubah lewat tray: " + newUsername);
            _trayIcon.ShowBalloonTip(2000, "Fingerprint Agent", "ARB_USERNAME diperbarui.", ToolTipIcon.Info);
        }

        private void OnChangeArbPassword(object sender, EventArgs e)
        {
            // Sengaja tidak di-prefill pakai password lama (Interaction.InputBox
            // tidak bisa masking input) — kosong berarti "batal", sama seperti
            // pola ARB_URL/ARB_USERNAME di atas.
            string input = Interaction.InputBox(
                "Password akun sistem ARB+ baru (dipakai Agent login ke progId MADC0005):",
                "Fingerprint Agent - Ubah ARB_PASSWORD",
                "");

            if (string.IsNullOrWhiteSpace(input))
                return; // dibatalkan, tidak ada perubahan

            string newPassword = input.Trim();
            EnvFile.Set(_envPath, "ARB_PASSWORD", newPassword);
            _auth.UpdatePassword(newPassword);

            Logger.Info("ARB_PASSWORD diubah lewat tray.");
            _trayIcon.ShowBalloonTip(2000, "Fingerprint Agent", "ARB_PASSWORD diperbarui.", ToolTipIcon.Info);
        }

        private void OnExit(object sender, EventArgs e)
        {
            Logger.Info("Agent dihentikan lewat menu tray.");
            _trayIcon.Visible = false;
            _auth.Logout(); // best-effort — supaya akun sistem ARB+ tidak nyangkut "masih login"
            _zk.Dispose();
            Application.Exit();
        }
    }
}
