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
        //private static CustomPtrToEdxHook? _graphicsHookTest;
        //private static GenericPatchHook? _UpdateGameStateHook;
        private static DetourHook? _GameStateHook;
        private static GenericPatchHook? _peekMessageHook;

        public static void InstallHooks()
        {
            if (Settings.Get("Misc", "BlackBackground", false))
            {
                TaskQueues.RenderThreadTaskQueue.Enqueue((unused) =>
                {
                    Hacks.LockBackgroundState(
                        BackgroundState.BlackBackground | BackgroundState.HudOff);
                });
            }
            
            TaskQueues.RenderThreadTaskQueue.Enqueue(D3D9PresentHookSetup);

            nint delegatePointer = Marshal.GetFunctionPointerForDelegate(D3D9PresentWrapperHook);

            Debug.Log($"Patching Present wrapper call to 0x{delegatePointer:X8}");

            _graphicsHook = new GraphicsDetourHook(
                GetD3D9PresentFuncPtr(),
                D3D9PresentHook);

            //_graphicsHookTest = new CustomPtrToEdxHook(
            //    Memory.BaseAddress + Offsets.GET_PRESENT_FUNCTION_POINTER_INSTRUCTIONS,
            //    10,
            //    delegatePointer);

            Debug.Log("Installing Direct3D9.Present hook..");
            _graphicsHook.Install();
            //_graphicsHookTest.Install();


            nint ugsTargetAddress = Memory.BaseAddress + Offsets.UPDATE_GAME_STATE_RET_INSTRUCTION;
            //nint ugsCallRelOffset = Util.Asm.CalculateRelativeOffset(
            //    ugsTargetAddress,
            //    Marshal.GetFunctionPointerForDelegate(UpdateGameStateHook));

            //_UpdateGameStateHook = new GenericPatchHook(
            //    [Util.Asm.CALL, ..BitConverter.GetBytes((int)ugsCallRelOffset),
            //    Util.Asm.RET],
            //    ugsTargetAddress);

            //_UpdateGameStateHook.Install();

            _GameStateHook = new DetourHook(
                Marshal.GetFunctionPointerForDelegate(UpdateGameStateHook),
                ugsTargetAddress
            );

            _GameStateHook.Install();

            nint peekMessageWrapperPtr = (nint)(delegate* unmanaged[Stdcall]<MSG*, HWND, uint, uint, uint, int>)&PeekMessageWrapper;
            Debug.Log($"peekMessageWrapper hook function pointer: 0x{peekMessageWrapperPtr:X8}");
            _peekMessageHook = new GenericPatchHook(
                BitConverter.GetBytes((int)peekMessageWrapperPtr),
                Memory.BaseAddress + Offsets.PEEK_MESSAGE_FUNCTION_POINTER);

            Debug.Log("Installing PeekMessageW wrapper hook..");
            _peekMessageHook.Install();

            TaskQueues.RenderThreadTaskQueue.Enqueue(UpdateMessageLoopJmp);
            TaskQueues.PeekMessageTaskQueue.Enqueue(RevertMessageLoopJmp);
        }
        public static void UninstallHooks()
        {
            _graphicsHook?.Uninstall();
            //_graphicsHookTest?.Uninstall();
            //_UpdateGameStateHook?.Uninstall();
            _GameStateHook?.Uninstall();
            _peekMessageHook?.Uninstall();
        }

        private static void D3D9PresentHookSetup(nint device)
        {
            // TODO: remove this after testing
            // Thread.BeginThreadAffinity();

            if (device == 0)
                throw new InvalidOperationException("Graphics init task was called with an invalid device pointer!");

            try
            {
                var resources = new Rendering.GraphicsResources();
                var graphics = new Rendering.Graphics(device, resources);
                var frameMeter = new FrameMeter.FrameMeter();
                _ = new Overlay(graphics, resources, frameMeter);
            }
            catch (Exception e)
            {
                Debug.Log($"[ERROR] Caught unhandled exception before it bubbled into native code:\n{e}\n");
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

                TaskQueues.RenderThreadTaskQueue.ExecutePending(device);
                Overlay.Instance?.RenderFrame(device);
            }
            catch (Exception e)
            {
                Debug.Log($"[ERROR] Caught unhandled exception before it bubbled into native code:\n{e}\n");
            }
        }

        private static readonly delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int> _Present
            = (delegate* unmanaged[Stdcall]<void*, void*, void*, void*, void*, int>) GetD3D9PresentFuncPtr();
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static int D3D9PresentWrapperHook(void* d3d9Device, void* pSourceRect, void* pDestRect, void* hDestWindowOverride, void* pDirtyRegion)
        {
            nint device = GGXXACPR.GGXXACPR.Direct3D9DevicePointer;

            try
            {
                if (device == nint.Zero || pSourceRect is not null || pDestRect is not null || hDestWindowOverride is not null || pDirtyRegion is not null)
                {
                    Debug.Log("[DEBUG] Unexpected parameters passed to Hook!");
                    Debug.Log($"[DEBUG] pSourceRect:0x{(nint)pSourceRect:X8}, pDestRect:0x{(nint)pDestRect:X8}");
                    Debug.Log($"[DEBUG] hDestWindowOverride:0x{(nint)hDestWindowOverride:X8}, pDirtyRegion:0x{(nint)pDirtyRegion:X8}");
                }

                TaskQueues.RenderThreadTaskQueue.ExecutePending(device);
                Overlay.Instance?.RenderFrame(device);
            }
            catch (Exception e)
            {
                Debug.Log($"[Error] Unhandled exception thrown in graphics hook:\n{e}\n");
            }

            return _Present((void*)device, pSourceRect, pDestRect, hDestWindowOverride, pDirtyRegion);
        }

        private static readonly delegate* unmanaged[Stdcall]<void> _WrappedFunction =
            (delegate* unmanaged[Stdcall]<void>)(Memory.BaseAddress + Offsets.GRAPHICS_HOOK_TARGET_FUNCTION_ADDRESS);
        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static void GraphicsWrapperHook()
        {
            try
            {
                var device = GGXXACPR.GGXXACPR.Direct3D9DevicePointer;

                if (device == 0)
                {
                    Debug.Log("[ERROR] Present graphics hook was called with an invalid device pointer");
                    return;
                }

                TaskQueues.RenderThreadTaskQueue.ExecutePending(device);
                Overlay.Instance?.RenderFrame(device);
            }
            catch (Exception e)
            {
                Debug.Log($"[ERROR] Caught unhandled exception before it bubbled into native code:\n{e}\n");
            }
            finally
            {
                _WrappedFunction();
            }
        }

        [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
        private static void UpdateGameStateHook()
        {
            try
            {
                Overlay.Instance?.UpdateGameState();
            }
            catch (Exception e)
            {
                Debug.Log($"[ERROR] Exception thrown from UpdateGameStateHook:\n{e}\n");
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
        private static int PeekMessageWrapper(MSG* lpMsg, HWND hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg)
        {
            TaskQueues.PeekMessageTaskQueue.ExecutePending();

            BOOL success = PInvoke.PeekMessage(lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax, (PEEK_MESSAGE_REMOVE_TYPE)wRemoveMsg);

            if (success && lpMsg is not null && lpMsg->message == WM_KEYDOWN &&
                 Enum.IsDefined(typeof(VirtualKeyCodes), (int)lpMsg->wParam.Value))
            {
                Input.HandleKeyDownEvent(lpMsg->lParam.Value, (VirtualKeyCodes)(int)lpMsg->wParam.Value);
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
        private static void RevertMessageLoopJmp()
        {
            Debug.Log("Reverting message loop patch");
            Util.Patch(Memory.BaseAddress + Offsets.MESSAGE_LOOP_REL_JMP_OFFSET_BYTE_ADDR, [_originalJmpOffsetByte]);
        }
    }
}
