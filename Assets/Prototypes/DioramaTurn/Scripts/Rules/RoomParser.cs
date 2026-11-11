using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    /// <summary>
    /// Reads a room from text maps. Layers go from the bottom (y = 0) up. In a layer, row r is z = r and
    /// column c is x = c. Characters: . empty, b block, r bridge, s start, g goal. Spaces are ignored.
    /// </summary>
    public static class RoomParser
    {
        public static RoomData Parse(IReadOnlyList<string[]> layers)
        {
            if (layers == null || layers.Count == 0) throw new RoomFormatException("a room needs at least one layer");
            int sizeZ = layers[0].Length;
            int sizeX = -1;
            var pieces = new List<Piece>();
            for (int y = 0; y < layers.Count; y++)
            {
                var rows = layers[y];
                if (rows.Length != sizeZ) throw new RoomFormatException($"layer {y} has {rows.Length} rows, layer 0 has {sizeZ}");
                for (int z = 0; z < rows.Length; z++)
                {
                    var row = rows[z].Replace(" ", "");
                    if (sizeX < 0) sizeX = row.Length;
                    if (row.Length != sizeX) throw new RoomFormatException($"layer {y} row {z} has {row.Length} columns, expected {sizeX}");
                    for (int x = 0; x < row.Length; x++)
                    {
                        char ch = row[x];
                        var cell = new Cell(x, y, z);
                        switch (ch)
                        {
                            case '.': break;
                            case 'b': pieces.Add(new Piece(cell, PieceKind.Block)); break;
                            case 'r': pieces.Add(new Piece(cell, PieceKind.Bridge)); break;
                            case 's': pieces.Add(new Piece(cell, PieceKind.Start)); break;
                            case 'g': pieces.Add(new Piece(cell, PieceKind.Goal)); break;
                            default:
                                throw new RoomFormatException($"unknown character '{ch}' in layer {y} row {z} column {x}");
                        }
                    }
                }
            }
            return new RoomData(sizeX, layers.Count, sizeZ, pieces);
        }

        /// <summary>One string, layers separated by a line holding only ---, bottom layer first.</summary>
        public static RoomData ParseText(string text)
        {
            var layers = new List<string[]>();
            var rows = new List<string>();
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line == "---")
                {
                    layers.Add(rows.ToArray());
                    rows.Clear();
                }
                else if (line.Length > 0)
                {
                    rows.Add(line);
                }
            }
            layers.Add(rows.ToArray());
            return Parse(layers);
        }
    }
}
