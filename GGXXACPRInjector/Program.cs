using System.Diagnostics;

namespace GGXXACPRInjector
{
    internal unsafe static partial class Program
    {
        private const string targetProcessName = "GGXXACPR_Win";
        private const string injectee = "GGXXACPROverlay.Bootstrapper.dll";
        private const string overlay = "GGXXACPROverlay.dll";
        private const string injecteeDependency = "nethost.dll";

        static void Main(string[] args)
        {
            // AppDomain.CurrentDomain.ProcessExit += new EventHandler(Eject);
            Console.WriteLine("== GGXXACPROverlay Injector ==");
            Console.WriteLine("Injector Version: v1.0.0");
            try
            {
                FileVersionInfo overlayVersion = FileVersionInfo.GetVersionInfo(overlay);
                Console.WriteLine($"Overlay Version: v{overlayVersion.ProductVersion}");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("\nCouldn't find GGXXACPROverlay.dll in current directory!");
                Console.WriteLine("Make sure you're running this injector from the mod directory\n");
                Console.WriteLine("Press any key to close this window...");
                _ = Console.ReadKey(true);
                return;
            }
            Console.WriteLine();

            bool injected = false;

            try
            {
                if (CheckDependencies())
                {
                    Injector injector = new Injector(targetProcessName, Path.GetFullPath(injectee), [Path.GetFullPath(injecteeDependency)]);
                    injected = injector.Inject();

                    if (injected)
                        Console.WriteLine("Overlay injected!\n");
                    else
                        Console.WriteLine("Overlay failed to inject.\n");
                }
                else
                {
                    Console.WriteLine("This overlay requries a .NET runtime installation, but a compatible version wasn't detected.");
                    Console.WriteLine("Please install the x86 version for console apps from Microsoft here:");
                    Console.WriteLine("https://dotnet.microsoft.com/en-us/download/dotnet/9.0/runtime");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
            }
            finally
            {
                if (injected)
                {
                    Console.WriteLine("Press any key to close this window...");
                    //while (!(targetProc?.HasExited ?? true) && !Console.KeyAvailable)
                    //{
                    //    Thread.Sleep(30);
                    //}
                }
                else
                {
                    Console.WriteLine("Press any key to continue...");
                }
                _ = Console.ReadKey(true);
            }
        }

        static bool CheckDependencies()
        {
            string hostfxrPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"dotnet\host\fxr\");

            return Path.Exists(hostfxrPath);
        }
    }
}
