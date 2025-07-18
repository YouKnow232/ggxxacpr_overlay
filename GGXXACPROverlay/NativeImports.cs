using System.Runtime.InteropServices;

namespace GGXXACPROverlay
{
    // TODO: Cleanup NativeMethods.txt of unused imports

    /// <summary>
    /// Owns LibraryImports that must be manually imported to get around some CsWin32 limitations.
    /// </summary>
    internal static unsafe partial class NativeImports
    {
        [LibraryImport("kernel32.dll")]
        internal static partial nint AddVectoredExceptionHandler(uint First, VectoredExceptionHandler handler);
        [LibraryImport("kernel32.dll")]
        internal static partial uint RemoveVectoredExceptionHandler(nint handle);
    }

    [Flags]
    internal enum ExceptionContinue
    {
        EXCEPTION_CONTINUE_SEARCH = 0,
        EXCEPTION_CONTINUE_EXECUTION = -1,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct EXCEPTION_POINTERS
    {
        public nint ExceptionRecord;
        public nint ContextRecord;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal unsafe delegate ExceptionContinue VectoredExceptionHandler(EXCEPTION_POINTERS* exceptionInfo);
}
