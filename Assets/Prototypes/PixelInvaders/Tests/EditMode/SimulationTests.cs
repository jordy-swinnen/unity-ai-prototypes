using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Prototypes.PixelInvaders.Tests
{
    public class SimulationTests
    {
        static PixelInvadersSettings MakeSettings()
        {
            var s = ScriptableObject.CreateInstance<PixelInvadersSettings>();
            s.Validate();
            return s;
        }

        [Test]
        public void Wave_spawns_55_invaders_with_expected_ranks_and_4_intact_bunkers()
        {
            var settings = MakeSettings();
            var sim = new PixelInvadersSimulation(settings, seed: 42);

            Assert.AreEqual(55, sim.AliveInvaderCount);
            Assert.AreEqual(1, sim.Wave);
            Assert.AreEqual(3, sim.Lives);
            Assert.AreEqual(0, sim.Score);
            Assert.AreEqual(settings.scores.defaultHighScore, sim.HighScore);
            Assert.AreEqual(4, sim.Bunkers.Count);

            int scouts = sim.Invaders.Count(i => i.Alive && i.Rank == InvaderRank.Scout);
            int guards = sim.Invaders.Count(i => i.Alive && i.Rank == InvaderRank.Guard);
            int heavies = sim.Invaders.Count(i => i.Alive && i.Rank == InvaderRank.Heavy);
            Assert.AreEqual(11, scouts);
            Assert.AreEqual(22, guards);
            Assert.AreEqual(22, heavies);
        }

        [Test]
        public void Formation_steps_in_lockstep_toggles_pose_cycles_bass_notes_and_drops_at_wall()
        {
            var settings = MakeSettings();
            var sim = new PixelInvadersSimulation(settings, seed: 42);

            float startX0 = sim.Invaders[0].Position.x;
            float startZ0 = sim.Invaders[0].Position.y;
            Assert.AreEqual(0, sim.PoseFrame);
            Assert.AreEqual(0, sim.MarchNoteIndex);

            // Step past the first interval
            sim.Step(settings.formation.maxStepInterval + 0.01f, default);
            Assert.AreEqual(1, sim.PoseFrame, "Pose frame must toggle 0 -> 1 on step");
            Assert.AreEqual(1, sim.MarchNoteIndex, "Bass note index must advance 0 -> 1 on step");
            Assert.AreEqual(startX0 + settings.formation.stepHorizontal, sim.Invaders[0].Position.x, 1e-4f);
            Assert.IsTrue(sim.Events.Any(e => e.Kind == GameEventKind.FormationStepped && !e.Flag));

            // Step until the formation hits the right wall and drops a row
            bool dropped = false;
            for (int i = 0; i < 20 && !dropped; i++)
            {
                sim.Step(settings.formation.maxStepInterval + 0.01f, default);
                if (sim.Events.Any(e => e.Kind == GameEventKind.FormationStepped && e.Flag))
                {
                    dropped = true;
                }
            }
            Assert.IsTrue(dropped, "Formation must drop a row and reverse when reaching the trench wall");
            Assert.AreEqual(startZ0 - settings.formation.stepVertical, sim.Invaders[0].Position.y, 1e-4f);
            Assert.AreEqual(-1, sim.MarchDirection);
        }

        [Test]
        public void Step_interval_shrinks_monotonically_as_invaders_are_destroyed()
        {
            var settings = MakeSettings();
            var sim = new PixelInvadersSimulation(settings, seed: 42);

            float interval55 = sim.CurrentStepInterval;
            for (int i = 0; i < 30; i++) sim.KillInvaderForTest(i);
            float interval25 = sim.CurrentStepInterval;
            for (int i = 30; i < 54; i++) sim.KillInvaderForTest(i);
            float interval1 = sim.CurrentStepInterval;

            Assert.AreEqual(settings.formation.maxStepInterval, interval55, 1e-4f);
            Assert.Less(interval25, interval55, "25 invaders must step faster than 55");
            Assert.Less(interval1, interval25, "1 invader must step faster than 25");
            Assert.AreEqual(settings.formation.minStepInterval, interval1, 1e-4f);
        }

        [Test]
        public void Base_cannon_enforces_one_active_shot_and_honours_fire_input_buffer()
        {
            var settings = MakeSettings();
            var sim = new PixelInvadersSimulation(settings, seed: 42);
            // Clear bunkers and aliens so shot flies uninterrupted to topRailZ
            for (int i = 0; i < 55; i++) sim.SetInvaderAliveForTest(i, false);
            foreach (var b in sim.Bunkers) b.ClearAll();

            // Fire first shot
            sim.Step(0.016f, new PixelInvadersInput { firePressed = true });
            Assert.AreEqual(1, sim.PlayerShots.Count);
            Assert.AreEqual(1, sim.ShotsFired);

            // Attempt second shot immediately while first is in flight: refused
            sim.Step(0.016f, new PixelInvadersInput { firePressed = true });
            Assert.AreEqual(1, sim.PlayerShots.Count, "Base weapon must allow only 1 active shot at a time");

            // Advance shot until it is within 0.05s of hitting topRailZ, then press fire (buffered)
            while (sim.PlayerShots.Count == 1 &&
                   (settings.arena.topRailZ - sim.PlayerShots[0].Position.y) / settings.combat.playerShotSpeed > 0.06f)
            {
                sim.Step(0.02f, default);
            }
            sim.Step(0.01f, new PixelInvadersInput { firePressed = true });
            // Step past top rail: first shot misses and buffered shot fires automatically
            for (int i = 0; i < 6; i++) sim.Step(0.02f, default);

            Assert.AreEqual(2, sim.ShotsFired, "Buffered fire input within 0.10s must launch a new shot when the active shot clears");
            Assert.AreEqual(1, sim.PlayerShots.Count);
        }

        [Test]
        public void Shot_kills_invaders_awards_rank_points_and_updates_high_score()
        {
            var settings = MakeSettings();
            settings.scores.defaultHighScore = 25;
            var sim = new PixelInvadersSimulation(settings, seed: 42);
            foreach (var b in sim.Bunkers) b.ClearAll();

            // Aim directly under a bottom-row Heavy (10 pts) at column 5 (x = 0)
            sim.SetCannonXForTest(0f);
            sim.Step(0.016f, new PixelInvadersInput { firePressed = true });
            for (int i = 0; i < 100 && sim.PlayerShots.Count > 0; i++)
            {
                sim.Step(0.02f, default);
            }
            Assert.AreEqual(54, sim.AliveInvaderCount);
            Assert.AreEqual(10, sim.Score);
            Assert.AreEqual(25, sim.HighScore);

            // Kill two more invaders in column 5 (another Heavy = +10 -> 20, then Guard = +20 -> 40 > HighScore)
            for (int kill = 0; kill < 2; kill++)
            {
                sim.Step(0.016f, new PixelInvadersInput { firePressed = true });
                for (int i = 0; i < 120 && sim.PlayerShots.Count > 0; i++)
                {
                    sim.Step(0.02f, default);
                }
            }
            Assert.AreEqual(52, sim.AliveInvaderCount);
            Assert.AreEqual(40, sim.Score);
            Assert.AreEqual(40, sim.HighScore, "HighScore must update as soon as Score exceeds it");
        }

        [Test]
        public void Only_lowest_living_invader_in_a_column_may_fire()
        {
            var settings = MakeSettings();
            var sim = new PixelInvadersSimulation(settings, seed: 42);

            for (int col = 0; col < 11; col++)
            {
                int shooterIdx = sim.GetLowestLivingInvaderInColumn(col);
                Assert.AreEqual(col, shooterIdx, "Initially row 0 holds the lowest invader in every column");
            }

            // Kill row 0, col 3 -> row 1, col 3 (index 11 + 3 = 14) becomes the lowest shooter
            sim.SetInvaderAliveForTest(3, false);
            Assert.AreEqual(14, sim.GetLowestLivingInvaderInColumn(3));
        }

        [Test]
        public void Ufo_awards_shot_table_points_and_drops_RapidFire_TwinShot_and_ShieldRepair_capsules()
        {
            var settings = MakeSettings();
            var sim = new PixelInvadersSimulation(settings, seed: 42);
            foreach (var b in sim.Bunkers) b.ClearAll();

            // Erode bunker 0 so ShieldRepair has something to restore later
            sim.Bunkers[0].ResetToArch();
            int fullBunkerCells = sim.Bunkers[0].InitialIntactCount;
            sim.Bunkers[0].ScrapeBox(-10f, 10f, -10f, 10f);
            Assert.AreEqual(0, sim.Bunkers[0].IntactCount);

            // 1. Spawn UFO with RapidFire drop, destroy it, catch capsule
            sim.SpawnUfoForTest(x: 0f, direction: 1, dropKind: PowerupKind.RapidFire);
            sim.SetCannonXForTest(0f);
            sim.KillUfoWithShotForTest();
            Assert.IsTrue(sim.ActiveCapsule.Active);
            Assert.AreEqual(PowerupKind.RapidFire, sim.ActiveCapsule.Kind);
            Assert.GreaterOrEqual(sim.Score, 100);

            // Step until capsule reaches cannon at z = -4.2
            for (int i = 0; i < 200 && sim.ActiveCapsule.Active; i++) sim.Step(0.02f, default);
            Assert.AreEqual(PowerupKind.RapidFire, sim.ActivePowerup);

            // RapidFire allows up to 3 active player shots (move to clear lane at x = -5.8 so bolts don't hit column 5)
            sim.SetCannonXForTest(-5.8f);
            sim.Step(0.02f, new PixelInvadersInput { firePressed = true });
            sim.Step(settings.ufo.rapidFireCooldown + 0.02f, new PixelInvadersInput { firePressed = true });
            sim.Step(settings.ufo.rapidFireCooldown + 0.02f, new PixelInvadersInput { firePressed = true });
            Assert.AreEqual(3, sim.PlayerShots.Count, "RapidFire must allow 3 simultaneous player shots");

            // Expire RapidFire after 6.0 s
            sim.Step(settings.ufo.powerupDuration + 0.1f, default);
            Assert.AreEqual(PowerupKind.None, sim.ActivePowerup);

            // 2. Collect TwinShot capsule and verify paired bolts spawn
            sim.ClearProjectilesForTest();
            sim.CollectPowerupForTest(PowerupKind.TwinShot);
            Assert.AreEqual(PowerupKind.TwinShot, sim.ActivePowerup);
            sim.Step(0.02f, new PixelInvadersInput { firePressed = true });
            Assert.AreEqual(2, sim.PlayerShots.Count, "TwinShot must spawn 2 parallel bolts per shot");
            Assert.AreEqual(settings.ufo.twinShotOffset * 2f,
                Mathf.Abs(sim.PlayerShots[1].Position.x - sim.PlayerShots[0].Position.x), 1e-4f);

            // 3. Collect ShieldRepair capsule and verify bunker restoration
            sim.CollectPowerupForTest(PowerupKind.ShieldRepair);
            Assert.AreEqual(fullBunkerCells, sim.Bunkers[0].IntactCount, "ShieldRepair must restore eroded bunker cells");
        }

        [Test]
        public void Clearing_wave_advances_to_next_wave_one_step_lower_and_invasion_ends_game()
        {
            var settings = MakeSettings();
            var sim = new PixelInvadersSimulation(settings, seed: 42);
            float wave1TopZ = sim.Invaders[44].Position.y; // row 4 col 0

            // Kill 54 invaders, then kill the last one
            for (int i = 0; i < 54; i++) sim.SetInvaderAliveForTest(i, false);
            sim.KillInvaderForTest(54);
            Assert.AreEqual(GamePhase.WaveClear, sim.Phase);

            sim.Step(settings.formation.waveClearDelay + 0.05f, default);
            Assert.AreEqual(GamePhase.Playing, sim.Phase);
            Assert.AreEqual(2, sim.Wave);
            Assert.AreEqual(55, sim.AliveInvaderCount);
            Assert.AreEqual(wave1TopZ - settings.formation.waveDropStep, sim.Invaders[44].Position.y, 1e-4f);

            // Force an invader down to defenseLineZ -> immediate GameOver
            sim.SetInvaderPositionForTest(0, new Vector2(0f, settings.arena.defenseLineZ));
            sim.Step(settings.formation.maxStepInterval + 0.02f, default);
            Assert.AreEqual(GamePhase.GameOver, sim.Phase);

            // Restart resets wave and lives but keeps HighScore
            int savedHigh = sim.HighScore;
            sim.Step(0.02f, new PixelInvadersInput { restartPressed = true });
            Assert.AreEqual(GamePhase.Playing, sim.Phase);
            Assert.AreEqual(1, sim.Wave);
            Assert.AreEqual(3, sim.Lives);
            Assert.AreEqual(0, sim.Score);
            Assert.AreEqual(savedHigh, sim.HighScore);
        }

        [Test]
        public void EffectStrength_zero_disables_all_shake_hitstop_and_slowmo()
        {
            var settings = MakeSettings();
            settings.feel.effectStrength = 0f;

            Assert.AreEqual(0f, settings.EffectiveShake(settings.feel.shakeDeath));
            Assert.AreEqual(0f, settings.EffectiveHitStop(settings.feel.deathHitStop));
            Assert.AreEqual(0f, settings.EffectiveSlowMoDuration());
        }
    }
}
