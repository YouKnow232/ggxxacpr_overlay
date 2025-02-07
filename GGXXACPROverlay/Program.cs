﻿using GameOverlay;
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
            Console.WriteLine("- PLACE HOLDER");
            Console.WriteLine("- Update this in release branch");
            Console.WriteLine();

            TimerService.EnableHighPrecisionTimers();

            try
            {
                using var overlay = new Overlay();
                overlay.Run();

                // Contorls
                Console.WriteLine("In this console window:");
                Console.WriteLine("Press '1' to toggle hitbox display");
                Console.WriteLine("Press '2' to toggle frame meter display");
                Console.WriteLine("Press 'q' to exit\n");

                ConsoleKey? key = null;
                Stream inputStream = Console.OpenStandardInput();
                while (overlay.IsRunning())
                {
                    if (Console.KeyAvailable) key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            overlay.ToggleHitboxOverlay();
                            break;
                        case ConsoleKey.D2:
                            overlay.ToggleFrameMeter();
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
                Console.WriteLine("Press any key to exit\n");
                Console.ReadKey(true);
            }
        }
    }
}
