using System;
using System.Drawing;
using System.Windows.Forms;

namespace FingerprintAgent
{
    // Satu-satunya hal yang terlihat oleh user: icon kecil di system tray.
    // Tidak ada jendela lain sama sekali — cocok untuk user non-IT yang
    // cuma perlu tahu "alat ini aktif" dan cara mematikannya kalau perlu.
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ZkFingerService _zk;

        public TrayApplicationContext(ZkFingerService zk)
        {
            _zk = zk;

            var menu = new ContextMenuStrip();
            menu.Items.Add("Fingerprint Agent — Aktif").Enabled = false;
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Keluar", null, OnExit);

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Fingerprint Agent — Aktif",
                ContextMenuStrip = menu,
                Visible = true,
            };

            _trayIcon.ShowBalloonTip(3000, "Fingerprint Agent", "Agent aktif dan siap dipakai.", ToolTipIcon.Info);
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
