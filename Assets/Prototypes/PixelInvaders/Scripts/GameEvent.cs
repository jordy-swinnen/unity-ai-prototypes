using UnityEngine;

namespace Prototypes.PixelInvaders
{
    public enum InvaderRank
    {
        Heavy = 0, // Bottom 2 rows (10 pts)
        Guard = 1, // Middle 2 rows (20 pts)
        Scout = 2, // Top row (30 pts)
    }

    public enum PowerupKind
    {
        None = 0,
        RapidFire = 1,
        TwinShot = 2,
        ShieldRepair = 3,
    }

    public enum GamePhase
    {
        Playing = 0,
        Respawning = 1,
        WaveClear = 2,
        GameOver = 3,
    }

    public enum GameEventKind
    {
        ShotFired,
        ShotMissed,
        ShotsCancelled,
        FormationStepped,
        InvaderKilled,
        BunkerEroded,
        UfoSpawned,
        UfoDestroyed,
        PowerupCollected,
        PowerupExpired,
        CannonDestroyed,
        CannonRespawned,
        WaveCleared,
        WaveStarted,
        GameOver,
    }

    public struct GameEvent
    {
        public GameEventKind Kind;
        public Vector2 Position;
        public int Value;
        public InvaderRank Rank;
        public PowerupKind Powerup;
        public bool Flag;
    }

    public struct PixelInvadersInput
    {
        public float moveAxis;
        public bool firePressed;
        public bool restartPressed;
    }

    public struct InvaderState
    {
        public int Index;
        public int Row;
        public int Col;
        public InvaderRank Rank;
        public Vector2 Position;
        public bool Alive;
    }

    public struct ProjectileState
    {
        public Vector2 Position;
        public float SpeedZ;
        public bool FromPlayer;
        public bool TwinBolt;
    }

    public struct UfoState
    {
        public bool Active;
        public Vector2 Position;
        public int Direction;
        public PowerupKind DropKind;
    }

    public struct CapsuleState
    {
        public bool Active;
        public Vector2 Position;
        public PowerupKind Kind;
    }
}
