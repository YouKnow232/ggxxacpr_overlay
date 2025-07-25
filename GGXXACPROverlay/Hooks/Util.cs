using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Diagnostics.Debug;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Threading;

namespace GGXXACPROverlay.Hooks
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    unsafe delegate int D3D9Present(void* d3d9Device, void* pSourceRect, void* pDestRect, void* hDestWindowOverride, void* pDirtyRegion);

    internal class Util
    {
        private const byte POPAD_OP_CODE = 0x61;
        private const byte RELATIVE_CALL_OP_CODE = 0xE8;
        private const byte RELATIVE_JUMP_OP_CODE = 0xE9;
        private const byte INT3_OP_CODE = 0xCC;
        private const int RELATIVE_JUMP_INSTRUCTION_SIZE = 5;

        private static readonly nint _page = Marshal.AllocHGlobal(0x1000);

        private static readonly unsafe delegate* unmanaged[Cdecl, SuppressGCTransition]<uint, uint, uint, int>
            _customCallingConventionParameters = WriteASMCallingConventionHelper(_page);
        /// <summary>
        /// Sets the EAX, ECX, and EDX cpu registers.
        /// </summary>
        public static unsafe delegate* unmanaged[Cdecl, SuppressGCTransition]<uint, uint, uint, int>
            CustomCallingConventionParameters => _customCallingConventionParameters;

        /// <summary>
        /// Writes a small helper funciton in x86 assembly that will help interface with some of +R's functions.
        /// </summary>
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe delegate* unmanaged[Cdecl, SuppressGCTransition]<uint, uint, uint, int> WriteASMCallingConventionHelper(nint address)
        {
            byte[] asm = [
                0x8B, 0x44, 0x24, 0x04, // mov eax, DWORD PTR [esp+0x04]
                0x8B, 0x4C, 0x24, 0x08, // mov ecx, DWORD PTR [esp+0x08]
                0x8B, 0x54, 0x24, 0x0C, // mov edx, DWORD PTR [esp+0x0C]
                0xC3,                   // ret
            ];

            Marshal.Copy(asm, 0, address, asm.Length);

            return (delegate* unmanaged[Cdecl, SuppressGCTransition]<uint, uint, uint, int>)address;
        }

        internal static bool SetBreakPoint(nint address, out byte originalByte)
        {
            return OverwriteAsmByte(address, INT3_OP_CODE, out originalByte);
        }
        internal static bool RevertBreakPoint(nint address, byte originalByte)
        {
            return OverwriteAsmByte(address, originalByte, out _);
        }
        internal static unsafe bool OverwriteAsmByte(nint address, byte newByte, out byte oldByte)
        {
            bool success;
            oldByte = *(byte*)address;

            success = PInvoke.VirtualProtect(
                (void*)address,
                1,
                PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE,
                out var oldProtect
            );
            if (!success) return false;

            *(byte*)address = newByte;

            success = PInvoke.VirtualProtect((void*)address, sizeof(byte), oldProtect, out _);
            if (!success) Debug.Log("revert VirtualProtect failed");

            PInvoke.FlushInstructionCache(PInvoke.GetCurrentProcess_SafeHandle(), (void*)address, sizeof(byte));

            return true;
        }

        public static nint CalculateNewRelativeOffset(nint originalOffset, nint originalSourceAddress, nint newSourceAddress)
        {
            return originalOffset + originalSourceAddress - newSourceAddress;
        }

        /// <summary>
        /// Allocates a page in global memory and writes a trampoline function. *DEPRECATED* See WriteToFromCallTrampoline implementation.
        /// </summary>
        /// <param name="returnAddress">Where the trampoline should return</param>
        /// <param name="originalBytes">the original function bytes that were overwritten for the detour.
        ///     This method will adapat any relative jmp instructions.</param>
        /// <returns>The allocated page and address of the trampoline function. This memory should be
        ///     freed with <c>VirtualFree</c> when uninstalling the hook.</returns>
        public static unsafe nint WriteTrampoline(nint returnAddress, byte[] originalBytes, out nuint bytesWritten)
        {
            bytesWritten = (nuint)(originalBytes.Length + RELATIVE_JUMP_INSTRUCTION_SIZE);
            Debug.Log($"Trampoline allocation size: {bytesWritten} bytes");
            nint trampolineAddress = (nint)PInvoke.VirtualAlloc(
                null,
                bytesWritten,
                VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE,
                PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE
            );
            if (trampolineAddress == 0) Debug.Log("Oops trampolineAddress fucky wucky!!");
            Debug.Log($"Trampoline allocated at 0x{trampolineAddress:X8}");

            int returnJumpOffset = (int)returnAddress - ((int)trampolineAddress + RELATIVE_JUMP_INSTRUCTION_SIZE + originalBytes.Length);
            byte[] returnJumpOffsetBytes = BitConverter.GetBytes(returnJumpOffset);

            byte[] trampolineBody = [.. originalBytes];
            
            if (originalBytes[0] == RELATIVE_JUMP_OP_CODE)
            {
                nint originalJumpOffset = BitConverter.ToInt32(originalBytes, sizeof(byte));
                nint originalAbsoluteJumpAddress = originalJumpOffset + returnAddress;
                int newReturnJumpOffset = (int)originalAbsoluteJumpAddress - ((int)trampolineAddress + originalBytes.Length);
                byte[] newReturnJumpOffsetBytes = BitConverter.GetBytes(newReturnJumpOffset);
                trampolineBody = [RELATIVE_JUMP_OP_CODE, .. newReturnJumpOffsetBytes];
            }

            byte[] trampolineAssembly = [.. trampolineBody, RELATIVE_JUMP_OP_CODE, .. returnJumpOffsetBytes];
            Debug.Log($"raw trampoline assembly: {BitConverter.ToString(trampolineAssembly)}");
            Marshal.Copy(trampolineAssembly, 0, trampolineAddress, trampolineAssembly.Length);

            return trampolineAddress;
        }

        /// <summary>
        /// Allocates a page in memory and writes a trampoline function to it. The trampoline function is composied of 4 parts:<br/>
        /// * A function call preamble that forwards the hooked function parameters to the hook function<br/>
        /// * A call to the hook<br/>
        /// * The original function preamble that was overwritten by the detour<br/>
        /// * A jump back to the original function
        /// </summary>
        /// <param name="hookAddress">Address of the hook function this calls</param>
        /// <param name="returnAddress">Address the trampoline should return to. Typically the detour installation address + 5</param>
        /// <param name="originalBytes">Instructions that the detour overwrote</param>
        /// <param name="bytesWritten">Size of the trampoline function</param>
        /// <returns>Trampoline function pointer / page address</returns>
        public static unsafe nint WriteToFromCallTrampoline(nint hookAddress, nint returnAddress, byte[] originalBytes, out nuint bytesWritten)
        {
            // Forwards function parameters to hook
            byte[] preambleAssembly = [
                0x60,                       // pushad
                0xFF, 0x74, 0x24, 0x34,     // push [esp+4C]
                0xFF, 0x74, 0x24, 0x34,     // push [esp+4C]
                0xFF, 0x74, 0x24, 0x34,     // push [esp+4C]
                0xFF, 0x74, 0x24, 0x34,     // push [esp+4C]
                0xFF, 0x74, 0x24, 0x34,     // push [esp+4C]
            ];

            bytesWritten = (nuint)(
                preambleAssembly.Length +           // Preamble
                RELATIVE_JUMP_INSTRUCTION_SIZE +    // Call Hook
                sizeof(byte) +                      // popad
                originalBytes.Length +              // originalInstructions
                RELATIVE_JUMP_INSTRUCTION_SIZE);    // Return jump

            Debug.Log($"Trampoline allocation size: {bytesWritten} bytes");
            nint trampolineAddress = (nint)PInvoke.VirtualAlloc(
                null,
                bytesWritten,
                VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT | VIRTUAL_ALLOCATION_TYPE.MEM_RESERVE,
                PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE
            );
            if (trampolineAddress == 0) Debug.Log("Oops trampolineAddress fucky wucky!!");
            Debug.Log($"Trampoline allocated at 0x{trampolineAddress:X8}");

            int hookCallOffset = (int)hookAddress - ((int)trampolineAddress + preambleAssembly.Length + RELATIVE_JUMP_INSTRUCTION_SIZE);
            byte[] hookCallAssembly = [RELATIVE_CALL_OP_CODE, ..BitConverter.GetBytes(hookCallOffset)];
            Debug.Log($"Hook address: 0x{hookAddress:X8}");
            int returnJumpOffset = (int)returnAddress - ((int)trampolineAddress + (int)bytesWritten);
            byte[] returnJumpOffsetBytes = BitConverter.GetBytes(returnJumpOffset);

            byte[] trampolineBody = [.. originalBytes];

            if (originalBytes[0] == RELATIVE_JUMP_OP_CODE)
            {
                nint originalJumpOffset = BitConverter.ToInt32(originalBytes, sizeof(byte));
                nint hookCallReturnAddress = trampolineAddress + preambleAssembly.Length + 1 + RELATIVE_JUMP_INSTRUCTION_SIZE * 2;
                int newReturnJumpOffset = (int)CalculateNewRelativeOffset(originalJumpOffset, returnAddress, hookCallReturnAddress);
                byte[] newReturnJumpOffsetBytes = BitConverter.GetBytes(newReturnJumpOffset);

                trampolineBody = [RELATIVE_JUMP_OP_CODE, .. newReturnJumpOffsetBytes];
            }

            byte[] trampolineAssembly = [
                .. preambleAssembly,
                .. hookCallAssembly,
                POPAD_OP_CODE,
                .. trampolineBody,
                RELATIVE_JUMP_OP_CODE, .. returnJumpOffsetBytes
            ];
            if (trampolineAssembly.Length != (int)bytesWritten) Debug.Log("WriteToFromCallTrampoline trampoline asm has unexpected number of bytes than expected.");

            Debug.Log($"raw trampoline assembly: {BitConverter.ToString(trampolineAssembly)}");
            Marshal.Copy(trampolineAssembly, 0, trampolineAddress, trampolineAssembly.Length);

            return trampolineAddress;

        }

        /// <summary>
        /// Patches in a detour to a function hook.
        /// </summary>
        /// <param name="patchAddress">The address to patch.</param>
        /// <param name="hookAddress">The address of the hook function.</param>
        /// <returns>The original bytes that were overwritten at <c>patchAddress</c>.</returns>
        public static unsafe byte[] PatchHookDetour(nint patchAddress, nint hookAddress)
        {
            int detourJumpOffset = (int)(hookAddress - (patchAddress + RELATIVE_JUMP_INSTRUCTION_SIZE));
            byte[] detourBytes = [RELATIVE_JUMP_OP_CODE, .. BitConverter.GetBytes(detourJumpOffset)];

            return Patch(patchAddress, detourBytes);
        }

        /// <summary>
        /// Patches a segment of memory executable, returning the overwritten bytes.
        /// </summary>
        /// <param name="patchAddress">Target Address</param>
        /// <param name="payload">Patch payload</param>
        /// <returns>Overwritten bytes</returns>
        public static unsafe byte[] Patch(nint patchAddress, byte[] payload)
        {
            bool success = PInvoke.VirtualProtect((void*)patchAddress, (nuint)payload.Length, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out var oldProtect);
            if (!success) throw new COMException($"Virtual Protect failed at address 0x{patchAddress:X8}", Marshal.GetLastSystemError());

            byte[] originalBytes = new byte[payload.Length];

            Marshal.Copy(patchAddress, originalBytes, 0, payload.Length);
            Marshal.Copy(payload, 0, patchAddress, payload.Length);

            success = PInvoke.VirtualProtect((void*)patchAddress, (nuint)payload.Length, oldProtect, out _);
            if (!success) throw new COMException($"Virtual Protect failed at address 0x{patchAddress:X8}", Marshal.GetLastSystemError());
            return originalBytes;
        }

        /// <summary>
        /// Safely pauses the main game thread such that it's EIP is not in any of the given dangerous memory regions.
        /// </summary>
        /// <param name="memoryRegions">Dangerous memory regions that the game thread should not be stopped at</param>
        /// <returns>A SafeHandle to the thread so the caller can resume the thread when finished</returns>
        public static SafeHandle SafelyPauseMainThread(params Range[] memoryRegions)
        {
            Debug.Log("Pausing Main Thread");
            foreach (var region in memoryRegions)
            {
                Debug.Log($"Region start: 0x{region.Start.Value:X8} | end: 0x{region.End.Value:X8}");
            }

            const int timeout = 1000;

            SafeHandle hMainThread = PInvoke.OpenThread_SafeHandle(
                THREAD_ACCESS_RIGHTS.THREAD_SUSPEND_RESUME | THREAD_ACCESS_RIGHTS.THREAD_GET_CONTEXT,
                false,
                (uint)Memory.MainThread.Id
            );
            if (hMainThread.IsInvalid) throw new COMException("Failed to open main thread", Marshal.GetLastSystemError());

            CONTEXT threadContext = new()
            {
                ContextFlags = CONTEXT_FLAGS.CONTEXT_ALL_X86
            };

            uint suspendCount = 0;
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.IsRunning && sw.ElapsedMilliseconds < timeout)
            {
                suspendCount = PInvoke.SuspendThread(hMainThread);
                Debug.Log($"Main Game Thread suspend count: {suspendCount}");
                if (suspendCount == uint.MaxValue) throw new COMException("Failed to suspend main thread", Marshal.GetLastSystemError());

                bool success = PInvoke.GetThreadContext(hMainThread, ref threadContext);
                if (!success) throw new COMException("Failed to get thread context", Marshal.GetLastSystemError());

                if (!memoryRegions.Any(range => threadContext.Eip >= range.Start.Value && threadContext.Eip <= range.End.Value))
                    break;

                suspendCount = PInvoke.ResumeThread(hMainThread);
                Debug.Log($"Main Game Thread resumed suspend count: {suspendCount}");
                if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());

                Thread.Sleep(1);
            }

            sw.Stop();
            if (sw.ElapsedMilliseconds > timeout) throw new TimeoutException("Could not find a safe stopping point for the main thread");

            Debug.Log($"Thread suspend count: {suspendCount}");
            return hMainThread;
        }


        ~Util()
        {
            Marshal.FreeHGlobal(_page);
        }
    }
}
