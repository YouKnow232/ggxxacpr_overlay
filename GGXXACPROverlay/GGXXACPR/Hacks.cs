using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Memory;

namespace GGXXACPROverlay.GGXXACPR
{
    /// <summary>
    /// Contains methods for altering the game code to achieve certain behaviors.
    /// </summary>
    internal static class Hacks
    {
        private const byte MOV_ECX_OP_CODE = 0xB9;
        private const byte NOP = 0x90;

        public static void LockBackgroundState(BackgroundState state)
        {
            byte[] newAsm = [
                MOV_ECX_OP_CODE, ..BitConverter.GetBytes((int)state),
                NOP];

            _lockBackgroundState = OverwriteExecutableMemory(
                Memory.BaseAddress + Offsets.FIX_BACKGROUND_STATE_INSTRUCTION,
                newAsm);

            Debug.Log($"Background state locked to: 0x{(int)state:X8}");
        }

        private static byte[] _lockBackgroundState = [];
        public static void RevertLockBackgroundState()
        {
            if (_lockBackgroundState.Length == 0) return;

            OverwriteExecutableMemory(
                Memory.BaseAddress + Offsets.FIX_BACKGROUND_STATE_INSTRUCTION,
                _lockBackgroundState
                );
        }

        /// <summary>
        /// Assumes <c>address</c> is not currently being executed or maybe executed while this function is running.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="newMemory"></param>
        /// <returns></returns>
        private static unsafe byte[] OverwriteExecutableMemory(nint address, byte[] newMemory)
        {
            // TEMP
            Debug.Log($"New Memory to write: {BitConverter.ToString(newMemory)}");
            uint targetMemory = *(uint*)address;
            Debug.Log($"Memory at 0x{address:X8}: 0x{targetMemory:X8}");

            byte[] output = new byte[newMemory.Length];
            Marshal.Copy(address, output, 0, newMemory.Length);

            bool success = PInvoke.VirtualProtect(
                (void*)address,
                (nuint)newMemory.Length,
                PAGE_PROTECTION_FLAGS.PAGE_EXECUTE_READWRITE,
                out PAGE_PROTECTION_FLAGS oldProtect);
            if (!success)
            {
                Debug.Log($"VirtualProtect failed: {Marshal.GetLastSystemError()}");
            }

            Marshal.Copy(newMemory, 0, address, newMemory.Length);

            // TEMP
            targetMemory = *(uint*)address;
            Debug.Log($"Memory at 0x{address:X8}: 0x{targetMemory:X8}");

            success = PInvoke.VirtualProtect(
                (void*)address,
                (nuint)newMemory.Length,
                oldProtect,
                out _);
            if (!success)
            {
                Debug.Log($"VirtualProtect failed: {Marshal.GetLastSystemError()}");
            }

            return output;
        }
    }
}
