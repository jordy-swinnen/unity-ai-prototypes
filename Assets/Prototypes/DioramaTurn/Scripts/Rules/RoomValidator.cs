using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    /// <summary>The readability rules for a shipped room.</summary>
    public static class RoomValidator
    {
        /// <summary>Returns a list of problems. An empty list means the room is valid.</summary>
        public static IReadOnlyList<string> Validate(RoomData room, int maxX, int maxY, int maxZ)
        {
            var errors = new List<string>();
            if (room.SizeX > maxX || room.SizeY > maxY || room.SizeZ > maxZ)
            {
                errors.Add($"room is {room.SizeX}x{room.SizeY}x{room.SizeZ}, the largest allowed is {maxX}x{maxY}x{maxZ}");
            }

            var pieces = room.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = i + 1; j < pieces.Count; j++)
                {
                    for (int view = 0; view < ViewRule.ViewCount; view++)
                    {
                        if (ViewRule.Screen(pieces[i].Cell, view).Equals(ViewRule.Screen(pieces[j].Cell, view)))
                        {
                            errors.Add($"{pieces[i].Cell} and {pieces[j].Cell} share one screen spot in view {view}");
                        }
                    }
                }
            }
            return errors;
        }
    }
}
