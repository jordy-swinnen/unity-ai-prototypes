using UnityEngine;

namespace Prototypes.NeonPong
{
    /// <summary>Left is player 1 (negative x), Right is player 2 (positive x).</summary>
    public enum Side
    {
        Left = 0,
        Right = 1,
    }

    public enum Phase
    {
        Serving,
        Playing,
        GameOver,
    }

    public enum EventKind
    {
        Serve,
        WallHit,
        PaddleHit,
        SpeedStep,
        Point,
        Win,
        Restart,
    }

    /// <summary>Something that happened during a step, for the feel layer to react to.</summary>
    public readonly struct GameEvent
    {
        public readonly EventKind Kind;
        /// <summary>The paddle side for a paddle hit, the scorer for a point or win.</summary>
        public readonly Side Side;
        /// <summary>Where it happened: x is the table length, y is the table width (world z).</summary>
        public readonly Vector2 Position;
        /// <summary>The ball speed after the event.</summary>
        public readonly float Speed;

        public GameEvent(EventKind kind, Side side, Vector2 position, float speed)
        {
            Kind = kind;
            Side = side;
            Position = position;
            Speed = speed;
        }
    }

    /// <summary>One number per player from -1 to 1 (positive is toward +z), and a restart key.</summary>
    public struct NeonPongInput
    {
        public float Left;
        public float Right;
        public bool Restart;
    }
}
