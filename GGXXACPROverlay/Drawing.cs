using System;
using System.Collections;
using GameOverlay.Drawing;
using GGXXACPROverlay.GGXXACPR;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace GGXXACPROverlay
{
    internal class Drawing
    {
        private const float LINE_THICKNESS = 1f;
        private const int LINE_THICKNESS_PX = 2;
        // Values in game pixels
        private const int PIVOT_CROSS_SIZE = 6;
        private const int FRAME_METER_Y = 400;
        private const int FRAME_METER_VERTICAL_SPACING = 1;

        public readonly struct Dimensions(int width, int height)
        {
            public readonly int Width = width;
            public readonly int Height = height;
        }
        public readonly struct PointInt(int x, int y)
        {
            public readonly int X = x;
            public readonly int Y = y;
        }

    //    internal static void BeginClipGameRegion(Graphics g, Dimensions windowDimensions)
    //    {
    //        g.ClipRegionStart(PixelToWindow(new Rectangle(-1, -1, GGXXACPR.GGXXACPR.SCREEN_WIDTH_PIXELS,
    //            GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS), windowDimensions));
    //    }
    //    internal static void EndClipGameRegion(Graphics g)
    //    {
    //        g.ClipRegionEnd();
    //    }

    //    internal static void DrawPlayerPivot(Graphics g, GraphicsResources r, GameState state, Player p, Dimensions windowDimensions)
    //    {
    //        DrawPivot(g, r, state, p.XPos, p.YPos, windowDimensions);
    //    }
    //    internal static void DrawEntityPivot(Graphics g, GraphicsResources r, GameState state, Entity e, Dimensions windowDimensions)
    //    {
    //        DrawPivot(g, r, state, e.XPos, e.YPos, windowDimensions);
    //    }
    //    private static void DrawPivot(Graphics g, GraphicsResources r, GameState state, int x, int y, Dimensions windowDimensions)
    //    {
    //        PointInt coor = PixelToWindow(WorldToPixel(new PointInt(x, y), state.Camera), windowDimensions);
    //        // GameOverlay.Net Lines seem to not include the starting point pixel. The line defintions are extended to compensate.
    //        var outline1 = new Line(coor.X - PIVOT_CROSS_SIZE - 2, coor.Y, coor.X + PIVOT_CROSS_SIZE + 1, coor.Y);
    //        var outline2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 2, coor.X, coor.Y + PIVOT_CROSS_SIZE + 1);
    //        var line1 = new Line(coor.X - PIVOT_CROSS_SIZE - 1, coor.Y, coor.X + PIVOT_CROSS_SIZE, coor.Y);
    //        var line2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 1, coor.X, coor.Y + PIVOT_CROSS_SIZE);
    //        g.DrawLine(r.GetBrush(GeneralPalette.BLACK), outline1, LINE_THICKNESS * 3);
    //        g.DrawLine(r.GetBrush(GeneralPalette.BLACK), outline2, LINE_THICKNESS * 3);
    //        g.DrawLine(r.GetBrush(GeneralPalette.WHITE), line1, LINE_THICKNESS);
    //        g.DrawLine(r.GetBrush(GeneralPalette.WHITE), line2, LINE_THICKNESS);
    //    }
    //    internal static void DrawEntityCore(Graphics g, GraphicsResources r, GameState state, Entity e, Dimensions windowDimensions)
    //    {
    //        PointInt coor = PixelToWindow(WorldToPixel(new PointInt(e.XPos + (e.CoreX * 100), e.YPos + (e.CoreY * 100)), state.Camera), windowDimensions);
    //        var line1 = new Line(coor.X - PIVOT_CROSS_SIZE - 1, coor.Y, coor.X + PIVOT_CROSS_SIZE, coor.Y);
    //        var line2 = new Line(coor.X, coor.Y - PIVOT_CROSS_SIZE - 1, coor.X, coor.Y + PIVOT_CROSS_SIZE);
    //        g.DrawLine(r.GetBrush(GeneralPalette.WHITE), line1, LINE_THICKNESS);
    //        g.DrawLine(r.GetBrush(GeneralPalette.WHITE), line2, LINE_THICKNESS);
    //    }
    //    internal static void DrawPlayerPushBox(Graphics g, GraphicsResources r, GameState state, Player player, Dimensions windowDimensions)
    //    {
    //        var mappedRect = MapHitboxToPlayerOrigin(player.PushBox, player);
    //        var drawRect = PixelToWindow(WorldToPixel(mappedRect, state.Camera), windowDimensions);

    //        if (player.Status.NoCollision)
    //        {
    //            g.DrawRectangle_InwardPixelBorder(
    //                r.GetBrush(GeneralPalette.COLLISION),
    //                drawRect,
    //                LINE_THICKNESS_PX);
    //        }
    //        else
    //        {
    //            g.OutlineFillRectangle_InwardPixelBorder(
    //                r.GetBrush(GeneralPalette.COLLISION),
    //                r.GetFillBrush(GeneralPalette.COLLISION),
    //                drawRect,
    //                LINE_THICKNESS_PX);
    //        }

    //        var borderThicknessYAdjust = (LINE_THICKNESS_PX) / 2;
    //        var underLine = new Line(drawRect.Left - 1, drawRect.Bottom - borderThicknessYAdjust,
    //            drawRect.Right, drawRect.Bottom - borderThicknessYAdjust);

    //        g.DrawLine(
    //            r.GetBrush(GeneralPalette.YELLOW),
    //            underLine,
    //            LINE_THICKNESS_PX
    //        );
    //    }
    //    internal static void DrawPlayerGrabBox(Graphics g, GraphicsResources r, GameState state, Player player, Dimensions windowDimensions, bool drawOverride)
    //    {
    //        if (DrawPlayerCommandGrabBox(g, r, state, player, windowDimensions)) { return; }
    //        if (player.CommandFlags.DisableThrow) { return; }

    //        if (state.GlobalFlags.ThrowFlags.Player1ThrowSuccess && player.PlayerIndex == 0 ||
    //            state.GlobalFlags.ThrowFlags.Player2ThrowSuccess && player.PlayerIndex == 1 ||
    //            drawOverride)
    //        {
    //            Hitbox throwBox = GGXXACPR.GGXXACPR.GetThrowBox(state, player);
    //            Rectangle mappedRect = MapHitboxToPlayerOrigin(throwBox, player);
    //            Rectangle drawRect = PixelToWindow(WorldToPixel(mappedRect, state.Camera), windowDimensions);

    //            g.OutlineFillRectangle_InwardPixelBorder(
    //                r.GetBrush(GeneralPalette.GRAB),
    //                r.GetFillBrush(GeneralPalette.GRAB),
    //                drawRect,
    //                LINE_THICKNESS_PX);
    //        }
    //    }
    //    private static bool DrawPlayerCommandGrabBox(Graphics g, GraphicsResources r, GameState state, Player player, Dimensions windowDimensions)
    //    {
    //        if ((player.Mark == 1) && MoveData.IsActiveByMark(player.CharId, player.ActionId))
    //        {
    //            var cmdThrowRange = MoveData.GetCommandGrabRange(player.CharId, player.ActionId);
    //            Hitbox cmdThrowHitboxRep = new()
    //            {
    //                XOffset = (short)(player.PushBox.XOffset - cmdThrowRange / 100),
    //                YOffset = player.PushBox.YOffset,
    //                Width   = (short)(player.PushBox.Width + cmdThrowRange * 2 / 100),
    //                Height  = player.PushBox.Height
    //            };

    //            Rectangle mappedRect = MapHitboxToPlayerOrigin(cmdThrowHitboxRep, player);
    //            Rectangle drawRect = PixelToWindow(WorldToPixel(mappedRect, state.Camera), windowDimensions);

    //            g.OutlineFillRectangle_InwardPixelBorder(
    //                r.GetBrush(GeneralPalette.GRAB),
    //                r.GetFillBrush(GeneralPalette.GRAB),
    //                drawRect,
    //                LINE_THICKNESS_PX);
    //            return true;
    //        }
    //        return false;
    //    }

    //    private static Rectangle MapHitboxToPlayerOrigin(Hitbox hitbox, Player player)
    //    {
    //        return MapHitboxToOrigin(hitbox, player.IsFacingRight, player.XPos, player.YPos);
    //    }
    //    private static Rectangle MapHitboxToEntityOrigin(Hitbox hitbox, Entity e)
    //    {
    //        return MapHitboxToOrigin(hitbox, e.IsFacingRight, e.XPos, e.YPos);
    //    }
    //    private static Rectangle MapHitboxToOrigin(Hitbox hitbox, bool isFacingRight, int xPos, int yPos)
    //    {
    //        var offset = isFacingRight ? ((hitbox.XOffset + hitbox.Width) * -100) : (hitbox.XOffset * 100);
    //        return new Rectangle(
    //            xPos + offset,
    //            yPos + hitbox.YOffset * 100,
    //            xPos + offset + hitbox.Width * 100,
    //            yPos + (hitbox.YOffset + hitbox.Height) * 100);
    //    }

    //    internal static void DrawPlayerBoxes(Graphics g, GraphicsResources r, BoxId[] drawList, GameState state, Player player, Dimensions windowDimensions)
    //    {
    //        if (player.HitboxSet == null) return;

    //        foreach (Hitbox hitbox in player.HitboxSet)
    //        {
    //            if (hitbox.BoxTypeId == BoxId.HURT && (
    //                    player.Extra.InvulnCounter > 0 ||
    //                    player.Status.DisableHurtboxes ||
    //                    player.Status.StrikeInvuln
    //                ) ||
    //                // hitboxes are technically disabled in recovery state, but we're going to draw them during hitstop anyway
    //                (hitbox.BoxTypeId == BoxId.HIT) && (
    //                    player.Status.DisableHitboxes &&
    //                    !(player.HitstopCounter > 0 &&
    //                    player.AttackFlags.HasConnected)    // Hitstop counter is also used in super flash, so need to check attack flags as well
    //                ) ||
    //                !drawList.Contains(hitbox.BoxTypeId))
    //            {
    //                continue;
    //            }

    //            Hitbox drawbox = ScaleHitbox(hitbox, player);
    //            Rectangle mappedRect = MapHitboxToPlayerOrigin(drawbox, player);
    //            Rectangle drawRect = PixelToWindow(WorldToPixel(mappedRect, state.Camera), windowDimensions);

    //            g.OutlineFillRectangle_InwardPixelBorder(
    //                r.GetOutlineBrush(drawbox.BoxTypeId),
    //                r.GetFillBrush(drawbox.BoxTypeId),
    //                drawRect,
    //                LINE_THICKNESS_PX);
    //        }
    //    }

    //    internal static void DrawProjectileBoxes(Graphics g, GraphicsResources r, BoxId[] drawList, GameState state, Dimensions windowDimensions)
    //    {
    //        Hitbox drawbox;
    //        Rectangle mappedRect, drawRect;
    //        foreach (Entity e in state.Entities)
    //        {
    //            foreach(Hitbox hitbox in e.HitboxSet)
    //            {
    //                if (e.Status.DisableHitboxes && hitbox.BoxTypeId == BoxId.HIT ||
    //                    e.Status.DisableHurtboxes && hitbox.BoxTypeId == BoxId.HURT ||
    //                    !drawList.Contains(hitbox.BoxTypeId))
    //                { continue; }

    //                drawbox = ScaleHitbox(hitbox, e);
    //                mappedRect = MapHitboxToEntityOrigin(drawbox, e);
    //                drawRect = PixelToWindow(WorldToPixel(mappedRect, state.Camera), windowDimensions);

    //                g.OutlineFillRectangle_InwardPixelBorder(
    //                    r.GetOutlineBrush(drawbox.BoxTypeId),
    //                    r.GetFillBrush(drawbox.BoxTypeId),
    //                    drawRect,
    //                    LINE_THICKNESS_PX);
    //            }
    //            DrawEntityPivot(g, r, state, e, windowDimensions);
    //        }
    //    }

    //    private const int FRAME_METER_Y_ALT = 90;
    //    private const int FRAME_METER_BASE_LINE_X = 5;
    //    internal static void DrawFrameMeter(Graphics g, GraphicsResources r, FrameMeter frameMeter, Dimensions windowDimensions)
    //    {
    //        FrameMeter.Frame[] frameArr;
    //        Rectangle rect, frameRect;
    //        Line line;
    //        int frameMeterLength = frameMeter.PlayerMeters[0].FrameArr.Length;

    //        int screenWidth = windowDimensions.Height * 4 / 3;

    //        int pipSpacing = (screenWidth - FRAME_METER_BASE_LINE_X * 2) / frameMeterLength;
    //        int pipWidth = pipSpacing - 1;
    //        int totalWidth = (pipSpacing * frameMeterLength) - 1;
    //        int pipHeight = pipWidth * 5 / 3;
    //        int entityPipHeight = pipWidth;
    //        int coreYPos = windowDimensions.Height * FRAME_METER_Y / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;
    //        int yPos = coreYPos - pipHeight - 1;
    //        int xPos = (windowDimensions.Width - totalWidth) / 2;
    //        int propertyHighlightHeight = pipHeight * 2 / 7;
    //        propertyHighlightHeight += propertyHighlightHeight == 0 ? 1 : 0;
    //        int propertyHighlightTop = pipHeight - propertyHighlightHeight;
    //        int borderThickness = 2 * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;

    //        // Border
    //        g.FillRectangle(r.GetBrush(GeneralPalette.BLACK), new Rectangle(
    //            xPos - borderThickness,
    //            yPos - borderThickness,
    //            xPos + totalWidth + borderThickness,
    //            yPos + 2 * pipHeight + FRAME_METER_VERTICAL_SPACING + borderThickness
    //        ));

    //        // Players
    //        for (int j = 0; j < frameMeter.PlayerMeters.Length; j++)
    //        {
    //            frameArr = frameMeter.PlayerMeters[j].FrameArr;
    //            for (int i = 0; i < frameArr.Length; i++)
    //            {
    //                frameRect = new Rectangle(
    //                    xPos + (i * pipSpacing),
    //                    yPos + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight)),
    //                    xPos + pipWidth + (i * pipSpacing),
    //                    yPos + pipHeight + (j * (FRAME_METER_VERTICAL_SPACING + pipHeight))
    //                );
    //                g.FillRectangle(r.GetBrush(frameArr[i].Type), frameRect);
    //                if (frameArr[i].SecondaryProperty != FrameMeter.SecondaryFrameProperty.Default)
    //                {
    //                    rect = new Rectangle(
    //                        frameRect.Left,
    //                        frameRect.Top,
    //                        frameRect.Right,
    //                        frameRect.Top + propertyHighlightHeight
    //                    );
    //                    g.FillRectangle(r.GetBrush(frameArr[i].SecondaryProperty), rect);
    //                    line = new Line(
    //                        frameRect.Left,
    //                        frameRect.Top + propertyHighlightHeight,
    //                        frameRect.Right,
    //                        frameRect.Top + propertyHighlightHeight
    //                    );
    //                    g.DrawLine(r.GetBrush(GeneralPalette.BLACK), line, LINE_THICKNESS);
    //                }
    //                if (frameArr[i].PrimaryProperty1 != FrameMeter.PrimaryFrameProperty.Default)
    //                {
    //                    rect = new Rectangle(
    //                        frameRect.Left,
    //                        frameRect.Top + propertyHighlightTop,
    //                        frameRect.Right,
    //                        frameRect.Bottom
    //                    );
    //                    g.FillRectangle(r.GetBrush(frameArr[i].PrimaryProperty1), rect);
    //                    line = new Line(
    //                        frameRect.Left,
    //                        frameRect.Top + propertyHighlightTop,
    //                        frameRect.Right,
    //                        frameRect.Top + propertyHighlightTop
    //                    );
    //                    g.DrawLine(r.GetBrush(GeneralPalette.BLACK), line, LINE_THICKNESS);
    //                    if (frameArr[i].PrimaryProperty2 != FrameMeter.PrimaryFrameProperty.Default)
    //                    {
    //                        rect = new Rectangle(
    //                            frameRect.Left + pipWidth / 2,
    //                            frameRect.Top + propertyHighlightTop,
    //                            frameRect.Right,
    //                            frameRect.Bottom
    //                        );
    //                        g.FillRectangle(r.GetBrush(frameArr[i].PrimaryProperty2), rect);
    //                    }
    //                }
    //            }
    //        }

    //        // Entity
    //        if (!frameMeter.EntityMeters[0].Hide)
    //        {
    //            // Border
    //            g.FillRectangle(r.GetBrush(GeneralPalette.BLACK), new Rectangle(
    //                xPos - borderThickness,
    //                yPos - borderThickness - FRAME_METER_VERTICAL_SPACING - entityPipHeight,
    //                xPos + totalWidth + borderThickness,
    //                yPos - FRAME_METER_VERTICAL_SPACING
    //            ));
    //            for (int i=0; i < frameMeter.EntityMeters[0].FrameArr.Length; i++)
    //            {
    //                rect = new Rectangle(
    //                    xPos + (i * pipSpacing),
    //                    yPos - FRAME_METER_VERTICAL_SPACING - entityPipHeight,
    //                    xPos + pipWidth + (i * pipSpacing),
    //                    yPos - FRAME_METER_VERTICAL_SPACING
    //                );
    //                g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[0].FrameArr[i].Type), rect);
    //            }
    //        }
    //        if (!frameMeter.EntityMeters[1].Hide)
    //        {
    //            // Border
    //            g.FillRectangle(r.GetBrush(GeneralPalette.BLACK), new Rectangle(
    //                xPos - borderThickness,
    //                yPos + 2 * (pipHeight + FRAME_METER_VERTICAL_SPACING),
    //                xPos + totalWidth + borderThickness,
    //                yPos + borderThickness + entityPipHeight + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight)
    //            ));
    //            for (int i = 0; i < frameMeter.EntityMeters[1].FrameArr.Length; i++)
    //                {
    //                rect = new Rectangle(
    //                    xPos + (i * pipSpacing),
    //                    yPos + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight),
    //                    xPos + pipWidth + (i * pipSpacing),
    //                    yPos + entityPipHeight + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight)
    //                );
    //                g.FillRectangle(r.GetBrush(frameMeter.EntityMeters[1].FrameArr[i].Type), rect);
    //            }
    //        }

    //        // Labels
    //        var p1LabelPosition = new Point(xPos + pipWidth, yPos - FRAME_METER_VERTICAL_SPACING -
    //            entityPipHeight - borderThickness + 4 - (r.Font.FontSize * 3 / 2));
    //        var p2LabelPosition = new Point(xPos + pipWidth, yPos + 2 * (FRAME_METER_VERTICAL_SPACING + pipHeight) +
    //            entityPipHeight + borderThickness + -4);

    //        // Semi-transparent label backgrounds
    //        var startupDimensions = g.MeasureString(r.Font, "S: 99  A: -99");
    //        g.FillRectangle(r.GetBrush(GeneralPalette.LABEL_BG), new Rectangle(p1LabelPosition.X - r.Font.FontSize / 2, p1LabelPosition.Y,
    //            p1LabelPosition.X + startupDimensions.X + r.Font.FontSize * 3 / 4, p1LabelPosition.Y + startupDimensions.Y + r.Font.FontSize / 8));
    //        g.FillRectangle(r.GetBrush(GeneralPalette.LABEL_BG), new Rectangle(p2LabelPosition.X - r.Font.FontSize / 2, p2LabelPosition.Y,
    //            p2LabelPosition.X + startupDimensions.X + r.Font.FontSize * 3 / 4, p2LabelPosition.Y + startupDimensions.Y + r.Font.FontSize / 8));

    //        // Startup
    //        DrawOutlinedText(g, r.Font, r.GetBrush(GeneralPalette.WHITE), r.GetBrush(GeneralPalette.BLACK), p1LabelPosition,
    //            $"S: {(frameMeter.PlayerMeters[0].Startup >= 0 ? frameMeter.PlayerMeters[0].Startup : "-")}");

    //        DrawOutlinedText(g, r.Font, r.GetBrush(GeneralPalette.WHITE), r.GetBrush(GeneralPalette.BLACK), p2LabelPosition,
    //            $"S: {(frameMeter.PlayerMeters[1].Startup >= 0 ? frameMeter.PlayerMeters[1].Startup : "-")}");


    //        // Advantage
    //        var display = frameMeter.PlayerMeters[0].DisplayAdvantage;
    //        IBrush p1AdvantageFontColor = r.GetBrush(GeneralPalette.WHITE);
    //        IBrush p2AdvantageFontColor = r.GetBrush(GeneralPalette.WHITE);
    //        if (display && frameMeter.PlayerMeters[0].Advantage > 0)
    //        {
    //            p1AdvantageFontColor = r.GetBrush(GeneralPalette.GREEN);
    //            p2AdvantageFontColor = r.GetBrush(GeneralPalette.RED);
    //        }
    //        else if (display && frameMeter.PlayerMeters[0].Advantage < 0)
    //        {
    //            p1AdvantageFontColor = r.GetBrush(GeneralPalette.RED);
    //            p2AdvantageFontColor = r.GetBrush(GeneralPalette.GREEN);
    //        }

    //        Point p1AdvLabelPosition = new Point(p1LabelPosition.X + (r.Font.FontSize * 3), p1LabelPosition.Y);
    //        Point p2AdvLabelPosition = new Point(p2LabelPosition.X + (r.Font.FontSize * 3), p2LabelPosition.Y);

    //        DrawOutlinedText(g, r.Font, p1AdvantageFontColor, r.GetBrush(GeneralPalette.BLACK), p1AdvLabelPosition,
    //            $"A: {(display ? frameMeter.PlayerMeters[0].Advantage : "-")}");
    //        DrawOutlinedText(g, r.Font, p2AdvantageFontColor, r.GetBrush(GeneralPalette.BLACK), p2AdvLabelPosition,
    //            $"A: {(display ? frameMeter.PlayerMeters[1].Advantage : "-")}");
    //    }

    //    private const int LEGEND_Y_POS_PX = 120;
    //    private const int LEGEND_ASSUMED_FRAME_METER_SIZE = 80;
    //    private const int LEGEND_MAX_COLUMN_SIZE = 6;
    //    internal static void DrawFrameMeterLegend(Graphics g, GraphicsResources r, Dimensions windowDimensions)
    //    {
    //        int screenWidth = windowDimensions.Height * 4 / 3;

    //        int pipSpacing = (screenWidth - FRAME_METER_BASE_LINE_X * 2) / LEGEND_ASSUMED_FRAME_METER_SIZE;
    //        int pipWidth = pipSpacing - 1;
    //        int totalWidth = (pipSpacing * LEGEND_ASSUMED_FRAME_METER_SIZE) - 1;
    //        int pipHeight = pipWidth * 5 / 3;
    //        int yPos = LEGEND_Y_POS_PX * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;
    //        int xPos = (windowDimensions.Width - totalWidth) / 2;
    //        int propertyHighlightHeight = pipHeight * 2 / 7;
    //        propertyHighlightHeight += propertyHighlightHeight == 0 ? 1 : 0;
    //        int propertyHighlightTop = pipHeight - propertyHighlightHeight;
    //        int borderThickness = 2 * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS;

    //        int legendEntryVerticalSpacing = (int)Math.Max((r.LegendFont.FontSize * 2), pipHeight + borderThickness * 2 + 2);
    //        int legendColumnSpacing = (int)(r.LegendFont.FontSize * 12);

    //        Rectangle rect;
    //        Rectangle frameRect;
    //        Line line;
    //        LegendEntry[] entries = GraphicsResources.GetLegend();

    //        // Background
    //        rect = new Rectangle(xPos - r.LegendFont.FontSize, yPos - r.LegendFont.FontSize,
    //            xPos + pipWidth * 2 + legendColumnSpacing + r.LegendFont.FontSize * 12,
    //            yPos + legendEntryVerticalSpacing * 5 + pipHeight + r.LegendFont.FontSize);
    //        g.FillRectangle(r.GetBrush(GeneralPalette.LABEL_BG), rect);

    //        for (int i = 0; i < entries.Length; i++)
    //        {
    //            frameRect = new Rectangle(
    //                xPos + ((i / LEGEND_MAX_COLUMN_SIZE) * legendColumnSpacing),
    //                yPos + ((i % LEGEND_MAX_COLUMN_SIZE) * legendEntryVerticalSpacing),
    //                xPos + pipWidth + ((i / LEGEND_MAX_COLUMN_SIZE) * legendColumnSpacing),
    //                yPos + pipHeight + ((i % LEGEND_MAX_COLUMN_SIZE) * legendEntryVerticalSpacing)
    //            );
    //            // Border
    //            rect = new Rectangle(frameRect.Left - borderThickness, frameRect.Top - borderThickness,
    //                frameRect.Right + borderThickness, frameRect.Bottom + borderThickness);
    //            g.FillRectangle(r.GetBrush(GeneralPalette.BLACK), rect);

    //            g.FillRectangle(r.GetBrush(entries[i].ExampleFrame.Type), frameRect);
    //            if (entries[i].ExampleFrame.SecondaryProperty != FrameMeter.SecondaryFrameProperty.Default)
    //            {
    //                rect = new Rectangle(
    //                    frameRect.Left,
    //                    frameRect.Top,
    //                    frameRect.Right,
    //                    frameRect.Top + propertyHighlightHeight
    //                );
    //                g.FillRectangle(r.GetBrush(entries[i].ExampleFrame.SecondaryProperty), rect);
    //                line = new Line(
    //                    frameRect.Left,
    //                    frameRect.Top + propertyHighlightHeight,
    //                    frameRect.Right,
    //                    frameRect.Top + propertyHighlightHeight
    //                );
    //                g.DrawLine(r.GetBrush(GeneralPalette.BLACK), line, LINE_THICKNESS);
    //            }
    //            if (entries[i].ExampleFrame.PrimaryProperty1 != FrameMeter.PrimaryFrameProperty.Default)
    //            {
    //                rect = new Rectangle(
    //                    frameRect.Left,
    //                    frameRect.Top + propertyHighlightTop,
    //                    frameRect.Right,
    //                    frameRect.Bottom
    //                );
    //                g.FillRectangle(r.GetBrush(entries[i].ExampleFrame.PrimaryProperty1), rect);
    //                line = new Line(
    //                    frameRect.Left,
    //                    frameRect.Top + propertyHighlightTop,
    //                    frameRect.Right,
    //                    frameRect.Top + propertyHighlightTop
    //                );
    //                g.DrawLine(r.GetBrush(GeneralPalette.BLACK), line, LINE_THICKNESS);
    //            }

    //            // Draw label
    //            var pos = new Point(frameRect.Right + r.LegendFont.FontSize, frameRect.Top);
    //            g.DrawOutlinedText(r.LegendFont, r.GetBrush(GeneralPalette.WHITE), r.GetBrush(GeneralPalette.BLACK),
    //                pos, entries[i].Label);
                    
    //        }
    //    }

    //    public static void DrawSettingsLabels(Graphics g, GraphicsResources r, OverlaySettings settings)
    //    {
    //        ArrayList labels = [];
    //        if (settings.RecordDuringHitstop) labels.Add("Hitstop");
    //        if (settings.RecordDuringSuperFlash) labels.Add("Superflash");

    //        if (labels.Count == 0) return;

    //        var label = String.Join(", ", labels.ToArray());

    //        g.DrawTextWithBackground(r.LegendFont, r.GetBrush(GeneralPalette.WHITE), r.GetBrush(GeneralPalette.LABEL_BG), new Point(), label);
    //    }

    //    // Kinda hacky text outline. Might make a proper text renderer later.
    //    private static void DrawOutlinedText(Graphics g, Font font, IBrush fillBrush, IBrush outlineBrush, Point location, string text)
    //    {
    //        Point[] outlineOffsets =
    //        [
    //            new Point(location.X, location.Y-1),
    //            new Point(location.X-1, location.Y),
    //            new Point(location.X+1, location.Y),
    //            new Point(location.X, location.Y+1),
    //        ];

    //        foreach(Point p in outlineOffsets)
    //        {
    //            g.DrawText(font, outlineBrush, p, text);
    //        }

    //        g.DrawText(font, fillBrush, location, text);
    //    }

    //    internal static int FlipVector(Player p) { return p.IsFacingRight ? -1 : 1; }
    //    internal static int FlipVector(Entity proj) { return proj.IsFacingRight ? -1 : 1; }

    //    // Converts the game coordinate space [0,-48000]:[64000:0] to pixel grid coordinate space [0,0]:[639,479]
    //    private static PointInt WorldToPixel(PointInt p, Camera cam)
    //    {
    //        float z = cam.Zoom;
    //        int x = (int)Math.Floor((p.X - cam.LeftEdge) * z / 100);
    //        int y = (int)Math.Floor((p.Y - cam.BottomEdge) * z / 100);
    //        y = y + GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS - GGXXACPR.GGXXACPR.SCREEN_GROUND_PIXEL_OFFSET;
    //        return new PointInt(x, y);
    //    }
    //    private static Rectangle WorldToPixel(Rectangle rect, Camera cam)
    //    {
    //        float z = cam.Zoom;
    //        int x1 = (int)Math.Floor((rect.Left - cam.LeftEdge) * z / 100);
    //        int y1 = (int)Math.Floor((rect.Top - cam.BottomEdge) * z / 100);
    //        int x2 = (int)Math.Floor((rect.Right - cam.LeftEdge) * z / 100);
    //        int y2 = (int)Math.Floor((rect.Bottom - cam.BottomEdge) * z / 100);
    //        y1 += GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS - GGXXACPR.GGXXACPR.SCREEN_GROUND_PIXEL_OFFSET;
    //        y2 += GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS - GGXXACPR.GGXXACPR.SCREEN_GROUND_PIXEL_OFFSET;
    //        return new Rectangle(x1, y1, x2, y2);
    //    }

    //    // Converts the game's pixel grid coordinate space [0,0]:[639,479] to the game window coordinate space [1,1]:[window width, window height]
    //    private static PointInt PixelToWindow(PointInt coor, Dimensions windowDimensions)
    //    {
    //        // Should math out to zero if not in widescreen
    //        int wideScreenOffset = (windowDimensions.Width - (windowDimensions.Height * 4 / 3)) / 2;

    //        // Adding 1 because top left pixel is [1,1] for graphics functions but it's [0,0] for game coordinates
    //        return new PointInt(
    //            (int)Math.Floor(1.0f * (coor.X + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
    //            (int)Math.Floor(1.0f * (coor.Y + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS)
    //        );
    //    }
    //    private static Rectangle PixelToWindow(Rectangle r, Dimensions windowDimensions)
    //    {
    //        // Should math out to zero if not in widescreen
    //        int wideScreenOffset = (windowDimensions.Width - (windowDimensions.Height * 4 / 3)) / 2;

    //        // Adding 1 because top left pixel is [1,1] for graphics functions but it's [0,0] for game coordinates
    //        return new Rectangle(
    //            ((r.Left + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
    //            (r.Top + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS,
    //            ((r.Right + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS) + wideScreenOffset,
    //            (r.Bottom + 1) * windowDimensions.Height / GGXXACPR.GGXXACPR.SCREEN_HEIGHT_PIXELS
    //        );
    //    }

    //    private static Hitbox ScaleHitbox(Hitbox hitbox, Player p)
    //    {
    //        if (p.ScaleX < 0 && p.ScaleY < 0) { return hitbox; }
    //        // If scale var is -1, apply the other var to both dimensions
    //        var scaleY = p.ScaleY < 0 ? p.ScaleX : p.ScaleY;
    //        var scaleX = p.ScaleX < 0 ? p.ScaleY : p.ScaleX;

    //        return new Hitbox()
    //        {
    //            XOffset = (short)Math.Floor(hitbox.XOffset * scaleX / 1000f),
    //            YOffset = (short)Math.Floor(hitbox.YOffset * scaleY / 1000f),
    //            Width = (short)Math.Floor(hitbox.Width * scaleX / 1000f),
    //            Height = (short)Math.Floor(hitbox.Height * scaleY / 1000f),
    //            BoxTypeId = hitbox.BoxTypeId,
    //            BoxFlags = hitbox.BoxFlags
    //        };
    //    }
    //    private static Hitbox ScaleHitbox(Hitbox hitbox, Entity e)
    //    {
    //        if (e.ScaleX < 0 && e.ScaleY < 0) { return hitbox; }
    //        // If scale var is -1, apply the other var to both dimensions
    //        var scaleY = e.ScaleY < 0 ? e.ScaleX : e.ScaleY;
    //        var scaleX = e.ScaleX < 0 ? e.ScaleY : e.ScaleX;

    //        return new Hitbox()
    //        {
    //            XOffset = (short)(hitbox.XOffset * scaleX / 1000),
    //            YOffset = (short)(hitbox.YOffset * scaleY / 1000),
    //            Width = (short)(hitbox.Width * scaleX / 1000),
    //            Height = (short)(hitbox.Height * scaleY / 1000),
    //            BoxTypeId = hitbox.BoxTypeId,
    //            BoxFlags = hitbox.BoxFlags
    //        };
    //    }
    //}

    //internal static class GraphicsExtensions
    //{

    //    /// <summary>
    //    /// Creates a new rectangle smaller on all sides by n pixels.
    //    /// This is used to counteract dimensions expansion from border thickness.
    //    /// </summary>
    //    private static Rectangle Shrink(Rectangle r, float n)
    //    {
    //        return new Rectangle(
    //            r.Left + n,
    //            r.Top + n,
    //            r.Right - n,
    //            r.Bottom - n
    //        );
    //    }

    //    /// <summary>
    //    /// Wrapper for g.OutlineFillRectangle. Stroke defines the pixel width of the border which extends inwards so as to maintain the given rectangle dimensions.
    //    /// </summary>
    //    /// <param name="g"></param>
    //    /// <param name="outline"></param>
    //    /// <param name="fill"></param>
    //    /// <param name="rectangle"></param>
    //    /// <param name="stroke">Border length in px</param>
    //    public static void OutlineFillRectangle_InwardPixelBorder(this Graphics g, IBrush outline, IBrush fill, Rectangle rectangle, int stroke)
    //    {
    //        if (!g.IsDrawing) throw new InvalidOperationException("Use Begin Scene");

    //        var r = Shrink(rectangle, (stroke - 1) / 2f);

    //        var _factory = g.GetFactory();
    //        var _device = g.GetRenderTarget();

    //        var rectangleGeometry = new RectangleGeometry(_factory,
    //            new RawRectangleF(r.Left, r.Top, r.Right, r.Bottom));

    //        var geometry = new PathGeometry(_factory);

    //        var sink = geometry.Open();

    //        rectangleGeometry.Outline(sink);

    //        sink.Close();

    //        _device.FillGeometry(geometry, fill.Brush);
    //        _device.DrawGeometry(geometry, outline.Brush, stroke);

    //        sink.Dispose();
    //        geometry.Dispose();
    //        rectangleGeometry.Dispose();
    //    }

    //    /// <summary>
    //    /// Wrapper for g.DrawRectangle. Border defined by stroke will expand inward.
    //    /// </summary>
    //    /// <param name="g"></param>
    //    /// <param name="brush"></param>
    //    /// <param name="rectangle"></param>
    //    /// <param name="stroke">Border length in px</param>
    //    public static void DrawRectangle_InwardPixelBorder(this Graphics g, IBrush brush, Rectangle rectangle, int stroke)
    //    {
    //        var r = Shrink(rectangle, (stroke - 1) / 2f);
    //        g.DrawRectangle(brush, r, stroke);
    //    }

    //    // Kinda hacky text outline. Might make a proper text renderer later.
    //    public static void DrawOutlinedText(this Graphics g, Font font, IBrush fillBrush, IBrush outlineBrush, Point location, string text)
    //    {
    //        Point[] outlineOffsets =
    //        [
    //            new Point(location.X, location.Y-1),
    //            new Point(location.X-1, location.Y),
    //            new Point(location.X+1, location.Y),
    //            new Point(location.X, location.Y+1),
    //        ];

    //        foreach (Point p in outlineOffsets)
    //        {
    //            g.DrawText(font, outlineBrush, p, text);
    //        }

    //        g.DrawText(font, fillBrush, location, text);
    //    }
    }
}
