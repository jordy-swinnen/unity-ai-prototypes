using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// The rules of Neon Pong. Plain C#, no engine physics, so tests run it step by step.
    /// The ball is a circle moving on x (length) and y (width, world z). Paddle hits use a swept
    /// check, so a fast ball cannot pass through a paddle. A seeded random source picks serves.
    /// </summary>
    public sealed class NeonPongSimulation
    {
        readonly NeonPongSettings s;
        readonly Random rng;
        readonly List<GameEvent> events = new List<GameEvent>();

        public NeonPongState State { get; } = new NeonPongState();

        /// <summary>Events from the most recent <see cref="Step"/> call.</summary>
        public IReadOnlyList<GameEvent> Events => events;

        public NeonPongSimulation(NeonPongSettings settings, int seed)
        {
            s = settings;
            rng = new Random(seed);
            NewMatch();
        }

        public void Step(float dt, NeonPongInput input)
        {
            events.Clear();
            if (State.Phase == Phase.GameOver && input.Restart)
            {
                NewMatch();
                events.Add(new GameEvent(EventKind.Restart, Side.Left, Vector2.zero, State.BallSpeed));
            }

            float remaining = dt;
            float slice = Mathf.Max(s.maxSubstep, 1e-4f);
            while (remaining > 1e-6f)
            {
                float h = Mathf.Min(remaining, slice);
                Advance(h, input);
                remaining -= h;
            }
        }

        void Advance(float h, NeonPongInput input)
        {
            if (State.Phase == Phase.GameOver)
            {
                State.PaddleVelocity[0] = 0f;
                State.PaddleVelocity[1] = 0f;
                return;
            }

            MovePaddle(Side.Left, input.Left, h);
            MovePaddle(Side.Right, input.Right, h);

            if (State.Phase == Phase.Serving)
            {
                State.ServeTimer -= h;
                if (State.ServeTimer <= 0f) Launch();
            }
            else
            {
                MoveBall(h);
            }
        }

        // --- paddles ---

        void MovePaddle(Side side, float axis, float h)
        {
            int i = (int)side;
            float target = Mathf.Clamp(axis, -1f, 1f) * s.paddleSpeed;
            float velocity = State.PaddleVelocity[i];
            if (s.paddleEaseTime <= 0f)
            {
                velocity = target;
            }
            else
            {
                float acceleration = s.paddleSpeed / s.paddleEaseTime;
                velocity = Mathf.MoveTowards(velocity, target, acceleration * h);
            }

            float limit = s.halfWidth - s.paddleHalfLength;
            float z = State.PaddleZ[i] + velocity * h;
            if (z > limit) { z = limit; velocity = 0f; }
            else if (z < -limit) { z = -limit; velocity = 0f; }

            State.PaddleZ[i] = z;
            State.PaddleVelocity[i] = velocity;
        }

        // --- serving ---

        void NewMatch()
        {
            State.Score[0] = 0;
            State.Score[1] = 0;
            State.PaddleZ[0] = 0f;
            State.PaddleZ[1] = 0f;
            State.PaddleVelocity[0] = 0f;
            State.PaddleVelocity[1] = 0f;
            State.Winner = Side.Left;
            State.ServeToward = rng.Next(2) == 0 ? Side.Left : Side.Right;
            StartServe();
        }

        void StartServe()
        {
            State.Phase = Phase.Serving;
            State.BallPos = Vector2.zero;
            State.BallVel = Vector2.zero;
            State.BallSpeed = s.baseSpeed;
            State.RallyHits = 0;
            State.ServeTimer = s.serveDelay;
        }

        void Launch()
        {
            float angle = ((float)rng.NextDouble() * 2f - 1f) * s.maxServeAngle * Mathf.Deg2Rad;
            float dir = State.ServeToward == Side.Right ? 1f : -1f;
            State.BallVel = new Vector2(dir * Mathf.Cos(angle), Mathf.Sin(angle)) * State.BallSpeed;
            State.Phase = Phase.Playing;
            events.Add(new GameEvent(EventKind.Serve, State.ServeToward, State.BallPos, State.BallSpeed));
        }

        // --- the ball ---

        void MoveBall(float h)
        {
            Vector2 pos = State.BallPos;
            Vector2 vel = State.BallVel;
            Vector2 next = pos + vel * h;

            TryPaddle(pos, ref vel, ref next, h);

            // Walls: reflect the overshoot, keep the speed.
            float wall = s.halfWidth - s.ballRadius;
            if (next.y > wall)
            {
                next.y = 2f * wall - next.y;
                vel.y = -vel.y;
                events.Add(new GameEvent(EventKind.WallHit, Side.Left, new Vector2(next.x, s.halfWidth), State.BallSpeed));
            }
            else if (next.y < -wall)
            {
                next.y = -2f * wall - next.y;
                vel.y = -vel.y;
                events.Add(new GameEvent(EventKind.WallHit, Side.Left, new Vector2(next.x, -s.halfWidth), State.BallSpeed));
            }

            State.BallPos = next;
            State.BallVel = vel;

            // End lines: the ball centre past a line scores for the other player.
            if (next.x >= s.halfLength) Score(Side.Left, next);
            else if (next.x <= -s.halfLength) Score(Side.Right, next);
        }

        /// <summary>
        /// Swept check of the ball's leading edge against the face of the paddle it is heading
        /// toward. On a hit it rewrites velocity and the end position of the step.
        /// </summary>
        void TryPaddle(Vector2 pos, ref Vector2 vel, ref Vector2 next, float h)
        {
            if (vel.x == 0f) return;

            Side side = vel.x > 0f ? Side.Right : Side.Left;
            float sign = side == Side.Right ? 1f : -1f;
            float face = sign * (s.paddleX - s.paddleHalfThickness);
            float lead = pos.x + sign * s.ballRadius;
            float leadNext = next.x + sign * s.ballRadius;

            bool crosses = sign > 0f
                ? lead <= face && leadNext >= face
                : lead >= face && leadNext <= face;
            if (!crosses) return;

            float t = Mathf.Approximately(leadNext, lead) ? 0f : (face - lead) / (leadNext - lead);
            float zHit = Mathf.Lerp(pos.y, next.y, t);
            float paddleZ = State.PaddleZ[(int)side];
            if (Mathf.Abs(zHit - paddleZ) > s.paddleHalfLength + s.ballRadius) return;

            float offset = Mathf.Clamp((zHit - paddleZ) / s.paddleHalfLength, -1f, 1f);
            float angle = offset * s.maxBounceAngle * Mathf.Deg2Rad;

            float before = State.BallSpeed;
            float speed = Mathf.Min(before + s.speedStep, s.maxSpeed);
            State.BallSpeed = speed;
            State.RallyHits++;

            vel = new Vector2(-sign * Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            var contact = new Vector2(face - sign * s.ballRadius, zHit);
            next = contact + vel * (h * (1f - t));

            events.Add(new GameEvent(EventKind.PaddleHit, side, new Vector2(face, zHit), speed));
            if (speed > before)
                events.Add(new GameEvent(EventKind.SpeedStep, side, new Vector2(face, zHit), speed));
        }

        void Score(Side scorer, Vector2 at)
        {
            State.Score[(int)scorer]++;
            events.Add(new GameEvent(EventKind.Point, scorer, at, State.BallSpeed));

            if (State.Score[(int)scorer] >= s.winScore)
            {
                State.Phase = Phase.GameOver;
                State.Winner = scorer;
                State.BallVel = Vector2.zero;
                events.Add(new GameEvent(EventKind.Win, scorer, at, State.BallSpeed));
                return;
            }

            // The next serve goes toward the player who lost the point.
            State.ServeToward = scorer == Side.Left ? Side.Right : Side.Left;
            StartServe();
        }
    }
}
