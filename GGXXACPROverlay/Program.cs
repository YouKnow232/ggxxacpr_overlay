using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace GGXXACPROverlay
{
    public static class Program
    {
        public static string Version { get; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "ERR";

        [UnmanagedCallersOnly(EntryPoint = "Main", CallConvs = [typeof(CallConvStdcall)])]
        public static unsafe uint Main(nint args, int argsSize)
        {
            // Ensures GC is initialized
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (!Settings.Load())
            {
                Debug.Log("Couldn't load OverlaySettings.ini. Creating default ini.");
                Settings.WriteDefault();
            }

            Debug.DebugStatements = Settings.Get("Debug", "ShowDebugStatements", true);
            if (Settings.Get("Debug", "DisplayConsole", false)) PInvoke.AllocConsole();
            Debug.Log("DLL Attached!");

            Hooks.HookInstaller.InstallHooks();

            return 0;
        }

        [UnmanagedCallersOnly(EntryPoint = "DetachAndUnload", CallConvs = [typeof(CallConvStdcall)])]
        public static unsafe uint DetachAndUnload(nint _, int __)
        {
            Debug.Log("[ERROR] DetachAndUnload not implemented");
            return 1;

            //Debug.Log("DetachAndUnload called!");
            //Hooks.UninstallHooks();
            //Thread.Sleep(100);  // Wait for VException Handler to execute
            //Overlay.Instance?.Dispose();
            //PInvoke.FreeConsole();

            ////PInvoke.FreeLibraryAndExitThread(_module, 0);
            //return 1;   // Make compiler happy
        }
    }
}
