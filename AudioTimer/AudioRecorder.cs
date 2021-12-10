using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AudioTimer
{
    class AudioRecorder
    {
        private WaveInEvent _waveSource;

        private int _samples;
        private float _max;
        private float _omax;
        private float _omin = float.MaxValue;
        private List<float> _peaks = new List<float>();
        private long _startTime;
        private long _stopTime;
        private TaskCompletionSource<bool> _tcs;
        private WaveFileWriter _writer;

        public void Start(string recording = null)
        {
            _peaks = new List<float>();
            _samples = 0;
            _max = 0;
            _omax = 0;
            _omin = float.MaxValue;

            _waveSource = new WaveInEvent
            {
                WaveFormat = new WaveFormat(48000, 16, 1)
            };

            //Console.WriteLine(waveSource.WaveFormat.AverageBytesPerSecond + "bps");

            var nsp = new NotifyingSampleProvider(new WaveToSampleProvider(new Wave16ToFloatProvider(new WaveInProvider(_waveSource))));

            nsp.Sample += NspOnSample;

            if (recording != null)
            {
                _writer = new WaveFileWriter(recording, _waveSource.WaveFormat);
            }

            _waveSource.RecordingStopped += waveSource_RecordingStopped;
            _waveSource.DataAvailable += delegate (object sender, WaveInEventArgs args)
            {
                _writer?.Write(args.Buffer, 0, args.BytesRecorded);
                var sampleBuffer = new float[args.BytesRecorded / 2];
                nsp.Read(sampleBuffer, 0, sampleBuffer.Length);
            };

            _startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _waveSource.StartRecording();
        }

        private void NspOnSample(object sender, SampleEventArgs e)
        {
            _max = Math.Max(_max, e.Left);
            _samples++;

            if (_samples % 480 == 0)
            {
                _peaks.Add(_max);
                _omax = Math.Max(_omax, _max);
                _omin = Math.Min(_omin, _max);
                _samples = 0;
                _max = 0;
            }
        }

        public void Stop()
        {
            _tcs = new TaskCompletionSource<bool>();
            _waveSource.StopRecording();
            _tcs.Task.Wait();
        }

        void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _stopTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            }

            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }

            _tcs?.TrySetResult(true);
        }

        public void Report()
        {
            if (_peaks == null || !_peaks.Any())
            {
                Console.WriteLine("No data recorded!");
                return;
            }

            // Sample rate 

            float r = _omax - _omin;
            _peaks.ForEach(p =>
            {
                int n = Convert.ToInt16(((p - _omin) / r) * 20);
                Console.WriteLine($"{new string('|', n)}{new string(' ', 20 - n)}{n}");
            });


            Console.WriteLine($"Range {_omin}-{_omax}");
            Console.WriteLine($"Recorded {_peaks.Count / 100.0}s of data");
            Console.WriteLine($"Realtime {(_stopTime - _startTime) / 1000.0}s of data");
        }

        public long NoisePeriod(float threshold)
        {
            if (_peaks == null || !_peaks.Any())
            {
                //Console.WriteLine("No data recorded!");
                return 0;
            }

            float r = _omax - _omin;
            int start = -1;
            int end = -1;
            int pos = 0;
            while (start < 0 && pos < _peaks.Count)
            {
                var n = (_peaks[pos] - _omin) / r;
                if (n > threshold)
                    start = pos;
                pos++;
            }

            pos = _peaks.Count - 1;
            while (end < 0 && pos >= 0)
            {
                var n = (_peaks[pos] - _omin) / r;
                if (n > threshold)
                    end = pos;
                pos--;
            }

            if (start < 0 || end < 0)
            {
                return 0;
            }

            return (end - start) * 10;
        }
    }
}
