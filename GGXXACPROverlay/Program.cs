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

            Console.WriteLine(Constants.CONSOLE_BETA_WARNING);
            Console.WriteLine(Constants.CONSOLE_NETPLAY_NOTICE);
            Console.WriteLine(Constants.CONSOLE_KNOWN_ISSUES);

            TimerService.EnableHighPrecisionTimers();

            try
            {
                using var overlay = new Overlay();
                overlay.Run();
                Console.WriteLine(Constants.CONSOLE_CONTROLS);

                ConsoleKey? key = null;
                Stream inputStream = Console.OpenStandardInput();
                while (overlay.IsRunning())
                {
                    if (Console.KeyAvailable) key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.NumPad1:
                        case ConsoleKey.D1:
                            overlay.ToggleHitboxOverlay();
                            break;
                        case ConsoleKey.NumPad2:
                        case ConsoleKey.D2:
                            overlay.ToggleThrowRangeDisplay();
                            break;
                        case ConsoleKey.NumPad3:
                        case ConsoleKey.D3:
                            overlay.ToggleFrameMeter();
                            break;
                        case ConsoleKey.NumPad4:
                        case ConsoleKey.D4:
                            overlay.ToggleDisplayLegend();
                            break;
                        case ConsoleKey.NumPad5:
                        case ConsoleKey.D5:
                            overlay.ToggleRecordHitstop();
                            break;
                        case ConsoleKey.NumPad6:
                        case ConsoleKey.D6:
                            overlay.ToggleRecordSuperFlash();
                            break;
                        case ConsoleKey.Q:
                            overlay.Dispose();
                            break;
                    }
                    key = null;

                    Thread.Sleep(30);
                }
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e.Message + "\n");
                Console.WriteLine(Constants.CONSOLE_EXIT_PROMPT);
                Console.ReadKey(true);
            }
        }
    }
}
