using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Buttplug;

namespace AudioTimer
{
    class Program
    {
        private static async Task WaitForKey()
        {
            Console.WriteLine("Press any key to continue.");
            while (!Console.KeyAvailable)
            {
                await Task.Delay(1);
            }
            Console.ReadKey(true);
        }

        private static Task movePiston(ButtplugClientDevice dev, uint speed, uint position)
        {
            return dev.SendRawWriteCmd(Endpoint.Tx, new byte[] { 0x03, (byte)position, (byte)speed }, false);
        }

        static async Task PistonMain(string[] args)
        {
            TaskCompletionSource<ButtplugClientDevice> _tcs = new TaskCompletionSource<ButtplugClientDevice>();
            ButtplugClient bp = new ButtplugClient("timer_client");


            bp.DeviceAdded += (sender, eventArgs) =>
            {
                Console.WriteLine($"Found device: {eventArgs.Device.Name}");
                if (eventArgs.Device.Name.Contains("Vorze Piston"))
                {
                    _tcs.SetResult(eventArgs.Device);
                }
            };
            bp.ErrorReceived += (sender, eventArgs) =>
            {
                Console.WriteLine($"Bang: {eventArgs.Exception.Message}");
                _tcs.SetException(eventArgs.Exception);

            };
            bp.ServerDisconnect += (sender, eventArgs) =>
            {
                Console.WriteLine("Server disconnected!");
                _tcs.SetCanceled();
            };

            await bp.ConnectAsync(new ButtplugEmbeddedConnectorOptions()
            {
                AllowRawMessages = true,
                ServerName = "timer_server"
            });

            await bp.StartScanningAsync();
            Console.WriteLine($"Looking for Vorze SA Piston");

            var dev = await _tcs.Task;
            if (dev == null)
            {
                return;
            }

            Console.WriteLine($"Found the test device");
            await WaitForKey();

            var audio = new AudioRecorder();
            var sm = new SoundMeter();

            //initialise
            await movePiston(dev, 0, 1);

            // reset
            await movePiston(dev, 20, 0);
            Thread.Sleep(1000);

            var log = new System.IO.StreamWriter(@"piston.csv", true);

            var pos = 0;
            var lastPos = 0;
            var time = 0L;
            var threshold = 0.25f;
            var doubleMaxNoise = 0.0;

            Console.WriteLine($"Testing forward {pos}");
            audio.Start();
            await movePiston(dev, 20, 200);
            Thread.Sleep(2000);
            audio.Stop();
            time = audio.NoisePeriod(threshold);
            audio.Report();
            await WaitForKey();

            await movePiston(dev, 20, 0);
            Thread.Sleep(2000);

            // loop speeds first
            for (int speed = 20; speed <= 60; speed += 5)
            {
                Console.WriteLine($"Testing speed {speed}");

                for (int range = 200; range > 0; range -= 10)
                {
                    Console.WriteLine($"Testing range {range}");

                    var file = "rec-" + DateTime.Now.ToFileTime() + ".wav";
                    lastPos = pos;
                    pos += range;
                    Console.WriteLine($"Testing forward {pos}");
                    audio.Start(file);
                    sm.StartMonitor();
                    await movePiston(dev, (uint)speed, (uint)pos);
                    Thread.Sleep(2000);
                    audio.Stop();
                    sm.EndMonitor(out doubleMaxNoise, out _, out _);
                    time = audio.NoisePeriod(threshold);
                    Console.WriteLine($"Found {time}ms of noise");
                    log.WriteLine($"forward,{speed},{range},{time},{lastPos},{pos},{file},{doubleMaxNoise}");
                    Thread.Sleep(1000);

                    file = "rec-" + DateTime.Now.ToFileTime() + ".wav";
                    lastPos = pos;
                    pos -= range;
                    Console.WriteLine($"Testing reverse {pos}");
                    audio.Start(file);
                    sm.StartMonitor();
                    await movePiston(dev, (uint)speed, (uint)pos);
                    Thread.Sleep(2000);
                    audio.Stop();
                    sm.EndMonitor(out doubleMaxNoise, out _, out _);
                    time = audio.NoisePeriod(threshold);
                    Console.WriteLine($"Found {time}ms of noise");
                    log.WriteLine($"reverse,{speed},{range},{time},{lastPos},{pos},{file},{doubleMaxNoise}");
                    Thread.Sleep(1000);
                }
            }
        }

        private static byte[] _auchCRCHi = { 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64, 0, 193, 129, 64, 1, 192, 128, 65, 0, 193, 129, 64, 1, 192, 128, 65, 1, 192, 128, 65, 0, 193, 129, 64};
        private static byte[] _auchCRCLo = { 0, 192, 193, 1, 195, 3, 2, 194, 198, 6, 7, 199, 5, 197, 196, 4, 204, 12, 13, 205, 15, 207, 206, 14, 10, 202, 203, 11, 201, 9, 8, 200, 216, 24, 25, 217, 27, 219, 218, 26, 30, 222, 223, 31, 221, 29, 28, 220, 20, 212, 213, 21, 215, 23, 22, 214, 210, 18, 19, 211, 17, 209, 208, 16, 240, 48, 49, 241, 51, 243, 242, 50, 54, 246, 247, 55, 245, 53, 52, 244, 60, 252, 253, 61, 255, 63, 62, 254, 250, 58, 59, 251, 57, 249, 248, 56, 40, 232, 233, 41, 235, 43, 42, 234, 238, 46, 47, 239, 45, 237, 236, 44, 228, 36, 37, 229, 39, 231, 230, 38, 34, 226, 227, 35, 225, 33, 32, 224, 160, 96, 97, 161, 99, 163, 162, 98, 102, 166, 167, 103, 165, 101, 100, 164, 108, 172, 173, 109, 175, 111, 110, 174, 170, 106, 107, 171, 105, 169, 168, 104, 120, 184, 185, 121, 187, 123, 122, 186, 190, 126, 127, 191, 125, 189, 188, 124, 180, 116, 117, 181, 119, 183, 182, 118, 114, 178, 179, 115, 177, 113, 112, 176, 80, 144, 145, 81, 147, 83, 82, 146, 150, 86, 87, 151, 85, 149, 148, 84, 156, 92, 93, 157, 95, 159, 158, 94, 90, 154, 155, 91, 153, 89, 88, 152, 136, 72, 73, 137, 75, 139, 138, 74, 78, 142, 143, 79, 141, 77, 76, 140, 68, 132, 133, 69, 135, 71, 70, 134, 130, 66, 67, 131, 65, 129, 128, 64};

        private static byte[] CRC16(byte[] t)
        {
            uint n = 255, o = 255;
            for (int i = 0; i < t.Length; i++)
            {
                uint a = n ^ (uint)t[i];
                n = o ^ _auchCRCHi[a];
                o = _auchCRCLo[a];
            }
            return new byte[] {(byte)n, (byte)o };
        }

        private static Task moveFredorch(ButtplugClientDevice dev, uint speed, uint position)
        {
            // Position on the fredorch is 0-15
            if (position > 15 || position < 0)
            {
                throw new ArgumentException("Position is out of range 0-15!");
            }
            var data = new List<byte>() { 0x01, 0x10, 0x00, 0x6B, 0x00, 0x05, 0x0a, 0x00, (byte)speed, 0x00, (byte)speed, 0x00, (byte)(position * 10), 0x00, (byte)(position * 10), 0x00, 0x01 };
            data.AddRange(CRC16(data.ToArray()));
            return dev.SendRawWriteCmd(Endpoint.Tx, data.ToArray(), false);
        }

        static async Task FredorchMain(string[] args)
        {
            TaskCompletionSource<ButtplugClientDevice> _tcs = new TaskCompletionSource<ButtplugClientDevice>();
            ButtplugClient bp = new ButtplugClient("timer_client");


            bp.DeviceAdded += (sender, eventArgs) =>
            {
                Console.WriteLine($"Found device: {eventArgs.Device.Name}");
                if (eventArgs.Device.Name.Contains("Fredorch Device"))
                {
                    _tcs.SetResult(eventArgs.Device);
                }
            };
            bp.ErrorReceived += (sender, eventArgs) =>
            {
                Console.WriteLine($"Bang: {eventArgs.Exception.Message}");
                _tcs.SetException(eventArgs.Exception);

            };
            bp.ServerDisconnect += (sender, eventArgs) =>
            {
                Console.WriteLine("Server disconnected!");
                _tcs.SetCanceled();
            };

            await bp.ConnectAsync(new ButtplugEmbeddedConnectorOptions()
            {
                AllowRawMessages = true,
                ServerName = "timer_server"
            });

            await bp.StartScanningAsync();
            Console.WriteLine($"Looking for Fredorch Device");

            var dev = await _tcs.Task;
            if (dev == null)
            {
                return;
            }

            Console.WriteLine($"Found the test device");
            await WaitForKey();

            var audio = new AudioRecorder();
            var sm = new SoundMeter();
            
            // reset
            await moveFredorch(dev, 15, 0);
            Thread.Sleep(1000);

            var log = new System.IO.StreamWriter(@"fredorch.csv", true);

            var pos = 0;
            var lastPos = 0;
            var time = 0L;
            var threshold = 0.5f;
            var doubleMaxNoise = 0.0;

            Console.WriteLine($"Testing forward {pos}");
            audio.Start();
            await moveFredorch(dev, 15, 15);
            Thread.Sleep(2000);
            audio.Stop();
            time = audio.NoisePeriod(threshold);
            audio.Report();
            await WaitForKey();

            await moveFredorch(dev, 15, 0);
            Thread.Sleep(5000);

            try
            {
                // loop speeds first
                for (int speed = 20; speed > 0; speed -= 1)
                {
                    Console.WriteLine($"Testing speed {speed}");
                    int timeEstimate = 1000 * Math.Max(1, 12 - (speed * 2));

                    for (int range = 15; range > 0; range--)
                    {
                        Console.WriteLine($"Testing range {range}");

                        var file = "rec-" + DateTime.Now.ToFileTime() + ".wav";
                        lastPos = pos;
                        pos += range;
                        Console.WriteLine($"Testing forward {pos}");
                        audio.Start(file);
                        sm.StartMonitor();
                        Thread.Sleep(500);
                        await moveFredorch(dev, (uint)speed, (uint)pos);
                        Thread.Sleep(timeEstimate);
                        audio.Stop();
                        sm.EndMonitor(out doubleMaxNoise, out _, out _);
                        time = audio.NoisePeriod(threshold);
                        Console.WriteLine($"Found {time}ms of noise");
                        log.WriteLine($"forward,{speed},{range},{time},{lastPos},{pos},{file},{doubleMaxNoise}");
                        log.FlushAsync();
                        Thread.Sleep(1000);

                        file = "rec-" + DateTime.Now.ToFileTime() + ".wav";
                        lastPos = pos;
                        pos -= range;
                        Console.WriteLine($"Testing reverse {pos}");
                        audio.Start(file);
                        sm.StartMonitor();
                        Thread.Sleep(500);
                        await moveFredorch(dev, (uint)speed, (uint)pos);
                        Thread.Sleep(timeEstimate);
                        audio.Stop();
                        sm.EndMonitor(out doubleMaxNoise, out _, out _);
                        time = audio.NoisePeriod(threshold);
                        Console.WriteLine($"Found {time}ms of noise");
                        log.WriteLine($"reverse,{speed},{range},{time},{lastPos},{pos},{file},{doubleMaxNoise}");
                        log.FlushAsync();
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (ButtplugException bpe)
            {
                Console.WriteLine(bpe);
            }
        }


        private static Task moveHismithServo(ButtplugClientDevice dev, uint position)
        {
            // Position on the fredorch is 0-15
            if (position > 100 || position < 0)
            {
                throw new ArgumentException("Position is out of range 0-100!");
            }
            var data = new List<byte>() { 0xcc, 0x0a, (byte)position, (byte)(position + 0x0a) };
            return dev.SendRawWriteCmd(Endpoint.Tx, data.ToArray(), false);
        }

        static async Task HismithServoMain(string[] args)
        {
            TaskCompletionSource<ButtplugClientDevice> _tcs = new TaskCompletionSource<ButtplugClientDevice>();
            ButtplugClient bp = new ButtplugClient("timer_client");


            bp.DeviceAdded += (sender, eventArgs) =>
            {
                Console.WriteLine($"Found device: {eventArgs.Device.Name}");
                if (eventArgs.Device.Name.Contains("Hismith Servo"))
                {
                    _tcs.SetResult(eventArgs.Device);
                }
            };
            bp.ErrorReceived += (sender, eventArgs) =>
            {
                Console.WriteLine($"Bang: {eventArgs.Exception.Message}");
                _tcs.SetException(eventArgs.Exception);

            };
            bp.ServerDisconnect += (sender, eventArgs) =>
            {
                Console.WriteLine("Server disconnected!");
                _tcs.SetCanceled();
            };

            await bp.ConnectAsync(new ButtplugWebsocketConnectorOptions(new Uri("ws://localhost:12345")));

            await bp.StartScanningAsync();
            Console.WriteLine($"Looking for Hismith Servo");

            var dev = await _tcs.Task;
            if (dev == null)
            {
                return;
            }

            Console.WriteLine($"Found the test device");
            await WaitForKey();

            var audio = new AudioRecorder();
            var sm = new SoundMeter();

            // reset
            await moveHismithServo(dev, 0);
            Thread.Sleep(1000);

            var log = new System.IO.StreamWriter(@"hismith.csv", true);

            var time = 0L;
            var threshold = 0.3f;
            var doubleMaxNoise = 0.0;

            Console.WriteLine($"Testing forward {100}");
            audio.Start();
            await moveHismithServo(dev, 100);
            Thread.Sleep(2000);
            audio.Stop();
            time = audio.NoisePeriod(threshold);
            audio.Report();
            await WaitForKey();

            await moveHismithServo(dev, 0);
            Thread.Sleep(5000);

            try
            {
                // loop start pos first
                for (uint start = 0; start < 100; start += 1)
                {
                    Console.WriteLine($"Testing start pos {start}");
                    await moveHismithServo(dev, start);
                    Thread.Sleep(1000);

                    int timeEstimate = 1000 * 3;

                    for (uint end = 100; end > start; end -= 1)
                    {
                        Console.WriteLine($"Testing range {start} to {end}");

                        var file = "rec-" + DateTime.Now.ToFileTime() + ".wav";
                        Console.WriteLine($"Testing forward {end-start}");
                        audio.Start(file);
                        sm.StartMonitor();
                        Thread.Sleep(500);
                        await moveHismithServo(dev, end);
                        Thread.Sleep(timeEstimate);
                        audio.Stop();
                        sm.EndMonitor(out doubleMaxNoise, out _, out _);
                        time = audio.NoisePeriod(threshold);
                        Console.WriteLine($"Found {time}ms of noise");
                        log.WriteLine($"forward,1,{end - start},{time},{start},{end},{file},{doubleMaxNoise}");
                        log.FlushAsync();
                        Thread.Sleep(1000);

                        file = "rec-" + DateTime.Now.ToFileTime() + ".wav";
                        Console.WriteLine($"Testing reverse {end-start}");
                        audio.Start(file);
                        sm.StartMonitor();
                        Thread.Sleep(500);
                        await moveHismithServo(dev, start);
                        Thread.Sleep(timeEstimate);
                        audio.Stop();
                        sm.EndMonitor(out doubleMaxNoise, out _, out _);
                        time = audio.NoisePeriod(threshold);
                        Console.WriteLine($"Found {time}ms of noise");
                        log.WriteLine($"reverse,1,{end - start},{time},{end},{start},{file},{doubleMaxNoise}");
                        log.FlushAsync();
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (ButtplugException bpe)
            {
                Console.WriteLine(bpe);
            }
        }


        static async Task Main(string[] args)
        {
            Console.WriteLine("Pick mode:\n1: Vorze Piston\n2: Fredorch F21S\n3: Hismith Servo\n");
            while (true)
            {
                var key = Console.ReadKey(true);
                if (uint.TryParse(key.KeyChar.ToString(), out var mode))
                {
                    switch (mode)
                    {
                        case 1:
                            await PistonMain(args);
                            return;
                        case 2:
                            await FredorchMain(args);
                            return;
                        case 3:
                            await HismithServoMain(args);
                            return;
                    }
                }
            }

        }
    }
}
