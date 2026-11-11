using System;
using UnityEngine;

namespace Prototypes.DioramaTurn
{
    [Serializable]
    public class TurnSettings
    {
        [Tooltip("Seconds for a quarter turn.")] public float duration = 0.45f;
        [Tooltip("Degrees the turn goes past its end and settles back.")] public float overshootDeg = 4f;
        [Tooltip("How many extra turns can wait while one runs.")] public int queueMax = 1;
    }

    [Serializable]
    public class WalkSettings
    {
        [Tooltip("Seconds for one step to the next node.")] public float stepTime = 0.22f;
        [Tooltip("Seconds for a hop across a join.")] public float hopTime = 0.35f;
        [Tooltip("Height of the hop arc, in cells.")] public float hopHeight = 0.6f;
        [Tooltip("How much Pip squashes on a step.")] public float squash = 0.18f;
    }

    [Serializable]
    public class RoomSettings
    {
        public int maxX = 5;
        public int maxY = 5;
        public int maxZ = 5;
        [Tooltip("A room still earns two stars up to par plus this many turns.")] public int parTwoStarsExtra = 2;
    }

    [Serializable]
    public class CameraSettings
    {
        public float pitchDeg = 35.264f;
        public float baseYawDeg = 45f;
        public float pixelsPerUnit = 32f;
        [Tooltip("The most screen pixels per sprite pixel.")] public int maxScale = 4;
        [Tooltip("Height of the room on screen, in world units, that must fit.")] public float roomExtentUnits = 10f;
        public bool snapSpritesToPixels = true;
        public float distance = 30f;
        [Tooltip("Sprites are pushed this far toward the camera so their feet do not sink into blocks.")] public float spritePushToCamera = 0.3f;
        [Tooltip("The screen height the builder sizes the camera for; at run time the real height is used.")] public int referenceHeight = 1080;
        [Tooltip("A click counts as a node when it lands within this much of the screen height from it.")] public float clickRadiusViewport = 0.09f;
    }

    [Serializable]
    public class LightSettings
    {
        [Tooltip("Warm key light colour.")] public Color keyColour = new Color(1f, 0.941f, 0.839f);
        public float keyIntensity = 1.1f;
        [Tooltip("How steeply the key light comes down.")] public float keyPitchDeg = 50f;
        [Tooltip("The key light's yaw relative to the camera, so every view is lit the same.")] public float keyYawOffsetDeg = -30f;
        [Tooltip("Cool sky fill.")] public Color fillColour = new Color(0.604f, 0.859f, 0.910f);
        [Tooltip("Tint of the shadows and the ambient.")] public Color shadowTint = new Color(0.357f, 0.294f, 0.541f);
        [Tooltip("Camera background: the soft sky the diorama sits in.")] public Color backdropColour = new Color(0.604f, 0.859f, 0.910f);
        public float shadowStrength = 0.8f;
    }

    [Serializable]
    public class LookSettings
    {
        [Tooltip("Baked occlusion strength on the lit shader (0 turns it off).")] public float occlusion = 0.6f;
        [Tooltip("Wood texture strength (0 turns it off).")] public float woodTexture = 1f;
        [Tooltip("Wood normal strength.")] public float woodNormal = 0.5f;
        [Tooltip("Wood repeats per metre.")] public float woodScale = 0.35f;
    }

    [Serializable]
    public class PostSettings
    {
        public float bloomIntensity = 0.35f;
        public float bloomThreshold = 1f;
        public float vignette = 0.2f;
        [Tooltip("Screen fraction kept sharp by the tilt-shift pass.")] public float tiltBandCentre = 0.5f;
        public float tiltBandHalfHeight = 0.28f;
        public float tiltBlur = 1f;
        [Tooltip("Colour grading, in -100..100 like the Volume component.")] public float saturation = 12f;
        public float contrast = 8f;
    }

    [Serializable]
    public class FeelSettings
    {
        [Tooltip("Scales the overshoot, the squash, particles and the goal slow motion. 0 turns them all off.")]
        [Range(0f, 1f)] public float effectStrength = 1f;
        [Tooltip("How slow the goal moment runs.")] public float goalSlowMo = 0.4f;
        [Tooltip("How long the goal slow motion lasts.")] public float goalSlowMoSeconds = 0.5f;
    }

    [Serializable]
    public class AudioSettings
    {
        [Range(0f, 1f)] public float master = 0.8f;
        [Range(0f, 1f)] public float sfx = 0.8f;
        [Tooltip("Music volume under the effects.")]
        [Range(0f, 1f)] public float music = 0.35f;
        [Tooltip("How far the music dips under the star moment.")] public float musicDuckDb = -9f;
        public float musicDuckSeconds = 1.5f;
    }

    /// <summary>Every tuning value of DioramaTurn. A value is never written into code.</summary>
    [CreateAssetMenu(menuName = "Prototypes/DioramaTurn/Settings")]
    public sealed class DioramaTurnSettings : ScriptableObject
    {
        public TurnSettings turn = new TurnSettings();
        public WalkSettings walk = new WalkSettings();
        public RoomSettings rooms = new RoomSettings();
        public CameraSettings camera = new CameraSettings();
        public LightSettings light = new LightSettings();
        public LookSettings look = new LookSettings();
        public PostSettings post = new PostSettings();
        public FeelSettings feel = new FeelSettings();
        public AudioSettings audio = new AudioSettings();

        void OnValidate() => Clamp();

        /// <summary>Keeps every value in a safe range.</summary>
        public void Clamp()
        {
            turn.duration = Mathf.Clamp(turn.duration, 0.05f, 3f);
            turn.overshootDeg = Mathf.Clamp(turn.overshootDeg, 0f, 15f);
            turn.queueMax = Mathf.Clamp(turn.queueMax, 0, 3);
            walk.stepTime = Mathf.Clamp(walk.stepTime, 0.05f, 2f);
            walk.hopTime = Mathf.Clamp(walk.hopTime, 0.05f, 2f);
            walk.hopHeight = Mathf.Clamp(walk.hopHeight, 0f, 3f);
            walk.squash = Mathf.Clamp(walk.squash, 0f, 0.5f);
            rooms.maxX = Mathf.Clamp(rooms.maxX, 1, 9);
            rooms.maxY = Mathf.Clamp(rooms.maxY, 1, 9);
            rooms.maxZ = Mathf.Clamp(rooms.maxZ, 1, 9);
            rooms.parTwoStarsExtra = Mathf.Clamp(rooms.parTwoStarsExtra, 0, 10);
            camera.pixelsPerUnit = Mathf.Max(1f, camera.pixelsPerUnit);
            camera.maxScale = Mathf.Clamp(camera.maxScale, 1, 8);
            camera.roomExtentUnits = Mathf.Max(1f, camera.roomExtentUnits);
            camera.distance = Mathf.Max(5f, camera.distance);
            camera.spritePushToCamera = Mathf.Clamp(camera.spritePushToCamera, 0f, 2f);
            camera.referenceHeight = Mathf.Clamp(camera.referenceHeight, 480, 4320);
            camera.clickRadiusViewport = Mathf.Clamp(camera.clickRadiusViewport, 0.01f, 0.25f);
            light.keyIntensity = Mathf.Clamp(light.keyIntensity, 0f, 5f);
            light.keyPitchDeg = Mathf.Clamp(light.keyPitchDeg, 5f, 89f);
            light.keyYawOffsetDeg = Mathf.Clamp(light.keyYawOffsetDeg, -180f, 180f);
            light.shadowStrength = Mathf.Clamp01(light.shadowStrength);
            look.occlusion = Mathf.Clamp01(look.occlusion);
            look.woodTexture = Mathf.Clamp01(look.woodTexture);
            look.woodNormal = Mathf.Clamp(look.woodNormal, 0f, 2f);
            look.woodScale = Mathf.Clamp(look.woodScale, 0.05f, 2f);
            post.bloomIntensity = Mathf.Clamp(post.bloomIntensity, 0f, 8f);
            post.bloomThreshold = Mathf.Clamp(post.bloomThreshold, 0f, 4f);
            post.vignette = Mathf.Clamp01(post.vignette);
            post.tiltBandCentre = Mathf.Clamp01(post.tiltBandCentre);
            post.tiltBandHalfHeight = Mathf.Clamp(post.tiltBandHalfHeight, 0.02f, 0.5f);
            post.tiltBlur = Mathf.Clamp(post.tiltBlur, 0f, 3f);
            post.saturation = Mathf.Clamp(post.saturation, -100f, 100f);
            post.contrast = Mathf.Clamp(post.contrast, -100f, 100f);
            feel.effectStrength = Mathf.Clamp01(feel.effectStrength);
            feel.goalSlowMo = Mathf.Clamp(feel.goalSlowMo, 0.05f, 1f);
            feel.goalSlowMoSeconds = Mathf.Clamp(feel.goalSlowMoSeconds, 0f, 3f);
            audio.master = Mathf.Clamp01(audio.master);
            audio.sfx = Mathf.Clamp01(audio.sfx);
            audio.music = Mathf.Clamp01(audio.music);
            audio.musicDuckDb = Mathf.Clamp(audio.musicDuckDb, -40f, 0f);
            audio.musicDuckSeconds = Mathf.Clamp(audio.musicDuckSeconds, 0f, 5f);
        }

        public SimulationTiming ToTiming() => new SimulationTiming(turn.duration, walk.stepTime, walk.hopTime, turn.queueMax);
    }
}
