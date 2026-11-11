namespace Prototypes.DioramaTurn
{
    public enum GameEventKind
    {
        TurnStarted,
        TurnSettled,
        JoinFormed,
        JoinBroken,
        StepTaken,
        HopTaken,
        Unreachable,
        TurnBlocked,
        GoalReached,
        Restarted,
    }

    /// <summary>Something that happened in the simulation, for the view, the sound and the feel to react to.</summary>
    public readonly struct GameEvent
    {
        public readonly GameEventKind Kind;
        /// <summary>The node the event is about (the step's end, the picked node, one end of a join).</summary>
        public readonly Cell Cell;
        /// <summary>The other end of a join, or the start of a step.</summary>
        public readonly Cell Other;
        public readonly int View;

        public GameEvent(GameEventKind kind, Cell cell, Cell other, int view)
        {
            Kind = kind;
            Cell = cell;
            Other = other;
            View = view;
        }
    }
}
