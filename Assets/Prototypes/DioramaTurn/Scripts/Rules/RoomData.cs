using System;
using System.Collections.Generic;

namespace Prototypes.DioramaTurn
{
    public enum PieceKind
    {
        /// <summary>A walkable 3D block (a Blender model).</summary>
        Block,
        /// <summary>A walkable flat sprite piece (a plank).</summary>
        Bridge,
        /// <summary>The block where Pip begins.</summary>
        Start,
        /// <summary>The block that carries the star.</summary>
        Goal,
    }

    public readonly struct Piece
    {
        public readonly Cell Cell;
        public readonly PieceKind Kind;

        public Piece(Cell cell, PieceKind kind)
        {
            Cell = cell;
            Kind = kind;
        }
    }

    public sealed class RoomFormatException : Exception
    {
        public RoomFormatException(string message) : base(message) { }
    }

    /// <summary>A room: the cells of its walkable pieces inside a box. Every piece is a node.</summary>
    public sealed class RoomData
    {
        readonly Dictionary<Cell, Piece> byCell = new Dictionary<Cell, Piece>();
        readonly List<Piece> pieces = new List<Piece>();

        public int SizeX { get; }
        public int SizeY { get; }
        public int SizeZ { get; }
        public Cell Start { get; }
        public Cell Goal { get; }
        public IReadOnlyList<Piece> Pieces => pieces;

        public RoomData(int sizeX, int sizeY, int sizeZ, IEnumerable<Piece> source)
        {
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            int starts = 0, goals = 0;
            foreach (var p in source)
            {
                var c = p.Cell;
                if (c.X < 0 || c.Y < 0 || c.Z < 0 || c.X >= sizeX || c.Y >= sizeY || c.Z >= sizeZ)
                {
                    throw new RoomFormatException($"piece {c} lies outside the {sizeX}x{sizeY}x{sizeZ} box");
                }
                if (byCell.ContainsKey(c)) throw new RoomFormatException($"two pieces on cell {c}");
                byCell.Add(c, p);
                pieces.Add(p);
                if (p.Kind == PieceKind.Start) { starts++; Start = c; }
                if (p.Kind == PieceKind.Goal) { goals++; Goal = c; }
            }
            if (starts != 1) throw new RoomFormatException($"a room needs exactly one Start, found {starts}");
            if (goals != 1) throw new RoomFormatException($"a room needs exactly one Goal, found {goals}");
        }

        public bool Contains(Cell c) => byCell.ContainsKey(c);
        public bool TryGetPiece(Cell c, out Piece piece) => byCell.TryGetValue(c, out piece);
    }
}
