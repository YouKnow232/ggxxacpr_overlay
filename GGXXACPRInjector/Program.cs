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
                Console.WriteLine($"Overlay Version: v{overlayVersion.FileVersion}");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("\nCouldn't find GGXXACPROverlay.dll in current directory!");
                Console.WriteLine("Make sure you're running this injector from the GGXXACPROverlay folder.\n");
                Console.WriteLine("Press any key to close this window...");
                _ = Console.ReadKey(true);
                return;
            }
            Console.WriteLine();


            bool injected = false;

            try
            {
                if (RuntimeInstaller.HandleRuntimeChecks())
                {
                    Injector injector = new Injector(targetProcessName, Path.GetFullPath(injectee), [Path.GetFullPath(injecteeDependency)]);
                    injected = injector.Inject();

                    if (injected)
                        Console.WriteLine("Overlay injected!\n");
                    else
                        Console.WriteLine("Overlay failed to inject.\n");
                }
            }
            catch (InvalidHashException)
            {
                Console.WriteLine("[ERROR] Something went wrong with the download!");
                Console.WriteLine("Try downloading the runtime manually.");
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
    }
}
