using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace GGXXACPROverlay
{
    public static class Program
    {
        [UnmanagedCallersOnly(EntryPoint = "Main", CallConvs = [typeof(CallConvStdcall)])]
        public static unsafe uint Main(nint args, int argsSize)
        {
            Debug.Log("[Overlay] Main() called!");
            return Start((void*)nint.Zero); // Just call Start directly?
        }

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
            //Debug.DebugStatements = Settings.Get("Debug", "ShowDebugStatements", true);
            Debug.DebugStatements = true;
            //if (Settings.Get("Debug", "DisplayConsole", true)) PInvoke.AllocConsole();
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

        //private static SafeFileHandle? _messageThread;

        [UnmanagedCallersOnly(EntryPoint = "DetachAndUnload", CallConvs = [typeof(CallConvStdcall)])]
        public static unsafe uint DetachAndUnload(nint _, int __)
        {
            Debug.Log("[ERROR] DetachAndUnload not implemented");
            return 1;

            Debug.Log("DetachAndUnload called!");
            // if (!_keyboardHook.IsNull) PInvoke.UnhookWindowsHookEx(_keyboardHook);
            // PInvoke.TerminateThread(_messageThread, 0);
            Hooks.UninstallHooks();
            // TODO: Actually check to see if the VException Handler executed
            Thread.Sleep(100);  // Wait for VException Handler to execute
            Overlay.Instance?.Dispose();
            PInvoke.FreeConsole();

            //PInvoke.FreeLibraryAndExitThread(_module, 0);
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
