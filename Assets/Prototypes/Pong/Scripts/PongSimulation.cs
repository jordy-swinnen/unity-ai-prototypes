using System;
using UnityEngine;
using Random = System.Random;

namespace Prototypes.Pong
{
    /// <summary>
    /// The rules of Pong. Plain C# with no Unity physics, so tests can run it step by step.
    /// The only random choice is the serve, and it comes from a seeded generator, so the same
    /// seed and the same inputs always give the same game.
    /// </summary>
    public sealed class PongSimulation
    {
        // Biggest slice of time moved in one go. At the top speed of 20 this is about 0.17 m,
        // far less than a paddle's thickness, so a fast ball can't skip over a paddle.
        const float MaxSubstep = 1f / 120f;

        readonly PongSettings settings;
        readonly Random random;

        public PongState State { get; } = new PongState();

        public PongSimulation(PongSettings settings, int seed)
        {
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            random = new Random(seed);
            Restart();
        }

        /// <summary>Starts a new game: scores and paddles reset, and the first serve goes either way.</summary>
        public void Restart()
        {
            State.Score1 = 0;
            State.Score2 = 0;
            State.Winner = 0;
            State.Paddle1Z = 0f;
            State.Paddle2Z = 0f;
            PrepareServe(random.Next(2) == 0 ? -1f : 1f);
        }

        /// <summary>Moves the game forward by <paramref name="deltaTime"/> seconds.</summary>
        public void Step(float deltaTime, PongInput input)
        {
            float remaining = deltaTime;
            while (remaining > 1e-6f)
            {
                float slice = Mathf.Min(remaining, MaxSubstep);
                StepSlice(slice, input);
                remaining -= slice;
            }
        }

        void StepSlice(float dt, PongInput input)
        {
            MovePaddles(dt, input);

            switch (State.Phase)
            {
                case PongPhase.Serving:
                    State.ServeTimer -= dt;
                    if (State.ServeTimer <= 0f)
                    {
                        Launch();
                    }
                    break;
                case PongPhase.Playing:
                    MoveBall(dt);
                    break;
            }
        }

        void MovePaddles(float dt, PongInput input)
        {
            float limit = settings.PaddleLimit;
            State.Paddle1Z = Mathf.Clamp(State.Paddle1Z + Mathf.Clamp(input.Player1, -1f, 1f) * settings.paddleSpeed * dt, -limit, limit);
            State.Paddle2Z = Mathf.Clamp(State.Paddle2Z + Mathf.Clamp(input.Player2, -1f, 1f) * settings.paddleSpeed * dt, -limit, limit);
        }

        void PrepareServe(float sign)
        {
            State.Phase = PongPhase.Serving;
            State.ServeTimer = settings.serveDelay;
            State.ServeSign = sign;
            State.ServeAngleDegrees = (float)(random.NextDouble() * 2.0 - 1.0) * settings.maxServeAngleDegrees;
            State.BallPosition = Vector2.zero;
            State.BallVelocity = Vector2.zero;
            State.BallSpeed = settings.ballBaseSpeed;
        }

        void Launch()
        {
            float radians = State.ServeAngleDegrees * Mathf.Deg2Rad;
            State.BallSpeed = settings.ballBaseSpeed;
            State.BallVelocity = new Vector2(State.ServeSign * Mathf.Cos(radians), Mathf.Sin(radians)) * State.BallSpeed;
            State.Phase = PongPhase.Playing;
        }

        void MoveBall(float dt)
        {
            Vector2 previous = State.BallPosition;
            Vector2 position = previous + State.BallVelocity * dt;

            // Paddles are checked on the straight path the ball took this slice.
            if (State.BallVelocity.x > 0f)
            {
                TryPaddleHit(previous, ref position, dt, State.Paddle2Z, 1f);
            }
            else if (State.BallVelocity.x < 0f)
            {
                TryPaddleHit(previous, ref position, dt, State.Paddle1Z, -1f);
            }

            // Walls: mirror the ball back into the table.
            float limit = settings.fieldHalfWidth - settings.ballRadius;
            if (position.y > limit)
            {
                position.y = 2f * limit - position.y;
                State.BallVelocity.y = -State.BallVelocity.y;
            }
            else if (position.y < -limit)
            {
                position.y = -2f * limit - position.y;
                State.BallVelocity.y = -State.BallVelocity.y;
            }

            State.BallPosition = position;

            if (position.x > settings.fieldHalfLength)
            {
                ScorePoint(1);
            }
            else if (position.x < -settings.fieldHalfLength)
            {
                ScorePoint(2);
            }
        }

        // side is +1 for the right paddle (player 2) and -1 for the left paddle (player 1).
        void TryPaddleHit(Vector2 previous, ref Vector2 position, float dt, float paddleZ, float side)
        {
            float contactX = settings.PaddleContactX * side;
            bool crossed = side > 0f
                ? previous.x <= contactX && position.x > contactX
                : previous.x >= contactX && position.x < contactX;
            if (!crossed)
            {
                return;
            }

            float fraction = (contactX - previous.x) / (position.x - previous.x);
            float hitZ = previous.y + (position.y - previous.y) * fraction;
            float offset = hitZ - paddleZ;
            if (Mathf.Abs(offset) > settings.paddleHalfLength + settings.ballRadius)
            {
                return;
            }

            // Middle of the paddle goes straight back. The edges go out at the maximum angle.
            float normalised = Mathf.Clamp(offset / settings.paddleHalfLength, -1f, 1f);
            float radians = normalised * settings.maxBounceAngleDegrees * Mathf.Deg2Rad;
            State.BallSpeed = Mathf.Min(State.BallSpeed + settings.ballSpeedStep, settings.ballMaxSpeed);
            State.BallVelocity = new Vector2(-side * Mathf.Cos(radians), Mathf.Sin(radians)) * State.BallSpeed;

            // Spend the rest of this slice moving away from the paddle.
            position = new Vector2(contactX, hitZ) + State.BallVelocity * (dt * (1f - fraction));
        }

        void ScorePoint(int player)
        {
            if (player == 1)
            {
                State.Score1++;
            }
            else
            {
                State.Score2++;
            }

            int score = player == 1 ? State.Score1 : State.Score2;
            if (score >= settings.winScore)
            {
                State.Phase = PongPhase.GameOver;
                State.Winner = player;
                State.BallPosition = Vector2.zero;
                State.BallVelocity = Vector2.zero;
                State.BallSpeed = settings.ballBaseSpeed;
                return;
            }

            // The next serve goes toward the player who just lost the point.
            PrepareServe(player == 1 ? 1f : -1f);
        }
    }
}
