using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace GGXXACPROverlay.Hooks
{
    // TODO: generalize this delegate entry point

    internal unsafe class GraphicsDetourHook : DisposableHook
    {
        private const int PATCH_SIZE = 5;

        public override bool IsInstalled => _isInstalled;
        private bool _isInstalled = false;

        private readonly nint _targetAddress;
        private readonly Action<nint> _hookBodyDelegate;
        private readonly GCHandle _hookBodyDelegateHandle;
        private readonly GCHandle _hookDelegateHandle;
        private readonly nint _nativeHookPtr;

        private readonly Range _workingMemoryRegion;
        private readonly byte[] _originalBytes;
        private nint _trampolineAddress;
        private nuint _trampolineSize;


        public GraphicsDetourHook(nint targetAddress, Action<nint> hookBodyDelegate)
        {
            _targetAddress = targetAddress;
            _hookBodyDelegate = hookBodyDelegate ?? throw new ArgumentNullException(nameof(hookBodyDelegate));
            _hookBodyDelegateHandle = GCHandle.Alloc(_hookBodyDelegate);
            D3D9Present graphicsHookDelegate = GraphicsHook;
            _hookDelegateHandle = GCHandle.Alloc(graphicsHookDelegate);
            _nativeHookPtr = Marshal.GetFunctionPointerForDelegate(graphicsHookDelegate);
            _workingMemoryRegion = (int)_targetAddress..((int)_targetAddress + PATCH_SIZE);
            _originalBytes = new byte[PATCH_SIZE];

        }

        public override void Install()
        {
            if (_isInstalled) throw new InvalidOperationException($"Hook already installed: {this}");

            Debug.Log($"Installing hook at address: 0x{_targetAddress:X8}");

            Marshal.Copy(_targetAddress, _originalBytes, 0, PATCH_SIZE);

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);
            _trampolineAddress = Util.WriteToFromCallTrampoline(_nativeHookPtr, _targetAddress + PATCH_SIZE, _originalBytes, out _trampolineSize);
            Debug.Log($"Trampoline function written at: 0x{_trampolineAddress:X8}");

            _ = Util.PatchHookDetour(_targetAddress, _trampolineAddress);
            Debug.Log($"Detour written at: 0x{_targetAddress:X8}");
            Debug.Log($"Graphics hook function ptr: 0x{_nativeHookPtr:X8}");

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            Debug.Log($"Thread suspend count: {suspendCount}");
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");

            _isInstalled = true;
        }

        public override void Uninstall()
        {
            if (!_isInstalled) throw new InvalidOperationException($"Attempted to uninstall hook that wasn't installed: {this}");

            Debug.Log($"Uninstalling hook at address: 0x{_targetAddress:X8}");

            using (SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion))
            {
                _ = Util.Patch(_targetAddress, _originalBytes);
                var suspendCount = PInvoke.ResumeThread(hMainThread);
                Debug.Log($"Thread suspend count: {suspendCount}");
            }

            PInvoke.VirtualFree((void*)_trampolineAddress, _trampolineSize, Windows.Win32.System.Memory.VIRTUAL_FREE_TYPE.MEM_RELEASE);

            _isInstalled = false;
        }

        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private int GraphicsHook(void* d3d9Device, void* pSourceRect, void* pDestRect, void* hDestWindowOverride, void* pDirtyRegion)
        {
            if (pSourceRect is not null || pDestRect is not null || hDestWindowOverride is not null || pDirtyRegion is not null)
            {
                Debug.Log("[DEBUG] Unexpected parameters passed to Hook!");
                Debug.Log($"[DEBUG] pSourceRect:0x{(nint)pSourceRect:X8}, pDestRect:0x{(nint)pDestRect:X8}");
                Debug.Log($"[DEBUG] hDestWindowOverride:0x{(nint)hDestWindowOverride:X8}, pDirtyRegion:0x{(nint)pDirtyRegion:X8}");
            }

            try
            {
                _hookBodyDelegate((nint)d3d9Device);
            }
            catch (Exception e)
            {
                Debug.Log($"[Error] Unhandled exception thrown in graphics hook: {e}");
            }

            return 0;
        }

        private bool _disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            Debug.Log("Hook Disposing");

            if (disposing)
            {
                // dispose managed
                _hookBodyDelegateHandle.Free();
                _hookDelegateHandle.Free();
            }
            // dispose unmanaged
            base.Dispose(disposing);
            _disposed = true;
        }

        ~GraphicsDetourHook()
        {
            Debug.Log("Hook instance finalized");
            if (IsInstalled)
            {
                Dispose(false);
            }
        }
    }
}
