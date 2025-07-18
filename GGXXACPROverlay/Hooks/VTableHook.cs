using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;
using Windows.Win32;

namespace GGXXACPROverlay.Hooks
{
    internal unsafe class VTableHook : DisposableHook
    {
        private const int WORKING_MEMORY_RANGE = 0x20;

        private bool _isInstalled = false;
        public override bool IsInstalled => _isInstalled;

        private readonly nint _targetVTableEntry;
        private readonly Action<IDirect3DDevice9> _hookBodyDelegate;
        private readonly GCHandle _hookDelegateHandle;
        private readonly nint _nativeHookPtr;

        private readonly Range _workingMemoryRegion;
        private nint _originalVTableFunctionPtr;
        private delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*,int> _originalVTableDelegate;

        private nint* _targetVTableEntryPtr;

        public VTableHook(nint targetVTableEntry, Action<IDirect3DDevice9> hookBodyDelegate)
        {
            _targetVTableEntry = targetVTableEntry;
            _targetVTableEntryPtr = (nint*)targetVTableEntry;
            _hookBodyDelegate = hookBodyDelegate ?? throw new ArgumentNullException(nameof(hookBodyDelegate));
            D3D9Present graphicsHookDelegate = GraphicsHook;
            _hookDelegateHandle = GCHandle.Alloc(graphicsHookDelegate);
            _nativeHookPtr = Marshal.GetFunctionPointerForDelegate(graphicsHookDelegate);
            _workingMemoryRegion =
                (int)(GGXXACPR.Offsets.PRESENT_CALL_INSTRUCTION - WORKING_MEMORY_RANGE)..
                ((int)GGXXACPR.Offsets.PRESENT_CALL_INSTRUCTION + WORKING_MEMORY_RANGE);
        }

        public override void Install()
        {
            if (_isInstalled) throw new InvalidOperationException($"Hook already installed: {this}");

            Debug.Log($"Installing hook at address: 0x{_targetVTableEntry:X8}");

            _originalVTableFunctionPtr = *_targetVTableEntryPtr;
            _originalVTableDelegate = (delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int>)*_targetVTableEntryPtr;

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            *_targetVTableEntryPtr = _nativeHookPtr;

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");

            _isInstalled = true;
        }

        public override void Uninstall()
        {
            if (!_isInstalled) throw new InvalidOperationException($"Attempted to uninstall hook that wasn't installed: {this}");

            Debug.Log($"Uninstalling VTable hook at address: 0x{_targetVTableEntry:X8}");

            using SafeHandle hMainThread = Util.SafelyPauseMainThread(_workingMemoryRegion);

            *_targetVTableEntryPtr = _originalVTableFunctionPtr;

            uint suspendCount = PInvoke.ResumeThread(hMainThread);
            if (suspendCount == uint.MaxValue) throw new COMException("Failed to resume main thread", Marshal.GetLastSystemError());
            if (suspendCount != 1) Debug.Log($"Suspend count expected to be 1 but was {suspendCount}");
            _isInstalled = false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        // [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private int GraphicsHook(void* d3d9Device, void* pSourceRect, void* pDestRect, void* hDestWindowOverride, void* pDirtyRegion)
        {
            try
            {
                _hookBodyDelegate(new IDirect3DDevice9((nint)d3d9Device));
            }
            catch (Exception e)
            {
                Debug.Log($"[Error] Unhandled exception thrown in graphics hook: {e}");
            }

            if (_originalVTableDelegate is null)
            {
                Debug.Log("Graphics Hook was called before graphics trampoline was initailzied!");
                return 0;
            }

            return _originalVTableDelegate(d3d9Device, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
        }


        ~VTableHook()
        {
            if (IsInstalled)
            {
                Dispose(false);
            }
        }
    }
}
