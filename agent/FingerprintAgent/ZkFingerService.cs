using System;
using System.Threading;
using libzkfpcsharp;

namespace FingerprintAgent
{
    // Membungkus panggilan ZKFinger SDK (terbukti jalan lewat Demo2 di Tahap 1).
    // Semua operasi ke device diserialkan lewat satu lock karena reader
    // fisiknya cuma satu dan SDK-nya tidak dirancang untuk dipanggil paralel.
    public class ZkFingerService : IDisposable
    {
        private IntPtr _devHandle = IntPtr.Zero;
        private IntPtr _dbHandle = IntPtr.Zero;
        private int _imageWidth;
        private int _imageHeight;
        private readonly object _deviceLock = new object();

        public void Start()
        {
            int ret = zkfp2.Init();
            if (ret != zkfp.ZKFP_ERR_OK)
                throw new InvalidOperationException("SDK Init gagal, kode=" + ret);

            int deviceCount = zkfp2.GetDeviceCount();
            if (deviceCount <= 0)
            {
                zkfp2.Terminate();
                throw new InvalidOperationException("Tidak ada device ZKTeco yang terdeteksi.");
            }

            _devHandle = zkfp2.OpenDevice(0);
            if (_devHandle == IntPtr.Zero)
            {
                zkfp2.Terminate();
                throw new InvalidOperationException("Gagal membuka device (OpenDevice).");
            }

            _dbHandle = zkfp2.DBInit();
            if (_dbHandle == IntPtr.Zero)
            {
                zkfp2.CloseDevice(_devHandle);
                zkfp2.Terminate();
                throw new InvalidOperationException("Gagal inisialisasi algorithm cache (DBInit).");
            }

            _imageWidth = GetIntParam(1);
            _imageHeight = GetIntParam(2);
        }

        private int GetIntParam(int code)
        {
            byte[] value = new byte[4];
            int size = 4;
            zkfp2.GetParameters(_devHandle, code, value, ref size);
            int result = 0;
            zkfp2.ByteArray2Int(value, ref result);
            return result;
        }

        // Polling AcquireFingerprint tiap 200ms sampai ada jari terdeteksi
        // atau timeout — sama seperti pola di demo SDK (Form1.cs DoCapture).
        public byte[] CaptureOnce(int timeoutMs = 15000)
        {
            lock (_deviceLock)
            {
                byte[] imgBuffer = new byte[Math.Max(_imageWidth * _imageHeight, 1)];
                byte[] template = new byte[2048];
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

                while (DateTime.UtcNow < deadline)
                {
                    int size = 2048;
                    int ret = zkfp2.AcquireFingerprint(_devHandle, imgBuffer, template, ref size);
                    if (ret == zkfp.ZKFP_ERR_OK)
                    {
                        byte[] result = new byte[size];
                        Array.Copy(template, result, size);
                        return result;
                    }
                    Thread.Sleep(200);
                }

                return null; // timeout, tidak ada jari terdeteksi
            }
        }

        // Gabungkan 3 hasil capture jari yang sama jadi satu template final
        // (persyaratan SDK — lihat design.md §11).
        public byte[] MergeTemplates(byte[] t1, byte[] t2, byte[] t3)
        {
            lock (_deviceLock)
            {
                byte[] merged = new byte[2048];
                int mergedLen = 2048;
                int ret = zkfp2.DBMerge(_dbHandle, t1, t2, t3, merged, ref mergedLen);
                if (ret != zkfp.ZKFP_ERR_OK)
                {
                    Logger.Error("DBMerge gagal, kode=" + ret);

                    string message = ret == -22
                        ? "Gagal menggabungkan hasil scan — pastikan pakai jari yang sama di ketiga " +
                          "percobaan (jangan ganti jari) dan tempelkan dengan mantap tiap kali, " +
                          "lalu coba enroll ulang."
                        : "Gagal menggabungkan hasil scan (kode=" + ret + "). Coba enroll ulang.";
                    throw new InvalidOperationException(message);
                }

                byte[] result = new byte[mergedLen];
                Array.Copy(merged, result, mergedLen);
                return result;
            }
        }

        // Perbandingan 1:1. Skor > 0 dianggap cocok (sama seperti demo SDK);
        // skornya tetap dikembalikan ke caller kalau nanti perlu threshold lain.
        public int Match(byte[] t1, byte[] t2)
        {
            lock (_deviceLock)
            {
                return zkfp2.DBMatch(_dbHandle, t1, t2);
            }
        }

        public void Dispose()
        {
            if (_dbHandle != IntPtr.Zero) zkfp2.DBFree(_dbHandle);
            if (_devHandle != IntPtr.Zero) zkfp2.CloseDevice(_devHandle);
            zkfp2.Terminate();
        }
    }
}
