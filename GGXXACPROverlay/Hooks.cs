using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Memory;

namespace GGXXACPROverlay
{
    internal static unsafe partial class Hooks
    {
        private const byte RELATIVE_JMP_OP_CODE = 0xE9;
        private const byte INT3_OP_CODE = 0xCC;
        private const int _pageSize = 1024;

        [Flags]
        public enum ExceptionContinue : int
        {
            EXCEPTION_CONTINUE_SEARCH = 0,
            EXCEPTION_CONTINUE_EXECUTION = -1,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EXCEPTION_POINTERS
        {
            public nint ExceptionRecord;
            public nint ContextRecord;
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate ExceptionContinue VectoredExceptionHandler(EXCEPTION_POINTERS* exceptionInfo);

        [LibraryImport("kernel32.dll")]
        private static partial nint AddVectoredExceptionHandler(uint First, VectoredExceptionHandler handler);
        [LibraryImport("kernel32.dll")]
        private static partial uint RemoveVectoredExceptionHandler(nint handle);

        // TODO: convert to UnmanagedFunctionPointer delegate?
        /// <summary>
        /// D3D9PresentTrampoline(void* D3D9Device, void* pSourceRect, void* pDestRect, void* hDestWindowOverride, void* pDirtyRegion)
        /// </summary>
        private static delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int> GraphicsTrampoline;
        private static byte originalGraphicsHookBreakPointByte;
        private static readonly byte[] originalD3D9PresentInstructions = new byte[5];
        private static nint graphicsHookHandlerHandle;
        private static VectoredExceptionHandler? graphicsHookHandler;   // static ref to ward off GC
        private static nint _page;

        /// <summary>
        /// Sets the EAX, ECX, and EDX cpu registers.
        /// </summary>
        internal static delegate* unmanaged[Cdecl, SuppressGCTransition]<uint, uint, uint, int> CustomCallingConventionParameters;

        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        public static bool InstallHooks()
        {
            _page = Marshal.AllocHGlobal(_pageSize);

            WriteASMCallingConventionHelper(_page);
            if (!InstallGraphicsHooks(_page + 0x100))
            {
                Debug.Log("InstallGraphicsHooks encountered an error.");
                UninstallHooks();
                return false;
            }
            // other hooks?

            // unpause threads

            return true;
        }

        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        public static bool UninstallHooks()
        {
            UnHookGraphics();
            Marshal.FreeHGlobal(_page);
            return true;
        }

        private static bool SetBreakPoint(nint address, out byte originalByte)
        {
            return OverwriteAsmByte(address, INT3_OP_CODE, out originalByte);
        }
        private static bool RevertBreakPoint(nint address, byte originalByte)
        {
            return OverwriteAsmByte(address, originalByte, out _);
        }
        private static bool OverwriteAsmByte(nint address, byte newByte, out byte oldByte)
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

            success = PInvoke.VirtualProtect(
                (void*)address,
                1,
                oldProtect,
                out _
            );
            if (!success) Debug.Log("revert VirtualProtect failed");

            PInvoke.FlushInstructionCache(PInvoke.GetCurrentProcess_SafeHandle(), (void*)address, 1);

            return true;
        }

        /// <summary>
        /// D3D9 Present function assumed offset:
        ///  GGXXACPR_Win+710580[0][0][17]
        /// </summary>
        /// <returns></returns>
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static nint GetD3D9PresentFuncPtr()
        {
            void* unsafePtr = (void*)GGXXACPR.GGXXACPR.Direct3D9DevicePointer;
            if (((nint*)unsafePtr)[0] == nint.Zero)
            {
                return nint.Zero;
            }

            return ((nint**)unsafePtr)[0][17];
        }

        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static bool InstallGraphicsHooks(nint trampolineAddress)
        {
            // Set breakpoint to control graphics thread
            Debug.Log("Begin breakpoint");
            nint handlerAddr = (nint)(delegate* unmanaged[Stdcall]<EXCEPTION_POINTERS*, ExceptionContinue>)&GraphicsBreakPointHandler;
            graphicsHookHandler = Marshal.GetDelegateForFunctionPointer<VectoredExceptionHandler>(handlerAddr);
            // TODO: Use GCHandles instead of static ref pinning?
            //GCHandle.Alloc(graphicsHookHandler, GCHandleType.Normal);
            graphicsHookHandlerHandle = AddVectoredExceptionHandler(1, graphicsHookHandler);
            Debug.Log($"VEHandler set: 0x{graphicsHookHandlerHandle:X8}");

            SetBreakPoint(GGXXACPR.GGXXACPR.GraphicsHookBreakPointAddress, out originalGraphicsHookBreakPointByte);
            Debug.Log("BreakPoint set");

            return true;
        }

        private static bool UnHookGraphics()
        {
            // Set breakpoint to control graphics thread
            Debug.Log("Begin unhook breakpoint");
            nint handlerAddr = (nint)(delegate* unmanaged[Stdcall]<EXCEPTION_POINTERS*, ExceptionContinue>)&UnhookGraphicsBreakPointHandler;
            graphicsHookHandler = Marshal.GetDelegateForFunctionPointer<VectoredExceptionHandler>(handlerAddr);
            // TODO: Use GCHandles instead of static ref pinning?
            //GCHandle.Alloc(graphicsHookHandler, GCHandleType.Normal);
            graphicsHookHandlerHandle = AddVectoredExceptionHandler(1, graphicsHookHandler);
            Debug.Log($"VEHandler set: 0x{graphicsHookHandlerHandle:X8}");

            SetBreakPoint(GGXXACPR.GGXXACPR.GraphicsHookBreakPointAddress, out originalGraphicsHookBreakPointByte);
            Debug.Log("BreakPoint set");

            return true;
        }


        /// <summary>
        /// Writes the trampoline part of a detour/trampoline hook.
        /// 
        /// Gotta be extra careful with the contents of stolenInstructions.
        ///  Any relative addresses will break in the trampoline function.
        /// </summary>
        /// <param name="stolenInstructions">Machine code overwritten by detour</param>
        /// <param name="returnAddress">Address to return to</param>
        /// <returns>Memory address of the Trampoline function</returns>
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static nint WriteTrampoline(nint trampolineAddr, Span<byte> stolenInstructions, nint returnAddress)
        {
            // Compute relative jump offset
            byte[] relAddrBytes = BitConverter.GetBytes(trampolineAddr - (returnAddress + 5));

            byte[] adjustedStolenInstructions = [.. stolenInstructions];
            // Make adjustments to stolenInstructions in case of stolen relative jmp instruction
            if (adjustedStolenInstructions[0] == RELATIVE_JMP_OP_CODE)
            {
                var relJmpOffset = BitConverter.ToInt32(adjustedStolenInstructions, 1);
                var targetAddress = relJmpOffset + (returnAddress + 5);
                Debug.Log($"Original targetAddress: 0x{targetAddress:X8}");
                int newRelJmpOffset = (int)(relJmpOffset + returnAddress - trampolineAddr);
                Debug.Log($"Trampoline newRelJmpOffset: 0x{newRelJmpOffset:X8}");
                byte[] newRelJmpBytes = BitConverter.GetBytes(newRelJmpOffset);
                adjustedStolenInstructions = [adjustedStolenInstructions[0], .. newRelJmpBytes];
            }
            // Generate trampoline asm
            byte[] asm = [.. adjustedStolenInstructions, RELATIVE_JMP_OP_CODE, .. relAddrBytes];
            // Write asm to allocated memory
            Marshal.Copy(asm, 0, trampolineAddr, asm.Length);

            // Save pointer as delegate
            //GraphicsTrampoline = Marshal.GetDelegateForFunctionPointer<D3D9PresentTrampoline>(trampolineAddr);
            GraphicsTrampoline = (delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int>)trampolineAddr;

            return trampolineAddr;
        }

        /// <summary>
        /// Writes a small helper funciton in x86 assembly that will help interface with some of +R's functions.
        /// </summary>
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static void WriteASMCallingConventionHelper(nint address)
        {
            byte[] asm = [
                0x8B, 0x44, 0x24, 0x04, // mov eax, DWORD PTR [esp+0x04]
                0x8B, 0x4C, 0x24, 0x08, // mov ecx, DWORD PTR [esp+0x08]
                0x8B, 0x54, 0x24, 0x0C, // mov edx, DWORD PTR [esp+0x0C]
                0xC3,                   // ret
            ];

            Marshal.Copy(asm, 0, address, asm.Length);

            CustomCallingConventionParameters = (delegate* unmanaged[Cdecl, SuppressGCTransition]<uint, uint, uint, int>)address;
        }


        #region Unmanaged Only
        private static bool _handling = false;
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = "GraphicsBreakPointHandler")]
        private static ExceptionContinue GraphicsBreakPointHandler(EXCEPTION_POINTERS* exceptionInfo)
        {
            if (_handling) return ExceptionContinue.EXCEPTION_CONTINUE_EXECUTION;
            _handling = true;

            Debug.Log("Breakpoint Handled!");
            if (exceptionInfo is not null) Debug.Log($"ContextRecordPtr: 0x{exceptionInfo->ContextRecord:X8}");

            // bail out if not expected EIP (GGXXACPR.GGXXACPR.GraphicsHookBreakPointAddress)
            // TODO

            // Graphics init here to make sure the d3d9 device is in a stable state to create new resources
            _ = new Overlay(new Graphics(GGXXACPR.GGXXACPR.Direct3D9DevicePointer));
            Debug.Log("Overlay initalized");

            nint targetAddress = GetD3D9PresentFuncPtr();
            nint trampolineAddress = _page + 0x100;

            // VirtualProtect first couple instruction at target function to execute/read/write
            bool success = PInvoke.VirtualProtect((void*)targetAddress, 1024, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out PAGE_PROTECTION_FLAGS prevFlags);
            if (!success)
            {
                Debug.Log($"VirtualProtect failed: {Marshal.GetLastSystemError()}");
                return ExceptionContinue.EXCEPTION_CONTINUE_SEARCH;
            }
            Debug.Log("VirtualProtect executed");

            // Record first couple instructions
            Marshal.Copy(targetAddress, originalD3D9PresentInstructions, 0, 5);

            // Generate trampoline function
            WriteTrampoline(trampolineAddress, originalD3D9PresentInstructions, targetAddress);
            Debug.Log("Trampoline written");

            // Overwrite recorded instructions with jump to hook function.
            nint hookAddr = (nint)(delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int>)&GraphicsHook;
            byte[] relAddrBytes = BitConverter.GetBytes((int)(hookAddr - (targetAddress + 5)));
            Debug.Log($"Relative jmp offset: 0x{hookAddr - (targetAddress + 5):X8}");
            byte[] asm = [RELATIVE_JMP_OP_CODE, .. relAddrBytes];
            Marshal.Copy(asm, 0, targetAddress, asm.Length);
            Debug.Log($"Hook Written at 0x{targetAddress:X8}");

            // Reverting VirtualProtect
            success = PInvoke.VirtualProtect((void*)targetAddress, 1024, prevFlags, out _);
            if (!success)
            {
                Debug.Log($"VirtualProtect revert failed: {Marshal.GetLastSystemError()}");
                return ExceptionContinue.EXCEPTION_CONTINUE_SEARCH;
            }
            Debug.Log("VirtualProtect reverted");
            PInvoke.FlushInstructionCache(PInvoke.GetCurrentProcess_SafeHandle(), (void*)targetAddress, 5);

            Debug.Log("GraphicsHook Successfully installed");

            RevertBreakPoint(GGXXACPR.GGXXACPR.GraphicsHookBreakPointAddress, originalGraphicsHookBreakPointByte);
            Debug.Log("Breakpoint byte reverted");
            PInvoke.FlushInstructionCache(PInvoke.GetCurrentProcess_SafeHandle(), (void*)GGXXACPR.GGXXACPR.GraphicsHookBreakPointAddress, 1);

            // Remove this exception handler
            var result = RemoveVectoredExceptionHandler(graphicsHookHandlerHandle);
            Debug.Log($"RemoveVectoredExceptionHandler: {result}");

            return ExceptionContinue.EXCEPTION_CONTINUE_EXECUTION;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = "UnhookGraphicsBreakPointHandler")]
        private static ExceptionContinue UnhookGraphicsBreakPointHandler(EXCEPTION_POINTERS* exceptionInfo)
        {
            Debug.Log("Breakpoint Handled!");
            if (exceptionInfo is not null) Debug.Log($"ContextRecordPtr: 0x{exceptionInfo->ContextRecord:X8}");

            // bail out if not expected EIP (GGXXACPR.GGXXACPR.GraphicsHookBreakPointAddress)
            // TODO

            nint targetAddress = GetD3D9PresentFuncPtr();
            nint trampolineAddress = _page + 0x100;

            // VirtualProtect first couple instruction at target function to execute/read/write
            bool success = PInvoke.VirtualProtect((void*)targetAddress, 1024, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out PAGE_PROTECTION_FLAGS prevFlags);
            if (!success)
            {
                Debug.Log($"VirtualProtect failed: {Marshal.GetLastSystemError()}");
                return ExceptionContinue.EXCEPTION_CONTINUE_SEARCH;
            }
            Debug.Log("VirtualProtect executed");

            // Overwrite recorded instructions with jump to hook function.
            Marshal.Copy(originalD3D9PresentInstructions, 0, targetAddress, originalD3D9PresentInstructions.Length);
            Debug.Log($"Original instructions rewritten at 0x{targetAddress:X8}");

            // Reverting VirtualProtect
            success = PInvoke.VirtualProtect((void*)targetAddress, 1024, prevFlags, out _);
            if (!success)
            {
                Debug.Log($"VirtualProtect revert failed: {Marshal.GetLastSystemError()}");
                return ExceptionContinue.EXCEPTION_CONTINUE_SEARCH;
            }
            Debug.Log("VirtualProtect reverted");
            PInvoke.FlushInstructionCache(PInvoke.GetCurrentProcess_SafeHandle(), (void*)targetAddress, 5);

            Debug.Log("GraphicsHook Successfully Uninstalled");

            RevertBreakPoint(GGXXACPR.GGXXACPR.GraphicsHookBreakPointAddress, originalGraphicsHookBreakPointByte);
            Debug.Log("Breakpoint byte reverted");
            PInvoke.FlushInstructionCache(PInvoke.GetCurrentProcess_SafeHandle(), (void*)GGXXACPR.GGXXACPR.GraphicsHookBreakPointAddress, 1);

            // Remove this exception handler
            var result = RemoveVectoredExceptionHandler(graphicsHookHandlerHandle);
            Debug.Log($"RemoveVectoredExceptionHandler: {result}");

            return ExceptionContinue.EXCEPTION_CONTINUE_EXECUTION;
        }

        /// <summary>
        /// *IMPORTANT* Make sure hook method signature is identical to IDirect3DDevice9::Present
        /// </summary>
        /// <param name="pSourceRect">See IDirect3DDevice9::Present</param>
        /// <param name="pDestRect">See IDirect3DDevice9::Present</param>
        /// <param name="hDestWindowOverride">See IDirect3DDevice9::Present</param>
        /// <param name="pDirtyRegion">See IDirect3DDevice9::Present</param>
        /// <returns>HRESULT</returns>
        /// <exception cref="NullReferenceException"></exception>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = "GraphicsHook")]
        private static int GraphicsHook(void* d3d9Device, void* pSourceRect, void* pDestRect, void* hDestWindowOverride, void* pDirtyRegion)
        {
            Overlay.Instance?.RenderFrame();

            // Call trampoline function to return program flow back to D3D9 Present
            return GraphicsTrampoline(d3d9Device, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
        }
        #endregion
    }
}
