using System.Collections.Generic;
using System.Linq;

namespace Prototypes.MedievalChess
{
    public enum GameStatus
    {
        Ongoing,
        Checkmate,
        Stalemate,
        ThreefoldRepetition,
        FiftyMoveRule,
        InsufficientMaterial,
        Resignation,
    }

    /// <summary>
    /// A game of chess: the position, the moves played, and how the game stands. It refuses illegal moves,
    /// counts repetitions, and decides when the game is over (checkmate, stalemate, the draw rules, resignation).
    /// </summary>
    public sealed class ChessGame
    {
        readonly List<Move> history = new List<Move>();
        readonly Dictionary<ulong, int> seen = new Dictionary<ulong, int>();
        List<Move> legal = new List<Move>();

        public Position Position { get; }
        public GameStatus Status { get; private set; }
        /// <summary>The side that won, or null for a draw or a game still going.</summary>
        public ChessSide? Winner { get; private set; }
        public IReadOnlyList<Move> History => history;
        public IReadOnlyList<Move> LegalMoves => legal;
        public bool IsOver => Status != GameStatus.Ongoing;

        public ChessGame() : this(Position.Start()) { }

        public ChessGame(Position position)
        {
            Position = position;
            seen[Position.RepetitionKey()] = 1;
            Refresh();
        }

        public IEnumerable<Move> LegalMovesFrom(int square) => legal.Where(m => m.From == square);

        /// <summary>The legal move with these squares and promotion, or an invalid move if there is none.</summary>
        public Move FindLegal(int from, int to, PieceType? promotion)
        {
            foreach (var m in legal) if (m.Matches(from, to, promotion)) return m;
            return default;
        }

        public bool TryMake(Move move)
        {
            if (IsOver) return false;
            var found = FindLegal(move.From, move.To, move.Promotion);
            if (!found.IsValid) return false;

            Position.MakeMove(found);
            history.Add(found);
            var key = Position.RepetitionKey();
            seen[key] = seen.TryGetValue(key, out int count) ? count + 1 : 1;
            Refresh();
            return true;
        }

        /// <summary>Makes a move written as e2e4 (or e7e8q for a promotion). Returns false if it is not legal.</summary>
        public bool TryMakeUci(string uci)
        {
            if (uci == null || (uci.Length != 4 && uci.Length != 5)) return false;
            if (!Squares.TryParse(uci.Substring(0, 2), out int from) || !Squares.TryParse(uci.Substring(2, 2), out int to)) return false;
            PieceType? promotion = null;
            if (uci.Length == 5)
            {
                switch (char.ToLowerInvariant(uci[4]))
                {
                    case 'q': promotion = PieceType.Queen; break;
                    case 'r': promotion = PieceType.Rook; break;
                    case 'b': promotion = PieceType.Bishop; break;
                    case 'n': promotion = PieceType.Knight; break;
                    default: return false;
                }
            }
            return TryMake(new Move(from, to, MoveFlags.None, promotion));
        }

        public void Resign(ChessSide side)
        {
            if (IsOver) return;
            Status = GameStatus.Resignation;
            Winner = Pieces.Opposite(side);
            legal = new List<Move>();
        }

        // Works out the legal moves and how the game stands after a move.
        void Refresh()
        {
            legal = MoveGenerator.Legal(Position);
            Winner = null;
            if (legal.Count == 0)
            {
                if (Position.InCheck())
                {
                    Status = GameStatus.Checkmate;
                    Winner = Pieces.Opposite(Position.SideToMove);
                }
                else Status = GameStatus.Stalemate;
            }
            else if (Position.IsInsufficientMaterial()) Status = GameStatus.InsufficientMaterial;
            else if (seen[Position.RepetitionKey()] >= 3) Status = GameStatus.ThreefoldRepetition;
            else if (Position.HalfmoveClock >= 100) Status = GameStatus.FiftyMoveRule;
            else Status = GameStatus.Ongoing;

            if (IsOver) legal = new List<Move>(); // nothing more can be played
        }
    }
}
