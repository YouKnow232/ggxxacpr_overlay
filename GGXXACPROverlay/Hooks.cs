using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Memory;

namespace GGXXACPROverlay
{
    internal static unsafe class Hooks
    {
        private const byte RELATIVE_JMP_OP_CODE = 0xE9;

        //delegate int D3D9PresentTrampoline(void* D3D9Device, void* pSourceRect, void* pDestRect, void* hDestWindowOverride, void* pDirtyRegion);
        //private static D3D9PresentTrampoline? GraphicsTrampoline;
        private static delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int> GraphicsTrampoline;


        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        public static bool InstallHooks()
        {
            if (!InstallGraphicsHooks())
            {
                Console.WriteLine("InstallGraphicsHooks encountered an error.");
            }
            // other hooks
            return true;
        }

        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        public static bool UninstallHooks()
        {
            return true;
        }

        /// <summary>
        /// D3D9 Present function assumed offset:
        ///  GGXXACPR_Win+710580[0][0][17]
        /// </summary>
        /// <returns></returns>
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe nint GetD3D9PresentFuncPtr()
        {
            var mainModule = Process.GetCurrentProcess().MainModule;
            if (mainModule == null) { return nint.Zero; }

            nint baseAddr = mainModule.BaseAddress;

            // GGXXACPR ptr to obj containing D3D9 VTable
            void* unsafePtr = (void*)(baseAddr + 0x710580);
            if (((nint*)unsafePtr)[0] == nint.Zero)
            {
                return nint.Zero;
            }
            if (((nint**)unsafePtr)[0][0] == nint.Zero)
            {
                return nint.Zero;
            }
            return ((nint***)unsafePtr)[0][0][17];
        }

        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe bool InstallGraphicsHooks()
        {
            Console.WriteLine("Begin Graphics Hook Installation");

            nint targetAddress = GetD3D9PresentFuncPtr();
            Console.WriteLine($"D3D9 Present function ptr: 0x{targetAddress:X8}");
            if (targetAddress == nint.Zero) { return false; }

            // VirtualProtect first couple instruction at target function to execute/read/write
            bool success = PInvoke.VirtualProtect((void*)targetAddress, 1024, PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE, out PAGE_PROTECTION_FLAGS prevFlags);
            if (!success)
            {
                Console.WriteLine($"VirtualProtect failed: {Marshal.GetLastSystemError()}");
                return false;
            }
            Console.WriteLine("VirtualProtect executed");

            // Record first couple instructions
            byte[] instructions = new byte[5];
            Marshal.Copy(targetAddress, instructions, 0, 5);

            // Overwrite recorded instructions with jump to hook function.

            //nint hookAddr = GraphicsTrampoline == null ? nint.Zero : Marshal.GetFunctionPointerForDelegate<D3D9PresentTrampoline>(GraphicsTrampoline);
            nint hookAddr = (nint)(delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int>)&GraphicsHook;
            byte[] relAddrBytes = BitConverter.GetBytes((int)(hookAddr - (targetAddress + 5)));
            Console.WriteLine($"Relative jmp offset: 0x{hookAddr - (targetAddress + 5):X8}");
            byte[] asm = [RELATIVE_JMP_OP_CODE, .. relAddrBytes];
            Marshal.Copy(asm, 0, targetAddress, asm.Length);
            Console.WriteLine($"Hook Written at 0x{targetAddress:X8}");

            // Generate trampoline function
            WriteTrampoline(instructions, targetAddress);
            Console.WriteLine("Trampoline written");

            // Reverting VirtualProtect
            success = PInvoke.VirtualProtect((void*)targetAddress, 1024, prevFlags, out _);
            if (!success)
            {
                Console.WriteLine($"VirtualProtect revert failed: {Marshal.GetLastSystemError()}");
                return false;
            }
            Console.WriteLine("VirtualProtect reverted");

            Console.WriteLine("GraphicsHook Successfully installed");
            return true;
        }


        /// <summary>
        /// Gotta be extra careful with the contents of stolenInstructions.
        ///  Any relative addresses will break in the trampoline function.
        /// </summary>
        /// <param name="stolenInstructions">Machine code overwritten by detour</param>
        /// <param name="returnAddress">Address to return to</param>
        /// <returns>Memory address of the Trampoline function</returns>
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static unsafe nint WriteTrampoline(Span<byte> stolenInstructions, nint returnAddress)
        {
            // Allocate memory for trampoline
            nint allocAddr = Marshal.AllocHGlobal(1024);
            nint trampolineAddr = allocAddr + 0x100;
            Console.WriteLine($"Trampoline allocated at 0x{trampolineAddr:X8}");
            // Compute relative jump offset
            byte[] relAddrBytes = BitConverter.GetBytes(trampolineAddr - (returnAddress + 5));

            byte[] adjustedStolenInstructions = [.. stolenInstructions];
            // Make adjustments to stolenInstructions in case of stolen relative jmp instruction
            if (adjustedStolenInstructions[0] == RELATIVE_JMP_OP_CODE)
            {
                var relJmpOffset = BitConverter.ToInt32(adjustedStolenInstructions, 1);
                var targetAddress = relJmpOffset + (returnAddress + 5);
                Console.WriteLine($"Original targetAddress: 0x{targetAddress:X8}");
                int newRelJmpOffset = (int)(relJmpOffset + returnAddress - trampolineAddr);
                Console.WriteLine($"Trampoline newRelJmpOffset: 0x{newRelJmpOffset:X8}");
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

        private static int counter = 0;
        /// <summary>
        /// *IMPORTANT* Make sure hook method signature is identical to D3D9::Present
        /// </summary>
        /// <param name="pSourceRect"></param>
        /// <param name="pDestRect"></param>
        /// <param name="hDestWindowOverride"></param>
        /// <param name="pDirtyRegion"></param>
        /// <returns>HRESULT</returns>
        /// <exception cref="NullReferenceException"></exception>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)], EntryPoint = "GraphicsHook")]
        private static unsafe int GraphicsHook(void* d3d9Device, void* pSourceRect, void* pDestRect, void* hDestWindowOverride, void* pDirtyRegion)
        {
            counter++;
            if (counter >= 6000) { counter = 0; }
            if (counter % 60 == 0)
            {
                Console.WriteLine($"GRAPHICS HOOK CALLED. Frame Counter: {counter}");
            }

            Graphics.MarshalDevice((nint)d3d9Device);
            Graphics.RenderOverlayFrame();

            if (GraphicsTrampoline == null)
            {
                throw new NullReferenceException("Trampoline function pointer is undefined.");
            }

            return GraphicsTrampoline(d3d9Device, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
        }
    }
}
