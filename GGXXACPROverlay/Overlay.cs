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

        private OverlaySettings _overlaySettings = new();

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

        public void ToggleHitboxOverlay() => _overlaySettings.DisplayHitBoxes = !_overlaySettings.DisplayHitBoxes;
        public void ToggleFrameMeter() => _overlaySettings.DisplayFrameMeter = !_overlaySettings.DisplayFrameMeter;
        public void ToggleThrowRangeDisplay() => _overlaySettings.AlwaysDrawThrowRange = !_overlaySettings.AlwaysDrawThrowRange;
        public void ToggleDisplayLegend() => _overlaySettings.DisplayFrameMeterLegend = !_overlaySettings.DisplayFrameMeterLegend;
        public void ToggleRecordHitstop() => _overlaySettings.RecordDuringHitstop = !_overlaySettings.RecordDuringHitstop;
        public void ToggleRecordSuperFlash() => _overlaySettings.RecordDuringSuperFlash = !_overlaySettings.RecordDuringSuperFlash;

        private void UpdateGameState(object? state)
        {
            if (!Memory.ProcessIsOpen())
            {
                Dispose();
                return;
            }

            _overlaySettings.ShouldRender = GGXXACPR.GGXXACPR.ShouldRender();

            lock (_gameStateUpdater)
            {
                WindowHelper.GetWindowClientBounds(_gameHandle, out WindowBounds gameWinDim);
                _windowDimensions = new Drawing.Dimensions(gameWinDim.Right - gameWinDim.Left, gameWinDim.Bottom - gameWinDim.Top);
                _gameState = GGXXACPR.GGXXACPR.GetGameState();
                int exitCode = _frameMeter.Update(_gameState, _overlaySettings);
                // Update refresh if bad gamestate read is detected
                // TODO: rework this after gamestate hooks
                if (exitCode > 0)
                {
                    Debug.WriteLine("Refreshing game state.");
                    _gameState = GGXXACPR.GGXXACPR.GetGameState();
                    _frameMeter.Update(_gameState, _overlaySettings);
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

            if (!_overlaySettings.ShouldRender) { return; }

            lock (_gameStateLock)
            {
                g.BeginScene();

                if (_overlaySettings.DisplayHitBoxes)
                {
                    Drawing.BeginClipGameRegion(g, _windowDimensions);

                    Drawing.DrawPlayerPushBox(g, _resources, _gameState, _gameState.Player1, _windowDimensions);
                    Drawing.DrawPlayerBoxes(g, _resources, _drawList, _gameState, _gameState.Player1, _windowDimensions);
                    Drawing.DrawPlayerGrabBox(g, _resources, _gameState, _gameState.Player1, _windowDimensions, _overlaySettings.AlwaysDrawThrowRange);

                    Drawing.DrawPlayerPushBox(g, _resources, _gameState, _gameState.Player2, _windowDimensions);
                    Drawing.DrawPlayerBoxes(g, _resources, _drawList, _gameState, _gameState.Player2, _windowDimensions);
                    Drawing.DrawPlayerGrabBox(g, _resources, _gameState, _gameState.Player2, _windowDimensions, _overlaySettings.AlwaysDrawThrowRange);

                    Drawing.DrawProjectileBoxes(g, _resources, _drawList, _gameState, _windowDimensions);

                    Drawing.DrawPlayerPivot(g, _resources, _gameState, _gameState.Player1, _windowDimensions);
                    Drawing.DrawPlayerPivot(g, _resources, _gameState, _gameState.Player2, _windowDimensions);

                    Drawing.EndClipGameRegion(g);
                }

                if (_overlaySettings.DisplayFrameMeter)
                {
                    Drawing.DrawFrameMeter(g, _resources, _frameMeter, _windowDimensions);
                }
                if (_overlaySettings.DisplayFrameMeterLegend)
                {
                    Drawing.DrawFrameMeterLegend(g, _resources, _windowDimensions);
                }

                Drawing.DrawSettingsLabels(g, _resources, _overlaySettings);

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
