using UnityEngine;

namespace Prototypes.NeonPong
{
    /// <summary>
    /// Particle bursts for the feel table: a spark on a wall, a fan of the paddle's colour on a
    /// paddle hit, a big burst at the end line on a point, and a shower on the win. The global
    /// effect strength scales the counts, and zero emits nothing.
    /// </summary>
    public class NeonPongBursts : MonoBehaviour
    {
        public NeonPongGame game;
        public ParticleSystem system;

        /// <summary>Particles emitted since the scene started. Tests read it.</summary>
        public int TotalEmitted { get; private set; }

        void OnEnable()
        {
            if (game != null) game.EventRaised += OnGameEvent;
        }

        void OnDisable()
        {
            if (game != null) game.EventRaised -= OnGameEvent;
        }

        void Update()
        {
            if (game == null || game.Feel == null) return;
            var main = system.main;
            main.simulationSpeed = game.Feel.Time.VisualScale;
        }

        void OnGameEvent(GameEvent e)
        {
            var s = game.settings;
            float k = game.Feel.Strength;
            if (k <= 0f) return;

            var table = new Vector3(e.Position.x, 0.3f, e.Position.y);
            switch (e.Kind)
            {
                case EventKind.WallHit:
                {
                    // A spark off the rail, pointing into the table.
                    float into = e.Position.y > 0f ? -1f : 1f;
                    Emit(table, s.ballColor, Count(s.wallBurst, k), new Vector3(0f, 0f, into), 70f, s.burstSpeed * 0.7f);
                    break;
                }
                case EventKind.PaddleHit:
                {
                    float away = e.Side == Side.Right ? -1f : 1f;
                    var color = e.Side == Side.Left ? s.player1Color : s.player2Color;
                    Emit(table, color, Count(s.paddleBurst, k), new Vector3(away, 0f, 0f), 75f, s.burstSpeed);
                    break;
                }
                case EventKind.Point:
                {
                    float lineX = e.Side == Side.Left ? s.halfLength : -s.halfLength;
                    float inward = e.Side == Side.Left ? -1f : 1f;
                    var color = e.Side == Side.Left ? s.player1Color : s.player2Color;
                    Emit(new Vector3(lineX, 0.3f, e.Position.y), color, Count(s.pointBurst, k),
                        new Vector3(inward, 0f, 0f), 80f, s.burstSpeed * 1.6f);
                    break;
                }
                case EventKind.Win:
                {
                    var color = e.Side == Side.Left ? s.player1Color : s.player2Color;
                    Emit(new Vector3(0f, 0.3f, 0f), color, Count(s.winBurst, k), Vector3.up, 180f, s.burstSpeed * 1.2f);
                    break;
                }
            }
        }

        static int Count(int baseCount, float strength) => Mathf.RoundToInt(baseCount * strength);

        void Emit(Vector3 position, Color color, int count, Vector3 direction, float spreadDegrees, float speed)
        {
            var s = game.settings;
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = color,
                startSize = s.burstSize,
                startLifetime = s.burstLifetime,
            };
            for (int i = 0; i < count; i++)
            {
                // A random direction within the cone, flattened mostly onto the table plane.
                var random = Random.onUnitSphere;
                random.y = Mathf.Abs(random.y) * 0.5f;
                var dir = Vector3.Slerp(direction, random, Mathf.Clamp01(spreadDegrees / 180f * Random.value * 2f)).normalized;
                emit.velocity = dir * speed * Random.Range(0.5f, 1f);
                system.Emit(emit, 1);
                TotalEmitted++;
            }
        }
    }
}
