using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Prototypes.NeonPong.Tests
{
    public class NeonPongSimulationTests
    {
        const float Tol = 0.001f;

        NeonPongSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<NeonPongSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        NeonPongSimulation Make(int seed = 1) => new NeonPongSimulation(settings, seed);

        static void Play(NeonPongSimulation sim, Vector2 pos, Vector2 vel)
        {
            sim.State.Phase = Phase.Playing;
            sim.State.BallPos = pos;
            sim.State.BallVel = vel;
            sim.State.BallSpeed = vel.magnitude;
        }

        static bool Has(NeonPongSimulation sim, EventKind kind) =>
            sim.Events.Any(e => e.Kind == kind);

        // --- start ---

        [Test]
        public void Starts_serving_with_zero_scores_and_centred_ball()
        {
            var sim = Make();
            Assert.AreEqual(Phase.Serving, sim.State.Phase);
            Assert.AreEqual(0, sim.State.GetScore(Side.Left));
            Assert.AreEqual(0, sim.State.GetScore(Side.Right));
            Assert.AreEqual(Vector2.zero, sim.State.BallPos);
            Assert.AreEqual(Vector2.zero, sim.State.BallVel);
            Assert.AreEqual(0f, sim.State.GetPaddleZ(Side.Left));
            Assert.AreEqual(0f, sim.State.GetPaddleZ(Side.Right));
        }

        [Test]
        public void Serve_launches_only_after_the_delay_at_base_speed()
        {
            var sim = Make();
            sim.Step(settings.serveDelay * 0.5f, default);
            Assert.AreEqual(Phase.Serving, sim.State.Phase);
            Assert.AreEqual(Vector2.zero, sim.State.BallVel);

            sim.Step(settings.serveDelay * 0.6f, default);
            Assert.AreEqual(Phase.Playing, sim.State.Phase);
            Assert.AreEqual(settings.baseSpeed, sim.State.BallVel.magnitude, Tol);
            Assert.IsTrue(Has(sim, EventKind.Serve));
        }

        [Test]
        public void First_serve_angle_stays_within_the_maximum_and_both_directions_occur()
        {
            var directions = new HashSet<int>();
            for (int seed = 0; seed < 40; seed++)
            {
                var sim = Make(seed);
                sim.Step(settings.serveDelay + 0.01f, default);
                var v = sim.State.BallVel;
                float angle = Mathf.Abs(Mathf.Atan2(v.y, Mathf.Abs(v.x))) * Mathf.Rad2Deg;
                Assert.LessOrEqual(angle, settings.maxServeAngle + Tol, $"seed {seed}");
                directions.Add(v.x > 0 ? 1 : -1);
            }
            Assert.AreEqual(2, directions.Count, "the first serve should go both ways across seeds");
        }

        // --- walls ---

        [Test]
        public void Top_wall_reflects_with_equal_angle()
        {
            var sim = Make();
            Play(sim, new Vector2(0f, 4.7f), new Vector2(8f, 4f));
            sim.Step(0.1f, default);
            Assert.AreEqual(8f, sim.State.BallVel.x, Tol);
            Assert.AreEqual(-4f, sim.State.BallVel.y, Tol);
            Assert.LessOrEqual(sim.State.BallPos.y + settings.ballRadius, settings.halfWidth + Tol);
            Assert.IsTrue(Has(sim, EventKind.WallHit));
        }

        [Test]
        public void Bottom_wall_reflects_with_equal_angle()
        {
            var sim = Make();
            Play(sim, new Vector2(0f, -4.7f), new Vector2(-8f, -4f));
            sim.Step(0.1f, default);
            Assert.AreEqual(-8f, sim.State.BallVel.x, Tol);
            Assert.AreEqual(4f, sim.State.BallVel.y, Tol);
            Assert.GreaterOrEqual(sim.State.BallPos.y - settings.ballRadius, -settings.halfWidth - Tol);
            Assert.IsTrue(Has(sim, EventKind.WallHit));
        }

        [Test]
        public void A_wall_hit_does_not_change_the_speed()
        {
            var sim = Make();
            Play(sim, new Vector2(0f, 4.7f), new Vector2(8f, 4f));
            float before = sim.State.BallVel.magnitude;
            sim.Step(0.1f, default);
            Assert.AreEqual(before, sim.State.BallVel.magnitude, Tol);
            Assert.AreEqual(before, sim.State.BallSpeed, Tol);
        }

        // --- paddles ---

        [Test]
        public void Paddle_centre_hit_returns_straight_back()
        {
            var sim = Make();
            Play(sim, new Vector2(6.5f, 0f), new Vector2(9f, 0f));
            sim.Step(0.2f, default);
            Assert.Less(sim.State.BallVel.x, 0f);
            Assert.AreEqual(0f, sim.State.BallVel.y, Tol);
            Assert.IsTrue(Has(sim, EventKind.PaddleHit));
        }

        [Test]
        public void Paddle_edge_hit_returns_at_the_maximum_angle_up_and_down()
        {
            foreach (float sign in new[] { 1f, -1f })
            {
                var sim = Make();
                Play(sim, new Vector2(6.5f, sign * settings.paddleHalfLength), new Vector2(9f, 0f));
                sim.Step(0.2f, default);
                var v = sim.State.BallVel;
                float angle = Mathf.Atan2(v.y, -v.x) * Mathf.Rad2Deg;
                Assert.AreEqual(sign * settings.maxBounceAngle, angle, 0.1f);
            }
        }

        [Test]
        public void Hit_beyond_the_paddle_end_but_touching_the_ball_radius_still_uses_the_maximum_angle()
        {
            var sim = Make();
            float z = settings.paddleHalfLength + settings.ballRadius * 0.5f;
            Play(sim, new Vector2(6.5f, z), new Vector2(9f, 0f));
            sim.Step(0.2f, default);
            Assert.IsTrue(Has(sim, EventKind.PaddleHit));
            float angle = Mathf.Atan2(sim.State.BallVel.y, -sim.State.BallVel.x) * Mathf.Rad2Deg;
            Assert.AreEqual(settings.maxBounceAngle, angle, 0.1f);
        }

        [Test]
        public void Left_paddle_returns_the_ball_to_the_right()
        {
            var sim = Make();
            Play(sim, new Vector2(-6.5f, 0f), new Vector2(-9f, 0f));
            sim.Step(0.2f, default);
            Assert.Greater(sim.State.BallVel.x, 0f);
            Assert.IsTrue(Has(sim, EventKind.PaddleHit));
        }

        [Test]
        public void Paddle_position_changes_the_return_angle()
        {
            var sim = Make();
            sim.State.SetPaddleZ(Side.Right, 2f);
            Play(sim, new Vector2(6.5f, 2.5f), new Vector2(9f, 0f)); // 0.5 above the paddle centre
            sim.Step(0.2f, default);
            float expected = 0.5f / settings.paddleHalfLength * settings.maxBounceAngle;
            float angle = Mathf.Atan2(sim.State.BallVel.y, -sim.State.BallVel.x) * Mathf.Rad2Deg;
            Assert.AreEqual(expected, angle, 0.1f);
        }

        [Test]
        public void Ball_that_misses_the_paddle_passes_and_scores()
        {
            var sim = Make();
            Play(sim, new Vector2(6.5f, 3f), new Vector2(9f, 0f));
            sim.Step(1.0f, default);
            Assert.IsFalse(Has(sim, EventKind.PaddleHit));
            Assert.AreEqual(1, sim.State.GetScore(Side.Left));
        }

        [Test]
        public void A_fast_ball_does_not_pass_through_a_paddle_even_in_one_big_step()
        {
            settings.maxSubstep = 1f; // one big step: only the swept check can catch it
            var sim = Make();
            Play(sim, new Vector2(6f, 0f), new Vector2(22f, 0f));
            sim.Step(0.2f, default);
            Assert.IsTrue(Has(sim, EventKind.PaddleHit));
            Assert.Less(sim.State.BallVel.x, 0f);
            Assert.AreEqual(0, sim.State.GetScore(Side.Left));
        }

        // --- speed ---

        [Test]
        public void Speed_rises_by_one_step_per_paddle_hit()
        {
            var sim = Make();
            Play(sim, new Vector2(6.5f, 0f), new Vector2(settings.baseSpeed, 0f));
            sim.Step(0.2f, default);
            Assert.AreEqual(settings.baseSpeed + settings.speedStep, sim.State.BallSpeed, Tol);
            Assert.AreEqual(sim.State.BallSpeed, sim.State.BallVel.magnitude, Tol);
            Assert.IsTrue(Has(sim, EventKind.SpeedStep));
            Assert.AreEqual(1, sim.State.RallyHits);
        }

        [Test]
        public void Speed_never_exceeds_the_maximum()
        {
            var sim = Make();
            Play(sim, new Vector2(6.5f, 0f), new Vector2(settings.maxSpeed - 0.1f, 0f));
            sim.Step(0.2f, default);
            Assert.AreEqual(settings.maxSpeed, sim.State.BallSpeed, Tol);

            // A second hit at the cap changes nothing.
            Play(sim, new Vector2(-6.5f, 0f), new Vector2(-settings.maxSpeed, 0f));
            sim.Step(0.2f, default);
            Assert.AreEqual(settings.maxSpeed, sim.State.BallSpeed, Tol);
        }

        [Test]
        public void Speed_resets_to_base_after_a_point()
        {
            var sim = Make();
            Play(sim, new Vector2(6.5f, 3f), new Vector2(15f, 0f));
            sim.Step(1.0f, default);
            Assert.AreEqual(settings.baseSpeed, sim.State.BallSpeed, Tol);
            Assert.AreEqual(0, sim.State.RallyHits);
        }

        // --- scoring and serving ---

        [Test]
        public void Right_end_line_scores_for_the_left_player()
        {
            var sim = Make();
            Play(sim, new Vector2(6.5f, 3f), new Vector2(9f, 0f));
            sim.Step(1.0f, default);
            Assert.AreEqual(1, sim.State.GetScore(Side.Left));
            Assert.AreEqual(0, sim.State.GetScore(Side.Right));
            Assert.IsTrue(Has(sim, EventKind.Point));
        }

        [Test]
        public void Left_end_line_scores_for_the_right_player()
        {
            var sim = Make();
            Play(sim, new Vector2(-6.5f, -3f), new Vector2(-9f, 0f));
            sim.Step(1.0f, default);
            Assert.AreEqual(0, sim.State.GetScore(Side.Left));
            Assert.AreEqual(1, sim.State.GetScore(Side.Right));
        }

        [Test]
        public void A_point_resets_the_ball_to_the_centre_and_waits_to_serve()
        {
            var sim = Make();
            Play(sim, new Vector2(6.5f, 3f), new Vector2(9f, 0f));
            sim.Step(1.0f, default);
            Assert.AreEqual(Phase.Serving, sim.State.Phase);
            Assert.AreEqual(Vector2.zero, sim.State.BallPos);
            Assert.AreEqual(Vector2.zero, sim.State.BallVel);
        }

        [Test]
        public void Next_serve_goes_toward_the_player_who_lost_the_point()
        {
            // The right player misses: the left player scores, the serve goes right (+x).
            var sim = Make();
            Play(sim, new Vector2(6.5f, 3f), new Vector2(9f, 0f));
            sim.Step(1.0f, default);
            sim.Step(settings.serveDelay + 0.05f, default);
            Assert.AreEqual(Phase.Playing, sim.State.Phase);
            Assert.Greater(sim.State.BallVel.x, 0f);

            // The left player misses: the right player scores, the serve goes left (-x).
            sim = Make();
            Play(sim, new Vector2(-6.5f, 3f), new Vector2(-9f, 0f));
            sim.Step(1.0f, default);
            sim.Step(settings.serveDelay + 0.05f, default);
            Assert.AreEqual(Phase.Playing, sim.State.Phase);
            Assert.Less(sim.State.BallVel.x, 0f);
        }

        // --- winning ---

        [Test]
        public void Reaching_the_win_score_ends_the_game_and_names_the_winner()
        {
            var sim = Make();
            sim.State.Score[(int)Side.Left] = settings.winScore - 1;
            Play(sim, new Vector2(6.5f, 3f), new Vector2(9f, 0f));
            sim.Step(1.0f, default);
            Assert.AreEqual(Phase.GameOver, sim.State.Phase);
            Assert.AreEqual(settings.winScore, sim.State.GetScore(Side.Left));
            Assert.AreEqual(Side.Left, sim.State.Winner);
            Assert.IsTrue(Has(sim, EventKind.Win));
        }

        [Test]
        public void Nothing_moves_or_scores_after_the_game_is_over()
        {
            var sim = Make();
            sim.State.Score[(int)Side.Left] = settings.winScore - 1;
            Play(sim, new Vector2(6.5f, 3f), new Vector2(9f, 0f));
            sim.Step(1.0f, default);

            var ball = sim.State.BallPos;
            var up = new NeonPongInput { Left = 1f, Right = 1f };
            sim.Step(5f, up);
            Assert.AreEqual(Phase.GameOver, sim.State.Phase);
            Assert.AreEqual(ball, sim.State.BallPos);
            Assert.AreEqual(settings.winScore, sim.State.GetScore(Side.Left));
            Assert.AreEqual(0f, sim.State.GetPaddleZ(Side.Left));
        }

        [Test]
        public void Restart_after_a_win_resets_scores_and_serves_again()
        {
            var sim = Make();
            sim.State.Score[(int)Side.Left] = settings.winScore - 1;
            Play(sim, new Vector2(6.5f, 3f), new Vector2(9f, 0f));
            sim.Step(1.0f, default);

            sim.Step(0.01f, new NeonPongInput { Restart = true });
            Assert.AreEqual(Phase.Serving, sim.State.Phase);
            Assert.AreEqual(0, sim.State.GetScore(Side.Left));
            Assert.AreEqual(0, sim.State.GetScore(Side.Right));
            Assert.IsTrue(Has(sim, EventKind.Restart));
        }

        [Test]
        public void Restart_is_ignored_while_the_game_is_running()
        {
            var sim = Make();
            Play(sim, new Vector2(0f, 0f), new Vector2(9f, 0f));
            sim.State.Score[(int)Side.Left] = 2;
            sim.Step(0.01f, new NeonPongInput { Restart = true });
            Assert.AreEqual(Phase.Playing, sim.State.Phase);
            Assert.AreEqual(2, sim.State.GetScore(Side.Left));
        }

        // --- paddles move ---

        [Test]
        public void A_paddle_never_leaves_the_table()
        {
            settings.winScore = 1000; // the ball scores while the paddles hug the edges
            var sim = Make();
            float limit = settings.halfWidth - settings.paddleHalfLength;
            sim.Step(10f, new NeonPongInput { Left = 1f, Right = -1f });
            Assert.AreEqual(limit, sim.State.GetPaddleZ(Side.Left), Tol);
            Assert.AreEqual(-limit, sim.State.GetPaddleZ(Side.Right), Tol);

            sim.Step(10f, new NeonPongInput { Left = -1f, Right = 1f });
            Assert.AreEqual(-limit, sim.State.GetPaddleZ(Side.Left), Tol);
            Assert.AreEqual(limit, sim.State.GetPaddleZ(Side.Right), Tol);
        }

        [Test]
        public void A_paddle_eases_in_and_reaches_full_speed()
        {
            settings.paddleEaseTime = 0.1f;
            var sim = Make();
            sim.Step(1f / 120f, new NeonPongInput { Left = 1f });
            Assert.Greater(sim.State.GetPaddleVelocity(Side.Left), 0f);
            Assert.Less(sim.State.GetPaddleVelocity(Side.Left), settings.paddleSpeed * 0.5f);

            sim.Step(0.2f, new NeonPongInput { Left = 1f });
            Assert.AreEqual(settings.paddleSpeed, sim.State.GetPaddleVelocity(Side.Left), Tol);
        }

        [Test]
        public void A_paddle_stops_when_input_is_released()
        {
            var sim = Make();
            sim.Step(0.5f, new NeonPongInput { Left = 1f });
            sim.Step(1f, default);
            Assert.AreEqual(0f, sim.State.GetPaddleVelocity(Side.Left), Tol);
            float z = sim.State.GetPaddleZ(Side.Left);
            sim.Step(0.5f, default);
            Assert.AreEqual(z, sim.State.GetPaddleZ(Side.Left), Tol);
        }

        [Test]
        public void Input_is_clamped_to_one()
        {
            var sim = Make();
            sim.Step(1f, new NeonPongInput { Left = 5f });
            Assert.LessOrEqual(sim.State.GetPaddleVelocity(Side.Left), settings.paddleSpeed + Tol);
        }

        // --- repeatability ---

        [Test]
        public void The_same_seed_and_inputs_give_the_same_game()
        {
            var a = Make(7);
            var b = Make(7);
            for (int i = 0; i < 1200; i++)
            {
                var input = new NeonPongInput
                {
                    Left = Mathf.Sin(i * 0.05f),
                    Right = Mathf.Cos(i * 0.03f),
                };
                a.Step(1f / 60f, input);
                b.Step(1f / 60f, input);
            }
            Assert.AreEqual(a.State.BallPos, b.State.BallPos);
            Assert.AreEqual(a.State.BallVel, b.State.BallVel);
            Assert.AreEqual(a.State.GetScore(Side.Left), b.State.GetScore(Side.Left));
            Assert.AreEqual(a.State.GetScore(Side.Right), b.State.GetScore(Side.Right));
            Assert.AreEqual(a.State.Phase, b.State.Phase);
        }

        [Test]
        public void Step_size_does_not_change_whether_a_centre_rally_hits_the_paddle()
        {
            foreach (float dt in new[] { 1f / 30f, 1f / 60f, 1f / 144f })
            {
                var sim = Make();
                Play(sim, new Vector2(0f, 0f), new Vector2(settings.maxSpeed, 0f));
                bool hit = false;
                for (int i = 0; i < 120 && !hit; i++)
                {
                    sim.Step(dt, default);
                    hit = Has(sim, EventKind.PaddleHit);
                }
                Assert.IsTrue(hit, $"dt {dt}");
            }
        }
    }
}
