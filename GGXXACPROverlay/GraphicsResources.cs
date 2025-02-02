﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using GameOverlay.Drawing;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal class GraphicsResources : IDisposable
    {
        private static readonly int _boxAlpha = 50;

        private static readonly Color _defaultHitboxClr = new Color(255, 0, 0);
        private static readonly Color _defaultHurtboxClr = new Color(0, 255, 0);
        private static readonly Color _defaultCollisionboxClr = new Color(0, 255, 255);

        private readonly Dictionary<BoxId, SolidBrush> _hitboxOutlinePalette;
        private readonly Dictionary<BoxId, SolidBrush> _hitboxFillPalette;
        private readonly Dictionary<FrameMeter.FrameType, SolidBrush> _frameTypePalette;
        private readonly Dictionary<FrameMeter.FrameProperty1, SolidBrush> _framePropertyPalette;
        private readonly Dictionary<FrameMeter.FrameProperty2, SolidBrush> _frameProperty2Palette;

        // Frame Meter

        [AllowNull]
        public SolidBrush PivotBrush { get; private set; }
        [AllowNull]
        public SolidBrush EntityCoreBrush { get; private set; }
        [AllowNull]
        public SolidBrush CollisionOutlineBrush { get; private set; }
        [AllowNull]
        public SolidBrush CollisionFillBrush { get; private set; }
        [AllowNull]

        public Font Font { get; private set; }
        [AllowNull]
        public SolidBrush FontBrush { get; private set; }
        [AllowNull]
        public SolidBrush FontBorderBrush { get; private set; }

        public GraphicsResources()
        {
            _hitboxOutlinePalette = [];
            _hitboxFillPalette = [];
            _frameTypePalette = [];
            _framePropertyPalette = [];
            _frameProperty2Palette = [];
        }

        public void Initilize(Graphics g)
        {
            PivotBrush = g.CreateSolidBrush(255, 255, 255);
            EntityCoreBrush = g.CreateSolidBrush(255, 0, 255);
            CollisionOutlineBrush = g.CreateSolidBrush(_defaultCollisionboxClr);
            CollisionFillBrush = g.CreateSolidBrush(new Color(_defaultCollisionboxClr, _boxAlpha));

            Font = g.CreateFont("Arial Bold", 14);
            FontBrush= g.CreateSolidBrush(255, 255, 255);
            FontBorderBrush = g.CreateSolidBrush(0, 0, 0);

            _hitboxOutlinePalette.Add(BoxId.DUMMY, g.CreateSolidBrush(10, 10, 10));
            _hitboxOutlinePalette.Add(BoxId.HIT, g.CreateSolidBrush(_defaultHitboxClr));
            _hitboxOutlinePalette.Add(BoxId.HURT, g.CreateSolidBrush(_defaultHurtboxClr));
            _hitboxOutlinePalette.Add(BoxId.UNKNOWN_3, g.CreateSolidBrush(10, 10, 10));
            _hitboxOutlinePalette.Add(BoxId.UNKNOWN_5, g.CreateSolidBrush(10, 10, 10));
            _hitboxOutlinePalette.Add(BoxId.UNKNOWN_6, g.CreateSolidBrush(10, 10, 10));

            _hitboxFillPalette.Add(BoxId.DUMMY, g.CreateSolidBrush(10, 10, 10, _boxAlpha));
            _hitboxFillPalette.Add(BoxId.HIT, g.CreateSolidBrush(new Color(_defaultHitboxClr, _boxAlpha)));
            _hitboxFillPalette.Add(BoxId.HURT, g.CreateSolidBrush(new Color(_defaultHurtboxClr, _boxAlpha)));
            _hitboxFillPalette.Add(BoxId.UNKNOWN_3, g.CreateSolidBrush(10, 10, 10, _boxAlpha));
            _hitboxFillPalette.Add(BoxId.UNKNOWN_5, g.CreateSolidBrush(10, 10, 10, _boxAlpha));
            _hitboxFillPalette.Add(BoxId.UNKNOWN_6, g.CreateSolidBrush(10, 10, 10, _boxAlpha));

            _frameTypePalette.Add(FrameMeter.FrameType.None, g.CreateSolidBrush(15, 15, 15));
            _frameTypePalette.Add(FrameMeter.FrameType.Neutral, g.CreateSolidBrush(27, 27, 27));
            _frameTypePalette.Add(FrameMeter.FrameType.CounterHitState, g.CreateSolidBrush(1, 181, 151));
            _frameTypePalette.Add(FrameMeter.FrameType.Startup, g.CreateSolidBrush(1, 181, 151));
            _frameTypePalette.Add(FrameMeter.FrameType.Active, g.CreateSolidBrush(203, 43, 103));
            _frameTypePalette.Add(FrameMeter.FrameType.Recovery, g.CreateSolidBrush(0, 111, 188));
            _frameTypePalette.Add(FrameMeter.FrameType.BlockStun, g.CreateSolidBrush(200, 200, 0));
            _frameTypePalette.Add(FrameMeter.FrameType.HitStun, g.CreateSolidBrush(200, 200, 0));

            _framePropertyPalette.Add(FrameMeter.FrameProperty1.Default, g.CreateSolidBrush(0, 0, 0));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.SlashBack, g.CreateSolidBrush(255, 0, 0));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.InvulnFull, g.CreateSolidBrush(255, 255, 255));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.InvulnThrow, g.CreateSolidBrush(255, 125, 0));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.InvulnStrike, g.CreateSolidBrush(0, 125, 255));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.Armor, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.Parry, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.GuardPointFull, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.GuardPointHigh, g.CreateSolidBrush(120, 80, 0));
            _framePropertyPalette.Add(FrameMeter.FrameProperty1.GuardPointLow, g.CreateSolidBrush(120, 80, 0));

            _frameProperty2Palette.Add(FrameMeter.FrameProperty2.Default, g.CreateSolidBrush(0, 0, 0));
            _frameProperty2Palette.Add(FrameMeter.FrameProperty2.FRC, g.CreateSolidBrush(255, 255, 0));
        }

        public SolidBrush GetOutlineBrush(BoxId type)
        {
            return _hitboxOutlinePalette[type];
        }
        public SolidBrush GetFillBrush(BoxId type)
        {
            return _hitboxFillPalette[type];
        }
        public SolidBrush GetBrush(FrameMeter.FrameType type)
        {
            return _frameTypePalette[type];
        }
        public SolidBrush GetBrush(FrameMeter.FrameProperty1 property)
        {
            return _framePropertyPalette[property];
        }
        public SolidBrush GetBrush(FrameMeter.FrameProperty2 property)
        {
            return _frameProperty2Palette[property];
        }

        ~GraphicsResources()
        {
            Dispose(false);
        }

        // IDisposable stuff
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                PivotBrush.Dispose();
                EntityCoreBrush.Dispose();
                CollisionOutlineBrush.Dispose();
                CollisionFillBrush.Dispose();
                Font.Dispose();
                FontBrush.Dispose();
                FontBorderBrush.Dispose();
                foreach (SolidBrush brush in _hitboxOutlinePalette.Values) { brush.Dispose(); }
                _hitboxOutlinePalette.Clear();
                foreach (SolidBrush brush in _hitboxFillPalette.Values) { brush.Dispose(); }
                _hitboxFillPalette.Clear();
                foreach (SolidBrush brush in _frameTypePalette.Values) { brush.Dispose(); }
                _frameTypePalette.Clear();
                foreach (SolidBrush brush in _framePropertyPalette.Values) { brush.Dispose(); }
                _framePropertyPalette.Clear();
                foreach (SolidBrush brush in _frameProperty2Palette.Values) { brush.Dispose(); }
                _frameProperty2Palette.Clear();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            Debug.WriteLine("Graphics Resources Disposed");
        }
    }
}
