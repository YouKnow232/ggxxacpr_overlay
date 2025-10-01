using System.Runtime.InteropServices;
using Windows.Win32;

namespace GGXXACPROverlay.Hooks
{
    internal class DetourHook : DisposableHook
    {
        protected bool _isInstalled = false;
        public override bool IsInstalled => _isInstalled;

        protected readonly nint _hookAddress;
        protected readonly nint _targetAddress;
        protected readonly Range _workingMemoryRegion;

        protected byte[] _originalBytes = [];

        public DetourHook(nint hookAddress, nint targetAddress, int memoryRange = 64)
        {
            _hookAddress = hookAddress;
            _targetAddress = targetAddress;
            _workingMemoryRegion =
                ((int)targetAddress - memoryRange)..((int)targetAddress + Util.Asm.RELATIVE_JUMP_INSTRUCTION_SIZE);
        }

        public override void Install()
        {
            if (_isInstalled) throw new InvalidOperationException($"Hook already installed: {this}");

            Debug.Log($"Installing hook at address: 0x{_targetAddress:X8}");

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            _originalBytes = new byte[5];
            Marshal.Copy(_targetAddress, _originalBytes, 0, 5);

            nint trampolineAddress = Util.WriteToFromCallTrampoline(_hookAddress, _targetAddress, _originalBytes, out var _);

            nint relativeJmpToTrampoline = Util.Asm.CalculateRelativeOffset(_targetAddress, trampolineAddress);
            byte[] _payload =
            [
                Util.Asm.JUMP, ..BitConverter.GetBytes((int)relativeJmpToTrampoline),
            ];

            _ = Util.Patch(_targetAddress, _payload);

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");
            _isInstalled = true;
        }

        public override void Uninstall()
        {
            if (!_isInstalled) throw new InvalidOperationException($"Attempted to uninstall hook that wasn't installed: {this}");

            Debug.Log($"Uninstalling hook at address: 0x{_targetAddress:X8}");

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            _ = Util.Patch(_targetAddress, _originalBytes);

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");
            _isInstalled = false;
        }

        ~DetourHook()
        {
            if (IsInstalled)
            {
                Dispose(false);
            }
        }
    }
}
