using System.Reflection;

namespace GGXXACPROverlay.GGXXACPR
{
    /// <summary>
    /// Handles move data that has been packaged in-app because it can't easily be read directly from memory.
    /// </summary>
    internal static class MoveData
    {
        private const int SPECIAL_CASE_COMMAND_THROW_ID = 0x19;
        private const int SPECIAL_CASE_COMMAND_THROW_RANGE = 11000; // GGXXACPR_Win.exe+12054F
        private const string MoveDataFileName = "MoveData.csv";

        private readonly struct CharIdActIdKey(CharacterID charId, int actId)
        {
            public readonly CharacterID charId = charId;
            public readonly int actId = actId;
        }
        private readonly struct CommandGrabData(CharacterID charId, int actId, int cmdGrabId)
        {
            public readonly CharacterID CharId = charId;
            public readonly int ActId = actId;
            public readonly int CmdGrabId = cmdGrabId;
        }
        private struct MoveDataEntry
        {
            public CharacterID charId;
            public int actId;
            public int sequenceIndex;
            public string moveInput;
            public string moveName;
            public int moveId;  // custom identifier
        }

        // TODO: Will be needed for FrameMeter
        // private static readonly Lookup<CharIdActIdKey, MoveDataEntry> _actIdToMoveIds = ReadRawMoveDataToLookup();

        // (charId, actId)
        private static readonly CommandGrabData[] _activeByMarkCommandGrabs = [
            new(CharacterID.SOL,       0x088,  1), // Wild Throw
            new(CharacterID.KY,        0x109,  2), // EX Ky Elegant Slash
            new(CharacterID.MAY,       0x06B,  4), // Overhead Kiss
            new(CharacterID.MAY,       0x0A2, 15), // IK
            new(CharacterID.POTEMKIN,  0x07A,  3), // Potbuster
            new(CharacterID.CHIPP,     0x08F,  6), // Leaf Grab
            new(CharacterID.CHIPP,     0x130, 22), // EX Chipp grab super
            new(CharacterID.EDDIE,     0x07C,  5), // Damned Fang
            new(CharacterID.BAIKEN,    0x109, 23), // EX Baiken grab super
            new(CharacterID.JAM,       0x0EE, 14), // Unknown (Unused)
            new(CharacterID.SLAYER,    0x070, 18), // BSU
            new(CharacterID.ZAPPA,     0x07E, 20), // IK
            new(CharacterID.BRIDGET,   0x11B, 24), // EX Bridget grab super
            new(CharacterID.ROBOKY,    0x0DF, 21), // S-KY-line
            new(CharacterID.ABA,       0x112, 25), // Close Key Grab
            new(CharacterID.ABA,       0x113, 25), // Moroha/ABA EX Close Key Grab
            new(CharacterID.ABA,       0x11A, 26), // Unknown (Unused Air keygrab?) actId 282, cmdGrabId 26
            new(CharacterID.ABA,       0x11B, 26), // Unknown (Unused Air keygrab?) actId 283, cmdGrabId 26
        ];

        private static Lookup<CharIdActIdKey, MoveDataEntry> ReadRawMoveDataToLookup()
        {
            List<MoveDataEntry> output = [];

            TextReader reader;
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(GGXXACPR), MoveDataFileName) ??
                    throw new FileNotFoundException($"Resource Stream returned null for {MoveDataFileName}");

                reader = new StreamReader(stream);

                reader.ReadLine();  // Skip column headers
                string? line = reader.ReadLine();
                string[] values;
                while (line is not null)
                {
                    values = line.Split(",", StringSplitOptions.TrimEntries);
                    if (values.Length < 6) { throw new InvalidDataException($"Move Data did not have the required amount of columns:\n\t{line}"); }
                    output.Add(new MoveDataEntry()
                    {
                        charId = (CharacterID)int.Parse(values[0]),
                        actId = int.Parse(values[1]),
                        sequenceIndex = int.Parse(values[2]),
                        moveInput = values[3],
                        moveName = values[4],
                        moveId = int.Parse(values[5])
                    });
                    line = reader.ReadLine();
                }

                Debug.Log("Move Data loaded successfully.");
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException || e is IOException || e is UnauthorizedAccessException)
                {
                    Debug.Log($"Could not find {MoveDataFileName}, move startup will be incorrect.");
                }
                else throw;
            }

            return (Lookup<CharIdActIdKey, MoveDataEntry>)output.ToLookup(
                moveData => new CharIdActIdKey(moveData.charId, moveData.actId)
            );
        }

        // TODO: Will be needed for FrameMeter
        /// <summary>
        /// Returns true if both actIds are played in the same move and actId1 comes before actId2 in a multi-act move.
        /// </summary>
        /// <param name="charId">Character Id</param>
        /// <param name="actId1">first actId</param>
        /// <param name="actId2">second actId</param>
        //public static bool IsPrevAnimSameMove(CharacterID charId, int actId1, int actId2)
        //{
        //    var key1 = new CharIdActIdKey(charId, actId1);
        //    var key2 = new CharIdActIdKey(charId, actId2);
        //    if (_actIdToMoveIds.Contains(key1) && _actIdToMoveIds.Contains(key2))
        //    {
        //        IEnumerable<MoveDataEntry> data1 = _actIdToMoveIds[key1];
        //        IEnumerable<MoveDataEntry> data2 = _actIdToMoveIds[key2];

        //        foreach (var data in data1)
        //        {
        //            if (data2.Any(mde => mde.moveId == data.moveId && mde.sequenceIndex > data.sequenceIndex))
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// Returns true if the given character's move is a command grab that is active when Player.Mark == 1.
        /// </summary>
        public static bool IsActiveByMark(CharacterID charId, int actId)
        {
            return _activeByMarkCommandGrabs.Any(data => data.CharId == charId && data.ActId == actId);
        }

        /// <summary>
        /// Command grab IDs aren't always visible in memory during their active frames.
        ///  So we're gonna have to look it up in this local cache
        /// </summary>
        /// <param name="charId"></param>
        /// <param name="actId"></param>
        /// <returns></returns>
        public static int GetCommandGrabId(CharacterID charId, int actId)
        {
            var cmdGrabId = _activeByMarkCommandGrabs.FirstOrDefault(
                data => data.CharId == charId && data.ActId == actId).CmdGrabId;

            return cmdGrabId;
        }

    }
}
