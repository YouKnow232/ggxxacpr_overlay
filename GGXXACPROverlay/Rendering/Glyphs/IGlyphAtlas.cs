using System.Collections;
using System.Drawing;
using System.Numerics;
using System.Text;
using Vortice.Mathematics;

namespace GGXXACPROverlay.Rendering.Glyphs
{
    /// <summary>
    /// Exposes a glyph atlas texture and a mapping of characters to Glyphs (individual character sprite textures).
    /// </summary>
    public interface IGlyphAtlas
    {
        public Bitmap Bitmap { get; }
        public Glyph ToGlyph(char c);
    }

    /// <summary>
    /// A sprite texture depicting a character.
    /// </summary>
    /// <param name="character">Represented character</param>
    /// <param name="bounds">Glyph boundary rectangle in UV coordinates</param>
    /// <param name="atlas">Parent Atlas</param>
    public readonly struct Glyph(char character, Rect bounds, IGlyphAtlas atlas)
    {
        public readonly char Character = character;
        public readonly Rect Bounds = bounds;
        public readonly IGlyphAtlas Atlas = atlas;
    }
    public static class Extensions
    {
        public static Vector2 CalculateBounds(this Glyph[] glyphString)
        {
            Vector2 output = Vector2.Zero;

            foreach(Rect bounds in glyphString.Select(g => g.Bounds))
            {
                output.X += bounds.Y;
                output.Y = Math.Max(output.X, bounds.X);
            }

            return output;
        }
    }
    /// <summary>
    /// Encapsulates an array of Glyphs. These Glyphs all share the same atlas.
    /// </summary>
    public struct GlyphString : IEnumerable<Glyph>
    {
        private readonly Glyph[] glyphs;

        public GlyphString(string text, IGlyphAtlas atlas, Vector2 position, float size, D3DCOLOR_ARGB color = default, float spacing = 0f)
        {
            int index = 0;
            glyphs = new Glyph[text.Length];
            foreach(char c in text)
            {
                glyphs[index++] = atlas.ToGlyph(c);
            }

            Position = position;
            Size = size;
            Color = color;
            Spacing = spacing;
        }
        public GlyphString(Glyph[] glyphs, Vector2 position, float size, D3DCOLOR_ARGB color = default, float spacing = 0f)
        {
            this.glyphs = glyphs ?? [];
            Position = position;
            Size = size;
            Color = color;
            Spacing = spacing;
        }

        public readonly int Length => glyphs.Length;
        public readonly string Text
        { 
            get
            {
                StringBuilder builder = new StringBuilder();

                foreach (var c in glyphs.Select(glyph => glyph.Character))
                {
                    builder.Append(c);
                }

                return builder.ToString();
            }
        }
        public readonly Rect Bounds
        {
            get
            {
                Rect output = new Rect(Position.X, Position.Y, 0, Size);

                foreach (Glyph g in glyphs)
                {
                    output.Width += (g.Bounds.Width / g.Bounds.Height * Size);
                }

                return output;
            }
        }
        public Vector2 Position { get; set; }
        public float Size { get; set; }
        public D3DCOLOR_ARGB Color { get; set; }
        public float Spacing { get; set; }

        public readonly IEnumerator<Glyph> GetEnumerator()
        {
            for (int i = 0; i < glyphs.Length; i++)
            {
                yield return glyphs[i];
            }
        }
        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
