using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace GGXXACPROverlay
{
    internal static partial class Memory
    {
        // C# bool is 4bytes so we need to use this struct to handle casting from the 1 byte Bool from the P/Invoke functions
        //private readonly struct BoolByte(bool boolean)
        //{
        //    private readonly byte b = (byte)(boolean ? 1 : 0);

        //    public static implicit operator BoolByte (bool b) { return new BoolByte (b); }
        //    public static implicit operator bool (BoolByte boolByte) { return boolByte.b > 0; }
        //}

        //[LibraryImport("kernel32.dll")]
        //private static partial nint OpenProcess(int dwDesiredAcess, BoolByte bInheritHandle, int dwProcessId);
        //[LibraryImport("kernel32.dll")]
        //private static partial BoolByte CloseHandle(nint hObject);
        //[LibraryImport("kernel32.dll")]
        //private static partial BoolByte ReadProcessMemory(
        //    nint hProcess,
        //    nint lpBaseAddress,
        //    [Out, MarshalUsing(CountElementName ="dwSize")] byte[] lpBuff,
        //    int dwSize,
        //    out int lpNumberOfBytesRead
        //);
        //[LibraryImport("kernel32.dll")]
        //private static partial int GetLastError();
        //[LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        //private static partial nint GetModuleHandleA(string? lpModuleName);


        private static readonly int _PROCESS_VM_READ = 0x0010;

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

        //internal static nint GetDllHandle(Type T)
        //{
        //    string? dll = T.Assembly.GetName().Name;
        //    if (dll != null)
        //    {
        //        nint modHandle = PInvoke.GetModuleHandle(dll);
        //        if (modHandle == nint.Zero)
        //        {
        //            Memory.HandleSystemError("GetModuleHandleA returned null pointer.");
        //        }
        //        return modHandle;

        //        throw new SystemException("GetModuleHandle returned null pointer.");
        //    }

        //    throw new SystemException("Executing Assemply full name is null.");
        //}

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
