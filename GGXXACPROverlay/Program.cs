using GameOverlay;
using System.Reflection;

namespace GGXXACPROverlay
{
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine($"GGXXACPR Overlay v{Assembly.GetEntryAssembly()?.GetName().Version}\n");

            TimerService.EnableHighPrecisionTimers();
            try
            {
                using var overlay = new Overlay();
                overlay.Run();

                Console.WriteLine("Press any key to exit");
                while (!Console.KeyAvailable && overlay.IsRunning())
                {
                    Thread.Sleep(5);
                }
                overlay.Dispose();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message + "\n");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        }
    }
}
