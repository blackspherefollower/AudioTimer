using System;
using System.Collections.Generic;
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
        private object _readlock = new object();
        private List<double> _monitorData;
        private Task _monitor = null;
        private bool _monitorEnd;

        public SoundMeter()
        {
            var devs = HidDevices.Enumerate(0x10c4, 0x82cd);
            if (devs.Any())
                _dev = devs.First(d => d.IsConnected && !d.IsOpen);

            if (_dev == null)
                return;

            if (!_dev.IsOpen)
                _dev.OpenDevice();
        }

        public bool Connected()
        {
            return _dev?.IsOpen == true;
        }

        public bool GetData(out DateTime date, out double level)
        {
            lock (_readlock)
            {
                date = default;
                level = 0;

                if (_dev?.IsOpen != true)
                    return false;

                var data = _dev.ReadReportSync(0x05).Data;
                if (data.Length <= 8)
                    return false;

                date = DateTimeOffset
                    .FromUnixTimeSeconds(BitConverter.ToUInt32(new[] { data[3], data[2], data[1], data[0] })).DateTime;
                level = ((double)BitConverter.ToUInt16(new[] { data[7], data[6] })) / 10;
                return true;
            }
        }

        public void StartMonitor()
        {
            if (_monitor != null)
            {
                _monitorEnd = true;
                _monitor.GetAwaiter().GetResult();
                _monitor = null;
            }
            _monitorData = new List<double>();
            _monitorEnd = false;
            _monitor = Task.Run(() =>
            {
                while (!_monitorEnd)
                {
                    if (GetData(out _, out var level))
                    {
                        _monitorData.Add(level);
                    }
                    Thread.Sleep(250);
                }
            });
        }

        public bool EndMonitor(out double max, out double avg, out double min)
        {
            max = 0.0;
            avg = 0.0;
            min = 0.0;

            if (_monitor == null)
                return false;

            _monitorEnd = true;
            _monitor.GetAwaiter().GetResult();
            _monitor = null;

            if (!_monitorData.Any())
                return false;

            max = _monitorData.Max();
            avg = _monitorData.Average();
            min = _monitorData.Min();
            return true;
        }

        public static int Main(string[] args)
        {
            var sm = new SoundMeter();

            sm.StartMonitor();
            Console.ReadKey(true);
            if (sm.EndMonitor(out var max, out var avg, out var min))
            {
                Console.WriteLine($"min: {min}dB max: {max}dB avg: {avg}dB");
            }
            return 0;
        }
    }
}
