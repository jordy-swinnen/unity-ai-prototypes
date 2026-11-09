# Unity 6 URP: effects reference

Confidence tags: **[read]** read on Unity's own documentation page, **[unverified]** not confirmed, so check it by compiling and looking at a render. Update this file as things get verified.

## Post-processing
- **[read]** URP 6.3 has 17 Volume overrides, including Bloom, Vignette, Color Adjustments, Tonemapping, Chromatic Aberration, Film Grain, Lens Distortion, Depth of Field, Motion Blur, Color Curves, Color Lookup, White Balance, Lift Gamma Gain, Shadows Midtones Highlights, Split Toning, Channel Mixer, Panini Projection and Screen Space Lens Flare. [URP effect list](https://docs.unity.com/en-us/engine/6000.3/manual/post-processing-and-full-screen-effects/urp/effect-list.md)
- **[read]** Bloom: Threshold (default 0.9, gamma space), Intensity (0 to 1, **default 0, which means off**), Scatter (default 0.7), Tint, Clamp, High Quality Filtering, Filter (Gaussian, Dual, Kawase), Lens Dirt.
- **[read]** Vignette: Color, Center, Intensity, Smoothness (default 0.2), Rounded.
- **[read]** Color Adjustments: Post Exposure, Contrast, Color Filter, Hue Shift, Saturation.
- **[read]** Tonemapping modes: None, Neutral (little change to hue and saturation, a good grading start) and ACES (more contrast, changes hue and saturation).
- **[read]** To use effects: create a Volume (GameObject > Volume), assign a Volume Profile (New creates one), add overrides with Add Override. A global Volume affects the camera everywhere. [Set up a volume](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/set-up-a-volume.html)
- **[read]** The camera needs **Post Processing** enabled in the Camera inspector, and the volume's layer must be in the camera's Volume Mask. New URP scenes do not use post-processing by default. [Add post-processing](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/add-post-processing.html)
- **[verified]** `camera.GetUniversalAdditionalCameraData().renderPostProcessing = true` turns on a camera's Post Processing (in the package source, and confirmed on renders). Also set `volumeLayerMask`.
- **[verified]** Creating a profile in code works: `ScriptableObject.CreateInstance<VolumeProfile>()`, `AssetDatabase.CreateAsset`, then for each effect `profile.Add<Bloom>(false)`, `.Override(value)` on its parameters, and `AssetDatabase.AddObjectToAsset(component, profile)`. A `Volume` with `isGlobal = true` and `sharedProfile` set picks it up. Remove old components with `RemoveObjectFromAsset` before rebuilding.
- Scenes built from an empty scene have no Volume, so the builder adds one.
- **[verified]** Bloom shows with the URP assets' HDR setting on (both assets in this project had it on already). Not tested with HDR off. No Renderer Data change was needed.

## Materials and emission
- **[verified]** Imported glTF materials can be replaced by URP Lit materials made in code and mapped by the glTF material name. Emission on a Lit material works with `EnableKeyword("_EMISSION")` and `SetColor("_EmissionColor", ...)`.
- **[verified]** An HDR colour read with `GetColor` is the value the shader sees (linear), and `SetColor` converts again. For a saturated glow compute `(colour.linear * intensity).gamma` and pass that. Doing it the other way gave pale pastel or far too bright results. The same applies to a `MaterialPropertyBlock` colour.
- **[verified]** Neutral tonemapping turns a bright core pale. Keep emission only a little above the bloom threshold, and let the Bloom halo carry the colour.
- **[verified]** The UI blends in linear light, so a flat full-screen overlay at alpha 0.04 already shows as about a fifth. Judge overlay alpha on a render.

## Camera shake
- **[read]** Cinemachine 3.1 is a free package that must be installed separately. Shake uses a Cinemachine Impulse Source with `GenerateImpulseWithForce(float)` (1 is normal) or `GenerateImpulseWithVelocity(Vector3)`, and an impulse listener on the camera. [Impulse Source](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineImpulseSource.html), [scripting API](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineImpulseSource.html)
- Not needed here: the camera shake is a few lines of our own (`ShakeState` in `NeonPong/Scripts/NeonPongFeel.cs`).
- For small games, prefer a short camera-shake component of your own (offset the camera by a decaying random vector, strength from settings). It needs no package and is easy to test.

## Particles and trails
- **[verified]** Trail Renderer and Particle System need no package. A material on `Universal Render Pipeline/Particles/Unlit` with `_Surface` 1, `_Blend` 2, `_SrcBlend` and `_DstBlend` 1, `_ZWrite` 0, the `_SURFACE_TYPE_TRANSPARENT` keyword and an HDR `_BaseColor` gives an additive glow that Bloom picks up. Emit particles from code with `ParticleSystem.Emit(EmitParams, 1)` per particle. A Trail Renderer with `alignment = TransformZ` and the transform pitched up 90 degrees lies flat for a top-down camera.
- Unity's time scale at 0 freezes particles and trails, which makes a still frame to capture.

## Time feedback
- Hit-stop and slow motion can be done by setting `Time.timeScale` briefly, or better by pausing only the simulation step so the camera and UI keep running. Either way, test that it ends.
