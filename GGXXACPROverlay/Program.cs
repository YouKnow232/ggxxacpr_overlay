using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace GGXXACPROverlay
{
    public static class Program
    {
        private const uint DLL_PROCESS_DETACH = 0;
        private const uint DLL_PROCESS_ATTACH = 1;

        internal static HMODULE _module;
        //private static HANDLE _targetProcessId;
        //private static HANDLE _targetProcessMainThread;
        //private static uint _mainThreadId;

        //internal static HMODULE ThisModule => _module;

        [UnmanagedCallersOnly(EntryPoint = "Main", CallConvs = [typeof(CallConvStdcall)])]
        public static unsafe uint Main(nint args, int argsSize)
        {
            Debug.Log("[Overlay] Main() called!");

            Console.WriteLine(typeof(Program).Assembly.GetName().Name);
            Console.WriteLine(Assembly.GetExecutingAssembly().FullName);
            Console.WriteLine(typeof(Program).Assembly.IsDynamic); // should be false
            Console.WriteLine(AssemblyLoadContext.GetLoadContext(typeof(Program).Assembly));

            return Start((void*)nint.Zero); // Just call Start directly?
        }

        //private static SafeFileHandle? _messageThread;

        /// <summary>
        /// Initialize hooks
        /// </summary>
        /// <param name="lpThreadParameter"></param>
        /// <returns></returns>
        public static unsafe uint Start(void* lpThreadParameter)
        {
            if (!Settings.Load())
            {
                Debug.Log("Couldn't load OverlaySettings.ini. Creating default ini.");
                Settings.WriteDefault();
            }

            // DEBUG: allocate new console window.
            Debug.DebugStatements = Settings.Get("Debug", "ShowDebugStatements", false);
            if (Settings.Get("Debug", "DisplayConsole", false)) PInvoke.AllocConsole();
            Debug.Log("DLL Attached!");

            //// Keyboard Hook
            //_mainThreadId = GetMainThread();
            //if (_mainThreadId != 0)
            //{
            //    _keyboardHook = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD, _hookProc, HINSTANCE.Null, _mainThreadId);
            //    if (_keyboardHook == 0) Debug.Log("Keyboard Hook failed to install");
            //    else Debug.Log($"KeyboardHook: 0x{(nint)_keyboardHook.Value:X8}");
            //}

            // Detour/Trampoline Hooks
            Hooks.InstallHooks();

            return 0;
        }

        //private static unsafe uint GetMainThread()
        //{
        //    using var snapshot = PInvoke.CreateToolhelp32Snapshot_SafeHandle(CREATE_TOOLHELP_SNAPSHOT_FLAGS.TH32CS_SNAPTHREAD, 0);
        //    if (snapshot.IsInvalid)
        //    {
        //        Debug.Log("CreateToolhelp32Snapshot failed");
        //        return 0;
        //    }

        //    THREADENTRY32 entry = new() { dwSize = (uint)sizeof(THREADENTRY32) };

        //    // TODO: wrong assumption?
        //    // ASSUMPTION: main window thread is first
        //    if (!PInvoke.Thread32First(snapshot, ref entry))
        //    {
        //        Debug.Log("Thread32First failed");
        //        return 0;
        //    }

        //    return entry.th32ThreadID;
        //}

        [UnmanagedCallersOnly(EntryPoint = "DetachAndUnload", CallConvs = [typeof(CallConvStdcall)])]
        public static unsafe uint DetachAndUnload(nint _, int __)
        {
            Debug.Log("DetachAndUnload called!");
            // if (!_keyboardHook.IsNull) PInvoke.UnhookWindowsHookEx(_keyboardHook);
            // PInvoke.TerminateThread(_messageThread, 0);
            Hooks.UninstallHooks();
            // TODO: Actually check to see if the VException Handler executed
            Thread.Sleep(100);  // Wait for VException Handler to execute
            Overlay.Instance?.Dispose();
            PInvoke.FreeConsole();

            PInvoke.FreeLibraryAndExitThread(_module, 0);
            return 1;   // Make compiler happy
        }

        //// internal unsafe delegate winmdroot.Foundation.LRESULT HOOKPROC(int code, winmdroot.Foundation.WPARAM wParam, winmdroot.Foundation.LPARAM lParam);
        ////private static readonly HOOKPROC _hookProc = KeyboardHook;
        ////private static HHOOK _keyboardHook;
        //[UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        //private static LRESULT KeyboardHook(int code, WPARAM wParam, LPARAM lParam)
        //{
        //    if (code >= 0)
        //    {
        //        int vkCode = (int)wParam.Value;

        //        Debug.Log($"Keyboard event hook reached!: {vkCode}");
        //        switch (vkCode)
        //        {
        //            case 0x1B:
        //                Debug.Log("KeyboardHook>> ESC keystroke detected!");
        //                break;
        //        }
        //    }

        //    return PInvoke.CallNextHookEx(_keyboardHook, code, wParam, lParam);
        //}
    }
}
