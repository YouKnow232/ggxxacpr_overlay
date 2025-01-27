using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace GGXXACPROverlay
{
    internal static partial class Memory
    {
        private static readonly int _PROCESS_VM_READ = 0x0010;
        private static readonly uint WS_POPUP = 0x80000000;
        private static readonly uint WS_EX_TOPMOST = 0x00000008;

        private static Process? _process;
        private static HANDLE _procHandle;
        private static nint _baseAddress;

        internal static bool OpenProcess(string processName)
        {
            Process[] results = Process.GetProcessesByName(processName);
            if (results.Length == 0)
            {
                throw new InvalidOperationException($"Could not find {processName} process. Restart overlay after launching {processName}.");
            }
            if (results.Length > 1) { Debug.WriteLine("Multiple Instances found, attaching to first one."); }

            _process = results[0];
            _baseAddress = _process.MainModule!.BaseAddress;
            _procHandle = PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ, true, (uint)_process.Id);
            Debug.WriteLine("Process Opened");

            return true;
        }

        internal static bool ProcessIsOpen()
        {
            if (_process == null) { return false; }
            return !_process.HasExited;
        }

        internal static void CloseProcess()
        {
            bool success = PInvoke.CloseHandle(_procHandle);
            if (!success) { HandleSystemError("Error closing process."); }
            else { Debug.WriteLine("Process Closed"); }
        }

        internal static nint GetGameWindowHandle()
        {
            if (_process == null) { throw new InvalidOperationException("Process has not been opened"); }
            return _process.MainWindowHandle;
        }

        internal static uint GetGameThreadID()
        {
            if (_process == null) { throw new InvalidOperationException("Process has not been opened"); }
            Debug.WriteLine($"threads: {_process.Threads}");
            Debug.WriteLine($"threads: {_process.Threads.Count}");
            return (uint)_process.Threads[1].Id;
        }

        internal static nint GetBaseAddress()
        {
            if (_baseAddress == 0) { throw new InvalidOperationException("Base Address is 0, has the process been opened with Memory.OpenProcess()? "); }
            return _baseAddress;
        }

        internal static byte[] ReadMemoryPlusBaseOffset(nint address, int size)
        {
            return ReadMemory(_baseAddress + address, size);
        }

        private static readonly int[] allowedErrors = [299];
        internal unsafe static byte[] ReadMemory(nint address, int size)
        {
            if (_process == null) { throw new InvalidOperationException("Process has not been opened"); }

            byte[] buffer = new byte[size];
            bool success;
            fixed (byte* pBuffer = buffer)
            {
                success = PInvoke.ReadProcessMemory(_procHandle, (void*)address, pBuffer, (nuint)size);
            }
            if (!success) {
                int errCode = Marshal.GetLastSystemError();
                if (allowedErrors.Contains(errCode))
                {
                    // Silently fail if expected error occurs. Usually when some game struct is not initialized (i.e. when there are no players)
                    // TODO: Do a gamestate check first before blindly dereferencing pointers
                    return [];
                } else {
                    Debug.WriteLine(errCode);
                    throw new SystemException($"Unexpected failure occured when calling ReadProcessMemory. System Error Code: {errCode}");
                }
            }
            return buffer;
        }

        public static void HandleSystemError()
        {
            HandleSystemError("Unhandled exception occured when calling p/invoke function.");
        }
        public static void HandleSystemError(string message)
        {
            int errCode = Marshal.GetLastSystemError();
            throw new SystemException($"{message} System Error Code: {errCode}");
        }
    }
}
