using System.Runtime.InteropServices;
using Windows.Win32;

namespace GGXXACPROverlay.Hooks
{
    internal class CustomPtrToEdxHook : DisposableHook
    {
        private const int LEA_INSTRUCTION_SIZE = 6;
        private const int WORKING_MEMORY_RANGE = 64;
        private const byte NOP = 0x90;

        private bool _isInstalled = false;
        public override bool IsInstalled => _isInstalled;

        private readonly nint _targetAddress;
        private readonly int _overwriteSize;
        private readonly nint _hookPtr;

        private byte[] _originalBytes = [];

        private readonly Range _workingMemoryRegion;

        public CustomPtrToEdxHook(nint startAddress, int instructionOverwriteSize, nint hookPtr)
        {
            if (instructionOverwriteSize < LEA_INSTRUCTION_SIZE)
                throw new ArgumentException("Not enough space for the hook. instructionOverwriteSize must be at least 6");

            _targetAddress = startAddress;
            _overwriteSize = instructionOverwriteSize;
            _hookPtr = hookPtr;
            _workingMemoryRegion =
                ((int)startAddress - WORKING_MEMORY_RANGE)..
                ((int)startAddress + instructionOverwriteSize);
        }

        public override void Install()
        {
            if (_isInstalled) throw new InvalidOperationException($"Hook already installed: {this}");

            Debug.Log($"Installing call replacement hook at address: 0x{_targetAddress:X8}");

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            byte[] hookBytes = BitConverter.GetBytes((int)_hookPtr);
            byte[] nopFill = new byte[_overwriteSize - LEA_INSTRUCTION_SIZE];
            Array.Fill(nopFill, NOP);

            byte[] customAsm = [
                0x8D, 0x15, ..hookBytes,    // lea eax, [hookPtr]
                ..nopFill];
            _originalBytes = Util.Patch(_targetAddress, customAsm);

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");
            _isInstalled = true;
        }

        public override void Uninstall()
        {
            if (!_isInstalled) throw new InvalidOperationException($"Attempted to uninstall hook that wasn't installed: {this}");

            Debug.Log($"Uninstalling call replacement hook at address: 0x{_targetAddress:X8}");

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            _ = Util.Patch(_targetAddress, _originalBytes);

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");
            _isInstalled = false;
        }

        ~CustomPtrToEdxHook()
        {
            if (IsInstalled)
            {
                Dispose(false);
            }
        }
    }
}
