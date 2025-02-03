using System.Diagnostics;
using System.Reflection;
using System.Resources;

namespace GGXXACPROverlay.GGXXACPR
{
    internal static class MoveData
    {
        private readonly struct CharIdActIdKey(int charId, int actId)
        {
            public readonly int charId = charId;
            public readonly int actId = actId;
        }
        private struct MoveDataEntry
        {
            public int charId;
            public int actId;
            public int sequenceIndex;
            public string moveInput;
            public string moveName;
            public int moveId;  // custom identifier
        }

        private static readonly string MoveDataFileName = "MoveData.csv";
        private static readonly List<MoveDataEntry> rawMoveData = [];
        private static readonly Lookup<CharIdActIdKey, MoveDataEntry> actIdToMoveIds;

        static MoveData()
        {
            string[] rows = [];
            TextReader reader;
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(GGXXACPR), MoveDataFileName) ??
                    throw new FileNotFoundException($"Resource Stream returned null for {MoveDataFileName}");

                reader = new StreamReader(stream);

                reader.ReadLine();  // Skip column headers
                string? line = reader.ReadLine();
                string[] values;
                while (line != null)
                {
                    values = line.Split(",", StringSplitOptions.TrimEntries);
                    if (values.Length < 6) { throw new InvalidDataException($"Move Data did not have the required amount of columns:\n\t{line}"); }
                    rawMoveData.Add(new MoveDataEntry()
                    {
                        charId          = int.Parse(values[0]),
                        actId           = int.Parse(values[1]),
                        sequenceIndex   = int.Parse(values[2]),
                        moveInput       = values[3],
                        moveName        = values[4],
                        moveId          = int.Parse(values[5])
                    });
                    line = reader.ReadLine();
                }

                Debug.WriteLine("Move Data loaded successfully.");
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException || e is IOException || e is UnauthorizedAccessException)
                {
                    Console.WriteLine($"Could not find {MoveDataFileName}, move startup will be incorrect.");
                }
                else
                {
                    throw;
                }
            }

            actIdToMoveIds = (Lookup<CharIdActIdKey, MoveDataEntry>)rawMoveData.ToLookup(
                moveData => new CharIdActIdKey(moveData.charId, moveData.actId)
            );
        }

        /// <summary>
        /// Returns true if both actIds are played in the same move and actId1 comes before actId2 in a multi-act move.
        /// </summary>
        /// <param name="charId">Character Id</param>
        /// <param name="actId1">first actId</param>
        /// <param name="actId2">second actId</param>
        public static bool IsPrevAnimSameMove(int charId, int actId1, int actId2)
        {
            var key1 = new CharIdActIdKey(charId, actId1);
            var key2 = new CharIdActIdKey(charId, actId2);
            if (actIdToMoveIds.Contains(key1) && actIdToMoveIds.Contains(key2))
            {
                IEnumerable<MoveDataEntry> data1 = actIdToMoveIds[key1];
                IEnumerable<MoveDataEntry> data2 = actIdToMoveIds[key2];
                foreach (var data in data1)
                {
                    if (data2.Where(mde => mde.moveId == data.moveId && mde.sequenceIndex > data.sequenceIndex).Any())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
