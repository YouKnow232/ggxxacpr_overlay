using System.Drawing;
using System.Reflection;
using Vortice.Mathematics;

namespace GGXXACPROverlay.Rendering.Glyphs
{
    public class ArialGlyphAtlas : IGlyphAtlas
    {
        private const string _embeddedResourceName = "GGXXACPROverlay.Arial.png";
        private const float CELL_SIZE = 0.1f;
        private const float GLYPH_X_OFFSET = 0.01f;
        private const float GLYPH_Y_OFFSET = 0.0075f;
        private const float GLYPH_WIDTH = 0.06f;
        private const float GLYPH_HEIGHT = 0.09f;

        public Bitmap Bitmap
        {
            get
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using Stream stream = asm.GetManifestResourceStream(_embeddedResourceName)
                    ?? throw new FileNotFoundException($"Couldn't find the resource '{_embeddedResourceName}'");

                return new Bitmap(stream);
            }
        }

        public Glyph ToGlyph(char c)
        {
            const int atlasStart = 33;

            int charValue = c - atlasStart;

            if (charValue is < 0 or > 93 && c is not ' ')
            {
                Debug.Log($"Invalid Glyph for character: '{c}'");
                return new Glyph(c, InvalidGlyphRect(), this);
            }
            else if (c is ' ')
            {
                charValue = 94; // Uhh, yea...
            }

            Rect bounds = GetBounds(c);
            bounds.X += charValue % 10 * CELL_SIZE;
            bounds.Y += charValue / 10 * CELL_SIZE;

            return new Glyph(c, bounds, this);
        }

        private static Rect InvalidGlyphRect()
            => new(CELL_SIZE * 9, CELL_SIZE * 9, GLYPH_WIDTH, GLYPH_HEIGHT);

        private static Rect GetBounds(char c)
            => c switch
            {
                '!' => new(0.041015625f, GLYPH_Y_OFFSET, 0.017578125f, GLYPH_HEIGHT),
                '“' => new(0.0337890625f, GLYPH_Y_OFFSET, 0.03125f, GLYPH_HEIGHT),
                '#' => new(0.02265625f, GLYPH_Y_OFFSET, 0.0517578125f, GLYPH_HEIGHT),
                '$' => new(0.0251953125f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '%' => new(0.0130859375f, GLYPH_Y_OFFSET, 0.0703125f, GLYPH_HEIGHT),
                '&' => new(0.0205078125f, GLYPH_Y_OFFSET, 0.056640625f, GLYPH_HEIGHT),
                '\'' => new(0.038671875f, GLYPH_Y_OFFSET, 0.017578125f, GLYPH_HEIGHT),
                '(' => new(0.0333984375f, GLYPH_Y_OFFSET, 0.029296875f, GLYPH_HEIGHT),
                ')' => new(0.0330078125f, GLYPH_Y_OFFSET, 0.029296875f, GLYPH_HEIGHT),
                '*' => new(0.0287109375f, GLYPH_Y_OFFSET, 0.03515625f, GLYPH_HEIGHT),
                '+' => new(0.0263671875f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                ',' => new(0.040625f, GLYPH_Y_OFFSET, 0.017578125f, GLYPH_HEIGHT),
                '-' => new(0.0333984375f, GLYPH_Y_OFFSET, 0.03125f, GLYPH_HEIGHT),
                '.' => new(0.03984375f, GLYPH_Y_OFFSET, 0.017578125f, GLYPH_HEIGHT),
                '/' => new(0.0326171875f, GLYPH_Y_OFFSET, 0.03125f, GLYPH_HEIGHT),
                '0' => new(0.0244140625f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '1' => new(0.02890625f, GLYPH_Y_OFFSET, 0.03125f, GLYPH_HEIGHT),
                '2' => new(0.02265625f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '3' => new(0.0232421875f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '4' => new(0.0208984375f, GLYPH_Y_OFFSET, 0.048828125f, GLYPH_HEIGHT),
                '5' => new(0.0263671875f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '6' => new(0.0259765625f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '7' => new(0.0255859375f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '8' => new(0.0251953125f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '9' => new(0.0248046875f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                ':' => new(0.0390625f, GLYPH_Y_OFFSET, 0.017578125f, GLYPH_HEIGHT),
                ';' => new(0.038671875f, GLYPH_Y_OFFSET, 0.017578125f, GLYPH_HEIGHT),
                '<' => new(0.0236328125f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '=' => new(0.0232421875f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '>' => new(0.0228515625f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                '?' => new(0.0263671875f, GLYPH_Y_OFFSET, 0.0458984375f, GLYPH_HEIGHT),
                '@' => new(0.009375f, GLYPH_Y_OFFSET, 0.08203125f, GLYPH_HEIGHT),
                'A' => new(0.0177734375f, GLYPH_Y_OFFSET, 0.0625f, GLYPH_HEIGHT),
                'B' => new(0.0232421875f, GLYPH_Y_OFFSET, 0.052734375f, GLYPH_HEIGHT),
                'C' => new(0.0189453125f, GLYPH_Y_OFFSET, 0.0595703125f, GLYPH_HEIGHT),
                'D' => new(0.0205078125f, GLYPH_Y_OFFSET, 0.056640625f, GLYPH_HEIGHT),
                'E' => new(0.0220703125f, GLYPH_Y_OFFSET, 0.052734375f, GLYPH_HEIGHT),
                'F' => new(0.024609375f, GLYPH_Y_OFFSET, 0.0478515625f, GLYPH_HEIGHT),
                'G' => new(0.0154296875f, GLYPH_Y_OFFSET, 0.0615234375f, GLYPH_HEIGHT),
                'H' => new(0.0189453125f, GLYPH_Y_OFFSET, 0.0546875f, GLYPH_HEIGHT),
                'I' => new(0.041015625f, GLYPH_Y_OFFSET, 0.017578125f, GLYPH_HEIGHT),
                'J' => new(0.026953125f, GLYPH_Y_OFFSET, 0.041015625f, GLYPH_HEIGHT),
                'K' => new(0.0236328125f, GLYPH_Y_OFFSET, 0.056640625f, GLYPH_HEIGHT),
                'L' => new(0.0271484375f, GLYPH_Y_OFFSET, 0.0458984375f, GLYPH_HEIGHT),
                'M' => new(0.016015625f, GLYPH_Y_OFFSET, 0.0634765625f, GLYPH_HEIGHT),
                'N' => new(0.0205078125f, GLYPH_Y_OFFSET, 0.0537109375f, GLYPH_HEIGHT),
                'O' => new(0.0171875f, GLYPH_Y_OFFSET, 0.0634765625f, GLYPH_HEIGHT),
                'P' => new(0.0216796875f, GLYPH_Y_OFFSET, 0.052734375f, GLYPH_HEIGHT),
                'Q' => new(0.014453125f, GLYPH_Y_OFFSET, 0.064453125f, GLYPH_HEIGHT),
                'R' => new(0.0189453125f, GLYPH_Y_OFFSET, 0.0595703125f, GLYPH_HEIGHT),
                'S' => new(0.0224609375f, GLYPH_Y_OFFSET, 0.0546875f, GLYPH_HEIGHT),
                'T' => new(0.0220703125f, GLYPH_Y_OFFSET, 0.0546875f, GLYPH_HEIGHT),
                'U' => new(0.02265625f, GLYPH_Y_OFFSET, 0.0546875f, GLYPH_HEIGHT),
                'V' => new(0.018359375f, GLYPH_Y_OFFSET, 0.060546875f, GLYPH_HEIGHT),
                'W' => new(0.0072265625f, GLYPH_Y_OFFSET, 0.08203125f, GLYPH_HEIGHT),
                'X' => new(0.017578125f, GLYPH_Y_OFFSET, 0.060546875f, GLYPH_HEIGHT),
                'Y' => new(0.0162109375f, GLYPH_Y_OFFSET, 0.0615234375f, GLYPH_HEIGHT),
                'Z' => new(0.0197265625f, GLYPH_Y_OFFSET, 0.0546875f, GLYPH_HEIGHT),
                '[' => new(0.0359375f, GLYPH_Y_OFFSET, 0.025390625f, GLYPH_HEIGHT),
                '\\' => new(0.0306640625f, GLYPH_Y_OFFSET, 0.03125f, GLYPH_HEIGHT),
                ']' => new(0.03515625f, GLYPH_Y_OFFSET, 0.025390625f, GLYPH_HEIGHT),
                '^' => new(0.0279296875f, GLYPH_Y_OFFSET, 0.04296875f, GLYPH_HEIGHT),
                '_' => new(0.020703125f, GLYPH_Y_OFFSET, 0.0556640625f, GLYPH_HEIGHT),
                '`' => new(0.033984375f, GLYPH_Y_OFFSET, 0.0244140625f, GLYPH_HEIGHT),
                'a' => new(0.023828125f, GLYPH_Y_OFFSET, 0.0478515625f, GLYPH_HEIGHT),
                'b' => new(0.0263671875f, GLYPH_Y_OFFSET, 0.044921875f, GLYPH_HEIGHT),
                'c' => new(0.0259765625f, GLYPH_Y_OFFSET, 0.044921875f, GLYPH_HEIGHT),
                'd' => new(0.02265625f, GLYPH_Y_OFFSET, 0.0458984375f, GLYPH_HEIGHT),
                'e' => new(0.0232421875f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                'f' => new(0.0306640625f, GLYPH_Y_OFFSET, 0.0341796875f, GLYPH_HEIGHT),
                'g' => new(0.025390625f, GLYPH_Y_OFFSET, 0.0458984375f, GLYPH_HEIGHT),
                'h' => new(0.0279296875f, GLYPH_Y_OFFSET, 0.04296875f, GLYPH_HEIGHT),
                'i' => new(0.040234375f, GLYPH_Y_OFFSET, 0.017578125f, GLYPH_HEIGHT),
                'j' => new(0.0310546875f, GLYPH_Y_OFFSET, 0.0263671875f, GLYPH_HEIGHT),
                'k' => new(0.0287109375f, GLYPH_Y_OFFSET, 0.0439453125f, GLYPH_HEIGHT),
                'l' => new(0.0390625f, GLYPH_Y_OFFSET, 0.0166015625f, GLYPH_HEIGHT),
                'm' => new(0.015234375f, GLYPH_Y_OFFSET, 0.064453125f, GLYPH_HEIGHT),
                'n' => new(0.0255859375f, GLYPH_Y_OFFSET, 0.04296875f, GLYPH_HEIGHT),
                'o' => new(0.022265625f, GLYPH_Y_OFFSET, 0.0478515625f, GLYPH_HEIGHT),
                'p' => new(0.0248046875f, GLYPH_Y_OFFSET, 0.044921875f, GLYPH_HEIGHT),
                'q' => new(0.025390625f, GLYPH_Y_OFFSET, 0.0458984375f, GLYPH_HEIGHT),
                'r' => new(0.03671875f, GLYPH_Y_OFFSET, 0.0322265625f, GLYPH_HEIGHT),
                's' => new(0.0265625f, GLYPH_Y_OFFSET, 0.0439453125f, GLYPH_HEIGHT),
                't' => new(0.033984375f, GLYPH_Y_OFFSET, 0.0302734375f, GLYPH_HEIGHT),
                'u' => new(0.0267578125f, GLYPH_Y_OFFSET, 0.04296875f, GLYPH_HEIGHT),
                'v' => new(0.0244140625f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                'w' => new(0.0142578125f, GLYPH_Y_OFFSET, 0.0654296875f, GLYPH_HEIGHT),
                'x' => new(0.02265625f, GLYPH_Y_OFFSET, 0.048828125f, GLYPH_HEIGHT),
                'y' => new(0.0232421875f, GLYPH_Y_OFFSET, 0.046875f, GLYPH_HEIGHT),
                'z' => new(0.0228515625f, GLYPH_Y_OFFSET, 0.0458984375f, GLYPH_HEIGHT),
                '{' => new(0.0341796875f, GLYPH_Y_OFFSET, 0.0322265625f, GLYPH_HEIGHT),
                '|' => new(0.0416015625f, GLYPH_Y_OFFSET, 0.015625f, GLYPH_HEIGHT),
                '}' => new(0.032421875f, GLYPH_Y_OFFSET, 0.0322265625f, GLYPH_HEIGHT),
                '~' => new(0.02421875f, GLYPH_Y_OFFSET, 0.048828125f, GLYPH_HEIGHT),
                ' ' => new(GLYPH_X_OFFSET, GLYPH_Y_OFFSET, 0.03f, GLYPH_HEIGHT),
                _   => new(GLYPH_X_OFFSET, GLYPH_Y_OFFSET, GLYPH_WIDTH, GLYPH_HEIGHT)
            };
    }
}
