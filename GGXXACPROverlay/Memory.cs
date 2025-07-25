using System.Diagnostics;

namespace GGXXACPROverlay
{
    internal static class Memory
    {
        public static Process Process { get; private set; }
        public static nint BaseAddress { get; private set; }
        public static ProcessThread MainThread { get; private set; }


        static Memory()
        {
            Process = Process.GetCurrentProcess();
            BaseAddress = Process.MainModule?.BaseAddress ?? 0;
            if (BaseAddress == 0) Debug.Log("BaseAddress is 0!");

            MainThread = Process.Threads[0];
        }
    }
}
