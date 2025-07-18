using System.Runtime.InteropServices;

namespace GGXXACPRInjector
{
    internal unsafe static partial class Program
    {
        private const string targetProcessName = "GGXXACPR_Win";
        private const string injectee = "GGXXACPROverlay.Bootstrapper.dll";
        private const string injecteeDependency = "nethost.dll";

        static void Main(string[] args)
        {
            // AppDomain.CurrentDomain.ProcessExit += new EventHandler(Eject);

            Console.WriteLine($"Current directory: {Path.GetFullPath("./")}");

            Console.WriteLine("== GGXXACPROverlay Injector ==");
            Console.WriteLine("Version: v0.0.1-alpha");
            Console.WriteLine();

            bool injected = false;

            try
            {
                if (CheckDependencies())
                {
                    Injector injector = new Injector(targetProcessName, Path.GetFullPath(injectee), [Path.GetFullPath(injecteeDependency)]);
                    injected = injector.Inject();
                }
                else
                {
                    Console.WriteLine("This overlay requries a .NET runtime installation, but a compatible version wasn't detected.");
                    Console.WriteLine("Please install the x86 version from Microsoft here:");
                    Console.WriteLine("https://builds.dotnet.microsoft.com/dotnet/Runtime/9.0.6/dotnet-runtime-9.0.6-win-x86.exe");
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
