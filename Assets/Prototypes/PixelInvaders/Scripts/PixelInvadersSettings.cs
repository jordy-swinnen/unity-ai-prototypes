using System;
using UnityEngine;

namespace Prototypes.PixelInvaders
{
    [Serializable]
    public struct ArenaSettings
    {
        public float halfWidth;
        public float cannonLimitX;
        public float alienLimitX;
        public float cannonZ;
        public float defenseLineZ;
        public float bunkerZ;
        public float wave1TopZ;
        public float ufoZ;
        public float topRailZ;
        public float bottomRailZ;

        public static ArenaSettings Default => new ArenaSettings
        {
            halfWidth = 7.0f,
            cannonLimitX = 6.0f,
            alienLimitX = 6.2f,
            cannonZ = -4.2f,
            defenseLineZ = -3.5f,
            bunkerZ = -2.8f,
            wave1TopZ = 3.6f,
            ufoZ = 4.8f,
            topRailZ = 5.2f,
            bottomRailZ = -4.8f,
        };
    }

    [Serializable]
    public struct FormationSettings
    {
        public int rows;
        public int columns;
        public float colSpacing;
        public float rowSpacing;
        public float stepHorizontal;
        public float stepVertical;
        public float maxStepInterval;
        public float minStepInterval;
        public float speedCurveExponent;
        public float waveDropStep;
        public int maxWaveDropSteps;
        public float waveClearDelay;

        public static FormationSettings Default => new FormationSettings
        {
            rows = 5,
            columns = 11,
            colSpacing = 1.0f,
            rowSpacing = 0.75f,
            stepHorizontal = 0.22f,
            stepVertical = 0.35f,
            maxStepInterval = 0.65f,
            minStepInterval = 0.065f,
            speedCurveExponent = 1.35f,
            waveDropStep = 0.30f,
            maxWaveDropSteps = 4,
            waveClearDelay = 1.6f,
        };
    }

    [Serializable]
    public struct CombatSettings
    {
        public int startingLives;
        public float cannonSpeed;
        public float cannonStopTime;
        public float playerShotSpeed;
        public float fireInputBuffer;
        public float alienShotSpeed;
        public float alienFireIntervalStart;
        public float alienFireIntervalMin;
        public int maxAlienShots;
        public float respawnDelay;
        public float alienHitRadius;
        public float cannonHitRadius;
        public float shotCancelRadius;

        public static CombatSettings Default => new CombatSettings
        {
            startingLives = 3,
            cannonSpeed = 7.5f,
            cannonStopTime = 0.04f,
            playerShotSpeed = 14.0f,
            fireInputBuffer = 0.10f,
            alienShotSpeed = 6.5f,
            alienFireIntervalStart = 1.25f,
            alienFireIntervalMin = 0.55f,
            maxAlienShots = 3,
            respawnDelay = 1.0f,
            alienHitRadius = 0.34f,
            cannonHitRadius = 0.32f,
            shotCancelRadius = 0.22f,
        };
    }

    [Serializable]
    public struct BunkerSettings
    {
        public int count;
        public int cols;
        public int rows;
        public float cellSize;
        public int blastRadiusCells;

        public static BunkerSettings Default => new BunkerSettings
        {
            count = 4,
            cols = 12,
            rows = 8,
            cellSize = 0.10f,
            blastRadiusCells = 1,
        };
    }

    [Serializable]
    public struct UfoPowerupSettings
    {
        public float ufoSpawnInterval;
        public int minAliensForUfo;
        public float ufoSpeed;
        public float ufoHitRadius;
        public float capsuleFallSpeed;
        public float capsulePickupRadius;
        public float powerupDuration;
        public int rapidFireMaxShots;
        public float rapidFireCooldown;
        public float twinShotOffset;
        public int twinShotMaxSalvos;
        public int shieldRepairBonus;

        public static UfoPowerupSettings Default => new UfoPowerupSettings
        {
            ufoSpawnInterval = 14.0f,
            minAliensForUfo = 8,
            ufoSpeed = 3.6f,
            ufoHitRadius = 0.48f,
            capsuleFallSpeed = 3.2f,
            capsulePickupRadius = 0.45f,
            powerupDuration = 6.0f,
            rapidFireMaxShots = 3,
            rapidFireCooldown = 0.16f,
            twinShotOffset = 0.22f,
            twinShotMaxSalvos = 2,
            shieldRepairBonus = 50,
        };
    }

    [Serializable]
    public struct ScoreSettings
    {
        public int heavyPoints;
        public int guardPoints;
        public int scoutPoints;
        public int[] ufoPointsTable;
        public int defaultHighScore;

        public static ScoreSettings Default => new ScoreSettings
        {
            heavyPoints = 10,
            guardPoints = 20,
            scoutPoints = 30,
            ufoPointsTable = new[] { 100, 150, 200, 300 },
            defaultHighScore = 1000,
        };
    }

    [Serializable]
    public struct CameraSettings
    {
        public float pitchDeg;
        public float fovDeg;
        public float distance;
        public float lookAtZ;
        public int pixelsPerUnit;

        public static CameraSettings Default => new CameraSettings
        {
            pitchDeg = 55.0f,
            fovDeg = 34.0f,
            distance = 14.8f,
            lookAtZ = -0.15f,
            pixelsPerUnit = 16,
        };
    }

    [Serializable]
    public struct LookSettings
    {
        public float bloomIntensity;
        public float bloomThreshold;
        public float vignetteIntensity;
        public float keyLightIntensity;
        public float shotLightIntensity;
        public float shotLightRange;
        public float textureStrength;
        public float normalStrength;
        public float occlusionStrength;

        public static LookSettings Default => new LookSettings
        {
            bloomIntensity = 0.65f,
            bloomThreshold = 1.0f,
            vignetteIntensity = 0.25f,
            keyLightIntensity = 1.15f,
            shotLightIntensity = 1.8f,
            shotLightRange = 2.2f,
            textureStrength = 0.85f,
            normalStrength = 0.55f,
            occlusionStrength = 0.65f,
        };
    }

    [Serializable]
    public struct FeelSettings
    {
        [Range(0f, 1f)] public float effectStrength;
        public float alienHitStop;
        public float ufoHitStop;
        public float deathHitStop;
        public float waveClearSlowMoScale;
        public float waveClearSlowMoDuration;
        public float shakeFire;
        public float shakeAlienKill;
        public float shakeBunkerHit;
        public float shakeUfoKill;
        public float shakeDeath;

        public static FeelSettings Default => new FeelSettings
        {
            effectStrength = 1.0f,
            alienHitStop = 0.035f,
            ufoHitStop = 0.060f,
            deathHitStop = 0.090f,
            waveClearSlowMoScale = 0.5f,
            waveClearSlowMoDuration = 0.35f,
            shakeFire = 0.02f,
            shakeAlienKill = 0.05f,
            shakeBunkerHit = 0.03f,
            shakeUfoKill = 0.10f,
            shakeDeath = 0.18f,
        };
    }

    [Serializable]
    public struct AudioSettings
    {
        [Range(0f, 1f)] public float masterVolume;
        [Range(0f, 1f)] public float musicVolume;
        public float musicDuckDb;
        public float musicDuckDuration;

        public static AudioSettings Default => new AudioSettings
        {
            masterVolume = 0.8f,
            musicVolume = 0.28f,
            musicDuckDb = -8.0f,
            musicDuckDuration = 1.2f,
        };
    }

    public sealed class PixelInvadersSettings : ScriptableObject
    {
        public ArenaSettings arena = ArenaSettings.Default;
        public FormationSettings formation = FormationSettings.Default;
        public CombatSettings combat = CombatSettings.Default;
        public BunkerSettings bunkers = BunkerSettings.Default;
        public UfoPowerupSettings ufo = UfoPowerupSettings.Default;
        public ScoreSettings scores = ScoreSettings.Default;
        public CameraSettings camera = CameraSettings.Default;
        public LookSettings look = LookSettings.Default;
        public FeelSettings feel = FeelSettings.Default;
        public AudioSettings audio = AudioSettings.Default;

        void OnValidate() => Validate();

        public void Validate()
        {
            if (formation.rows <= 0) formation = FormationSettings.Default;
            if (combat.startingLives <= 0) combat = CombatSettings.Default;
            if (bunkers.cols <= 0) bunkers = BunkerSettings.Default;
            if (ufo.ufoSpawnInterval <= 0f) ufo = UfoPowerupSettings.Default;
            if (scores.ufoPointsTable == null || scores.ufoPointsTable.Length == 0) scores = ScoreSettings.Default;
            if (camera.fovDeg <= 0f) camera = CameraSettings.Default;
            feel.effectStrength = Mathf.Clamp01(feel.effectStrength);
            look.textureStrength = Mathf.Clamp01(look.textureStrength);
            look.normalStrength = Mathf.Clamp01(look.normalStrength);
            look.occlusionStrength = Mathf.Clamp01(look.occlusionStrength);
            audio.masterVolume = Mathf.Clamp01(audio.masterVolume);
            audio.musicVolume = Mathf.Clamp01(audio.musicVolume);
        }

        public float EffectiveShake(float baseShake) => baseShake * Mathf.Clamp01(feel.effectStrength);
        public float EffectiveHitStop(float baseHitStop) => baseHitStop * Mathf.Clamp01(feel.effectStrength);
        public float EffectiveSlowMoDuration() => feel.waveClearSlowMoDuration * Mathf.Clamp01(feel.effectStrength);
    }
}
