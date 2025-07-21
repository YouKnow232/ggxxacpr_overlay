using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GGXXACPROverlay.GGXXACPR;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace GGXXACPROverlay.Hooks
{
    public static unsafe class HookInstaller
    {
        private static GraphicsDetourHook? _graphicsHook;
        private static FunctionWrapperHook? _peekMessageHook;
        private static GCHandle _d3d9PresentHookBodyGCHandle;

        public static void InstallHooks()
        {
            if (Settings.Get("Misc", "BlackBackground", false))
            {
                RenderThreadTaskQueue.Enqueue((_) =>
                {
                    Hacks.LockBackgroundState(
                        BackgroundState.BlackBackground | BackgroundState.HudOff);
                });
            }
                
            RenderThreadTaskQueue.Enqueue(D3D9PresentHookSetup);
            var _d3d9PresentHookBody = D3D9PresentHook;
            _d3d9PresentHookBodyGCHandle = GCHandle.Alloc(_d3d9PresentHookBody);
            _graphicsHook = new GraphicsDetourHook(GetD3D9PresentFuncPtr(), _d3d9PresentHookBody);

            Debug.Log("Installing Direct3D9.Present hook..");
            _graphicsHook.Install();


            nint peekMessageWrapperPtr = (nint)(delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, int>)&PeekMessageWrapper;
            Debug.Log($"peekMessageWrapper hook function pointer: 0x{peekMessageWrapperPtr:X8}");
            _peekMessageHook = new FunctionWrapperHook(Memory.BaseAddress + Offsets.PEEK_MESSAGE_FUNCTION_POINTER, peekMessageWrapperPtr);

            Debug.Log("Installing PeekMessageW wrapper hook..");
            _peekMessageHook.Install();

            RenderThreadTaskQueue.Enqueue(UpdateMessageLoopJmp);

            Thread.Sleep(500);

            RenderThreadTaskQueue.Enqueue(RevertMessageLoopJmp);
        }
        public static void UninstallHooks()
        {
            _graphicsHook?.Uninstall();
            _peekMessageHook?.Uninstall();
            _d3d9PresentHookBodyGCHandle.Free();
        }

        private static void D3D9PresentHookSetup(nint device)
        {
            Thread.BeginThreadAffinity();

            if (device == 0)
                throw new InvalidOperationException("Graphics init task was called with an invalid device pointer!");

            try
            {
                _ = new Overlay(new Graphics(device));
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

        private const uint WM_KEYDOWN = 0x0100;
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int PeekMessageWrapper(void* lpMsg, void* hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg)
        {
            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            MSG* msg = (MSG*)lpMsg;
            BOOL success = PInvoke.PeekMessage(msg, new HWND(hWnd), wMsgFilterMin, wMsgFilterMax, (PEEK_MESSAGE_REMOVE_TYPE)wRemoveMsg);

            if (success && msg is not null && msg->message == WM_KEYDOWN &&
                 Enum.IsDefined(typeof(VirtualKeyCodes), (int)msg->wParam.Value))
            {
                Input.HandleKeyDownEvent(msg->lParam.Value, (VirtualKeyCodes)(int)msg->wParam.Value);
            }

            return success.Value;
        }

        private static byte _originalJmpOffsetByte;
        /// <summary>
        /// Adjusts a relative jmp offset in the message loop to update api function pointers
        /// </summary>
        /// <param name="unused">Discards unused device pointer</param>
        private static void UpdateMessageLoopJmp(nint unused)
        {
            Debug.Log("Patching message loop to update function pointers");
            _originalJmpOffsetByte = Util.Patch(Memory.BaseAddress + Offsets.MESSAGE_LOOP_REL_JMP_OFFSET_BYTE_ADDR, [0xB8])[0];
        }

        /// <summary>
        /// Reverts the relative jmp change made by <c>UpdateMessageLoopJmp</c>.
        /// </summary>
        /// <param name="unused">Discards unused device pointer</param>
        private static void RevertMessageLoopJmp(nint unused)
        {
            Debug.Log("Reverting message loop patch");
            Util.Patch(Memory.BaseAddress + Offsets.MESSAGE_LOOP_REL_JMP_OFFSET_BYTE_ADDR, [_originalJmpOffsetByte]);
        }
    }
}
