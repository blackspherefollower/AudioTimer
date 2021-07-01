using System;
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

        private static Task move(ButtplugClientDevice dev, uint speed, uint position)
        {
            return dev.SendRawWriteCmd(Endpoint.Tx, new byte[] { 0x03, (byte)speed, (byte)position }, false);
        }

        static async Task Main(string[] args)
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
            bp.ErrorReceived += (sender, eventArgs) => {
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

            await move( dev, 20, 0 );
            Thread.Sleep(1000);

            var log = new System.IO.StreamWriter(@"piston.csv", true);

            var pos = 0;
            var lastPos = 0;
            var time = 0L;

            Console.WriteLine($"Testing forward {pos}");
            audio.Start();
            await move( dev, 20, 200 );
            Thread.Sleep(2000);
            audio.Stop();
            time = audio.NoisePeriod(0.25f);
            audio.Report();
            await WaitForKey();

            await move(dev, 20, 0);
            Thread.Sleep(2000);

            // loop speeds first
            for (int speed = 20; speed <= 60; speed+=5 )
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
                    await move(dev, (uint)speed, (uint)pos);
                    Thread.Sleep(2000);
                    audio.Stop();
                    time = audio.NoisePeriod(0.25f);
                    Console.WriteLine($"Found {time}ms of noise");
                    log.WriteLine($"forward,{speed},{range},{time},{lastPos},{pos},{file}");
                    Thread.Sleep(1000);

                    file = "rec-" + DateTime.Now.ToFileTime() + ".wav";
                    lastPos = pos;
                    pos -= range;
                    Console.WriteLine($"Testing reverse {pos}");
                    audio.Start(file);
                    await move(dev, (uint)speed, (uint)pos);
                    Thread.Sleep(2000);
                    audio.Stop();
                    time = audio.NoisePeriod(0.25f);
                    Console.WriteLine($"Found {time}ms of noise");
                    log.WriteLine($"reverse,{speed},{range},{time},{lastPos},{pos},{file}");
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
