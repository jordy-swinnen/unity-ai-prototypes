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
- **[unverified]** The C# property that turns on a camera's Post Processing from code. A likely name is `renderPostProcessing` on `UniversalAdditionalCameraData`. Confirm by compiling, and confirm the effect by a render with the effect switched on and off.
- **[unverified]** Creating a profile in code (`VolumeProfile`, `Volume`, `Bloom` and the other overrides in `UnityEngine.Rendering.Universal`). The scene builder has to do this, since scenes are built by code. Check it by a render.
- **[unverified]** Whether the Unity 6 URP template's `SampleScene` already contains a Volume. Scenes built from an empty scene have none.
- **[unverified]** Any Universal Renderer Data setting needed for post-processing.

## Materials and emission
- **[unverified]** Emissive look on imported glTF materials, and on materials made in code with the URP Lit shader. Test with a render.
- **[unverified]** HDR emission colour values above 1 for strong bloom.

## Camera shake
- **[read]** Cinemachine 3.1 is a free package that must be installed separately. Shake uses a Cinemachine Impulse Source with `GenerateImpulseWithForce(float)` (1 is normal) or `GenerateImpulseWithVelocity(Vector3)`, and an impulse listener on the camera. [Impulse Source](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineImpulseSource.html), [scripting API](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/api/Unity.Cinemachine.CinemachineImpulseSource.html)
- **[unverified]** Whether a Cinemachine Brain is required, and the package id (likely `com.unity.cinemachine`).
- For small games, prefer a short camera-shake component of your own (offset the camera by a decaying random vector, strength from settings). It needs no package and is easy to test.

## Particles and trails
- **[unverified]** Which built-in components are best here: Particle System, Trail Renderer, and VFX Graph. For primitives and small effects, Trail Renderer and Particle System need no extra package. Confirm material and shader choices by a render.

## Time feedback
- Hit-stop and slow motion can be done by setting `Time.timeScale` briefly, or better by pausing only the simulation step so the camera and UI keep running. Either way, test that it ends.
