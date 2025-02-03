using GameOverlay;
using System.Reflection;

namespace GGXXACPROverlay
{
    internal class Program
    {
        static void Main()
        {
            // Version
            Console.WriteLine($"GGXXACPR Overlay v{Assembly.GetEntryAssembly()?.GetName().Version}\n");
            // Disclaimer
            Console.WriteLine("This is a beta build. It has known issues and may even have some unknown ones.");
            Console.WriteLine("You can help report issues here https://github.com/YouKnow232/ggxxacpr_overlay/issues");
            Console.WriteLine();
            Console.WriteLine("Please close the overlay during netplay.");
            Console.WriteLine();
            // Known Issues
            Console.WriteLine("Known Issues:");
            Console.WriteLine("- Fullscreen is not currently supported");
            Console.WriteLine("- Generally buggy behavior in replay mode. No rewinding, skipping ahead while paused");
            Console.WriteLine("- Startup may be wrong even if the meter accurately records it");
            Console.WriteLine("- Projectile startup is not implemented");
            Console.WriteLine("- Throw boxes, active frames, and startup are not implemented");
            Console.WriteLine("- Collision boxes may be wrong. They currently only account for standing and crouching states");
            Console.WriteLine("- Justice's fullscreen super has incorrect invuln");
            Console.WriteLine("- Frame meter will overwrite guardpoint with throw invuln if both are true e.g. Anji's 3K, 6H, and Rin");
            Console.WriteLine();

            TimerService.EnableHighPrecisionTimers();

            try
            {
                using var overlay = new Overlay();
                overlay.Run();

                Console.WriteLine("Press any key to exit\n");
                while (!Console.KeyAvailable && overlay.IsRunning())
                {
                    Thread.Sleep(30);
                }
                overlay.Dispose();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message + "\n");
                Console.WriteLine("Press any key to exit\n");
                Console.ReadKey();
            }
        }
    }
}
