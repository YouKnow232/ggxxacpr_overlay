using System.Diagnostics;
using System.Reflection;

namespace GGXXACPRInjector
{
    internal unsafe static partial class Program
    {
        private const string TARGET_PROCESS_NAME = "GGXXACPR_Win";
        private const string FILE_ROOT = @".\bin\";
        private const string INJECTEE_DLL = "GGXXACPROverlay.Bootstrapper.dll";
        private const string OVERLAY_DLL = "GGXXACPROverlay.dll";
        private const string INJECTEE_DEPENDENCY = "nethost.dll";

        static void Main()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();

            Console.WriteLine("== GGXXACPROverlay Injector ==");
            Console.WriteLine($"Injector Version: v{version}");
            try
            {
                FileVersionInfo overlayVersion = FileVersionInfo.GetVersionInfo(Path.Combine(FILE_ROOT, OVERLAY_DLL));
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
                    Injector injector = new Injector(
                        TARGET_PROCESS_NAME,
                        Path.GetFullPath(Path.Combine(FILE_ROOT, INJECTEE_DLL)),
                        [Path.GetFullPath(Path.Combine(FILE_ROOT, INJECTEE_DEPENDENCY))]);
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
