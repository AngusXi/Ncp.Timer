using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ncp.Timer.HashedTimingWheel;

namespace Ncp.Timer.HashedTimingWheel.Example
{
    class Program
    {
        static bool isRunning = true;
        static void Main(string[] args)
        {
            var timerService = new TimerService();
            var sw = new Stopwatch();
            sw.Start();
            RunTimer(timerService);
            while (isRunning)
            {
                timerService.UpdateTick(sw.ElapsedMilliseconds);
                Task.Delay(10);
            }
            Console.WriteLine("End");
        }
        static void RunTimer(TimerService timerService)
        {
            var acount = 0;
            var bcount = 0;
            timerService.AddTimer(0, 100, 10, null,(o)=> {
                acount++;
                Console.WriteLine($"A:{DateTime.UtcNow} {acount}"); });
            timerService.AddTimer(0, 1000, 100, null, (o) => {
                bcount++;
                Console.WriteLine($"B:{DateTime.UtcNow} {bcount}"); 
                if(bcount == 100)
                {
                    isRunning = false;
                }
            });
        }
    }
}
