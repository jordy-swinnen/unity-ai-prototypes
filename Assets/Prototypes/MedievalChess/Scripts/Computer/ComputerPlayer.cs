using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Prototypes.MedievalChess
{
    /// <summary>What the computer chose, and how it got there.</summary>
    public sealed class ComputerChoice
    {
        public Move Move;
        public SearchResult Search;
        /// <summary>True when the move is a deliberate slip (a random legal move).</summary>
        public bool Blundered;
        /// <summary>True when the player was cancelled before it answered.</summary>
        public bool Cancelled;
    }

    /// <summary>
    /// Picks the computer's move for a level. It sees only the game (the position and the positions seen
    /// so far) and plays by the same rules as anyone else. <see cref="ChooseAsync"/> runs the search on another
    /// thread, waits out the level's minimum think time, and can be cancelled.
    /// </summary>
    public sealed class ComputerPlayer
    {
        readonly EvaluationSettings settings;

        public ComputerPlayer(EvaluationSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>Chooses a move now, on this thread, with no pause for show.</summary>
        public ComputerChoice Choose(ChessGame game, DifficultyProfile profile, Random rng, CancellationToken token)
        {
            var legal = game.LegalMoves;
            if (legal.Count == 0) return new ComputerChoice { Search = new SearchResult() };

            bool blunder = profile.blunderOneIn > 0 && rng.Next(profile.blunderOneIn) == 0;
            var searcher = new Searcher(new Evaluator(settings), settings, profile.tableLog2);
            var options = new SearchOptions
            {
                MaxDepth = Math.Max(1, profile.depth),
                Quiescence = profile.quiescence,
                TimeCapSeconds = profile.timeCapSeconds,
                SlackCentipawns = profile.slackCentipawns,
            };
            var result = searcher.Search(game.Position.Clone(), game.Hashes, options, rng, token);
            var move = result.Best;
            if (blunder || !move.IsValid) move = legal[rng.Next(legal.Count)];
            return new ComputerChoice { Move = move, Search = result, Blundered = blunder };
        }

        /// <summary>
        /// Chooses a move on another thread. The game is copied first, so it can change meanwhile.
        /// Cancelling stops the search at once and the task still finishes (with the best move found, or none).
        /// </summary>
        public Task<ComputerChoice> ChooseAsync(ChessGame game, DifficultyProfile profile, int seed, CancellationToken token)
        {
            var snapshot = game.Snapshot();
            var profileCopy = profile.Clone();
            return Task.Run(async () =>
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var choice = Choose(snapshot, profileCopy, new Random(seed), token);
                double remaining = profileCopy.minThinkSeconds - clock.Elapsed.TotalSeconds;
                if (remaining > 0 && !token.IsCancellationRequested)
                {
                    try { await Task.Delay(TimeSpan.FromSeconds(remaining), token); }
                    catch (OperationCanceledException) { choice.Cancelled = true; }
                }
                if (token.IsCancellationRequested) choice.Cancelled = true;
                return choice;
            });
        }
    }
}
