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

        public static unsafe void ToggleBlackBG()
        {
            if (_lockBackgroundState.Length == 0)
            {
                BackgroundState blackNoHud = BackgroundState.BlackBackground | BackgroundState.HudOff;

                LockBackgroundState(blackNoHud);
                *GGXXACPR.BackgroundState = (int)blackNoHud;
            }
            else
            {
                RevertLockBackgroundState();
            }
        }
        public static void LockBackgroundState(BackgroundState state)
        {
            byte[] newAsm = [
                MOV_ECX_OP_CODE, ..BitConverter.GetBytes((int)state),
                NOP];

            _lockBackgroundState = OverwriteExecutableMemory(
                Memory.BaseAddress + Offsets.FIX_BACKGROUND_STATE_INSTRUCTION,
                newAsm);
        }

        private static byte[] _lockBackgroundState = [];
        public static unsafe void RevertLockBackgroundState()
        {
            if (_lockBackgroundState.Length == 0) return;

            OverwriteExecutableMemory(
                Memory.BaseAddress + Offsets.FIX_BACKGROUND_STATE_INSTRUCTION,
                _lockBackgroundState
                );

            _lockBackgroundState = [];
            *GGXXACPR.BackgroundState = (int)BackgroundState.Default;
        }

        public static unsafe void TogglePauseNoMenu()
        {
            // TODO: check if training mode

            int* state = GGXXACPR.TrainingPauseState;
            int* display = GGXXACPR.TrainingPauseDisplay;

            if (*state == 2)
            {
                *display = 1;
                *state = 0;
            }
            else if (*state == 0)
            {
                *display = 0;
                *state = 2;
            }
        }

        public static unsafe void FrameStepFromPause()
        {
            // TODO: check if training mode

            int* state = GGXXACPR.TrainingPauseState;
            int* display = GGXXACPR.TrainingPauseDisplay;

            if (*state == 2 && *display == 0)
            {
                *state = 0;
                RenderThreadTaskQueue.Enqueue(Repause);
            }
        }

        private static unsafe void Repause(nint unused) => *GGXXACPR.TrainingPauseState = 2;


        /// <summary>
        /// Assumes <c>address</c> is not currently being executed or maybe executed while this function is running.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="newMemory"></param>
        /// <returns></returns>
        private static unsafe byte[] OverwriteExecutableMemory(nint address, byte[] newMemory)
        {
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
