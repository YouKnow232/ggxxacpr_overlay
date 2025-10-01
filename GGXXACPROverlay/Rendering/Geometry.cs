using Clipper2Lib;
using Poly2Tri;
using Poly2Tri.Triangulation;
using Poly2Tri.Triangulation.Delaunay;
using Poly2Tri.Triangulation.Polygon;
using Vortice.Mathematics;

namespace GGXXACPROverlay.Rendering
{
    /// <summary>
    /// Performs geometry functions for combining hitbox
    /// </summary>
    public static class Geometry
    {
        public static RentedArraySlice<Vertex3PositionColor> GetCombinedGeometry(Span<Rect> quads, D3DCOLOR_ARGB color)
        {
            if (quads.Length == 0) return new();

            PathsD input = ToPaths(quads);
            PathsD contour = Clipper.Union(input, FillRule.NonZero);

            List<Polygon> borderPolys = new List<Polygon>(contour.Count);
            for (int i = 0; i < contour.Count; i++)
            {
                borderPolys.Add(ToPoly(contour[i], null));
            }

            RentedArraySlice<Vertex3PositionColor> output = new(borderPolys.Sum(poly => poly.Triangles.Count) * 3);
            int outIndex = 0;

            foreach (var Point in borderPolys
                    .SelectMany(static poly => poly.Triangles)
                    .SelectMany(static tri => tri.Points))
            {
                output[outIndex++] = ToColorVertex(Point, color);
            }

            return output;
        }

        public static RentedArraySlice<Vertex3PositionColor> GetBorderGeometry(Span<Rect> quads, D3DCOLOR_ARGB color, float borderSize)
        {
            if (quads.Length == 0) return new();

            PathsD input = ToPaths(quads);
            PathsD outerBorders = Clipper.Union(input, FillRule.NonZero);

            PathsD inwardBorders = Clipper.InflatePaths(outerBorders, -borderSize, JoinType.Miter, EndType.Polygon, precision: 8);
            inwardBorders = Clipper.ReversePaths(inwardBorders);

            List<Polygon> borderPolys = new List<Polygon>(outerBorders.Count);
            for (int i = 0; i < outerBorders.Count; i++)
            {
                borderPolys.Add(ToPoly(outerBorders[i], inwardBorders[i]));
            }

            RentedArraySlice<Vertex3PositionColor> output = new(borderPolys.Sum(poly => poly.Triangles.Count) * 3);
            int outIndex = 0;

            foreach (Polygon poly in borderPolys)
            {
                foreach (DelaunayTriangle tri in poly.Triangles)
                {
                    output[outIndex++] = ToColorVertex(tri.Points[0], color);
                    output[outIndex++] = ToColorVertex(tri.Points[1], color);
                    output[outIndex++] = ToColorVertex(tri.Points[2], color);
                }
            }

            return output;
        }

        private static PathsD ToPaths(Span<Rect> rectangles)
        {
            PathsD paths = [];

            foreach (Rect r in rectangles)
            {
                paths.Add(ToPath(r));
            }

            return paths;
        }

        private static PathD ToPath(Rect r)
        {
            PathD output = [];
            output.Add(new PointD(r.Left,  r.Top));
            output.Add(new PointD(r.Right, r.Top));
            output.Add(new PointD(r.Right, r.Bottom));
            output.Add(new PointD(r.Left,  r.Bottom));
            return output;
        }

        private static Polygon ToPoly(PathD outerPath, PathD? innerPath)
        {
            var outerPoints = new List<PolygonPoint>(outerPath.Count);

            foreach (PointD point in outerPath)
            {
                outerPoints.Add(new PolygonPoint(point.x, point.y));
            }

            Polygon polygon = new(outerPoints)
            {
                WindingOrder = Poly2Tri.Utility.Point2DList.WindingOrderType.Clockwise
            };

            if (innerPath is not null)
            {
                var holePoints = new List<PolygonPoint>(innerPath.Count);

                foreach (PointD point in innerPath)
                {
                    holePoints.Add(new PolygonPoint(point.x, point.y));
                }

                polygon.AddHole(new Polygon(holePoints));
            }

            P2T.Triangulate(polygon);

            return polygon;
        }

        private static Vertex3PositionColor ToColorVertex(TriangulationPoint point, D3DCOLOR_ARGB color)
            => new Vertex3PositionColor(new((float)point.X, (float)point.Y, 0f), color, new());
    }
}
