using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;
using Windows.Win32.System.Memory;
using Microsoft.Win32.SafeHandles;

namespace GGXXACPRInjector
{
    internal unsafe class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("== GGXXACPROverlay Injector ==");
            Console.WriteLine("Version: ?");

            // TODO: decide how to package dll
            return Inject("GGXXACPR_Win", "C:\\Users\\chase\\workspace\\GGXXACPROverlay\\GGXXACPROverlay\\bin\\Release\\net9.0-windows\\publish\\win-x86\\GGXXACPROverlay");

            // TODO: unhook UX?
        }

        static int Inject(string targetProcName, string dllName)
        {
            // Find target process
            var results = Process.GetProcessesByName(targetProcName);
            if (results.Length < 1)
            {
                Console.Error.WriteLine($"Process \"{targetProcName}\" not found");
                return 1;
            }

            nint targetProcess = results[0].Id;

            SafeFileHandle procHandle = PInvoke.OpenProcess_SafeHandle(
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
                return 1;
            }

            // *IMPORTANT* This method of getting the address of LoadLibraryA relies on the injector's bitness being the same as the target process
            // Create a delegate function for the LoadLibraryA function in kernel32
            FARPROC loadLibraryAddr = PInvoke.GetProcAddress(PInvoke.GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (loadLibraryAddr.IsNull)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"GetProcAddress failed: Error Code {errCode}");
                return 1;
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
                return 1;
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
                return 1;
            }

            // Create a remote thread in the target process that begins execution at LoadLibraryA
            //  with a pointer parameter pointing to the dll file name previously written in memory
            void* lpThreadParameter = (void*)loadLibraryAddr.Value;
            LPTHREAD_START_ROUTINE lpStartAddress = Marshal.GetDelegateForFunctionPointer<LPTHREAD_START_ROUTINE>(loadLibraryAddr.Value);
            SafeFileHandle remoteThreadHandle = PInvoke.CreateRemoteThread(procHandle, null, 0, lpStartAddress, allocMemAddress, 0, null);
            if (remoteThreadHandle == null)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"CreateRemoteThread failed: Error Code {errCode}");
                return 1;
            }

            Console.WriteLine("Completed without error");
            return 0;
        }
    }
}
