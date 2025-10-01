using System.Runtime.InteropServices;
using Windows.Win32;

namespace GGXXACPROverlay.Hooks
{
    internal class GenericPatchHook : DisposableHook
    {
        protected bool _isInstalled = false;
        public override bool IsInstalled => _isInstalled;

        protected readonly byte[] _payload;
        protected readonly nint _targetAddress;
        protected readonly Range _workingMemoryRegion;

        protected byte[] _originalBytes = [];

        public GenericPatchHook(byte[] payload, nint targetAddress, int workingMemoryRange = 64)
        {
            _payload = payload;
            _targetAddress = targetAddress;
            _workingMemoryRegion =
                ((int)targetAddress - workingMemoryRange)..((int)targetAddress + payload.Length);
        }

        public override void Install()
        {
            if (_isInstalled) throw new InvalidOperationException($"Hook already installed: {this}");

            Debug.Log($"Installing hook at address: 0x{_targetAddress:X8}");

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            _originalBytes = Util.Patch(_targetAddress, _payload);

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

        ~GenericPatchHook()
        {
            if (IsInstalled)
            {
                Dispose(false);
            }
        }
    }
}
