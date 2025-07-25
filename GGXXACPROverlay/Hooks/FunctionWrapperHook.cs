using System.Runtime.InteropServices;
using Windows.Win32;

namespace GGXXACPROverlay.Hooks
{
    internal unsafe class FunctionWrapperHook : DisposableHook
    {
        public override bool IsInstalled => _isInstalled;
        private bool _isInstalled = false;

        private readonly nint _targetFunctionPtrAddress;
        private readonly nint _nativeHookPtr;

        private nint _originalFunctionPtr;
        private readonly Range _workingMemoryRegion;

        /// <summary>
        /// Replaces a function address ptr with the address of a wrapper function.
        /// </summary>
        /// <param name="originalFunctionPtrAddress">Address of the function pointer variable to rewrite</param>
        /// <param name="nativeHookPtr">Address of the new wrapper function</param>
        public FunctionWrapperHook(nint originalFunctionPtrAddress, nint nativeHookPtr)
        {
            _targetFunctionPtrAddress = originalFunctionPtrAddress;
            _nativeHookPtr = nativeHookPtr;
            _workingMemoryRegion =
                (int)originalFunctionPtrAddress..
                ((int)originalFunctionPtrAddress + sizeof(int));
        }

        public override void Install()
        {
            if (_isInstalled) throw new InvalidOperationException($"Hook already installed: {this}");

            Debug.Log($"Installing hook at address: 0x{_targetFunctionPtrAddress:X8}");

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            byte[] overwritten = Util.Patch(_targetFunctionPtrAddress, BitConverter.GetBytes(_nativeHookPtr));
            _originalFunctionPtr = (nint)BitConverter.ToUInt32(overwritten);

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");

            _isInstalled = true;
        }

        public override void Uninstall()
        {
            if (!_isInstalled) throw new InvalidOperationException($"Attempted to uninstall hook that wasn't installed: {this}");

            Debug.Log($"Uninstalling VTable hook at address: 0x{_targetFunctionPtrAddress:X8}");

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            _ = Util.Patch(_targetFunctionPtrAddress, BitConverter.GetBytes(_originalFunctionPtr));

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");
            _isInstalled = false;
        }


        ~FunctionWrapperHook()
        {
            if (IsInstalled)
            {
                Dispose(false);
            }
        }
    }
}
