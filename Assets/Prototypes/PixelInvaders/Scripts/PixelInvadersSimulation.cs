using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototypes.PixelInvaders
{
    /// <summary>
    /// Deterministic pure-C# simulation of PixelInvaders (no Unity physics or MonoBehaviours).
    /// Steps at a fixed delta time and emits GameEvents consumed by the view and audio directors.
    /// </summary>
    public sealed class PixelInvadersSimulation
    {
        static readonly float[] DefaultBunkerXs = { -4.5f, -1.5f, 1.5f, 4.5f };
        static readonly PowerupKind[] DropCycle =
        {
            PowerupKind.RapidFire,
            PowerupKind.TwinShot,
            PowerupKind.ShieldRepair,
        };

        readonly PixelInvadersSettings settings;
        readonly System.Random rng;
        readonly InvaderState[] invaders;
        readonly List<BunkerGrid> bunkers = new List<BunkerGrid>();
        readonly List<ProjectileState> playerShots = new List<ProjectileState>();
        readonly List<ProjectileState> alienShots = new List<ProjectileState>();
        readonly List<GameEvent> events = new List<GameEvent>();

        float stepTimer;
        float alienFireTimer;
        float ufoSpawnTimer;
        float fireBufferTimer;
        float fireCooldownTimer;
        float phaseTimer;
        int dropCycleIndex;

        public IReadOnlyList<InvaderState> Invaders => invaders;
        public IReadOnlyList<BunkerGrid> Bunkers => bunkers;
        public IReadOnlyList<ProjectileState> PlayerShots => playerShots;
        public IReadOnlyList<ProjectileState> AlienShots => alienShots;
        public IReadOnlyList<GameEvent> Events => events;

        public GamePhase Phase { get; private set; }
        public int Wave { get; private set; }
        public int Lives { get; private set; }
        public int Score { get; private set; }
        public int HighScore { get; private set; }
        public int ShotsFired { get; private set; }
        public int AliveInvaderCount { get; private set; }

        public float CannonX { get; private set; }
        public float CannonZ => settings.arena.cannonZ;
        public float CannonMoveVelocity { get; private set; }

        public int PoseFrame { get; private set; }
        public int MarchNoteIndex { get; private set; }
        public int MarchDirection { get; private set; } = 1;

        public UfoState ActiveUfo { get; private set; }
        public CapsuleState ActiveCapsule { get; private set; }
        public PowerupKind ActivePowerup { get; private set; }
        public float PowerupTimeLeft { get; private set; }

        public float CurrentStepInterval => ComputeStepInterval(AliveInvaderCount);

        public PixelInvadersSimulation(PixelInvadersSettings settings, int seed = 1337)
        {
            this.settings = settings != null ? settings : ScriptableObject.CreateInstance<PixelInvadersSettings>();
            this.settings.Validate();
            rng = new System.Random(seed);
            invaders = new InvaderState[this.settings.formation.rows * this.settings.formation.columns];
            HighScore = this.settings.scores.defaultHighScore;
            InitBunkers();
            StartNewGame();
        }

        void InitBunkers()
        {
            bunkers.Clear();
            int count = Mathf.Max(1, settings.bunkers.count);
            for (int i = 0; i < count; i++)
            {
                float bx = i < DefaultBunkerXs.Length
                    ? DefaultBunkerXs[i]
                    : Mathf.Lerp(-4.5f, 4.5f, count == 1 ? 0.5f : (float)i / (count - 1));
                bunkers.Add(new BunkerGrid(
                    bx,
                    settings.arena.bunkerZ,
                    settings.bunkers.cols,
                    settings.bunkers.rows,
                    settings.bunkers.cellSize));
            }
        }

        public void StartNewGame()
        {
            Score = 0;
            Lives = settings.combat.startingLives;
            Wave = 1;
            ShotsFired = 0;
            CannonX = 0f;
            CannonMoveVelocity = 0f;
            ActivePowerup = PowerupKind.None;
            PowerupTimeLeft = 0f;
            ActiveUfo = default;
            ActiveCapsule = default;
            playerShots.Clear();
            alienShots.Clear();
            events.Clear();
            foreach (var b in bunkers) b.ResetToArch();
            SpawnWave(1);
            Phase = GamePhase.Playing;
        }

        void SpawnWave(int waveNumber)
        {
            Wave = waveNumber;
            int rows = settings.formation.rows;
            int cols = settings.formation.columns;
            int dropSteps = Mathf.Min(waveNumber - 1, settings.formation.maxWaveDropSteps);
            float topZ = settings.arena.wave1TopZ - dropSteps * settings.formation.waveDropStep;
            float bottomZ = topZ - (rows - 1) * settings.formation.rowSpacing;
            float leftX = -0.5f * (cols - 1) * settings.formation.colSpacing;

            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                InvaderRank rank = r == rows - 1
                    ? InvaderRank.Scout
                    : (r >= 2 ? InvaderRank.Guard : InvaderRank.Heavy);
                float z = bottomZ + r * settings.formation.rowSpacing;
                for (int c = 0; c < cols; c++)
                {
                    float x = leftX + c * settings.formation.colSpacing;
                    invaders[idx] = new InvaderState
                    {
                        Index = idx,
                        Row = r,
                        Col = c,
                        Rank = rank,
                        Position = new Vector2(x, z),
                        Alive = true,
                    };
                    idx++;
                }
            }

            AliveInvaderCount = invaders.Length;
            MarchDirection = 1;
            PoseFrame = 0;
            MarchNoteIndex = 0;
            stepTimer = CurrentStepInterval;
            alienFireTimer = settings.combat.alienFireIntervalStart;
            ufoSpawnTimer = settings.ufo.ufoSpawnInterval;
            events.Add(new GameEvent { Kind = GameEventKind.WaveStarted, Value = waveNumber });
        }

        float ComputeStepInterval(int alive)
        {
            if (alive <= 1) return settings.formation.minStepInterval;
            int total = Mathf.Max(2, settings.formation.rows * settings.formation.columns);
            float t = Mathf.Clamp01((float)(alive - 1) / (total - 1));
            float curved = Mathf.Pow(t, settings.formation.speedCurveExponent);
            return Mathf.Lerp(settings.formation.minStepInterval, settings.formation.maxStepInterval, curved);
        }

        public void Step(float dt, PixelInvadersInput input)
        {
            events.Clear();
            if (dt <= 0f) return;

            if (Phase == GamePhase.GameOver)
            {
                if (input.restartPressed) StartNewGame();
                return;
            }

            if (Phase == GamePhase.Respawning)
            {
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    CannonX = 0f;
                    CannonMoveVelocity = 0f;
                    Phase = GamePhase.Playing;
                    events.Add(new GameEvent
                    {
                        Kind = GameEventKind.CannonRespawned,
                        Position = new Vector2(CannonX, CannonZ),
                        Value = Lives,
                    });
                }
                return;
            }

            if (Phase == GamePhase.WaveClear)
            {
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    playerShots.Clear();
                    alienShots.Clear();
                    ActiveUfo = default;
                    ActiveCapsule = default;
                    SpawnWave(Wave + 1);
                    Phase = GamePhase.Playing;
                }
                return;
            }

            StepCannonAndWeapons(dt, input);
            StepProjectiles(dt);
            StepUfoAndCapsules(dt);
            StepFormation(dt);
            StepAlienFiring(dt);
        }

        void StepCannonAndWeapons(float dt, PixelInvadersInput input)
        {
            float axis = Mathf.Clamp(input.moveAxis, -1f, 1f);
            CannonMoveVelocity = axis * settings.combat.cannonSpeed;
            CannonX = Mathf.Clamp(
                CannonX + CannonMoveVelocity * dt,
                -settings.arena.cannonLimitX,
                settings.arena.cannonLimitX);

            if (PowerupTimeLeft > 0f)
            {
                PowerupTimeLeft -= dt;
                if (PowerupTimeLeft <= 0f)
                {
                    var expired = ActivePowerup;
                    ActivePowerup = PowerupKind.None;
                    PowerupTimeLeft = 0f;
                    events.Add(new GameEvent { Kind = GameEventKind.PowerupExpired, Powerup = expired });
                }
            }

            if (fireCooldownTimer > 0f) fireCooldownTimer -= dt;
            if (fireBufferTimer > 0f) fireBufferTimer -= dt;
            if (input.firePressed) fireBufferTimer = settings.combat.fireInputBuffer;

            if (fireBufferTimer > 0f && CanFirePlayerShot())
            {
                FirePlayerShot();
                fireBufferTimer = 0f;
            }
        }

        bool CanFirePlayerShot()
        {
            if (fireCooldownTimer > 0f) return false;
            switch (ActivePowerup)
            {
                case PowerupKind.RapidFire:
                    return playerShots.Count < settings.ufo.rapidFireMaxShots;
                case PowerupKind.TwinShot:
                    return playerShots.Count <= (settings.ufo.twinShotMaxSalvos - 1) * 2;
                default:
                    return playerShots.Count == 0;
            }
        }

        void FirePlayerShot()
        {
            ShotsFired++;
            if (ActivePowerup == PowerupKind.TwinShot)
            {
                float off = settings.ufo.twinShotOffset;
                playerShots.Add(new ProjectileState
                {
                    Position = new Vector2(CannonX - off, CannonZ + 0.35f),
                    SpeedZ = settings.combat.playerShotSpeed,
                    FromPlayer = true,
                    TwinBolt = true,
                });
                playerShots.Add(new ProjectileState
                {
                    Position = new Vector2(CannonX + off, CannonZ + 0.35f),
                    SpeedZ = settings.combat.playerShotSpeed,
                    FromPlayer = true,
                    TwinBolt = true,
                });
                fireCooldownTimer = settings.ufo.rapidFireCooldown;
            }
            else
            {
                playerShots.Add(new ProjectileState
                {
                    Position = new Vector2(CannonX, CannonZ + 0.35f),
                    SpeedZ = settings.combat.playerShotSpeed,
                    FromPlayer = true,
                    TwinBolt = false,
                });
                fireCooldownTimer = ActivePowerup == PowerupKind.RapidFire ? settings.ufo.rapidFireCooldown : 0.02f;
            }

            events.Add(new GameEvent
            {
                Kind = GameEventKind.ShotFired,
                Position = new Vector2(CannonX, CannonZ + 0.35f),
                Powerup = ActivePowerup,
            });
        }

        void StepProjectiles(float dt)
        {
            // Advance player shots
            for (int i = playerShots.Count - 1; i >= 0; i--)
            {
                var shot = playerShots[i];
                shot.Position.y += shot.SpeedZ * dt;
                playerShots[i] = shot;

                // Check bunker hit from below
                if (TryHitBunker(shot.Position))
                {
                    playerShots.RemoveAt(i);
                    continue;
                }

                // Check alien hit
                if (TryHitInvader(shot.Position))
                {
                    playerShots.RemoveAt(i);
                    if (Phase != GamePhase.Playing) return;
                    continue;
                }

                // Check UFO hit
                if (ActiveUfo.Active &&
                    Vector2.Distance(shot.Position, ActiveUfo.Position) <= settings.ufo.ufoHitRadius)
                {
                    playerShots.RemoveAt(i);
                    DestroyActiveUfo();
                    continue;
                }

                // Check top rail miss
                if (shot.Position.y >= settings.arena.topRailZ)
                {
                    events.Add(new GameEvent { Kind = GameEventKind.ShotMissed, Position = shot.Position });
                    playerShots.RemoveAt(i);
                }
            }

            // If a buffered press is waiting and a shot just cleared on this step, launch immediately
            if (fireBufferTimer > 0f && CanFirePlayerShot())
            {
                FirePlayerShot();
                fireBufferTimer = 0f;
            }

            // Advance alien shots & check shot-vs-shot cancellation
            for (int i = alienShots.Count - 1; i >= 0; i--)
            {
                var shot = alienShots[i];
                shot.Position.y -= shot.SpeedZ * dt;
                alienShots[i] = shot;

                // Shot-vs-shot cancel
                bool cancelled = false;
                for (int p = playerShots.Count - 1; p >= 0; p--)
                {
                    if (Vector2.Distance(shot.Position, playerShots[p].Position) <= settings.combat.shotCancelRadius)
                    {
                        events.Add(new GameEvent { Kind = GameEventKind.ShotsCancelled, Position = shot.Position });
                        playerShots.RemoveAt(p);
                        alienShots.RemoveAt(i);
                        cancelled = true;
                        break;
                    }
                }
                if (cancelled) continue;

                // Bunker hit from above
                if (TryHitBunker(shot.Position))
                {
                    alienShots.RemoveAt(i);
                    continue;
                }

                // Cannon hit
                if (Vector2.Distance(shot.Position, new Vector2(CannonX, CannonZ)) <= settings.combat.cannonHitRadius)
                {
                    alienShots.RemoveAt(i);
                    LoseCannonLife();
                    return;
                }

                if (shot.Position.y <= settings.arena.bottomRailZ)
                {
                    alienShots.RemoveAt(i);
                }
            }
        }

        bool TryHitBunker(Vector2 pos)
        {
            for (int b = 0; b < bunkers.Count; b++)
            {
                if (bunkers[b].HitAtWorld(pos.x, pos.y, settings.bunkers.blastRadiusCells))
                {
                    events.Add(new GameEvent
                    {
                        Kind = GameEventKind.BunkerEroded,
                        Position = pos,
                        Value = b,
                    });
                    return true;
                }
            }
            return false;
        }

        bool TryHitInvader(Vector2 shotPos)
        {
            float hitR2 = settings.combat.alienHitRadius * settings.combat.alienHitRadius;
            for (int i = 0; i < invaders.Length; i++)
            {
                if (!invaders[i].Alive) continue;
                if ((invaders[i].Position - shotPos).sqrMagnitude <= hitR2)
                {
                    KillInvader(i);
                    return true;
                }
            }
            return false;
        }

        void KillInvader(int index)
        {
            if (!invaders[index].Alive) return;
            invaders[index].Alive = false;
            AliveInvaderCount--;

            int pts = PointsForRank(invaders[index].Rank);
            AddScore(pts);

            events.Add(new GameEvent
            {
                Kind = GameEventKind.InvaderKilled,
                Position = invaders[index].Position,
                Rank = invaders[index].Rank,
                Value = pts,
            });

            if (AliveInvaderCount <= 0)
            {
                Phase = GamePhase.WaveClear;
                phaseTimer = settings.formation.waveClearDelay;
                events.Add(new GameEvent { Kind = GameEventKind.WaveCleared, Value = Wave });
            }
        }

        int PointsForRank(InvaderRank rank)
        {
            switch (rank)
            {
                case InvaderRank.Scout: return settings.scores.scoutPoints;
                case InvaderRank.Guard: return settings.scores.guardPoints;
                default: return settings.scores.heavyPoints;
            }
        }

        void AddScore(int delta)
        {
            Score += delta;
            if (Score > HighScore) HighScore = Score;
        }

        void StepUfoAndCapsules(float dt)
        {
            if (ActiveUfo.Active)
            {
                var ufo = ActiveUfo;
                ufo.Position.x += ufo.Direction * settings.ufo.ufoSpeed * dt;
                if (Mathf.Abs(ufo.Position.x) > settings.arena.halfWidth + 0.6f)
                {
                    ufo.Active = false;
                }
                ActiveUfo = ufo;
            }
            else if (AliveInvaderCount >= settings.ufo.minAliensForUfo)
            {
                ufoSpawnTimer -= dt;
                if (ufoSpawnTimer <= 0f)
                {
                    ufoSpawnTimer = settings.ufo.ufoSpawnInterval;
                    int dir = (ShotsFired % 2 == 0) ? 1 : -1;
                    float startX = -dir * (settings.arena.halfWidth - 0.2f);
                    PowerupKind drop = DropCycle[dropCycleIndex % DropCycle.Length];
                    dropCycleIndex++;
                    ActiveUfo = new UfoState
                    {
                        Active = true,
                        Position = new Vector2(startX, settings.arena.ufoZ),
                        Direction = dir,
                        DropKind = drop,
                    };
                    events.Add(new GameEvent { Kind = GameEventKind.UfoSpawned, Position = ActiveUfo.Position });
                }
            }

            if (ActiveCapsule.Active)
            {
                var cap = ActiveCapsule;
                cap.Position.y -= settings.ufo.capsuleFallSpeed * dt;
                if (Vector2.Distance(cap.Position, new Vector2(CannonX, CannonZ)) <= settings.ufo.capsulePickupRadius)
                {
                    cap.Active = false;
                    ActiveCapsule = cap;
                    ApplyPowerup(cap.Kind, cap.Position);
                }
                else if (cap.Position.y <= settings.arena.bottomRailZ)
                {
                    cap.Active = false;
                    ActiveCapsule = cap;
                }
                else
                {
                    ActiveCapsule = cap;
                }
            }
        }

        void DestroyActiveUfo()
        {
            var ufo = ActiveUfo;
            ActiveUfo = default;
            int[] table = settings.scores.ufoPointsTable;
            int pts = table[Mathf.Abs(ShotsFired) % table.Length];
            AddScore(pts);

            ActiveCapsule = new CapsuleState
            {
                Active = true,
                Position = ufo.Position,
                Kind = ufo.DropKind,
            };

            events.Add(new GameEvent
            {
                Kind = GameEventKind.UfoDestroyed,
                Position = ufo.Position,
                Value = pts,
                Powerup = ufo.DropKind,
            });
        }

        void ApplyPowerup(PowerupKind kind, Vector2 pos)
        {
            if (kind == PowerupKind.ShieldRepair)
            {
                foreach (var b in bunkers) b.RepairAll();
                AddScore(settings.ufo.shieldRepairBonus);
            }
            else
            {
                ActivePowerup = kind;
                PowerupTimeLeft = settings.ufo.powerupDuration;
            }

            events.Add(new GameEvent
            {
                Kind = GameEventKind.PowerupCollected,
                Position = pos,
                Powerup = kind,
            });
        }

        void StepFormation(float dt)
        {
            if (AliveInvaderCount <= 0) return;
            stepTimer -= dt;
            if (stepTimer > 0f) return;

            bool hitWall = false;
            float dx = MarchDirection * settings.formation.stepHorizontal;
            for (int i = 0; i < invaders.Length; i++)
            {
                if (!invaders[i].Alive) continue;
                float nextX = invaders[i].Position.x + dx;
                if (nextX > settings.arena.alienLimitX || nextX < -settings.arena.alienLimitX)
                {
                    hitWall = true;
                    break;
                }
            }

            if (hitWall)
            {
                MarchDirection = -MarchDirection;
                for (int i = 0; i < invaders.Length; i++)
                {
                    if (!invaders[i].Alive) continue;
                    invaders[i].Position.y -= settings.formation.stepVertical;
                }
            }
            else
            {
                for (int i = 0; i < invaders.Length; i++)
                {
                    if (!invaders[i].Alive) continue;
                    invaders[i].Position.x += dx;
                }
            }

            PoseFrame = 1 - PoseFrame;
            MarchNoteIndex = (MarchNoteIndex + 1) & 3;
            stepTimer = CurrentStepInterval;

            events.Add(new GameEvent
            {
                Kind = GameEventKind.FormationStepped,
                Value = MarchNoteIndex,
                Flag = hitWall,
            });

            // Scrape bunkers and check invasion line
            for (int i = 0; i < invaders.Length; i++)
            {
                if (!invaders[i].Alive) continue;
                Vector2 p = invaders[i].Position;
                for (int b = 0; b < bunkers.Count; b++)
                {
                    if (bunkers[b].ScrapeBox(p.x - 0.35f, p.x + 0.35f, p.y - 0.30f, p.y + 0.30f) > 0)
                    {
                        events.Add(new GameEvent
                        {
                            Kind = GameEventKind.BunkerEroded,
                            Position = p,
                            Value = b,
                        });
                    }
                }
                if (p.y <= settings.arena.defenseLineZ)
                {
                    Phase = GamePhase.GameOver;
                    events.Add(new GameEvent
                    {
                        Kind = GameEventKind.GameOver,
                        Position = p,
                        Flag = true,
                    });
                    return;
                }
            }
        }

        void StepAlienFiring(float dt)
        {
            if (AliveInvaderCount <= 0 || alienShots.Count >= settings.combat.maxAlienShots) return;

            float ratio = Mathf.Clamp01((float)AliveInvaderCount / invaders.Length);
            float fireInterval = Mathf.Lerp(
                settings.combat.alienFireIntervalMin,
                settings.combat.alienFireIntervalStart,
                ratio);

            alienFireTimer -= dt;
            if (alienFireTimer > 0f) return;
            alienFireTimer = fireInterval;

            // Collect lowest living invader in each column
            var shooters = new List<int>(settings.formation.columns);
            for (int col = 0; col < settings.formation.columns; col++)
            {
                int lowest = GetLowestLivingInvaderInColumn(col);
                if (lowest >= 0) shooters.Add(lowest);
            }
            if (shooters.Count == 0) return;

            int chosen = shooters[rng.Next(shooters.Count)];
            alienShots.Add(new ProjectileState
            {
                Position = invaders[chosen].Position + new Vector2(0f, -0.35f),
                SpeedZ = settings.combat.alienShotSpeed,
                FromPlayer = false,
            });
        }

        public int GetLowestLivingInvaderInColumn(int col)
        {
            int cols = settings.formation.columns;
            int rows = settings.formation.rows;
            for (int r = 0; r < rows; r++)
            {
                int idx = r * cols + col;
                if (invaders[idx].Alive) return idx;
            }
            return -1;
        }

        void LoseCannonLife()
        {
            Lives = Mathf.Max(0, Lives - 1);
            playerShots.Clear();
            alienShots.Clear();
            ActiveCapsule = default;
            ActivePowerup = PowerupKind.None;
            PowerupTimeLeft = 0f;

            events.Add(new GameEvent
            {
                Kind = GameEventKind.CannonDestroyed,
                Position = new Vector2(CannonX, CannonZ),
                Value = Lives,
            });

            if (Lives <= 0)
            {
                Phase = GamePhase.GameOver;
                events.Add(new GameEvent
                {
                    Kind = GameEventKind.GameOver,
                    Position = new Vector2(CannonX, CannonZ),
                    Flag = false,
                });
            }
            else
            {
                Phase = GamePhase.Respawning;
                phaseTimer = settings.combat.respawnDelay;
            }
        }

        // Test helpers
        public void KillInvaderForTest(int index) => KillInvader(index);
        public void SetInvaderAliveForTest(int index, bool alive)
        {
            if (invaders[index].Alive == alive) return;
            invaders[index].Alive = alive;
            AliveInvaderCount += alive ? 1 : -1;
        }
        public void SetInvaderPositionForTest(int index, Vector2 pos) => invaders[index].Position = pos;
        public void SetCannonXForTest(float x) => CannonX = Mathf.Clamp(x, -settings.arena.cannonLimitX, settings.arena.cannonLimitX);
        public void SpawnUfoForTest(float x, int direction, PowerupKind dropKind)
        {
            ActiveUfo = new UfoState
            {
                Active = true,
                Position = new Vector2(x, settings.arena.ufoZ),
                Direction = direction,
                DropKind = dropKind,
            };
        }
        public void KillUfoWithShotForTest()
        {
            ShotsFired++;
            DestroyActiveUfo();
        }
        public void CollectPowerupForTest(PowerupKind kind) => ApplyPowerup(kind, new Vector2(CannonX, CannonZ));
        public void ClearProjectilesForTest()
        {
            playerShots.Clear();
            alienShots.Clear();
        }
    }
}
