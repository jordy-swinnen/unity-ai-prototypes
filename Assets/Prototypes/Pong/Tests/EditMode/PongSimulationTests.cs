using NUnit.Framework;
using UnityEngine;

namespace Prototypes.Pong.Tests
{
    public class PongSimulationTests
    {
        const float Tolerance = 0.001f;

        PongSettings settings;
        PongSimulation sim;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<PongSettings>();
            sim = new PongSimulation(settings, 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        // Puts the game in the middle of a rally with the ball where the test wants it.
        void StartRally(Vector2 position, Vector2 velocity)
        {
            var state = sim.State;
            state.Phase = PongPhase.Playing;
            state.BallPosition = position;
            state.BallVelocity = velocity;
            state.BallSpeed = velocity.magnitude;
        }

        static Vector2 FromAngle(float degrees, float speed)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
        }

        // ---- settings ----

        [Test]
        public void Settings_HaveTheValuesFromTheSpec()
        {
            Assert.AreEqual(8f, settings.ballBaseSpeed);
            Assert.AreEqual(0.75f, settings.ballSpeedStep);
            Assert.AreEqual(20f, settings.ballMaxSpeed);
            Assert.AreEqual(60f, settings.maxBounceAngleDegrees);
            Assert.AreEqual(10f, settings.paddleSpeed);
            Assert.AreEqual(5, settings.winScore);
            Assert.AreEqual(1f, settings.serveDelay);
        }

        // ---- walls ----

        [Test]
        public void Ball_ReflectsOffTheTopWall_WithEqualAngle()
        {
            Vector2 velocity = FromAngle(30f, 8f);
            StartRally(new Vector2(0f, 4.7f), velocity);

            sim.Step(0.05f, default);

            Assert.AreEqual(velocity.x, sim.State.BallVelocity.x, Tolerance);
            Assert.AreEqual(-velocity.y, sim.State.BallVelocity.y, Tolerance);
            Assert.AreEqual(8f, sim.State.BallVelocity.magnitude, Tolerance);
            Assert.LessOrEqual(sim.State.BallPosition.y, settings.fieldHalfWidth - settings.ballRadius + Tolerance);
        }

        [Test]
        public void Ball_ReflectsOffTheBottomWall_WithEqualAngle()
        {
            Vector2 velocity = FromAngle(-30f, 8f);
            StartRally(new Vector2(0f, -4.7f), velocity);

            sim.Step(0.05f, default);

            Assert.AreEqual(velocity.x, sim.State.BallVelocity.x, Tolerance);
            Assert.AreEqual(-velocity.y, sim.State.BallVelocity.y, Tolerance);
            Assert.GreaterOrEqual(sim.State.BallPosition.y, -(settings.fieldHalfWidth - settings.ballRadius) - Tolerance);
        }

        // ---- paddles ----

        [Test]
        public void PaddleCentreHit_SendsTheBallStraightBack()
        {
            StartRally(new Vector2(6.9f, 0f), new Vector2(8f, 0f));

            sim.Step(0.05f, default);

            Assert.Less(sim.State.BallVelocity.x, 0f);
            Assert.AreEqual(0f, sim.State.BallVelocity.y, Tolerance);
            Assert.AreEqual(0, sim.State.Score1);
        }

        [Test]
        public void PaddleTopEdgeHit_SendsTheBallBackAtTheMaximumAngle()
        {
            StartRally(new Vector2(6.9f, settings.paddleHalfLength), new Vector2(8f, 0f));

            sim.Step(0.05f, default);

            Vector2 v = sim.State.BallVelocity;
            Assert.Less(v.x, 0f);
            Assert.AreEqual(settings.maxBounceAngleDegrees, Mathf.Atan2(v.y, -v.x) * Mathf.Rad2Deg, 0.1f);
        }

        [Test]
        public void PaddleBottomEdgeHit_SendsTheBallBackAtTheMaximumAngleDownward()
        {
            StartRally(new Vector2(6.9f, -settings.paddleHalfLength), new Vector2(8f, 0f));

            sim.Step(0.05f, default);

            Vector2 v = sim.State.BallVelocity;
            Assert.Less(v.x, 0f);
            Assert.AreEqual(-settings.maxBounceAngleDegrees, Mathf.Atan2(v.y, -v.x) * Mathf.Rad2Deg, 0.1f);
        }

        [Test]
        public void LeftPaddleHit_SendsTheBallToTheRight()
        {
            StartRally(new Vector2(-6.9f, 0f), new Vector2(-8f, 0f));

            sim.Step(0.05f, default);

            Assert.Greater(sim.State.BallVelocity.x, 0f);
            Assert.AreEqual(0, sim.State.Score2);
        }

        [Test]
        public void BallMissingThePaddle_DoesNotBounce()
        {
            StartRally(new Vector2(6.9f, 2.5f), new Vector2(8f, 0f));

            sim.Step(0.2f, default);

            Assert.AreEqual(1, sim.State.Score1);
            Assert.AreEqual(0, sim.State.Score2);
        }

        [Test]
        public void FastBall_DoesNotPassThroughAPaddleInOneBigStep()
        {
            StartRally(new Vector2(6.5f, 0f), new Vector2(settings.ballMaxSpeed, 0f));

            sim.Step(0.1f, default);

            Assert.Less(sim.State.BallVelocity.x, 0f);
            Assert.AreEqual(0, sim.State.Score1);
        }

        // ---- speed ----

        [Test]
        public void EachPaddleHit_RaisesTheSpeedByTheStep()
        {
            StartRally(new Vector2(6.9f, 0f), new Vector2(settings.ballBaseSpeed, 0f));

            sim.Step(0.05f, default);

            float expected = settings.ballBaseSpeed + settings.ballSpeedStep;
            Assert.AreEqual(expected, sim.State.BallSpeed, Tolerance);
            Assert.AreEqual(expected, sim.State.BallVelocity.magnitude, Tolerance);
        }

        [Test]
        public void Speed_NeverGoesAboveTheMaximum()
        {
            StartRally(new Vector2(6.9f, 0f), new Vector2(settings.ballMaxSpeed - 0.1f, 0f));

            sim.Step(0.05f, default);

            Assert.AreEqual(settings.ballMaxSpeed, sim.State.BallSpeed, Tolerance);
            Assert.AreEqual(settings.ballMaxSpeed, sim.State.BallVelocity.magnitude, Tolerance);
        }

        // ---- scoring ----

        [Test]
        public void BallPassingTheRightEndLine_ScoresForPlayer1()
        {
            StartRally(new Vector2(7.9f, 3f), new Vector2(8f, 0f));

            sim.Step(0.05f, default);

            Assert.AreEqual(1, sim.State.Score1);
            Assert.AreEqual(0, sim.State.Score2);
        }

        [Test]
        public void BallPassingTheLeftEndLine_ScoresForPlayer2()
        {
            StartRally(new Vector2(-7.9f, 3f), new Vector2(-8f, 0f));

            sim.Step(0.05f, default);

            Assert.AreEqual(0, sim.State.Score1);
            Assert.AreEqual(1, sim.State.Score2);
        }

        [Test]
        public void AfterAPoint_BallResetsToTheCentreAtBaseSpeed()
        {
            StartRally(new Vector2(-7.9f, 3f), new Vector2(-12f, 0f));

            sim.Step(0.05f, default);

            Assert.AreEqual(PongPhase.Serving, sim.State.Phase);
            Assert.AreEqual(Vector2.zero, sim.State.BallPosition);
            Assert.AreEqual(Vector2.zero, sim.State.BallVelocity);
            Assert.AreEqual(settings.ballBaseSpeed, sim.State.BallSpeed, Tolerance);
        }

        [Test]
        public void AfterPlayer1Scores_TheServeGoesTowardPlayer2()
        {
            StartRally(new Vector2(7.9f, 3f), new Vector2(8f, 0f));
            sim.Step(0.05f, default);

            sim.Step(settings.serveDelay + 0.05f, default);

            Assert.AreEqual(PongPhase.Playing, sim.State.Phase);
            Assert.Greater(sim.State.BallVelocity.x, 0f);
            Assert.AreEqual(settings.ballBaseSpeed, sim.State.BallVelocity.magnitude, Tolerance);
        }

        [Test]
        public void AfterPlayer2Scores_TheServeGoesTowardPlayer1()
        {
            StartRally(new Vector2(-7.9f, 3f), new Vector2(-8f, 0f));
            sim.Step(0.05f, default);

            sim.Step(settings.serveDelay + 0.05f, default);

            Assert.AreEqual(PongPhase.Playing, sim.State.Phase);
            Assert.Less(sim.State.BallVelocity.x, 0f);
        }

        [Test]
        public void ServeWaitsForTheServeDelay()
        {
            StartRally(new Vector2(7.9f, 3f), new Vector2(8f, 0f));
            sim.Step(0.05f, default);

            sim.Step(settings.serveDelay * 0.5f, default);

            Assert.AreEqual(PongPhase.Serving, sim.State.Phase);
            Assert.AreEqual(Vector2.zero, sim.State.BallPosition);
        }

        [Test]
        public void ServeAngle_StaysWithinTheLimit()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var other = new PongSimulation(settings, seed);
                other.Step(settings.serveDelay + 0.01f, default);

                Vector2 v = other.State.BallVelocity;
                float angle = Mathf.Atan2(v.y, Mathf.Abs(v.x)) * Mathf.Rad2Deg;
                Assert.LessOrEqual(Mathf.Abs(angle), settings.maxServeAngleDegrees + 0.1f, $"seed {seed}");
            }
        }

        // ---- winning ----

        [Test]
        public void ReachingTheWinScore_EndsTheGameAndNamesTheWinner()
        {
            sim.State.Score1 = settings.winScore - 1;
            StartRally(new Vector2(7.9f, 3f), new Vector2(8f, 0f));

            sim.Step(0.05f, default);

            Assert.AreEqual(settings.winScore, sim.State.Score1);
            Assert.AreEqual(PongPhase.GameOver, sim.State.Phase);
            Assert.AreEqual(1, sim.State.Winner);
            Assert.AreEqual(Vector2.zero, sim.State.BallVelocity);
        }

        [Test]
        public void AfterGameOver_NothingScoresAnyMore()
        {
            sim.State.Score2 = settings.winScore - 1;
            StartRally(new Vector2(-7.9f, 3f), new Vector2(-8f, 0f));
            sim.Step(0.05f, default);

            sim.Step(5f, default);

            Assert.AreEqual(PongPhase.GameOver, sim.State.Phase);
            Assert.AreEqual(2, sim.State.Winner);
            Assert.AreEqual(settings.winScore, sim.State.Score2);
            Assert.AreEqual(0, sim.State.Score1);
        }

        [Test]
        public void Restart_StartsANewGame()
        {
            sim.State.Score1 = settings.winScore - 1;
            StartRally(new Vector2(7.9f, 3f), new Vector2(8f, 0f));
            sim.Step(0.05f, default);

            sim.Restart();

            Assert.AreEqual(0, sim.State.Score1);
            Assert.AreEqual(0, sim.State.Score2);
            Assert.AreEqual(0, sim.State.Winner);
            Assert.AreEqual(PongPhase.Serving, sim.State.Phase);
            Assert.AreEqual(Vector2.zero, sim.State.BallPosition);
        }

        // ---- paddle movement ----

        [Test]
        public void Paddles_MoveAtThePaddleSpeed()
        {
            sim.Step(0.1f, new PongInput(1f, -1f));

            Assert.AreEqual(settings.paddleSpeed * 0.1f, sim.State.Paddle1Z, Tolerance);
            Assert.AreEqual(-settings.paddleSpeed * 0.1f, sim.State.Paddle2Z, Tolerance);
        }

        [Test]
        public void Paddles_NeverLeaveTheTable()
        {
            float limit = settings.fieldHalfWidth - settings.paddleHalfLength;

            for (int i = 0; i < 600; i++)
            {
                float direction = (i / 150) % 2 == 0 ? 1f : -1f;
                sim.Step(1f / 60f, new PongInput(direction, -direction));

                Assert.LessOrEqual(Mathf.Abs(sim.State.Paddle1Z), limit + Tolerance);
                Assert.LessOrEqual(Mathf.Abs(sim.State.Paddle2Z), limit + Tolerance);
            }
        }

        [Test]
        public void Paddles_StopAtTheEdgeOfTheTable()
        {
            sim.Step(5f, new PongInput(1f, -1f));

            float limit = settings.fieldHalfWidth - settings.paddleHalfLength;
            Assert.AreEqual(limit, sim.State.Paddle1Z, Tolerance);
            Assert.AreEqual(-limit, sim.State.Paddle2Z, Tolerance);
        }

        // ---- determinism ----

        [Test]
        public void SameSeedAndInput_GiveTheSameGame()
        {
            var a = new PongSimulation(settings, 42);
            var b = new PongSimulation(settings, 42);

            for (int i = 0; i < 1200; i++)
            {
                var input = new PongInput(Mathf.Sin(i * 0.05f), Mathf.Cos(i * 0.03f));
                a.Step(1f / 60f, input);
                b.Step(1f / 60f, input);
            }

            Assert.AreEqual(b.State.BallPosition, a.State.BallPosition);
            Assert.AreEqual(b.State.BallVelocity, a.State.BallVelocity);
            Assert.AreEqual(b.State.Paddle1Z, a.State.Paddle1Z);
            Assert.AreEqual(b.State.Paddle2Z, a.State.Paddle2Z);
            Assert.AreEqual(b.State.Score1, a.State.Score1);
            Assert.AreEqual(b.State.Score2, a.State.Score2);
            Assert.AreEqual(b.State.Phase, a.State.Phase);
        }
    }
}
