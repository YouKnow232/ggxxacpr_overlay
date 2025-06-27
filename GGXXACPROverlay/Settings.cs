using System.Reflection;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal static class Settings
    {
        private const string path = "OverlaySettings.ini";
        private const string defaultSettingsResource = "GGXXACPROverlay.OverlaySettings.ini";

        public static D3DCOLOR_ARGB Default = new(0xFF0000u);
        public static D3DCOLOR_ARGB Hitbox { get; private set; }        = new(0x80FF0000);
        public static D3DCOLOR_ARGB Hurtbox { get; private set; }       = new(0x8000FF00);
        public static D3DCOLOR_ARGB Collision { get; private set; }     = new(0x8000FFFF);
        public static D3DCOLOR_ARGB Grab { get; private set; }          = new(0x80FF00FF);
        public static D3DCOLOR_ARGB CLHitbox { get; private set; }      = new(0x80FF8000);
        public static D3DCOLOR_ARGB PivotCrossColor { get; private set; } = new(0xFF800080);
        public static float PivotCrossSize { get; private set; }        = 10.0f;
        public static float PivotCrossThickness { get; private set; }   = 2.0f;
        public static float HitboxBorderThickness { get; private set; } = 2.0f;

        public static readonly BoxId[] BoxDrawList = [BoxId.HIT, BoxId.HURT, BoxId.USE_EXTRA];

        public static Dictionary<string, Dictionary<string, string>> Sections { get; } = [];

        public static bool Load()
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException
                    || ex is UnauthorizedAccessException
                    || ex is IOException)
                {
                    Debug.Log($"Could not load settings file: {ex.Message}");
                    return false;
                }
                else throw;
            }

            string currentSection = "";
            foreach (string rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith(';') || line.StartsWith('#'))
                    continue;

                // Section header
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    currentSection = line[1..^1].Trim();
                    if (!Sections.ContainsKey(currentSection))
                    {
                        Sections.Add(currentSection, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
                    }
                }
                else if (line.Contains('=') && currentSection != null)
                {
                    var parts = line.Split('=', 2);
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    Sections[currentSection][key] = value;
                }
            }

            CacheProperties();

            return true;
        }

        private static void CacheProperties()
        {
            Hitbox      = new(Get("Hitboxes.Palette", "Color_Hitbox", Hitbox.ARGB));
            Hurtbox     = new(Get("Hitboxes.Palette", "Color_Hurtbox", Hurtbox.ARGB));
            Collision   = new(Get("Hitboxes.Palette", "Color_Pushbox", Collision.ARGB));
            Grab        = new(Get("Hitboxes.Palette", "Color_Grabbox", Grab.ARGB));
            CLHitbox    = new(Get("Hitboxes.Palette", "Color_CleanHit", CLHitbox.ARGB));
            PivotCrossColor = new(Get("Hitboxes.Palette", "Color_Pivot", PivotCrossColor.ARGB));
            // TODO: clamp some of these values
            PivotCrossSize      = Get("Hitboxes", "PivotSize", PivotCrossSize);
            PivotCrossThickness = Get("Hitboxes", "PivotThickness", PivotCrossThickness);
            HitboxBorderThickness = Get("Hitboxes", "BorderThickenss", HitboxBorderThickness);
        }

        public static T Get<T>(string section, string key, T defaultValue)
        {
            if (!Sections.TryGetValue(section, out var keys) || !keys.TryGetValue(key, out var rawValue))
                return defaultValue;

            try
            {
                Type type = typeof(T);

                if (type == typeof(bool))
                {
                    return (T)(object)(rawValue.Equals("true", StringComparison.OrdinalIgnoreCase) || rawValue == "1");
                }
                else if (type == typeof(uint))
                {
                    if (rawValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        return (T)(object)Convert.ToUInt32(rawValue[2..], 16);
                    return (T)(object)Convert.ToUInt32(rawValue);
                }
                else if (type == typeof(int))
                {
                    if (rawValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        return (T)(object)Convert.ToInt32(rawValue[2..], 16);
                    return (T)(object)Convert.ToInt32(rawValue);
                }

                return (T)Convert.ChangeType(rawValue, type);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static void WriteDefault()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using Stream? fileStream = asm.GetManifestResourceStream(defaultSettingsResource);

            if (fileStream == null) { throw new NullReferenceException($"Could not find default settings ini resource: {defaultSettingsResource}"); }

            try
            {
                using var fs = File.Create(path);
                fileStream.CopyTo(fs);
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Debug.Log($"Couldn't create file: {ex.Message}");
                    return;
                }
            }
        }
    }
}
