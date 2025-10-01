using System.Buffers;
using System.Numerics;
using GGXXACPROverlay.FrameMeter;
using GGXXACPROverlay.GGXXACPR;
using GGXXACPROverlay.Rendering.Glyphs;
using Vortice.Mathematics;

// TODO: reevaluate preallocated buffers
namespace GGXXACPROverlay.Rendering
{
    /// <summary>
    /// Helper functions that convert game data from the GGXXACPR class into a ready to draw
    /// format using display information from the Settings class.
    /// </summary>
    internal static class Drawing
    {
        public static D3DCOLOR_ARGB GetBoxColor(ushort id) => GetBoxColor((BoxId)id);
        public static D3DCOLOR_ARGB GetBoxColor(BoxId id)
            => id switch
            {
                BoxId.HIT  => Settings.Hitboxes.Palette.Hitbox,
                BoxId.HURT => Settings.Hitboxes.Palette.Hurtbox,
                _          => Settings.Hitboxes.Palette.Default,
            };

        public static D3DCOLOR_ARGB GetAdvantageLabelColor(int advantage)
            => advantage switch
            {
                > 0 => new D3DCOLOR_ARGB(0x60000080),
                < 0 => new D3DCOLOR_ARGB(0x60800000),
                0 => D3DCOLOR_ARGB.CLEAR,
            };

        public static RentedArraySlice<ColorRectangle> GetHitboxPrimitives(Span<Hitbox> boxes)
        {
            RentedArraySlice<ColorRectangle> output = new(boxes.Length);
            for (int i = 0; i < boxes.Length; i++) Convert(ref boxes[i], out output[i]);

            return output;
        }
        public static RentedArraySlice<Rect> ToRects(Span<Hitbox> boxes)
        {
            RentedArraySlice<Rect> output = new(boxes.Length);
            for (int i = 0; i < boxes.Length; i++) Convert(ref boxes[i], out output[i]);

            return output;
        }
        public static RentedArraySlice<Vertex3PositionColor> GetCombinedHitboxPrimitives(Span<Hitbox> boxes)
        {
            if (boxes.Length == 0) return new();

            D3DCOLOR_ARGB borderColor = GetBoxColor(boxes[0].BoxTypeId);
            using var rects = ToRects(boxes);
            return Geometry.GetCombinedGeometry(rects, borderColor);
        }
        public static RentedArraySlice<Vertex3PositionColor> GetBorderPrimitives(Span<Hitbox> boxes)
        {
            if (boxes.Length == 0) return new();

            D3DCOLOR_ARGB borderColor = new(GetBoxColor(boxes[0].BoxTypeId).ARGB | 0xFF000000);
            using var rects = ToRects(boxes);
            float borderSizeInModelCoor = Settings.Hitboxes.HitboxBorderThickness * GGXXACPR.GGXXACPR.WorldCoorPerViewPixel() / 100.0f;
            return Geometry.GetBorderGeometry(rects, borderColor, borderSizeInModelCoor);
        }

        private static void Convert(ref Hitbox h, out ColorRectangle output)
        {
            var colorValue = Settings.Hitboxes.Palette.Default;
            if (h.BoxTypeId == (ushort)BoxId.HIT) colorValue = Settings.Hitboxes.Palette.Hitbox;
            else if (h.BoxTypeId == (ushort)BoxId.HURT) colorValue = Settings.Hitboxes.Palette.Hurtbox;
            output = new ColorRectangle(h.XOffset, h.YOffset, h.Width, h.Height, colorValue);
        }
        private static void Convert(ref Hitbox h, out Rect output)
            => output = new Rect(h.XOffset, h.YOffset, h.Width, h.Height);

        public static ColorRectangle GetCLHitBox(Player p)
            => new ColorRectangle(GGXXACPR.GGXXACPR.GetCLRect(p), Settings.Hitboxes.Palette.CLHitbox);

        /// <summary>
        /// Expresses the character origin point (pivot) as two rectangles forming a cross.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="p"></param>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public static RentedArraySlice<ColorRectangle> GetPivot(Player p, float ratio)
            => GetPivot(p.XPos, p.YPos, ratio);
        public static RentedArraySlice<ColorRectangle> GetPivot(Entity e, float ratio)
            => GetPivot(e.XPos, e.YPos, ratio);
        private static RentedArraySlice<ColorRectangle> GetPivot(int x, int y, float ratio)
        {
            ColorRectangle[] temp = ArrayPool<ColorRectangle>.Shared.Rent(2);
            var halfSize = Settings.Hitboxes.PivotCrossSize * ratio / 2.0f;
            var halfThickness = Settings.Hitboxes.PivotCrossThickness * ratio / 2.0f;

            temp[0] = new ColorRectangle(
                x - halfSize,
                y - halfThickness,
                halfSize * 2,
                halfThickness * 2,
                Settings.Hitboxes.Palette.PivotCrossColor
            );
            temp[1] = new ColorRectangle(
                x - halfThickness,
                y - halfSize,
                halfThickness * 2,
                halfSize * 2,
                Settings.Hitboxes.Palette.PivotCrossColor
            );

            return new(temp, 0, 2);
        }

        public static ColorRectangle GetPushboxPrimitives(Player p)
        {
            Rect push = GGXXACPR.GGXXACPR.GetPushBox(p);
            return new ColorRectangle(push, Settings.Hitboxes.Palette.Push);
        }

        public static ColorRectangle GetGrabboxPrimitives(Player p)
        {
            return new ColorRectangle(GGXXACPR.GGXXACPR.GetGrabBox(p), Settings.Hitboxes.Palette.Grab);
        }
        public static ColorRectangle GetCommnadGrabboxPrimitives(Player p)
        {
            GGXXACPR.GGXXACPR.GetCommandGrabBox(p, GGXXACPR.GGXXACPR.GetPushBox(p), out Rect cmdGrabBox);
            return new ColorRectangle(cmdGrabBox, Settings.Hitboxes.Palette.Grab);
        }

        public static ColorRectangle GetPushRangeBoxPrimitive(Rect r)
            => new ColorRectangle(r, Settings.Hitboxes.Palette.MiscPushRange);
        public static ColorRectangle GetPivotRangeBoxPrimitive(Rect r)
            => new ColorRectangle(r, Settings.Hitboxes.Palette.MiscPivotRange);

        public static RentedArraySlice<ColorRectangle> GetPushAndGrabPrimitives(Player p)
        {
            ColorRectangle[] temp = ArrayPool<ColorRectangle>.Shared.Rent(2);
            Rect push = GGXXACPR.GGXXACPR.GetPushBox(p);
            temp[0] = new ColorRectangle(push, Settings.Hitboxes.Palette.Push);
            ThrowDetection throwFlags = GGXXACPR.GGXXACPR.ThrowFlags;

            if (GGXXACPR.GGXXACPR.GetCommandGrabBox(p, push, out Rect cmdGrab))
            {
                temp[1] = new ColorRectangle(cmdGrab, Settings.Hitboxes.Palette.Grab);
                return new(temp);
            }
            else if (p.PlayerIndex == 0 && throwFlags.HasFlag(ThrowDetection.Player1ThrowSuccess) ||
                     p.PlayerIndex == 1 && throwFlags.HasFlag(ThrowDetection.Player2ThrowSuccess))
            {
                Rect grab = GGXXACPR.GGXXACPR.GetGrabBox(p, push);
                temp[1] = new ColorRectangle(grab, Settings.Hitboxes.Palette.Grab);
                return new(temp);
            }

            return new(temp, 0, 1);
        }

        private struct FrameMeterDimensions
        {
            public int meterLength;
            public int screenWidth;
            public int pipSpacing;
            public int pipSpacingVertical;
            public int pipWidth;
            public int totalWidth;
            public int pipHeight;
            public int entityPipHeight;
            public int coreYPos;
            public int yPos;
            public int xPos;
            public int propertyHighlightHeight;
            public int propertyHighlightTop;
            public int borderThickness;
            public int fontSize;
        }
        private static readonly Dictionary<Vortice.Direct3D9.Viewport, FrameMeterDimensions> _dimensionCache = [];
        private static FrameMeterDimensions GetDimensions(Vortice.Direct3D9.Viewport viewPort)
        {
            if (_dimensionCache.TryGetValue(viewPort, out FrameMeterDimensions value))
            {
                return value;
            }

            const int FRAME_METER_VERTICAL_SPACING = 1;
            const int FRAME_METER_Y = 400;
            const int FRAME_METER_BASE_LINE_X = 5;

            FrameMeterDimensions d = new();

            d.meterLength = FrameMeter.FrameMeter.METER_LENGTH;
            d.screenWidth = viewPort.Height * 4 / 3;

            d.pipSpacing = (d.screenWidth - FRAME_METER_BASE_LINE_X * 2) / d.meterLength;
            d.pipSpacingVertical = FRAME_METER_VERTICAL_SPACING;
            d.pipWidth = d.pipSpacing - 1;
            d.totalWidth = d.pipSpacing * d.meterLength - 1;
            d.pipHeight = d.pipWidth * 5 / 3;
            d.entityPipHeight = d.pipWidth;
            d.coreYPos = viewPort.Height * FRAME_METER_Y / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;
            d.yPos = d.coreYPos - d.pipHeight - 1;
            d.xPos = (viewPort.Width - d.totalWidth) / 2;
            d.propertyHighlightHeight = d.pipHeight * 2 / 7;
            d.propertyHighlightHeight += d.propertyHighlightHeight == 0 ? 1 : 0;
            d.propertyHighlightTop = d.pipHeight - d.propertyHighlightHeight;
            d.borderThickness = 2 * viewPort.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;
            d.fontSize = (int)Math.Max(18f, viewPort.Height / 34.286f);

            _dimensionCache.Add(viewPort, d);

            return d;
        }

        public static RentedArraySlice<ColorRectangle> GetFrameMeterPrimitives(
            FrameMeter.FrameMeter frameMeter,
            Vortice.Direct3D9.Viewport viewport,
            IGlyphAtlas atlas,
            out RentedArraySlice<GlyphString> glyphs)
        {
            const float labelSpacing = 1.0f;

            int outputIndex = 0;
            ColorRectangle[] output = ArrayPool<ColorRectangle>.Shared.Rent(1024);
            int glyphIndex = 0;
            GlyphString[] glyphArr = ArrayPool<GlyphString>.Shared.Rent(64);

            FrameMeterDimensions dim = GetDimensions(viewport);

            // Main border
            output[outputIndex++] = new(
                dim.xPos - dim.borderThickness,
                dim.yPos - dim.borderThickness,
                dim.totalWidth + dim.borderThickness * 2,
                 2 * dim.pipHeight + dim.pipSpacingVertical + dim.borderThickness * 2,
                D3DCOLOR_ARGB.BLACK);

            // P1 Entity Border
            if (!frameMeter.EntityMeters[0].Hide)
            {
                output[outputIndex++] = new(
                    dim.xPos - dim.borderThickness,
                    dim.yPos - dim.borderThickness - dim.pipSpacingVertical - dim.entityPipHeight,
                    dim.totalWidth + dim.borderThickness * 2,
                    dim.borderThickness + dim.entityPipHeight,
                    D3DCOLOR_ARGB.BLACK);
            }

            // P2 Entity Border
            if (!frameMeter.EntityMeters[1].Hide)
            {
                output[outputIndex++] = new(
                    dim.xPos - dim.borderThickness,
                    dim.yPos + 2 * (dim.pipHeight + dim.pipSpacingVertical),
                    dim.totalWidth + dim.borderThickness * 2,
                    dim.borderThickness + dim.entityPipHeight,
                    D3DCOLOR_ARGB.BLACK);
            }

            // Player pips and property highlights
            for (int j = 0; j < frameMeter.PlayerMeters.Length; j++)
            {
                var frameArr = frameMeter.PlayerMeters[j].FrameArr;
                for (int i = 0; i < frameArr.Length; i++)
                {
                    var pipRectangle = new Rect(
                        dim.xPos + i * dim.pipSpacing,
                        dim.yPos + j * (dim.pipSpacingVertical + dim.pipHeight),
                        dim.pipWidth,
                        dim.pipHeight);

                    output[outputIndex++] = new ColorRectangle(pipRectangle, Settings.FrameMeter.Palette.GetColor(frameArr[i].Type));
                    if (frameArr[i].SecondaryProperty != SecondaryFrameProperty.Default)
                    {
                        // highlight border
                        output[outputIndex++] = new(
                            pipRectangle.Left,
                            pipRectangle.Top + 1,
                            pipRectangle.Width,
                            dim.propertyHighlightHeight,
                            D3DCOLOR_ARGB.BLACK
                        );
                        // highlight
                        output[outputIndex++] = new(
                            pipRectangle.Left,
                            pipRectangle.Top,
                            pipRectangle.Width,
                            dim.propertyHighlightHeight,
                            Settings.FrameMeter.Palette.GetColor(frameArr[i].SecondaryProperty)
                        );
                    }
                    if (frameArr[i].PrimaryProperty1 != PrimaryFrameProperty.Default)
                    {
                        // highlight border
                        output[outputIndex++] = new(
                            pipRectangle.Left,
                            pipRectangle.Bottom - dim.propertyHighlightHeight - 1,
                            pipRectangle.Width,
                            dim.propertyHighlightHeight,
                            D3DCOLOR_ARGB.BLACK
                        );
                        // highlight
                        output[outputIndex++] = new(
                            pipRectangle.Left,
                            pipRectangle.Top + dim.propertyHighlightTop,
                            pipRectangle.Width,
                            dim.propertyHighlightHeight,
                            Settings.FrameMeter.Palette.GetColor(frameArr[i].PrimaryProperty1)
                        );
                        if (frameArr[i].PrimaryProperty2 != PrimaryFrameProperty.Default)
                        {
                            int halfPipWidth = dim.pipWidth / 2;
                            output[outputIndex++] = new(
                                pipRectangle.Left + halfPipWidth,
                                pipRectangle.Top + dim.propertyHighlightTop,
                                dim.pipWidth - halfPipWidth,
                                pipRectangle.Height,
                            Settings.FrameMeter.Palette.GetColor(frameArr[i].PrimaryProperty2)
                            );
                        }
                    }
                    if (frameArr[i].RunSum > 0)
                    {
                        string label = frameArr[i].RunSum.ToString();

                        GlyphString gString = new GlyphString(
                            label[^1..^0],
                            atlas,
                            new(dim.xPos + i * dim.pipSpacing, dim.yPos + j * (dim.pipSpacingVertical + dim.pipHeight)),
                            dim.pipHeight);

                        Rect r = gString.Bounds;
                        gString.Position = new(
                            gString.Position.X + (dim.pipWidth - r.Width) / 2,
                            gString.Position.Y + (dim.pipHeight - r.Height) / 2);

                        glyphArr[glyphIndex++] = gString;

                        if (frameArr[i].RunSum > 9 && i > 0)
                        {
                            gString = new GlyphString(
                                label[^2..^1],
                                atlas,
                                new(dim.xPos + (i - 1) * dim.pipSpacing, dim.yPos + j * (dim.pipSpacingVertical + dim.pipHeight)),
                                dim.pipHeight);

                            r = gString.Bounds;
                            gString.Position = new(
                                gString.Position.X + (dim.pipWidth - r.Width) / 2,
                                gString.Position.Y + (dim.pipHeight - r.Height) / 2);

                            glyphArr[glyphIndex++] = gString;
                        }
                    }
                }
            }

            // Entity pips
            if (!frameMeter.EntityMeters[0].Hide)
            {
                for (int i = 0; i < frameMeter.EntityMeters[0].FrameArr.Length; i++)
                {
                    output[outputIndex++] = new(
                        dim.xPos + i * dim.pipSpacing,
                        dim.yPos - dim.pipSpacingVertical - dim.entityPipHeight,
                        dim.pipWidth,
                        dim.entityPipHeight,
                        Settings.FrameMeter.Palette.GetColor(frameMeter.EntityMeters[0].FrameArr[i].Type)
                    );
                }
            }
            if (!frameMeter.EntityMeters[1].Hide)
            {
                for (int i = 0; i < frameMeter.EntityMeters[1].FrameArr.Length; i++)
                {
                    output[outputIndex++] = new(
                        dim.xPos + i * dim.pipSpacing,
                        dim.yPos + 2 * (dim.pipSpacingVertical + dim.pipHeight),
                        dim.pipWidth,
                        dim.entityPipHeight,
                        Settings.FrameMeter.Palette.GetColor(frameMeter.EntityMeters[1].FrameArr[i].Type)
                    );
                }
            }

            // Labels P1
            string startupLabel = frameMeter.PlayerMeters[0].Startup >= 0 ? frameMeter.PlayerMeters[0].Startup.ToString() : "-";
            glyphArr[glyphIndex++] = new GlyphString(
                $"Startup: {startupLabel}",
                atlas,
                new Vector2(dim.xPos, dim.yPos - dim.entityPipHeight - dim.borderThickness - dim.fontSize),
                size: dim.fontSize,
                spacing: labelSpacing);

            float advantageXOffset = new GlyphString("Startup: -99   ", atlas, Vector2.Zero, dim.fontSize, spacing: 2.0f).Bounds.Width;

            string advantageLabel = frameMeter.PlayerMeters[0].DisplayAdvantage ? frameMeter.PlayerMeters[0].Advantage.ToString() : "-";
            glyphArr[glyphIndex++] = new GlyphString(
                $"Advantage: {advantageLabel}",
                atlas,
                new Vector2(dim.xPos + advantageXOffset, dim.yPos - dim.entityPipHeight - dim.borderThickness - dim.fontSize),
                size: dim.fontSize,
                GetAdvantageLabelColor(frameMeter.PlayerMeters[0].Advantage),
                spacing: labelSpacing);

            // Labels P2
            startupLabel = frameMeter.PlayerMeters[1].Startup >= 0 ? frameMeter.PlayerMeters[1].Startup.ToString() : "-";
            glyphArr[glyphIndex++] = new GlyphString(
                $"Startup: {startupLabel}",
                atlas,
                new Vector2(dim.xPos, dim.coreYPos + dim.pipHeight + dim.entityPipHeight + dim.borderThickness * 2),
                size: dim.fontSize,
                spacing: labelSpacing);

            advantageLabel = frameMeter.PlayerMeters[1].DisplayAdvantage ? frameMeter.PlayerMeters[1].Advantage.ToString() : "-";
            glyphArr[glyphIndex++] = new GlyphString(
                $"Advantage: {advantageLabel}",
                atlas,
                new Vector2(dim.xPos + advantageXOffset, dim.coreYPos + dim.pipHeight + dim.entityPipHeight + dim.borderThickness * 2),
                size: dim.fontSize,
                GetAdvantageLabelColor(frameMeter.PlayerMeters[1].Advantage),
                spacing: labelSpacing);

            glyphs = new RentedArraySlice<GlyphString>(glyphArr, 0, glyphIndex);
            return new RentedArraySlice<ColorRectangle>(output, 0, outputIndex);
        }

        public static RentedArraySlice<GlyphString> GetLegendPrimitives(Vortice.Direct3D9.Viewport viewport, IGlyphAtlas atlas, out RentedArraySlice<ColorRectangle> primitives)
        {
            FrameMeterDimensions dim = GetDimensions(viewport);

            GlyphString[] strings = ArrayPool<GlyphString>.Shared.Rent(32);
            int index = 0;
            ColorRectangle[] primitiveArray = ArrayPool<ColorRectangle>.Shared.Rent(64);
            int primIndex = 0;

            const float X_POS = 20f;
            const float LEGEND_OFFSET = 50f;
            const float LEGEND_LABEL_OFFSET = 100f;
            float ITEM_INCREMENT = dim.fontSize * 1.4f;

            GlyphString gString = new GlyphString($"GGXXACPR Overlay v{Program.Version}", atlas, Vector2.Zero, dim.fontSize);
            float bgWidth = gString.Bounds.Width * 1.5f;
            strings[index++] = gString;

            strings[index++] = new GlyphString(
                "F1 = Toggle Hitboxes",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            strings[index++] = new GlyphString(
                "F2 = Toggle Hitstun Meters",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            strings[index++] = new GlyphString(
                "F3 = Toggle Frame Meter",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            strings[index++] = new GlyphString(
                "F4 = Toggle Throw Range Box",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            strings[index++] = new GlyphString(
                "F5 = Freeze Frame",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            strings[index++] = new GlyphString(
                "F6 = Frame Step",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            strings[index++] = new GlyphString(
                "F7 = Black Background",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            strings[index++] = new GlyphString(
                "F8 = Reload OverlaySettings.ini",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            strings[index++] = new GlyphString(
                "F9 = Toggle this help menu",
                atlas,
                new Vector2(X_POS, (index - 1) * ITEM_INCREMENT),
                dim.fontSize);

            int hotKeyCount = index;

            // FM legend
            Settings.FrameMeter.Palette.GetLegends(out var frameTypes, out var pPropTypes, out var sPropTypes);
            float legendBGWidth = 0f;

            foreach (var key in frameTypes.Keys)
            {
                var glyphString = new GlyphString(
                    frameTypes[key],
                    atlas,
                    new Vector2(bgWidth + LEGEND_LABEL_OFFSET, ITEM_INCREMENT * (index + 1 - hotKeyCount)),
                    dim.fontSize);
                legendBGWidth = Math.Max(legendBGWidth, glyphString.Bounds.Width);
                strings[index++] = glyphString;


            }
            foreach (var key in pPropTypes.Keys)
            {
                var glyphString = new GlyphString(
                    pPropTypes[key],
                    atlas,
                    new Vector2(bgWidth + LEGEND_LABEL_OFFSET, ITEM_INCREMENT * (index + 1 - hotKeyCount)),
                    dim.fontSize);
                legendBGWidth = Math.Max(legendBGWidth, glyphString.Bounds.Width);
                strings[index++] = glyphString;
            }
            foreach (var key in sPropTypes.Keys)
            {
                var glyphString = new GlyphString(
                    sPropTypes[key],
                    atlas,
                    new Vector2(bgWidth + LEGEND_LABEL_OFFSET, ITEM_INCREMENT * (index + 1 - hotKeyCount)),
                    dim.fontSize);
                legendBGWidth = Math.Max(legendBGWidth, glyphString.Bounds.Width);
                strings[index++] = glyphString;
            }

            primitiveArray[primIndex++] = new ColorRectangle(
                new Rect(
                    -1f,
                    -1f,
                    bgWidth,
                    hotKeyCount * ITEM_INCREMENT),
                new D3DCOLOR_ARGB(0xC0000000));
            primitiveArray[primIndex++] = new ColorRectangle(
                new Rect(
                    bgWidth - 1f,
                    -1f,
                    legendBGWidth + LEGEND_LABEL_OFFSET * 2,
                    (index + 1 - hotKeyCount) * ITEM_INCREMENT),
                new D3DCOLOR_ARGB(0xC0000000));

            int pipIndex = 1;
            foreach (var key in frameTypes.Keys)
            {
                Rect pipRect = new Rect(
                        bgWidth + LEGEND_OFFSET,
                        ITEM_INCREMENT * pipIndex,
                        dim.pipWidth,
                        dim.pipHeight);

                // border
                primitiveArray[primIndex++] = new ColorRectangle(
                    new Rect(
                        pipRect.X - dim.borderThickness,
                        pipRect.Y - dim.borderThickness,
                        pipRect.Width + dim.borderThickness * 2,
                        pipRect.Height + dim.borderThickness * 2),
                    D3DCOLOR_ARGB.BLACK);
                // pip
                primitiveArray[primIndex++] = new ColorRectangle(pipRect, key);

                pipIndex++;
            }
            foreach (var key in pPropTypes.Keys)
            {
                Rect pipRect = new Rect(
                        bgWidth + LEGEND_OFFSET,
                        ITEM_INCREMENT * pipIndex,
                        dim.pipWidth,
                        dim.pipHeight);

                // border
                primitiveArray[primIndex++] = new ColorRectangle(
                    new Rect(
                        pipRect.X - dim.borderThickness,
                        pipRect.Y - dim.borderThickness,
                        pipRect.Width + dim.borderThickness * 2,
                        pipRect.Height + dim.borderThickness * 2),
                    D3DCOLOR_ARGB.BLACK);
                // pip
                primitiveArray[primIndex++] = new ColorRectangle(pipRect, Settings.FrameMeter.Palette.GetColor(FrameType.None));
                // primary prop border
                primitiveArray[primIndex++] = new ColorRectangle(
                    new Rect(
                        pipRect.Left,
                        pipRect.Bottom - dim.propertyHighlightHeight - 1,
                        pipRect.Width,
                        dim.propertyHighlightHeight),
                    D3DCOLOR_ARGB.BLACK);
                // primary prop
                primitiveArray[primIndex++] = new ColorRectangle(
                    new Rect(
                        pipRect.Left,
                        pipRect.Top + dim.propertyHighlightTop,
                        pipRect.Width,
                        dim.propertyHighlightHeight),
                    key);

                pipIndex++;
            }
            foreach (var key in sPropTypes.Keys)
            {
                Rect pipRect = new Rect(
                        bgWidth + LEGEND_OFFSET,
                        ITEM_INCREMENT * pipIndex,
                        dim.pipWidth,
                        dim.pipHeight);

                // border
                primitiveArray[primIndex++] = new ColorRectangle(
                    new Rect(
                        pipRect.X - dim.borderThickness,
                        pipRect.Y - dim.borderThickness,
                        pipRect.Width + dim.borderThickness * 2,
                        pipRect.Height + dim.borderThickness * 2),
                    D3DCOLOR_ARGB.BLACK);
                // pip
                primitiveArray[primIndex++] = new ColorRectangle(pipRect, Settings.FrameMeter.Palette.GetColor(FrameType.None));
                // secondary prop border
                primitiveArray[primIndex++] = new ColorRectangle(
                    new Rect(
                        pipRect.Left,
                        pipRect.Top + 1,
                        pipRect.Width,
                        dim.propertyHighlightHeight),
                    D3DCOLOR_ARGB.BLACK);
                // secondary prop
                primitiveArray[primIndex++] = new ColorRectangle(
                    new Rect(
                        pipRect.Left,
                        pipRect.Top,
                        pipRect.Width,
                        dim.propertyHighlightHeight),
                    key);

                pipIndex++;
            }

            primitives = new RentedArraySlice<ColorRectangle>(primitiveArray, 0, primIndex);
            return new(strings, 0, index);
        }
    }
}
