using System.Diagnostics;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal class Overlay : IDisposable
    {
        private static readonly string gameProcessName = "GGXXACPR_Win";

        private readonly GraphicsResources _resources;
        private readonly BoxId[] _drawList = new BoxId[2];

        private readonly StickyWindow _overlayWindow;

        private readonly Timer _gameStateUpdater;
        private readonly object _gameStateLock = new();
        private readonly nint _gameHandle;
        private Drawing.Dimensions _windowDimensions;

        private GameState _gameState;

        private readonly FrameMeter _frameMeter = new();

        private bool shouldRender = false;
        private bool drawHitboxes = true;
        private bool drawFrameMeter = true;

        public Overlay()
        {
            _drawList[0] = BoxId.HIT;
            _drawList[1] = BoxId.HURT;

            Memory.OpenProcess(gameProcessName);
            _gameHandle = Memory.GetGameWindowHandle();

            var g = new Graphics();
            _resources = new GraphicsResources();

            _overlayWindow = new StickyWindow(_gameHandle, g)
            {
                FPS = 240,
                AttachToClientArea = true,
                BypassTopmost = true
            };

            _gameStateUpdater = new Timer(UpdateGameState, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(12));

            _overlayWindow.DestroyGraphics += CleanupGraphics;
            _overlayWindow.DrawGraphics += RenderFrame;
            _overlayWindow.SetupGraphics += SetupGraphics;
        }
        public void Run()
        {
            _overlayWindow.Create();
        }

        public bool IsRunning()
        {
            return _overlayWindow.IsRunning;
        }

        public void ToggleHitboxOverlay() { drawHitboxes = !drawHitboxes; }
        public void ToggleFrameMeter() { drawFrameMeter = !drawFrameMeter; }

        //private void UpdateToggles(object? state, KeyboardEventArgs e)
        //{
        //    Debug.WriteLine($"CALLBACK!! e: {e}");
        //}

        //private void TempSetHook()
        //{
        //    _inputListenerCallback = new EventHandler<KeyboardEventArgs>(UpdateToggles);

        //    var modHandle = PInvoke.GetModuleHandle(typeof(Hooks).Assembly.GetName().Name);
        //    Debug.WriteLine($"Hooks module is invalid: {modHandle.IsInvalid}");
        //    hookHandle = PInvoke.SetWindowsHookEx(WINDOWS_HOOK_ID.WH_KEYBOARD, Hooks.KeyboardHookCallback, modHandle, Memory.GetGameThreadID());
        //    if (hookHandle == null)
        //    {
        //        Memory.HandleSystemError("Set Windows Hook returned null");
        //    }
        //    Hooks.HHook = hookHandle;
        //    Debug.WriteLine("Hooks set.");

        //    Hooks.OnKeyUp += _inputListenerCallback;
        //    Hooks.DebugMessage += (object? sender, DebugEventArgs e) =>
        //    {
        //        this.Debug1(e.Message);
        //        Debug.WriteLine(e.Message);
        //    };
        //    Debug.WriteLine("Callbacks defined.");
        //}

        //private void TempInitPipe()
        //{
        //    // NamedPipeServerStream() communicate with ggxxacpr?
        //}

        private void UpdateGameState(object? state)
        {
            if (!Memory.ProcessIsOpen())
            {
                Dispose();
                return;
            }

            shouldRender = GGXXACPR.GGXXACPR.ShouldRender();

            lock (_gameStateUpdater)
            {
                WindowHelper.GetWindowClientBounds(_gameHandle, out WindowBounds gameWinDim);
                _windowDimensions = new Drawing.Dimensions(gameWinDim.Right - gameWinDim.Left, gameWinDim.Bottom - gameWinDim.Top);
                _gameState = GGXXACPR.GGXXACPR.GetGameState();
                int exitCode = _frameMeter.Update(_gameState);
                // Update refresh if bad gamestate read is detected
                // TODO: rework this after gamestate hooks
                if (exitCode > 0)
                {
                    Debug.WriteLine("Refreshing game state.");
                    _gameState = GGXXACPR.GGXXACPR.GetGameState();
                    _frameMeter.Update(_gameState);
                }
            }
        }

        private void SetupGraphics(object? sender, SetupGraphicsEventArgs e)
        {
            var g = e.Graphics;

            if (e.RecreateResources)
            {
                _resources.Dispose();
            }

            _resources.Initilize(g);

            Debug.WriteLine("Graphics setup");
        }
        private void RenderFrame(object? sender, DrawGraphicsEventArgs e)
        {
            var g = e.Graphics;
            g.ClearScene();

            if (!shouldRender) { return; }

            lock (_gameStateLock)
            {
                g.BeginScene();

                if (drawHitboxes)
                {
                    Drawing.DrawPlayerPushBox(g, _resources, _gameState, _gameState.Player1, _windowDimensions);
                    Drawing.DrawPlayerBoxes(g, _resources, _drawList, _gameState, _gameState.Player1, _windowDimensions);

                    Drawing.DrawPlayerPushBox(g, _resources, _gameState, _gameState.Player2, _windowDimensions);
                    Drawing.DrawPlayerBoxes(g, _resources, _drawList, _gameState, _gameState.Player2, _windowDimensions);

                    Drawing.DrawProjectileBoxes(g, _resources, _drawList, _gameState, _windowDimensions);

                    Drawing.DrawPlayerPivot(g, _resources, _gameState, _gameState.Player1, _windowDimensions);
                    Drawing.DrawPlayerPivot(g, _resources, _gameState, _gameState.Player2, _windowDimensions);
                }

                if (drawFrameMeter)
                {
                    Drawing.DrawFrameMeter(g, _resources, _frameMeter, _windowDimensions);
                }

                g.EndScene();
            }
        }
        private void CleanupGraphics(object? sender, DestroyGraphicsEventArgs e)
        {
            _resources.Dispose();
            Debug.WriteLine("Graphics cleaned up");
        }

        ~Overlay()
        {
            Dispose(false);
        }

        // IDisposable stuff
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                _overlayWindow.Dispose();
                _gameStateUpdater.Dispose();
                Memory.CloseProcess();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            Debug.WriteLine("Overlay Disposed");
        }
    }
}
