using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Prototypes.MedievalChess
{
    public sealed class SearchOptions
    {
        public int MaxDepth = 4;
        public bool Quiescence = true;
        public double TimeCapSeconds = 1.0;
        public int SlackCentipawns;
    }

    public sealed class SearchResult
    {
        /// <summary>The move chosen. With slack this is picked at random among the near-best moves.</summary>
        public Move Best;
        /// <summary>The move that scored highest.</summary>
        public Move BestByScore;
        public int Score;
        /// <summary>The deepest search that finished.</summary>
        public int Depth;
        public long Nodes;
        public double Seconds;
        /// <summary>True when the time cap or a cancel cut the search short.</summary>
        public bool Stopped;
        public List<Move> Candidates = new List<Move>();
    }

    /// <summary>
    /// Alpha-beta search with iterative deepening, a transposition table, move ordering, a search of
    /// captures at the end of each line, and a clock. It touches only its own copy of the data, so it can
    /// run on another thread. One searcher is for one search at a time.
    /// </summary>
    public sealed class Searcher
    {
        const int MaxPly = 64;
        const int Infinity = 1000000;
        const int FlagExact = 0, FlagLower = 1, FlagUpper = 2;
        static readonly int[] OrderValue = { 1, 3, 3, 5, 9, 10 };

        struct Entry
        {
            public ulong Key;
            public Move Move;
            public int Score;
            public short Depth;
            public byte Flag;
        }

        readonly Evaluator evaluator;
        readonly EvaluationSettings s;
        readonly int mate;
        readonly Entry[] table;
        readonly int tableMask;

        readonly List<Move>[] moveBuffers = new List<Move>[MaxPly + 2];
        readonly int[][] scoreBuffers = new int[MaxPly + 2][];
        readonly Move[,] killers = new Move[MaxPly + 2, 2];
        readonly int[] history = new int[2 * 64 * 64];
        readonly ulong[] path = new ulong[1024];

        // state of the search in progress
        Position pos;
        SearchOptions options;
        CancellationToken token;
        Stopwatch watch;
        double cap;
        long nodes;
        bool stopped;
        int pathLength;

        public Searcher(Evaluator evaluator, EvaluationSettings settings, int tableLog2)
        {
            this.evaluator = evaluator;
            s = settings;
            mate = settings.mateScore;
            if (tableLog2 > 0)
            {
                table = new Entry[1 << tableLog2];
                tableMask = table.Length - 1;
            }
            for (int i = 0; i < moveBuffers.Length; i++)
            {
                moveBuffers[i] = new List<Move>(64);
                scoreBuffers[i] = new int[256];
            }
        }

        int MateThreshold => mate - MaxPly - 100;

        public SearchResult Search(Position position, IReadOnlyList<ulong> gameHashes, SearchOptions opts, Random rng, CancellationToken ct)
        {
            pos = position;
            options = opts;
            token = ct;
            cap = opts.TimeCapSeconds;
            nodes = 0;
            stopped = false;
            watch = Stopwatch.StartNew();
            if (table != null) Array.Clear(table, 0, table.Length);
            Array.Clear(history, 0, history.Length);
            Array.Clear(killers, 0, killers.Length);

            // the path of positions so far: the game's recent ones (back to the last irreversible move), then the search's own
            pathLength = 0;
            int keep = Math.Min(gameHashes.Count, position.HalfmoveClock + 1);
            for (int i = gameHashes.Count - keep; i < gameHashes.Count; i++) path[pathLength++] = gameHashes[i];
            if (pathLength == 0 || path[pathLength - 1] != position.Hash) path[pathLength++] = position.Hash;

            var result = new SearchResult();
            var rootMoves = new List<Move>(64);
            MoveGenerator.GenerateLegal(pos, rootMoves);
            if (rootMoves.Count == 0)
            {
                result.Score = pos.InCheck() ? -mate : 0;
                result.Seconds = watch.Elapsed.TotalSeconds;
                return result;
            }

            OrderRoot(rootMoves);
            var best = rootMoves[0];
            int bestScore = 0;
            int completed = 0;
            var rootScores = new int[rootMoves.Count];

            if (token.IsCancellationRequested) stopped = true;

            for (int depth = 1; depth <= opts.MaxDepth && !stopped; depth++)
            {
                if (depth > 1 && watch.Elapsed.TotalSeconds > cap * 0.4) break; // the next one would not finish

                int alpha = -Infinity;
                int iterationBest = -Infinity;
                Move iterationMove = default;
                var scores = new int[rootMoves.Count];
                for (int i = 0; i < rootMoves.Count; i++)
                {
                    Push(rootMoves[i]);
                    int score = -Negamax(depth - 1, 1, -Infinity, -alpha);
                    Pop();
                    if (stopped) break;
                    scores[i] = score;
                    if (score > iterationBest)
                    {
                        iterationBest = score;
                        iterationMove = rootMoves[i];
                        if (score > alpha) alpha = score;
                    }
                }
                if (stopped) break;

                best = iterationMove;
                bestScore = iterationBest;
                completed = depth;
                rootScores = scores;
                SortRoot(rootMoves, rootScores);
                if (Math.Abs(bestScore) >= MateThreshold) break; // a mate is found, and it is the shortest this deep
            }

            result.BestByScore = best;
            result.Score = bestScore;
            result.Depth = completed;
            result.Candidates.Add(best);

            // Slack: other moves that are nearly as good, found with a narrow search around the line they must reach.
            if (options.SlackCentipawns > 0 && completed >= 1 && Math.Abs(bestScore) < MateThreshold && !stopped)
            {
                int threshold = bestScore - options.SlackCentipawns;
                foreach (var m in rootMoves)
                {
                    if (m.Equals(best)) continue;
                    Push(m);
                    int score = -Negamax(completed - 1, 1, -threshold, -(threshold - 1));
                    Pop();
                    if (stopped) break;
                    if (score >= threshold) result.Candidates.Add(m);
                }
            }

            result.Best = result.Candidates.Count == 1 ? result.Candidates[0] : result.Candidates[rng.Next(result.Candidates.Count)];
            result.Nodes = nodes;
            result.Stopped = stopped;
            result.Seconds = watch.Elapsed.TotalSeconds;
            return result;
        }

        // --- making moves along the search path ---

        void Push(Move m)
        {
            pos.MakeMove(m);
            path[pathLength++] = pos.Hash;
        }

        void Pop()
        {
            pathLength--;
            pos.UnmakeMove();
        }

        bool IsRepetition()
        {
            // only positions since the last capture or pawn move can repeat, and only with the same side to move
            int limit = Math.Max(0, pathLength - 1 - pos.HalfmoveClock);
            for (int i = pathLength - 3; i >= limit; i -= 2)
                if (path[i] == pos.Hash) return true;
            return false;
        }

        // --- the search ---

        int Negamax(int depth, int ply, int alpha, int beta)
        {
            nodes++;
            if ((nodes & 1023) == 0 && CheckStop()) return 0;
            if (stopped) return 0;

            if (pos.HalfmoveClock >= 100 || IsRepetition()) return 0;
            if (ply >= MaxPly - 1) return evaluator.Evaluate(pos);

            // no mate can be found that is slower than one already found
            alpha = Math.Max(alpha, -mate + ply);
            beta = Math.Min(beta, mate - ply - 1);
            if (alpha >= beta) return alpha;

            bool inCheck = pos.InCheck();
            if (inCheck && depth <= 0) depth = 1; // never stop in check
            if (depth <= 0)
                return options.Quiescence ? Quiesce(alpha, beta, ply, s.maxQuiescenceDepth) : evaluator.Evaluate(pos);

            int alphaOriginal = alpha;
            Move ttMove = default;
            if (table != null)
            {
                ref var e = ref table[(int)(pos.Hash & (ulong)tableMask)];
                if (e.Key == pos.Hash)
                {
                    ttMove = e.Move;
                    if (e.Depth >= depth)
                    {
                        int stored = FromTable(e.Score, ply);
                        if (e.Flag == FlagExact) return stored;
                        if (e.Flag == FlagLower) alpha = Math.Max(alpha, stored);
                        else beta = Math.Min(beta, stored);
                        if (alpha >= beta) return stored;
                    }
                }
            }

            var moves = moveBuffers[ply];
            MoveGenerator.GenerateLegal(pos, moves);
            if (moves.Count == 0) return inCheck ? -mate + ply : 0;

            var order = scoreBuffers[ply];
            ScoreMoves(moves, order, ttMove, ply);

            int best = -Infinity;
            Move bestMove = default;
            for (int i = 0; i < moves.Count; i++)
            {
                Pick(moves, order, i);
                var m = moves[i];
                Push(m);
                int score = -Negamax(depth - 1, ply + 1, -beta, -alpha);
                Pop();
                if (stopped) return 0;

                if (score > best)
                {
                    best = score;
                    bestMove = m;
                    if (score > alpha)
                    {
                        alpha = score;
                        if (alpha >= beta)
                        {
                            if (!m.IsCapture && !m.Promotion.HasValue) RememberQuiet(m, ply, depth);
                            break;
                        }
                    }
                }
            }

            if (table != null)
            {
                ref var e = ref table[(int)(pos.Hash & (ulong)tableMask)];
                e.Key = pos.Hash;
                e.Move = bestMove;
                e.Depth = (short)depth;
                e.Score = ToTable(best, ply);
                e.Flag = best <= alphaOriginal ? (byte)FlagUpper : best >= beta ? (byte)FlagLower : (byte)FlagExact;
            }
            return best;
        }

        // The end of a line: let the captures play out so the score is not taken in the middle of an exchange.
        int Quiesce(int alpha, int beta, int ply, int qDepth)
        {
            nodes++;
            if ((nodes & 1023) == 0 && CheckStop()) return 0;
            if (stopped) return 0;
            if (pos.HalfmoveClock >= 100) return 0;
            if (ply >= MaxPly - 1) return evaluator.Evaluate(pos);

            bool inCheck = pos.InCheck();
            int standPat = evaluator.Evaluate(pos);
            if (!inCheck)
            {
                if (standPat >= beta) return standPat;
                if (standPat > alpha) alpha = standPat;
            }
            if (qDepth <= 0) return standPat;

            var moves = moveBuffers[ply];
            if (inCheck) MoveGenerator.GenerateLegal(pos, moves);
            else MoveGenerator.GenerateLegalCaptures(pos, moves);
            if (inCheck && moves.Count == 0) return -mate + ply;
            if (moves.Count == 0) return alpha > standPat ? alpha : standPat;

            var order = scoreBuffers[ply];
            ScoreMoves(moves, order, default, ply);
            int best = inCheck ? -Infinity : standPat;
            for (int i = 0; i < moves.Count; i++)
            {
                Pick(moves, order, i);
                Push(moves[i]);
                int score = -Quiesce(-beta, -alpha, ply + 1, qDepth - 1);
                Pop();
                if (stopped) return 0;
                if (score > best)
                {
                    best = score;
                    if (score > alpha)
                    {
                        alpha = score;
                        if (alpha >= beta) break;
                    }
                }
            }
            return best;
        }

        bool CheckStop()
        {
            if (token.IsCancellationRequested || watch.Elapsed.TotalSeconds >= cap) stopped = true;
            return stopped;
        }

        // --- move ordering ---

        // The transposition move first, then captures (the biggest victim by the smallest attacker), promotions, killers, history.
        void ScoreMoves(List<Move> moves, int[] scores, Move ttMove, int ply)
        {
            int side = (int)pos.SideToMove;
            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                int score;
                if (ttMove.IsValid && m.Equals(ttMove)) score = 1000000;
                else if (m.IsCapture)
                {
                    int victim = (m.Flags & MoveFlags.EnPassant) != 0 ? 0 : (int)Pieces.TypeOf(pos[m.To]);
                    int attacker = (int)Pieces.TypeOf(pos[m.From]);
                    score = 100000 + OrderValue[victim] * 100 - OrderValue[attacker] + (m.Promotion.HasValue ? 5000 : 0);
                }
                else if (m.Promotion.HasValue) score = 90000 + OrderValue[(int)m.Promotion.Value];
                else if (m.Equals(killers[ply, 0])) score = 80000;
                else if (m.Equals(killers[ply, 1])) score = 79000;
                else score = history[(side * 64 + m.From) * 64 + m.To];
                scores[i] = score;
            }
        }

        // Moves the best remaining move to position i, so a cutoff stops us sorting the rest.
        static void Pick(List<Move> moves, int[] scores, int i)
        {
            int best = i;
            for (int j = i + 1; j < moves.Count; j++) if (scores[j] > scores[best]) best = j;
            if (best == i) return;
            (moves[i], moves[best]) = (moves[best], moves[i]);
            (scores[i], scores[best]) = (scores[best], scores[i]);
        }

        void RememberQuiet(Move m, int ply, int depth)
        {
            if (!m.Equals(killers[ply, 0]))
            {
                killers[ply, 1] = killers[ply, 0];
                killers[ply, 0] = m;
            }
            int index = ((int)pos.SideToMove * 64 + m.From) * 64 + m.To;
            history[index] = Math.Min(history[index] + depth * depth, 70000);
        }

        void OrderRoot(List<Move> moves)
        {
            var scores = new int[moves.Count];
            ScoreMoves(moves, scores, default, 0);
            for (int i = 0; i < moves.Count; i++) Pick(moves, scores, i);
        }

        static void SortRoot(List<Move> moves, int[] scores)
        {
            // best first, so the next iteration starts with the move that looked best
            var order = new int[moves.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, (a, b) => scores[b].CompareTo(scores[a]));
            var copy = moves.ToArray();
            for (int i = 0; i < order.Length; i++) moves[i] = copy[order[i]];
        }

        // --- mate scores in the table are stored as "mate in n from here" ---

        int ToTable(int score, int ply)
        {
            if (score >= MateThreshold) return score + ply;
            if (score <= -MateThreshold) return score - ply;
            return score;
        }

        int FromTable(int score, int ply)
        {
            if (score >= MateThreshold) return score - ply;
            if (score <= -MateThreshold) return score + ply;
            return score;
        }
    }
}
