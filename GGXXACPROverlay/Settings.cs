using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using GGXXACPROverlay.FrameMeter;

namespace GGXXACPROverlay
{
    internal static class Settings
    {
        private const string path = "GGXXACPROverlay\\OverlaySettings.ini";
        private const string defaultSettingsResource = "GGXXACPROverlay.OverlaySettings.ini";

        public static HitboxSettings Hitboxes { get; private set; } = new();
        public static FrameMeterSettings FrameMeter { get; private set; } = new();
        public static MiscSettings Misc { get; private set; } = new();

        private static Dictionary<string, Dictionary<string, string>> Sections { get; } = [];

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

            LoadAllSettings(Hitboxes);
            LoadAllSettings(Hitboxes.Palette);
            LoadAllSettings(FrameMeter);
            LoadAllSettings(FrameMeter.Palette);
            LoadAllSettings(Misc);

            return true;
        }

        public static void ReloadSettings()
        {
            Sections.Clear();
            if (!Load())
            {
                Debug.Log("Couldn't load OverlaySettings.ini. Creating default ini.");
                WriteDefault();
            }
        }

        private static void LoadAllSettings
            <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
            (T target)
        {
            ArgumentNullException.ThrowIfNull(target);

            Type targetType = typeof(T);
            var properties = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                if (!property.CanWrite || !property.CanRead)
                    continue;

                var attr = property.GetCustomAttribute<SettingKeyAttribute>();
                if (attr == null)
                    continue;

                var defaultValue = property.GetValue(target);
                var settingValue = GetNonGeneric(property.PropertyType, attr.Section, attr.Key, defaultValue);

                property.SetValue(target, settingValue);
            }
        }

        public static object? GetNonGeneric(Type type, string section, string key, object? defaultValue)
        {
            if (!Sections.TryGetValue(section, out var keys) || !keys.TryGetValue(key, out var rawValue))
            {
                Debug.Log($"[Settings] Section or key not found: [{section}] {key}");
                return defaultValue;
            }

            try
            {
                if (type.IsArray && (type.GetElementType()?.IsEnum ?? false))
                {
                    // TODO refactor this if there's every another enum array setting
                    return ParseEnumArray<DrawOperation>(rawValue);
                }
                else if (type == typeof(bool))
                {
                    return rawValue.Equals("true", StringComparison.OrdinalIgnoreCase) || rawValue == "1";
                }
                else if (type == typeof(uint))
                {
                    if (rawValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        return Convert.ToUInt32(rawValue[2..], 16);
                    return Convert.ToUInt32(rawValue);
                }
                else if (type == typeof(int))
                {
                    if (rawValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        return Convert.ToInt32(rawValue[2..], 16);
                    return Convert.ToInt32(rawValue);
                }
                else if (type == typeof(D3DCOLOR_ARGB))
                {
                    uint packedValue;
                    if (rawValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        packedValue = Convert.ToUInt32(rawValue[2..], 16);
                    else 
                        packedValue = Convert.ToUInt32(rawValue);

                    return new D3DCOLOR_ARGB(packedValue);
                }
                else if (type.IsEnum)
                {
                    return Enum.Parse(type, rawValue, ignoreCase: true);
                }
                else
                {
                    return Convert.ChangeType(rawValue, type);
                }
            }
            catch
            {
                Debug.Log($"[Settings] Failed to parse {section}.{key}");
                return defaultValue;
            }
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


    public class MiscSettings
    {
        [SettingKey("Misc", "DisplayHSDMeter")]
        public bool DisplayHSDMeter { get; set; } = true;
        [SettingKey("Fun", "IgnoreDIsableHitboxFlag")]
        public bool IgnoreDisableHitboxFlag { get; private set; } = false;
        [SettingKey("Misc", "DisplayHelpDialog")]
        public bool DisplayHelpDialog { get; set; } = true;
    }
    public class HitboxSettings
    {
        [SettingKey("Hitboxes", "Display")]
        public bool DisplayBoxes { get; set; } = true;
        [SettingKey("Hitboxes", "CombineBoxes")]
        public bool CombineBoxes { get; set; } = true;
        [SettingKey("Hitboxes", "PivotSize")]
        public float PivotCrossSize { get; set; } = 15.0f;
        [SettingKey("Hitboxes", "PivotThickness")]
        public float PivotCrossThickness { get; set; } = 3.0f;
        [SettingKey("Hitboxes", "BorderThickness")]
        public float HitboxBorderThickness { get; set; } = 2.0f;
        [SettingKey("Hitboxes", "WidescreenClipping")]
        public bool WidescreenClipping { get; set; } = true;
        [SettingKey("Hitboxes", "HideP1")]
        public bool HideP1 { get; set; } = false;
        [SettingKey("Hitboxes", "HideP2")]
        public bool HideP2 { get; set; } = false;
        [SettingKey("Hitboxes", "AlwaysDrawThrowRange")]
        public bool AlwaysDrawThrowRange { get; set; } = false;
        [SettingKey("Hitboxes", "DrawOrder")]
        public DrawOperation[] DrawOrder { get; private set; } = [
            DrawOperation.MiscRange,// back
            DrawOperation.Push,
            DrawOperation.Hurt,
            DrawOperation.Hit,
            DrawOperation.CleanHit,
            DrawOperation.Grab,
            DrawOperation.Pivot     // front
        ];
        //[SettingKey("Hitboxes", "DrawInfiniteHeight")]
        public bool DrawInfiniteHeight { get; set; } = true;
        public HitboxPalette Palette { get; private set; } = new();
    }
    public class HitboxPalette
    {
        public D3DCOLOR_ARGB Default { get; set; }          = new(0x00FF0000);
        [SettingKey("Hitboxes.Palette", "Color_Hitbox")]
        public D3DCOLOR_ARGB Hitbox { get; set; }           = new(0x80FF0000);
        [SettingKey("Hitboxes.Palette", "Color_Hurtbox")]
        public D3DCOLOR_ARGB Hurtbox { get; set; }          = new(0x8000FF00);
        [SettingKey("Hitboxes.Palette", "Color_Pushbox")]
        public D3DCOLOR_ARGB Push { get; set; }             = new(0x8000FFFF);
        [SettingKey("Hitboxes.Palette", "Color_Grabbox")]
        public D3DCOLOR_ARGB Grab { get; set; }             = new(0x80FF00FF);
        [SettingKey("Hitboxes.Palette", "Color_CleanHit")]
        public D3DCOLOR_ARGB CLHitbox { get; set; }         = new(0x80FF8000);
        [SettingKey("Hitboxes.Palette", "Color_MiscPushRange")]
        public D3DCOLOR_ARGB MiscPushRange { get; set; }    = new(0x80FF00FF);
        [SettingKey("Hitboxes.Palette", "Color_MiscPivotRange")]
        public D3DCOLOR_ARGB MiscPivotRange { get; set; }   = new(0x80FF8000);
        [SettingKey("Hitboxes.Palette", "Color_Pivot")]
        public D3DCOLOR_ARGB PivotCrossColor { get; set; }  = new(0xFF800080);
    }
    public class FrameMeterSettings
    {
        [SettingKey("FrameMeter", "Display")]
        public bool Display { get; set; } = true;
        [SettingKey("FrameMeter", "HitstopPause")]
        public bool PauseDuringHitstop { get; set; } = true;
        [SettingKey("FrameMeter", "SuperFlashPause")]
        public bool PauseDuringSuperFlash { get; set; } = true;
        [SettingKey("FrameMeter", "IgnoreDistantProjectiles")]
        public bool IgnoreDistantProjectiles { get; set; } = false;
        public FrameMeterPalette Palette { get; private set; } = new();
    }

    public class FrameMeterPalette
    {
        // TODO: Performance concerns with Dictionary allocations
        /// <summary>
        /// Creates dictionaries representing the color legend for the Frame Meter.
        /// </summary>
        public void GetLegends(
            out Dictionary<D3DCOLOR_ARGB, string> frameTypes,
            out Dictionary<D3DCOLOR_ARGB, string> primaryFrameTypes,
            out Dictionary<D3DCOLOR_ARGB, string> secondaryFrameTypes)
        {
            frameTypes = [];
            primaryFrameTypes = [];
            secondaryFrameTypes = [];

            Dictionary<D3DCOLOR_ARGB, HashSet<FrameType>> frameTypeDict = [];
            Dictionary<D3DCOLOR_ARGB, HashSet<PrimaryFrameProperty>> primaryPropDict = [];
            Dictionary<D3DCOLOR_ARGB, HashSet<SecondaryFrameProperty>> secondaryPropDict = [];

            // Frame types
            foreach (FrameType fType in Enum.GetValues<FrameType>())
            {
                if (fType is FrameType.None) continue;

                var color = GetColor(fType);
                if (!frameTypeDict.TryAdd(color, [fType]))
                {
                    frameTypeDict[color].Add(fType);
                }
            }

            // primary props
            foreach (PrimaryFrameProperty pfType in Enum.GetValues<PrimaryFrameProperty>())
            {
                if (pfType is PrimaryFrameProperty.Default or PrimaryFrameProperty.FRC or PrimaryFrameProperty.TEST) continue;

                var color = GetColor(pfType);
                if (!primaryPropDict.TryAdd(color, [pfType]))
                {
                    primaryPropDict[color].Add(pfType);
                }
            }

            // secondary props
            foreach (SecondaryFrameProperty sfType in Enum.GetValues<SecondaryFrameProperty>())
            {
                if (sfType is SecondaryFrameProperty.Default) continue;

                var color = GetColor(sfType);
                if (!secondaryPropDict.TryAdd(color, [sfType]))
                {
                    secondaryPropDict[color].Add(sfType);
                }
            }

            foreach (var key in frameTypeDict.Keys)
                frameTypes[key] = GetLegendLabel(frameTypeDict[key]);
            foreach (var key in primaryPropDict.Keys)
                primaryFrameTypes[key] = GetLegendLabel(primaryPropDict[key]);
            foreach (var key in secondaryPropDict.Keys)
                secondaryFrameTypes[key] = GetLegendLabel(secondaryPropDict[key]);
        }


        private static readonly HashSet<FrameType> hitblockstunSet = [
            FrameType.Blockstun,
            FrameType.Hitstun,
            FrameType.TechableHitstun,
            FrameType.KnockDownHitstun];
        private static readonly HashSet<FrameType> hitstunSet = [
            FrameType.Hitstun,
            FrameType.TechableHitstun,
            FrameType.KnockDownHitstun];
        private static readonly HashSet<FrameType> active = [
            FrameType.Active,
            FrameType.ActiveThrow];
        private static string GetLegendLabel(HashSet<FrameType> types)
        {
            if (types.Count == 0)
                return "";
            else if (types.Count == 1)
                return types.FirstOrDefault().GetDescription();
            else
            {
                if (hitblockstunSet.IsSubsetOf(types))
                {
                    types.ExceptWith(hitblockstunSet);
                    string remainingLabel = GetLegendLabel(types);
                    string append = remainingLabel == "" ? "" : (", " + remainingLabel);
                    return "Hit/Blockstun" + append;
                }
                else if (hitstunSet.IsSubsetOf(types))
                {
                    types.ExceptWith(hitstunSet);
                    string remainingLabel = GetLegendLabel(types);
                    string append = remainingLabel == "" ? "" : (", " + remainingLabel);
                    return "Hitstun" + append;
                }
                else if (active.IsSubsetOf(types))
                {
                    types.ExceptWith(active);
                    string remainingLabel = GetLegendLabel(types);
                    string append = remainingLabel == "" ? "" : (", " + remainingLabel);
                    return "Active" + append;
                }

                StringBuilder output = new StringBuilder();
                foreach(var type in types)
                {
                    output.Append(type.GetDescription()).Append(", ");
                }

                output.Remove(output.Length - 2, 2);
                return output.ToString();
            }
        }
        private static readonly HashSet<PrimaryFrameProperty> guardPointSet = [
            PrimaryFrameProperty.GuardPointFull,
            PrimaryFrameProperty.GuardPointHigh,
            PrimaryFrameProperty.GuardPointLow];
        private static string GetLegendLabel(HashSet<PrimaryFrameProperty> pProps)
        {
            if (pProps.Count == 0)
                return "";
            else if (pProps.Count == 1)
                return pProps.FirstOrDefault().GetDescription();
            else
            {
                if (guardPointSet.IsSubsetOf(pProps))
                {
                    pProps.ExceptWith(guardPointSet);  // TODO: make sure set mutability isn't an issue here
                    string remainingLabel = GetLegendLabel(pProps);
                    string append = remainingLabel == "" ? "" : (", " + remainingLabel);
                    return "Guard Point" + append;
                }

                StringBuilder output = new StringBuilder();
                foreach (var prop in pProps)
                {
                    output.Append(prop.GetDescription()).Append(", ");
                }

                output.Remove(output.Length - 2, 2);
                return output.ToString();
            }
        }
        private static string GetLegendLabel(HashSet<SecondaryFrameProperty> sProps)
        {
            if (sProps.Count == 0)
                return "";
            else if (sProps.Count == 1)
                return sProps.FirstOrDefault().GetDescription();
            else
            {

                StringBuilder output = new StringBuilder();
                foreach (var prop in sProps)
                {
                    output.Append(prop.GetDescription()).Append(", ");
                }

                output.Remove(output.Length - 2, 2);
                return output.ToString();
            }
        }

        public D3DCOLOR_ARGB GetColor(FrameType type)
            => type switch
            {
                FrameType.Neutral           => Neutral,
                FrameType.Movement          => Movement,
                FrameType.CounterHitState   => CounterHitState,
                FrameType.Startup           => Startup,
                FrameType.Active            => Active,
                FrameType.ActiveThrow       => ActiveThrow,
                FrameType.Recovery          => Recovery,
                FrameType.Blockstun         => BlockStun,
                FrameType.Hitstun           => HitStun,
                FrameType.TechableHitstun   => TechableHitstun,
                FrameType.KnockDownHitstun  => KnockDownHitstun,
                _                           => new D3DCOLOR_ARGB(0xFF0F0F0F),
            };
        public D3DCOLOR_ARGB GetColor(PrimaryFrameProperty frameProperty)
            => frameProperty switch
            {
                PrimaryFrameProperty.InvulnFull     => InvulnFull,
                PrimaryFrameProperty.InvulnStrike   => InvulnStrike,
                PrimaryFrameProperty.InvulnThrow    => InvulnThrow,
                PrimaryFrameProperty.Parry          => Parry,
                PrimaryFrameProperty.GuardPointFull => GuardPointFull,
                PrimaryFrameProperty.GuardPointHigh => GuardPointHigh,
                PrimaryFrameProperty.GuardPointLow  => GuardPointLow,
                PrimaryFrameProperty.Armor          => Armor,
                PrimaryFrameProperty.Slashback      => SlashBack,
                PrimaryFrameProperty.TEST           => new D3DCOLOR_ARGB(0xFF00FFFF),
                _                                   => new D3DCOLOR_ARGB(0xFF0F0F0F),
            };
        public D3DCOLOR_ARGB GetColor(SecondaryFrameProperty frameProperty)
            => frameProperty switch
            {
                SecondaryFrameProperty.FRC => FRC,
                _ => new D3DCOLOR_ARGB(0xFF0F0F0F),
            };

        [SettingKey("FrameMeter.Palette", "Color_Neutral")]
        public D3DCOLOR_ARGB Neutral { get; private set; }          =new(0xFF1B1B1B);
        [SettingKey("FrameMeter.Palette", "Color_Movement")]
        public D3DCOLOR_ARGB Movement { get; private set; }         =new(0xFF41F8FC);
        [SettingKey("FrameMeter.Palette", "Color_CounterHitState")]
        public D3DCOLOR_ARGB CounterHitState { get; private set; }  =new(0xFF01B597);
        [SettingKey("FrameMeter.Palette", "Color_Startup")]
        public D3DCOLOR_ARGB Startup { get; private set; }          =new(0xFF01B597);
        [SettingKey("FrameMeter.Palette", "Color_Active")]
        public D3DCOLOR_ARGB Active { get; private set; }           =new(0xFFCB2B67);
        [SettingKey("FrameMeter.Palette", "Color_ActiveThrow")]
        public D3DCOLOR_ARGB ActiveThrow { get; private set; }      =new(0xFFCB2B67);
        [SettingKey("FrameMeter.Palette", "Color_Recovery")]
        public D3DCOLOR_ARGB Recovery { get; private set; }         =new(0xFF006FBC);
        [SettingKey("FrameMeter.Palette", "Color_BlockStun")]
        public D3DCOLOR_ARGB BlockStun { get; private set; }        =new(0xFFC8C800);
        [SettingKey("FrameMeter.Palette", "Color_HitStun")]
        public D3DCOLOR_ARGB HitStun { get; private set; }          =new(0xFFC8C800);
        [SettingKey("FrameMeter.Palette", "Color_TechableHitstun")]
        public D3DCOLOR_ARGB TechableHitstun { get; private set; }  =new(0xFF969600);
        [SettingKey("FrameMeter.Palette", "Color_KnockDownHitstun")]
        public D3DCOLOR_ARGB KnockDownHitstun { get; private set; } =new(0xFF969600);
        [SettingKey("FrameMeter.Palette", "Color_InvulnFull")]
        public D3DCOLOR_ARGB InvulnFull { get; private set; }       =new(0xFFFFFFFF);
        [SettingKey("FrameMeter.Palette", "Color_InvulnStrike")]
        public D3DCOLOR_ARGB InvulnStrike { get; private set; }     =new(0xFF0080FF);
        [SettingKey("FrameMeter.Palette", "Color_InvulnThrow")]
        public D3DCOLOR_ARGB InvulnThrow { get; private set; }      =new(0xFFFF8000);
        [SettingKey("FrameMeter.Palette", "Color_Parry")]
        public D3DCOLOR_ARGB Parry { get; private set; }            =new(0xFF785000);
        [SettingKey("FrameMeter.Palette", "Color_GuardPointFull")]
        public D3DCOLOR_ARGB GuardPointFull { get; private set; }   =new(0xFF785000);
        [SettingKey("FrameMeter.Palette", "Color_GuardPointHigh")]
        public D3DCOLOR_ARGB GuardPointHigh { get; private set; }   =new(0xFF785000);
        [SettingKey("FrameMeter.Palette", "Color_GuardPointLow")]
        public D3DCOLOR_ARGB GuardPointLow { get; private set; }    =new(0xFF785000);
        [SettingKey("FrameMeter.Palette", "Color_Armor")]
        public D3DCOLOR_ARGB Armor { get; private set; }            =new(0xFF785000);
        [SettingKey("FrameMeter.Palette", "Color_FRC")]
        public D3DCOLOR_ARGB FRC { get; private set; }              =new(0xFFFFFF00);
        [SettingKey("FrameMeter.Palette", "Color_SlashBack")]
        public D3DCOLOR_ARGB SlashBack { get; private set; }        =new(0xFFFF0000);
    }
}