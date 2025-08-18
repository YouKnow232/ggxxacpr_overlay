using System.ComponentModel;
using System.Reflection;
using GGXXACPROverlay.GGXXACPR;

namespace GGXXACPROverlay
{
    internal static class Settings
    {
        private const string path = "GGXXACPROverlay\\OverlaySettings.ini";
        private const string defaultSettingsResource = "GGXXACPROverlay.OverlaySettings.ini";

        public static bool DisplayBoxes { get; set; }                   = true;
        public static bool CombineBoxes { get; set; }                   = true;
        public static D3DCOLOR_ARGB Default { get; private set; }       = new(0x00FF0000);
        public static D3DCOLOR_ARGB Hitbox { get; private set; }        = new(0x80FF0000);
        public static D3DCOLOR_ARGB Hurtbox { get; private set; }       = new(0x8000FF00);
        public static D3DCOLOR_ARGB Push { get; private set; }          = new(0x8000FFFF);
        public static D3DCOLOR_ARGB Grab { get; private set; }          = new(0x80FF00FF);
        public static D3DCOLOR_ARGB CLHitbox { get; private set; }      = new(0x80FF8000);
        public static D3DCOLOR_ARGB MiscPushRange { get; private set; } = new(0x80FF00FF);
        public static D3DCOLOR_ARGB MiscPivotRange { get; private set; } = new(0x80FF8000);
        public static D3DCOLOR_ARGB PivotCrossColor { get; private set; } = new(0xFF800080);
        public static float PivotCrossSize { get; private set; }        = 15.0f;
        public static float PivotCrossThickness { get; private set; }   = 3.0f;
        public static float HitboxBorderThickness { get; private set; } = 2.0f;
        public static bool WidescreenClipping { get; private set; }     = true;
        public static bool HideP1 { get; set; }                         = false;
        public static bool HideP2 { get; set; }                         = false;
        public static bool AlwaysDrawThrowRange { get; set; }           = false;
        public static bool DrawInfiniteHeight { get; private set; }     = true;

        public static readonly BoxId[] BoxDrawList = [BoxId.HIT, BoxId.HURT, BoxId.USE_EXTRA];
        // Pivot, CleanHit, Hit, Hurt, Grab, Push
        public static DrawOperation[] DrawOrder { get; private set; } = [
            DrawOperation.MiscRange,// back
            DrawOperation.Push,
            DrawOperation.Hurt,
            DrawOperation.Hit,
            DrawOperation.CleanHit,
            DrawOperation.Grab,
            DrawOperation.Pivot     // front
        ];
        public static bool DisplayHSDMeter { get; set; } = true;
        public static bool IgnoreDisableHitboxFlag { get; private set; } = false;
        public static bool RecordDuringHitstop { get; set; } = false;
        public static bool RecordDuringSuperFlash { get; set; } = false;


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
                    Debug.Log($"[Settings] Could not load settings file: {ex.Message}");
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

            // Caching settings that are checked at least once per frame
            DisplayBoxes    = Get("Hitboxes", "Display", DisplayBoxes);
            CombineBoxes    = Get("Hitboxes", "CombineBoxes", CombineBoxes);
            Hitbox          = new(Get("Hitboxes.Palette", "Color_Hitbox", Hitbox.ARGB));
            Hurtbox         = new(Get("Hitboxes.Palette", "Color_Hurtbox", Hurtbox.ARGB));
            Push            = new(Get("Hitboxes.Palette", "Color_Pushbox", Push.ARGB));
            Grab            = new(Get("Hitboxes.Palette", "Color_Grabbox", Grab.ARGB));
            CLHitbox        = new(Get("Hitboxes.Palette", "Color_CleanHit", CLHitbox.ARGB));
            MiscPushRange   = new(Get("Hitboxes.Palette", "Color_MiscPushRange", MiscPushRange.ARGB));
            MiscPivotRange  = new(Get("Hitboxes.Palette", "Color_MiscPivotRange", MiscPivotRange.ARGB));
            PivotCrossColor = new(Get("Hitboxes.Palette", "Color_Pivot", PivotCrossColor.ARGB));
            // TODO: clamp some of these values
            PivotCrossSize          = Get("Hitboxes", "PivotSize", PivotCrossSize);
            PivotCrossThickness     = Get("Hitboxes", "PivotThickness", PivotCrossThickness);
            HitboxBorderThickness   = Get("Hitboxes", "BorderThickness", HitboxBorderThickness);
            WidescreenClipping      = Get("Hitboxes", "WidescreenClipping", WidescreenClipping);
            HideP1                  = Get("Hitboxes", "HideP1", HideP1);
            HideP2                  = Get("Hitboxes", "HideP2", HideP2);
            AlwaysDrawThrowRange    = Get("Hitboxes", "AlwaysDrawThrowRange", AlwaysDrawThrowRange);
            DrawOrder               = Get("Hitboxes", "DrawOrder", DrawOrder);
            DisplayHSDMeter         = Get("Misc", "DisplayHSDMeter", DisplayHSDMeter);
            IgnoreDisableHitboxFlag = Get("Fun", "IgnoreDisableHitboxFlag", IgnoreDisableHitboxFlag);
            return true;
        }

        public static T Get<T>(string section, string key, T defaultValue)
        {
            if (!Sections.TryGetValue(section, out var keys) || !keys.TryGetValue(key, out var rawValue))
            {
                Debug.Log($"[Settings] Section or key not found: [{section}] {key}");
                return defaultValue;
            }

            try
            {
                Type type = typeof(T);

                if (type.IsArray && (type.GetElementType()?.IsEnum ?? false))
                {
                    return (T)(object)ParseEnumArray<DrawOperation>(rawValue);
                }
                else if (type == typeof(bool))
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
                Debug.Log($"[Settings] Failed to parse {section}.{key}");
                return defaultValue;
            }
        }

        public static T[] ParseEnumArray<T>(string rawValue) where T : Enum
        {
            string[] entries = rawValue.Split(',', StringSplitOptions.TrimEntries);
            T[] output = new T[entries.Length];

            for (int i = 0; i < entries.Length; i++)
            {
                try
                {
                    output[i] = (T)Enum.Parse(typeof(T), entries[i], true);
                }
                catch (Exception e)
                {
                    Debug.Log($"[Settings] Enum.Parse threw an exception: {e}");
                    output[i] = default;
                }
            }

            return output;
        }

        public static void WriteDefault()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            using Stream? fileStream = asm.GetManifestResourceStream(defaultSettingsResource)
                ?? throw new FileNotFoundException($"Could not find default settings ini resource: {defaultSettingsResource}");

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
                }
            }
        }
    }
}
