using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Threading;

namespace GGXXACPRInjector
{
    public class Injector(string processName, string mainDllDir, string[] dllDependencies)
    {
        private const int _timeout = 10000;

        private readonly string _processName = processName;
        private readonly string _dllDir = mainDllDir;
        private readonly string[] _dependencies = dllDependencies;

        private Process? _process;
        private SafeFileHandle? _procHandle;
        private uint bootstrapperHandle;

        // private static nint ejectFunctionOffset;

        public Injector(string processName, string mainDllDir) : this(processName, mainDllDir, []) { }


        public unsafe bool Inject()
        {
            var results = Process.GetProcessesByName(_processName);
            if ( results.Length == 0 || results[0] is null)
            {
                Console.WriteLine($"Couldn't find {_processName}. Please open +R then launch this exe again.");
                return false;
            }

            _process = results[0];
            nint targetProcess = _process.Id;

            _procHandle = PInvoke.OpenProcess_SafeHandle(
                PROCESS_ACCESS_RIGHTS.PROCESS_CREATE_THREAD
                | PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_INFORMATION
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_OPERATION
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_WRITE
                | PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ,
                false, (uint)targetProcess);

            if (_procHandle == null)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"OpenProcess_SafeHandle failed: Error Code {errCode}");
                return false;
            }

            // *IMPORTANT* This method of getting the address of LoadLibraryA relies on the injector's bitness being the same as the target process.
            // Create a delegate function for the LoadLibraryA function in kernel32
            FARPROC loadLibraryAddr = PInvoke.GetProcAddress(PInvoke.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            LPTHREAD_START_ROUTINE lpStartAddress = Marshal.GetDelegateForFunctionPointer<LPTHREAD_START_ROUTINE>(loadLibraryAddr.Value);

            if (loadLibraryAddr.IsNull)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"GetProcAddress failed: Error Code {errCode}");
                return false;
            }

            foreach (string dep in _dependencies)
            {
                Inject(dep, lpStartAddress);
                Thread.Sleep(30);   // Wait for dll to initalize
            }
            Inject(_dllDir, lpStartAddress);

            //// Allocate memory for the injectee file name
            //uint dllNameLength = (uint)(_dllDir.Length + 1 * Marshal.SizeOf<char>());

            //void* allocMemAddress = PInvoke.VirtualAllocEx(
            //    _procHandle,
            //    (void*)nint.Zero,
            //    dllNameLength,
            //    VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE,
            //    PAGE_PROTECTION_FLAGS.PAGE_READWRITE);

            //if (allocMemAddress == null)
            //{
            //    int errCode = Marshal.GetLastPInvokeError();
            //    Console.Error.WriteLine($"VirtualAllocEx failed: Error Code {errCode}");
            //    return false;
            //}

            //// Write the dll's filename into process memory
            //nuint bytesWritten;
            //byte[] buffer = Encoding.Default.GetBytes(_dllDir);
            //BOOL success;
            //fixed (byte* bufferPtr = &buffer[0])
            //{
            //    success = PInvoke.WriteProcessMemory(
            //        _procHandle,
            //        allocMemAddress,
            //        bufferPtr,
            //        dllNameLength,
            //        &bytesWritten);
            //}

            //if (!success)
            //{
            //    int errCode = Marshal.GetLastPInvokeError();
            //    Console.Error.WriteLine($"WriteProcessMemory failed: Error Code {errCode}");
            //    return false;
            //}

            //// Create a remote thread in the target process that begins execution at LoadLibraryA
            ////  with a string pointer parameter to the dll file name previously written in memory
            //void* lpThreadParameter = (void*)loadLibraryAddr.Value;
            //LPTHREAD_START_ROUTINE lpStartAddress = Marshal.GetDelegateForFunctionPointer<LPTHREAD_START_ROUTINE>(loadLibraryAddr.Value);
            //SafeFileHandle remoteThreadHandle = PInvoke.CreateRemoteThread(_procHandle, null, 0, lpStartAddress, allocMemAddress, 0, null);
            //if (remoteThreadHandle == null)
            //{
            //    int errCode = Marshal.GetLastPInvokeError();
            //    Console.Error.WriteLine($"CreateRemoteThread failed: Error Code {errCode}");
            //    PInvoke.VirtualFreeEx(_procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
            //    return false;
            //}

            //var waitEvent = PInvoke.WaitForSingleObject(remoteThreadHandle, _timeout);
            //if (waitEvent != WAIT_EVENT.WAIT_OBJECT_0)
            //{
            //    Console.WriteLine($"Remote thread failed. Return event: {waitEvent}");
            //    PInvoke.VirtualFreeEx(_procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
            //    return false;
            //}
            //PInvoke.GetExitCodeThread(remoteThreadHandle, out bootstrapperHandle);
            //remoteThreadHandle.Close();

            //if (bootstrapperHandle == 0)
            //{
            //    Console.Error.WriteLine($"Inject failed. Remote LoadLibrary returned 0.");
            //    PInvoke.VirtualFreeEx(_procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
            //    return false;
            //}

            //// release allocated memory
            //PInvoke.VirtualFreeEx(_procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);

            Console.WriteLine("Overlay injected");
            return true;
        }

        private unsafe bool Inject(string path, LPTHREAD_START_ROUTINE loadlibraryDelegate)
        {
            if (_procHandle is null) return false;

            // Allocate memory for the injectee file name
            uint dllNameLength = (uint)(path.Length + 1 * Marshal.SizeOf<char>());

            void* allocMemAddress = PInvoke.VirtualAllocEx(
                _procHandle,
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
            byte[] buffer = Encoding.Default.GetBytes(path);
            BOOL success = false;
            fixed (byte* bufferPtr = &buffer[0])
            {
                success = PInvoke.WriteProcessMemory(
                    _procHandle,
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
            SafeFileHandle remoteThreadHandle = PInvoke.CreateRemoteThread(_procHandle, null, 0, loadlibraryDelegate, allocMemAddress, 0, null);
            if (remoteThreadHandle == null)
            {
                int errCode = Marshal.GetLastPInvokeError();
                Console.Error.WriteLine($"CreateRemoteThread failed: Error Code {errCode}");
                PInvoke.VirtualFreeEx(_procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
                return false;
            }

            var waitEvent = PInvoke.WaitForSingleObject(remoteThreadHandle, _timeout);
            if (waitEvent != WAIT_EVENT.WAIT_OBJECT_0)
            {
                Console.WriteLine($"Remote thread failed. Return event: {waitEvent}");
                PInvoke.VirtualFreeEx(_procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
                return false;
            }
            PInvoke.GetExitCodeThread(remoteThreadHandle, out bootstrapperHandle);
            remoteThreadHandle.Close();

            if (bootstrapperHandle == 0)
            {
                Console.Error.WriteLine($"Inject failed. Remote LoadLibrary returned 0.");
                PInvoke.VirtualFreeEx(_procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);
                return false;
            }

            // release allocated memory
            PInvoke.VirtualFreeEx(_procHandle, allocMemAddress, 0, VIRTUAL_FREE_TYPE.MEM_RELEASE);

            return true;
        }


        private nint GetEjectMethodOffset()
        {
            SafeHandle localHandle = PInvoke.LoadLibraryEx(_processName, Windows.Win32.System.LibraryLoader.LOAD_LIBRARY_FLAGS.DONT_RESOLVE_DLL_REFERENCES);
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




        public bool Eject()
        {
            throw new NotImplementedException();

            //if (!injected || targetProc is null || (procHandle?.IsClosed ?? true)) return;

            //Console.WriteLine("[DEBUG] Calling remote unload function...");

            ////LPTHREAD_START_ROUTINE lpStartAddress = Marshal.GetDelegateForFunctionPointer<LPTHREAD_START_ROUTINE>((nint)remoteUnloadFunctionPtr);
            //HANDLE remoteThreadHandle = (HANDLE)CreateRemoteThread(procHandle.DangerousGetHandle(), 0, 0, (nint)(bootstrapperHandle + ejectFunctionOffset), 0, 0, out _);
            //if (remoteThreadHandle == nint.Zero)
            //{
            //    int errCode = Marshal.GetLastPInvokeError();
            //    Console.Error.WriteLine($"[ERROR] CreateRemoteThread failed: Error Code 0x{errCode:X8}");
            //    return;
            //}

            //PInvoke.WaitForSingleObject(remoteThreadHandle, _timeout);
            //uint result = 0;
            //if (!PInvoke.GetExitCodeThread(remoteThreadHandle, &result))
            //{
            //    int errCode = Marshal.GetLastPInvokeError();
            //    Console.Error.WriteLine($"[ERROR] GetExitCodeThread failed: Error Code 0x{errCode:X8}");
            //    return;
            //}
            //PInvoke.CloseHandle(remoteThreadHandle);

            //Console.WriteLine($"[DEBUG] Overlay ejected. Result: 0x{result:X8}");
        }


    }
}
