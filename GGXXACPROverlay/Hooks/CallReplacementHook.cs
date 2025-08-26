namespace GGXXACPROverlay.Hooks
{
    internal class CallReplacementHook : GenericPatchHook
    {
        private const int CALL_INSTRUCTION_SIZE = 5;

        public CallReplacementHook(nint callInstructionAddress, nint hookAddress)
            : base(CreatePayload(callInstructionAddress, hookAddress), callInstructionAddress + 1)
        { }
        
        private static byte[] CreatePayload(nint callInstructionAddress, nint hookAddress)
        {
            return BitConverter.GetBytes((int)hookAddress - ((int)callInstructionAddress + CALL_INSTRUCTION_SIZE));
        }
        
        //private const int WORKING_MEMORY_RANGE = 64;

        //private bool _isInstalled = false;
        //public override bool IsInstalled => _isInstalled;

        //private readonly nint _callInstructionAddress;
        //private readonly nint _hookPtr;

        //private byte[] _originalBytes = [];

        //private readonly Range _workingMemoryRegion;

        //public CallReplacementHook(nint callInstructionAddress, nint hookPtr)
        //{
        //    _callInstructionAddress = callInstructionAddress;
        //    _hookPtr = hookPtr;
        //    _workingMemoryRegion =
        //        ((int)callInstructionAddress - WORKING_MEMORY_RANGE)..
        //        ((int)callInstructionAddress + CALL_INSTRUCTION_SIZE);
        //}

        //public override void Install()
        //{
        //    if (_isInstalled) throw new InvalidOperationException($"Hook already installed: {this}");

        //    Debug.Log($"Installing call replacement hook at address: 0x{_callInstructionAddress:X8}");

        //    using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

        //    int callOffset = (int)_hookPtr - ((int)_callInstructionAddress + CALL_INSTRUCTION_SIZE);
        //    _originalBytes = Util.Patch(_callInstructionAddress + 1, BitConverter.GetBytes(callOffset));

        //    uint suspendCount = PInvoke.ResumeThread(hMainThread);
        //    if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
        //    if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");
        //    _isInstalled = true;
        //}

        //public override void Uninstall()
        //{
        //    if (!_isInstalled) throw new InvalidOperationException($"Attempted to uninstall hook that wasn't installed: {this}");

        //    Debug.Log($"Uninstalling call replacement hook at address: 0x{_callInstructionAddress:X8}");

        //    using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

        //    _ = Util.Patch(_callInstructionAddress + 1, _originalBytes);

        //    uint suspendCount = PInvoke.ResumeThread(hMainThread);
        //    if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
        //    if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");
        //    _isInstalled = false;
        //}

        //~CallReplacementHook()
        //{
        //    if (IsInstalled)
        //    {
        //        Dispose(false);
        //    }
        //}
    }
}
