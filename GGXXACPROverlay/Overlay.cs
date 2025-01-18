using System.Diagnostics;
using GameOverlay.Drawing;
using GameOverlay.Windows;

namespace GGXXACPROverlay
{
    internal class Overlay : IDisposable
    {
        private static readonly string gameProcessName = "GGXXACPR_Win";
        private static readonly int boxAlpha = 50;

        private enum Boxtypes
        {
            None=0,
            Hit,
            Hurt,
            Collision,
            Grab
        }

        // TODO: Reogranize brushes
        private SolidBrush bgBrush;
        private SolidBrush pivotBrush;
        private readonly SolidBrush[] _outlineBrushes;
        private readonly SolidBrush[] _fillBrushes;

        private readonly StickyWindow _overlayWindow;

        private readonly Timer _gameStateUpdater;
        private readonly object _gameStateLock = new object();
        private readonly nint _gameHandle;
        private Drawing.Dimensions _windowDimensions;
        private readonly Player[] _players = new Player[2];
        private Camera _camera = new ();
        private Projectile[] _projectiles = [];

        public Overlay()
        {
            _outlineBrushes = new SolidBrush[Enum.GetNames(typeof(Boxtypes)).Length];
            _fillBrushes = new SolidBrush[Enum.GetNames(typeof(Boxtypes)).Length];

            bool success = Memory.OpenProcess(gameProcessName);
            _gameHandle = Memory.GetGameWindowHandle();

            var g = new Graphics();

            _overlayWindow = new StickyWindow(_gameHandle, g)
            {
                FPS = 240,
                AttachToClientArea = true,
                BypassTopmost = true
            };

            _gameStateUpdater = new Timer(UpdateGameState, null, TimeSpan.Zero, TimeSpan.FromSeconds(1d/60d));

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

        private void UpdateGameState(object? state)
        {
            if (!Memory.ProcessIsOpen())
            {
                Dispose();
                return;
            }

            lock (_gameStateUpdater)
            {
                WindowHelper.GetWindowClientBounds(_gameHandle, out WindowBounds gameWinDim);
                _windowDimensions = new Drawing.Dimensions(gameWinDim.Right - gameWinDim.Left, gameWinDim.Bottom - gameWinDim.Top);
                _players[0] = GGXXACPR.GetPlayerStruct(PlayerId.P1);
                _players[1] = GGXXACPR.GetPlayerStruct(PlayerId.P2);
                _camera = GGXXACPR.GetCameraStruct();
                _projectiles = GGXXACPR.GetProjectiles();
            }
        }

        private void SetupGraphics(object? sender, SetupGraphicsEventArgs e)
        {
            var g = e.Graphics;

            if (e.RecreateResources)
            {
                bgBrush.Dispose();
                pivotBrush.Dispose();
                foreach (var brush in _outlineBrushes) brush.Dispose();
                foreach (var brush in _fillBrushes) brush.Dispose();
            }

            bgBrush = g.CreateSolidBrush(0, 0, 0, 0);
            pivotBrush = g.CreateSolidBrush(255, 255, 255, 255);

            _outlineBrushes[(int)Boxtypes.None]       = g.CreateSolidBrush(10, 10, 10);
               _fillBrushes[(int)Boxtypes.None]       = g.CreateSolidBrush(10, 10, 10, boxAlpha);
            _outlineBrushes[(int)Boxtypes.Hit]        = g.CreateSolidBrush(255, 0, 0);
               _fillBrushes[(int)Boxtypes.Hit]        = g.CreateSolidBrush(255, 0, 0, boxAlpha);
            _outlineBrushes[(int)Boxtypes.Hurt]       = g.CreateSolidBrush(0, 255, 0);
               _fillBrushes[(int)Boxtypes.Hurt]       = g.CreateSolidBrush(0, 255, 0, boxAlpha);
            _outlineBrushes[(int)Boxtypes.Collision]  = g.CreateSolidBrush(0, 255, 255);
               _fillBrushes[(int)Boxtypes.Collision]  = g.CreateSolidBrush(0, 255, 255, boxAlpha);
            Debug.WriteLine("Graphics setup");
        }
        private void RenderFrame(object? sender, DrawGraphicsEventArgs e)
        {
            var g = e.Graphics;

            lock (_gameStateLock)
            {
                g.ClearScene(bgBrush);
                g.BeginScene();

                Drawing.DrawPlayerPushBox(
                    g,
                    _outlineBrushes[(int)Boxtypes.Collision],
                    _fillBrushes[(int)Boxtypes.Collision],
                    _players[0],
                    _camera,
                    _windowDimensions
                );
                Drawing.DrawPlayerBoxesById(
                    g,
                    _outlineBrushes[(int)Boxtypes.Hurt],
                    _fillBrushes[(int)Boxtypes.Hurt],
                    2,
                    _players[0],
                    _camera,
                    _windowDimensions
                );
                Drawing.DrawPlayerBoxesById(
                    g,
                    _outlineBrushes[(int)Boxtypes.Hit],
                    _fillBrushes[(int)Boxtypes.Hit],
                    1,
                    _players[0],
                    _camera,
                    _windowDimensions
                );


                Drawing.DrawPlayerPushBox(
                    g,
                    _outlineBrushes[(int)Boxtypes.Collision],
                    _fillBrushes[(int)Boxtypes.Collision],
                    _players[1],
                    _camera,
                    _windowDimensions
                );
                Drawing.DrawPlayerBoxesById(
                    g,
                    _outlineBrushes[(int)Boxtypes.Hurt],
                    _fillBrushes[(int)Boxtypes.Hurt],
                    2,
                    _players[1],
                    _camera,
                    _windowDimensions
                );
                Drawing.DrawPlayerBoxesById(
                    g,
                    _outlineBrushes[(int)Boxtypes.Hit],
                    _fillBrushes[(int)Boxtypes.Hit],
                    1,
                    _players[1],
                    _camera,
                    _windowDimensions
                );

                Drawing.DrawProjectileBoxes(
                    g,
                    _outlineBrushes[(int)Boxtypes.Hurt],
                    _fillBrushes[(int)Boxtypes.Hurt],
                    2,
                    _projectiles,
                    _camera,
                    _windowDimensions
                );
                Drawing.DrawProjectileBoxes(
                    g,
                    _outlineBrushes[(int)Boxtypes.Hit],
                    _fillBrushes[(int)Boxtypes.Hit],
                    1,
                    _projectiles,
                    _camera,
                    _windowDimensions
                );
                Drawing.DrawPlayerPivot(g, pivotBrush, _players[0], _camera, _windowDimensions);
                Drawing.DrawPlayerPivot(g, pivotBrush, _players[1], _camera, _windowDimensions);

                g.EndScene();
            }
        }
        private void CleanupGraphics(object? sender, DestroyGraphicsEventArgs e)
        {
            bgBrush.Dispose();
            pivotBrush.Dispose();
            foreach (var brush in _outlineBrushes) brush?.Dispose();
            foreach (var brush in _fillBrushes) brush?.Dispose();
            Debug.WriteLine("Graphics cleaned up");
        }

        // Will use these to organize RenderFrame eventually
        //private void RenderPlayer(Graphics g, Player p)
        //{
        //    //
        //}
        //private void RenderProjEntities(Graphics g, Projectile[] projectiles)
        //{
        //    //
        //}

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
