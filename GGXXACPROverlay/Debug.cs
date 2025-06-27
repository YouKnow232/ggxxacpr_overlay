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
        private static void DebugOff(object? _) { }

        /// <summary>
        /// Controls Debug.Log behavior. Debug.Log will print messages if true.
        /// </summary>
        public static bool DebugStatements { get => DebugBehavior == DebugOn; set => DebugBehavior = value ? DebugOn : DebugOff; }
    }
}
