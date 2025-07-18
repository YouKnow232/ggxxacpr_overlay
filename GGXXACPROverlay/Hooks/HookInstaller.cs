using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GGXXACPROverlay.Hooks
{
    public static unsafe class HookInstaller
    {
        private static DetourTrampolineHook? _graphicsHook;
        private static GCHandle _graphicsHookGCHandle;

        public static void InstallHooks()
        {
            if (Settings.Get("Misc", "BlackBackground", false))
            {
                RenderThreadTaskQueue.Enqueue((_) =>
                {
                    GGXXACPR.Hacks.LockBackgroundState(
                        GGXXACPR.BackgroundState.BlackBackground | GGXXACPR.BackgroundState.HudOff);
                });
            }
                
            RenderThreadTaskQueue.Enqueue(D3D9PresentHookSetup);
            _graphicsHook = new DetourTrampolineHook(GetD3D9PresentFuncPtr(), D3D9PresentHook);
            _graphicsHookGCHandle = GCHandle.Alloc(_graphicsHook);

            _graphicsHook.Install();
        }
        public static void UninstallHooks()
        {
            _graphicsHook?.Uninstall();
            _graphicsHookGCHandle.Free();
        }

        private static void D3D9PresentHookSetup(nint device)
        {
            Thread.BeginThreadAffinity();

            if (device == 0)
                throw new InvalidOperationException("Graphics init task was called with an invalid device pointer!");

            try
            {
                _ = new Overlay(new Graphics(device));

                // TEMP DEBUG
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
            }
            catch (Exception e)
            {
                Debug.Log($"[ERROR] Caught unhandled exception before it bubbled into native code: {e}");
            }
        }
        private static void D3D9PresentHook(nint device)
        {
            try
            {
                if (device == 0)
                {
                    Debug.Log("[ERROR] Present graphics hook was called with an invalid device pointer");
                    return;
                }

                RenderThreadTaskQueue.ExecutePending(device);
                Overlay.Instance?.RenderFrame(device);
            }
            catch (Exception e)
            {
                Debug.Log($"[ERROR] Caught unhandled exception before it bubbled into native code: {e}");
            }
        }

        /// <summary>
        /// D3D9 Present function assumed offset:
        ///  GGXXACPR_Win+710580[0][0][17]
        /// </summary>
        /// <returns></returns>
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static nint GetD3D9PresentFuncPtr()
        {
            void* unsafePtr = (void*)GGXXACPR.GGXXACPR.Direct3D9DevicePointer;
            if (((nint*)unsafePtr)[0] == nint.Zero)
            {
                return nint.Zero;
            }

            return ((nint**)unsafePtr)[0][17];
        }
    }
}
