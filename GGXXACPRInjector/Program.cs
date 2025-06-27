using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.System.Memory;
using Microsoft.Win32.SafeHandles;

namespace GGXXACPRInjector
{
    internal unsafe partial class Program
    {
        private const int _timeout = 10000;
        private const string injecteeDirectory = "C:\\Users\\chase\\workspace\\GGXXACPROverlay\\Release\\GGXXACPROverlay.Bootstrapper.dll";

        private static bool injected = false;
        private static Process? targetProc;
        private static SafeFileHandle? procHandle;
        private static uint bootstrapperHandle;
        private static nint ejectFunctionOffset;


        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial nint CreateRemoteThread(
            nint hProcess,
            nint lpThreadAttributes,
            uint dwStackSize,
            nint lpStartAddress,
            nint lpParameter,
            uint dwCreationFlags,
            out uint lpThreadId);

        static void Main(string[] args)
        {
            // AppDomain.CurrentDomain.ProcessExit += new EventHandler(Eject);

            Console.WriteLine("== GGXXACPROverlay Injector ==");
            Console.WriteLine("Version: dev-0.0.1");
            Console.WriteLine();

            string targetProcessName = "GGXXACPR_Win";

            ejectFunctionOffset = GetEjectMethodOffset();
            //ejectFunctionOffset = 0x1210;

            // TODO: decide how to package dlls
            injected = Inject(targetProcessName, injecteeDirectory);

            // TODO: unhook UX?
            if (injected)
            {
                Console.WriteLine("Press any key to close the overlay...");
                //while (!(targetProc?.HasExited ?? true) && !Console.KeyAvailable)
                //{
                //    Thread.Sleep(30);
                //}
            }
            else
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
            }
        }

        static nint GetEjectMethodOffset()
        {
            SafeHandle localHandle = PInvoke.LoadLibraryEx(injecteeDirectory, Windows.Win32.System.LibraryLoader.LOAD_LIBRARY_FLAGS.DONT_RESOLVE_DLL_REFERENCES);
            if (localHandle.IsInvalid)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"[ERROR] Local LoadLibraryEx failed: Error Code 0x{errCode:X8}");
                return 1;
            }
            FARPROC localFunctionPtr = PInvoke.GetProcAddress(localHandle, "Eject");
            if (localFunctionPtr.IsNull)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"[ERROR] Local GetProcAddress failed: Error Code 0x{errCode:X8}");
                return 2;
            }

            nint offset = localFunctionPtr.Value - localHandle.DangerousGetHandle();

            localHandle.Close();

            return offset;
        }

        static void Eject(object? sender, EventArgs e)
        {
            if (!injected || targetProc is null || (procHandle?.IsClosed ?? true)) return;

            Console.WriteLine("[DEBUG] Calling remote unload function...");

            //LPTHREAD_START_ROUTINE lpStartAddress = Marshal.GetDelegateForFunctionPointer<LPTHREAD_START_ROUTINE>((nint)remoteUnloadFunctionPtr);
            HANDLE remoteThreadHandle = (HANDLE)CreateRemoteThread(procHandle.DangerousGetHandle(), 0, 0, (nint)(bootstrapperHandle + ejectFunctionOffset), 0, 0, out _);
            if (remoteThreadHandle == nint.Zero)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"[ERROR] CreateRemoteThread failed: Error Code 0x{errCode:X8}");
                return;
            }

            PInvoke.WaitForSingleObject(remoteThreadHandle, _timeout);
            uint result = 0;
            if (!PInvoke.GetExitCodeThread(remoteThreadHandle, &result))
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"[ERROR] GetExitCodeThread failed: Error Code 0x{errCode:X8}");
                return;
            }
            PInvoke.CloseHandle(remoteThreadHandle);

            Console.WriteLine($"[DEBUG] Overlay ejected. Result: 0x{result:X8}");
        }

        static bool Inject(string targetProcName, string dllName)
        {
            // Find target process
            var results = Process.GetProcessesByName(targetProcName);
            if (results.Length < 1)
            {
                Console.Error.WriteLine($"Process \"{targetProcName}\" not found");
                return false;
            }

            targetProc = results[0];
            nint targetProcess = targetProc.Id;

            procHandle = PInvoke.OpenProcess_SafeHandle(
                PROCESS_ACCESS_RIGHTS.PROCESS_CREATE_THREAD
                | PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_OPERATION
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_WRITE
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ,
                false, (uint)targetProcess);

            if (procHandle == null)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"OpenProcess_SafeHandle failed: Error Code {errCode}");
                return false;
            }

            // *IMPORTANT* This method of getting the address of LoadLibraryA relies on the injector's bitness being the same as the target process.
            // Create a delegate function for the LoadLibraryA function in kernel32
            FARPROC loadLibraryAddr = PInvoke.GetProcAddress(PInvoke.GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (loadLibraryAddr.IsNull)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"GetProcAddress failed: Error Code {errCode}");
                return false;
            }

            // Allocate memory for the injectee file name
            uint dllNameLength = (uint)(dllName.Length + 1 * Marshal.SizeOf<char>());

            void* allocMemAddress = PInvoke.VirtualAllocEx(
                procHandle,
                (void*)nint.Zero,
                dllNameLength,
                VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE,
                PAGE_PROTECTION_FLAGS.PAGE_READWRITE);

            if (allocMemAddress == null)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"VirtualAllocEx failed: Error Code {errCode}");
                return false;
            }

            // Write the dll's filename into process memory
            nuint bytesWritten;
            byte[] buffer = System.Text.Encoding.Default.GetBytes(dllName);
            BOOL success;
            fixed (byte* bufferPtr = &buffer[0])
            {
                success = PInvoke.WriteProcessMemory(
                    procHandle,
                    allocMemAddress,
                    bufferPtr,
                    dllNameLength,
                    &bytesWritten);
            }

            if (!success)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"WriteProcessMemory failed: Error Code {errCode}");
                return false;
            }

            // Create a remote thread in the target process that begins execution at LoadLibraryA
            //  with a string pointer parameter to the dll file name previously written in memory
            void* lpThreadParameter = (void*)loadLibraryAddr.Value;
            LPTHREAD_START_ROUTINE lpStartAddress = Marshal.GetDelegateForFunctionPointer<LPTHREAD_START_ROUTINE>(loadLibraryAddr.Value);
            SafeFileHandle remoteThreadHandle = PInvoke.CreateRemoteThread(procHandle, null, 0, lpStartAddress, allocMemAddress, 0, null);
            if (remoteThreadHandle == null)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"CreateRemoteThread failed: Error Code {errCode}");
                return false;
            }

            PInvoke.WaitForSingleObject(remoteThreadHandle, _timeout);
            PInvoke.GetExitCodeThread(remoteThreadHandle, out bootstrapperHandle);
            Console.WriteLine($"[DEBUG] Thread returned Eject function pointer: 0x{bootstrapperHandle:X8}");
            remoteThreadHandle.Close();

            if (bootstrapperHandle == 0)
            {
                Console.Error.WriteLine($"Remote bootstrapper failed: {bootstrapperHandle}");
                return false;
            }

            // release allocated memory
            PInvoke.VirtualFreeEx(procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);

            Console.WriteLine("Overlay injected");
            return true;
        }


        //internal static class Constants
        //{
        //    public const string CONSOLE_BETA_WARNING =
        //        "This is a beta build. It has known issues and may even have some unknown ones.\n" +
        //        "You can help report issues here https://github.com/YouKnow232/ggxxacpr_overlay/issues\n";
        //    public const string CONSOLE_NETPLAY_NOTICE =
        //        "Please close the overlay during netplay.\n";
        //    public const string CONSOLE_KNOWN_ISSUES =
        //        "Known Issues:\n" +
        //        "- PLACE HOLDER\n" +
        //        "- Update this in release branch\n";
        //    public const string CONSOLE_CONTROLS =
        //        "In this console window:\n" +
        //        "Press '1' to toggle hitbox display\n" +
        //        "Press '2' to toggle always-on throw range display\n" +
        //        " *Air throw boxes only check for the pushbox's bottom edge highlighted in yellow\n\n" +
        //        "Press '3' to toggle frame meter display\n" +
        //        "Press '4' to display frame meter legend\n" +
        //        "Press '5' to toggle frame meter hitstop pausing\n" +
        //        "Press '6' to toggle frame meter super flash pausing\n" +
        //        "\nPress 'q' to exit\n";
        //    public const string CONSOLE_EXIT_PROMPT =
        //        "Press any key to exit\n";
        //}
    }
}
