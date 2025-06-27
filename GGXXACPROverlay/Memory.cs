using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GGXXACPROverlay
{
    internal static class Memory
    {

        public static Process Process;
        public static nint BaseAddress;

        static Memory()
        {
            Process = Process.GetCurrentProcess();
            BaseAddress = Process.MainModule?.BaseAddress ?? 0;
            if (BaseAddress == 0) { Debug.Log("BaseAddress is 0!"); }
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
