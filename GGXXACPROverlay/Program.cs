using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace GGXXACPROverlay
{
    internal class Program
    {
        private const uint DLL_PROCESS_DETACH = 0;
        private const uint DLL_PROCESS_ATTACH = 1;
        [UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = [typeof(CallConvStdcall)])]
        public static bool DllMain(nint module, uint reason, nint reserved)
        {
            switch (reason)
            {
                case DLL_PROCESS_ATTACH:
                    PInvoke.AllocConsole();
                    Console.WriteLine("DLL Attached!");
                    // Initialize graphics stuff (find the d3d9 device pointer)
                    //Console.WriteLine("Graphics Initialized");
                    return Hooks.InstallHooks();
                case DLL_PROCESS_DETACH:
                    Graphics.Dispose();
                    return Hooks.UninstallHooks();
                default:
                    break;
            }

            return true;
        }

        //static void Main()
        //{
        //    // Version
        //    Console.WriteLine($"GGXXACPR Overlay v{Assembly.GetEntryAssembly()?.GetName().Version}\n");

        //    Console.WriteLine(Constants.CONSOLE_BETA_WARNING);
        //    Console.WriteLine(Constants.CONSOLE_NETPLAY_NOTICE);
        //    Console.WriteLine(Constants.CONSOLE_KNOWN_ISSUES);

        //    TimerService.EnableHighPrecisionTimers();

        //    try
        //    {
        //        using var overlay = new Overlay();
        //        overlay.Run();
        //        Console.WriteLine(Constants.CONSOLE_CONTROLS);

        //        ConsoleKey? key = null;
        //        Stream inputStream = Console.OpenStandardInput();
        //        while (overlay.IsRunning())
        //        {
        //            if (Console.KeyAvailable) key = Console.ReadKey(true).Key;
        //            switch (key)
        //            {
        //                case ConsoleKey.NumPad1:
        //                case ConsoleKey.D1:
        //                    overlay.ToggleHitboxOverlay();
        //                    break;
        //                case ConsoleKey.NumPad2:
        //                case ConsoleKey.D2:
        //                    overlay.ToggleThrowRangeDisplay();
        //                    break;
        //                case ConsoleKey.NumPad3:
        //                case ConsoleKey.D3:
        //                    overlay.ToggleFrameMeter();
        //                    break;
        //                case ConsoleKey.NumPad4:
        //                case ConsoleKey.D4:
        //                    overlay.ToggleDisplayLegend();
        //                    break;
        //                case ConsoleKey.NumPad5:
        //                case ConsoleKey.D5:
        //                    overlay.ToggleRecordHitstop();
        //                    break;
        //                case ConsoleKey.NumPad6:
        //                case ConsoleKey.D6:
        //                    overlay.ToggleRecordSuperFlash();
        //                    break;
        //                case ConsoleKey.Q:
        //                    overlay.Dispose();
        //                    break;
        //            }
        //            key = null;

        //            Thread.Sleep(30);
        //        }
        //    }
        //    catch (InvalidOperationException e)
        //    {
        //        Console.WriteLine(e.Message + "\n");
        //        Console.WriteLine(Constants.CONSOLE_EXIT_PROMPT);
        //        Console.ReadKey(true);
        //    }
        //}
    }
}
