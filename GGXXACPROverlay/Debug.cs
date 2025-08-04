using System.Reflection.Metadata.Ecma335;

namespace GGXXACPROverlay
{
    internal static class Debug
    {
        private delegate void WriteLine(object? message);
        /// <summary>
        /// Prints a message to the console if debug statements are on, else does nothing.
        /// </summary>
        private static WriteLine DebugBehavior = DebugOn;

        /// <summary>
        /// Prints a message to the console if debug statements are on, else does nothing.
        /// </summary>
        public static void Log(object? message) => DebugBehavior(message);
        public static void Log() => DebugBehavior("");

        private static void DebugOn(object? message)
        {
            Console.WriteLine("[GGXXACPROverlay] " + message?.ToString());
        }
        private static void DebugOff(object? _)
        {
            // Intentionally blank
        }

        /// <summary>
        /// Controls Debug.Log behavior. Debug.Log will print messages if true.
        /// </summary>
        public static bool DebugStatements { get => DebugBehavior == DebugOn; set => DebugBehavior = (value ? DebugOn : DebugOff); }


        private static readonly long[] _buffer = new long[240];
        private static int _bufferIndex = 0;
        public static void LogDatumForAverage(long data)
        {
            if (_bufferIndex >= _buffer.Length) return;

            _buffer[_bufferIndex++] = data;
        }
        public static void ReportAverage(string msg)
        {
            double avg = _bufferIndex > 0 ? _buffer[.._bufferIndex].Average() : 0;
            Console.WriteLine(msg, avg);

            _bufferIndex = 0;
            Array.Clear(_buffer);
        }
    }
}
