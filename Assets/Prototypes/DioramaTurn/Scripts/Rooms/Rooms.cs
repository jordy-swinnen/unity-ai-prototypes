using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    public sealed class RoomDefinition
    {
        public readonly string Name;
        /// <summary>Layers from the bottom up; rows are z, columns are x. See <see cref="RoomParser"/>.</summary>
        public readonly string[][] Layers;
        /// <summary>The least number of turns (checked against the solver by a test).</summary>
        public readonly int Par;

        public RoomDefinition(string name, string[][] layers, int par)
        {
            Name = name;
            Layers = layers;
            Par = par;
        }
    }

    /// <summary>
    /// The three shipped rooms. They were found with a search over room shapes that keeps the readability
    /// rule (no two nodes on one screen spot in any view), then checked here by the solver.
    /// </summary>
    public static class Rooms
    {
        static readonly RoomDefinition[] All_ =
        {
            new RoomDefinition("First Turn", new[]
            {
                new[] { ".....", "...sb", "....b", ".....", "....." },
                new[] { ".....", ".....", ".....", ".....", "..bbg" },
            }, 1),
            new RoomDefinition("Plank", new[]
            {
                new[] { ".....", ".....", ".b...", ".bs..", "....." },
                new[] { ".....", "...br", ".....", ".....", "....." },
                new[] { "gb...", ".....", ".....", ".....", "....." },
            }, 2),
            new RoomDefinition("Around Again", new[]
            {
                new[] { "br...", ".....", ".....", "....s", "....b" },
                new[] { "...b.", "...r.", ".....", ".....", "....." },
                new[] { ".....", ".....", "g....", "b....", "....." },
            }, 5),
        };

        public static IReadOnlyList<RoomDefinition> All => All_;

        public static RoomData Build(int index) => RoomParser.Parse(All_[index].Layers);
    }
}
