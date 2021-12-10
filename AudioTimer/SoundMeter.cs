using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;

namespace AudioTimer
{
    class SoundMeter
    {
        private HidDevice _dev;

        public SoundMeter()
        {
            _dev = HidDevices.Enumerate(0x10c4, 0x82cd).First(d => d.IsConnected && !d.IsOpen);
            if (_dev == null)
                return;

            if (!_dev.IsOpen)
                _dev.OpenDevice();
        }

        public bool Connected()
        {
            return _dev.IsOpen;
        }

        public bool GetData(out DateTime date, out double level)
        {
            date = default;
            level = 0;

            if (!_dev.IsOpen)
                return false;

            var data = _dev.ReadReportSync(0x05).Data;
            if (data.Length <= 8)
                return false;

            date = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(new[] { data[3], data[2], data[1], data[0] })).DateTime;
            level = ((double)BitConverter.ToUInt16(new[] { data[7], data[6] })) / 10;
            return true;
        }

        public static int Main(string[] args)
        {
            var sm = new SoundMeter();

            while (sm.Connected())
            {
                if (sm.GetData(out var date, out var level))
                {
                    Console.WriteLine(date + " - " + level + "dB");
                }

                Thread.Sleep(500);
            }
            return 0;
        }
    }
}
