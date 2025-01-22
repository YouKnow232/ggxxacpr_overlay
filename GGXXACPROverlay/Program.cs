using GameOverlay;
using System.Reflection;

namespace GGXXACPROverlay
{
    internal class Program
    {
        static void Main()
        {
            Console.WriteLine($"GGXXACPR Overlay v{Assembly.GetEntryAssembly()?.GetName().Version}\n");
            // Disclaimer
            Console.WriteLine("This is an beta build and may have some bugs.");
            Console.WriteLine("You can help report issues here https://github.com/YouKnow232/ggxxacpr_overlay/issues");
            Console.WriteLine();
            Console.WriteLine("Please close the overlay during netplay.");
            Console.WriteLine();
            // Known Issues
            Console.WriteLine("Known Issues:");
            Console.WriteLine("- Frame Meter startup being wrong on rare occurances usually coupled with a missing active frame");
            Console.WriteLine("- Frame Meter includes some super flash frames in startup");
            Console.WriteLine("- Frame Meter doesn't rewind with replay");
            Console.WriteLine("- Hitbox flickering in replay playback");
            Console.WriteLine("- No throw boxes yet");
            Console.WriteLine();

            TimerService.EnableHighPrecisionTimers();
            try
            {
                using var overlay = new Overlay();
                overlay.Run();

                Console.WriteLine("Press any key to exit");
                while (!Console.KeyAvailable && overlay.IsRunning())
                {
                    Thread.Sleep(30);
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
