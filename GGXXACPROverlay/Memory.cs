using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace GGXXACPROverlay
{
    internal static partial class Memory
    {
        // C# bool is 4bytes so we need to use this struct to handle casting from the 1 byte Bool from the P/Invoke functions
        private readonly struct BoolByte(bool boolean)
        {
            private readonly byte b = (byte)(boolean ? 1 : 0);

            public static implicit operator BoolByte (bool b) { return new BoolByte (b); }
            public static implicit operator bool (BoolByte boolByte) { return boolByte.b > 0; }
        }

        [LibraryImport("kernel32.dll")]
        private static partial nint OpenProcess(int dwDesiredAcess, BoolByte bInheritHandle, int dwProcessId);
        [LibraryImport("kernel32.dll")]
        private static partial BoolByte CloseHandle(nint hObject);
        [LibraryImport("kernel32.dll")]
        private static partial BoolByte ReadProcessMemory(
            nint hProcess,
            nint lpBaseAddress,
            [Out, MarshalUsing(CountElementName ="dwSize")] byte[] lpBuff,
            int dwSize,
            out int lpNumberOfBytesRead
        );
        [LibraryImport("kernel32.dll")]
        private static partial int GetLastError();

        private static readonly int _PROCESS_VM_READ = 0x0010;

        private static Process? _process;
        private static nint _procHandle;
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
            _procHandle = OpenProcess(_PROCESS_VM_READ, true, _process.Id);
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
            bool success = CloseHandle(_procHandle);
            if (!success) { HandleSystemError("Error closing process."); }
            else { Debug.WriteLine("Process Closed"); }
        }

        internal static nint GetGameWindowHandle()
        {
            if (_process == null) { throw new InvalidOperationException("Process has not been opened"); }
            return _process.MainWindowHandle;
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
        internal static byte[] ReadMemory(nint address, int size)
        {
            if (_process == null) { throw new InvalidOperationException("Process has not been opened"); }

            byte[] buffer = new byte[size];
            bool success = ReadProcessMemory(_procHandle, address, buffer, size, out int _);
            if (!success) {
                int errCode = GetLastError();
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
            int errCode = GetLastError();
            throw new SystemException($"{message} System Error Code: {errCode}");
        }
    }
}
