# Surfaces: textures, baked occlusion, shaders and dissolves

**The agent decides.** A person should not have to ask for textures, a shader or occlusion. Decide it for every game from the spec's look brief, record the decision and the reason in the spec's "Surfaces" row, and build it. Ask the person only for the cases in "When to ask" below. Medieval Chess shipped flat colours first, and "can the models and surfaces be better?" was the first thing the person asked. Don't repeat that.

## Decide per material
Go through every material in the brief. Pick the cheapest row that serves the pillars.

| The brief says | Do this | Why |
|---|---|---|
| Neon, glass, holograms, flat or stylised colour, strongly emissive | **Flat colour + emission + Bloom.** No texture. | A texture muddies a clean emissive look. Say "none, by design" in the spec. |
| Stone, wood, metal, cloth, bone, skin, any real material, or "heavy", "weathered", "handmade" | **Procedural texture** (albedo + normal) | Flat colour looks like plastic. This is the most common gap. |
| Objects that sit close together in dim light (figures, props with folds, crevices) | **Baked ambient occlusion** | A dim hall hides shape. Occlusion puts the shadow back in the cracks. |
| A big floor or board of repeating tiles | **World-space (triplanar) mapping** | No tile repeats its neighbour, no UV work. |
| Pieces or props that move or animate | **Object-space grain + vertex-colour occlusion** | Needs no UV maps and follows the object. |
| An object is removed, destroyed or spawned (a capture, a death, a pickup, a summon) | **A dissolve or burn shader**, plus the particles | The change then reads as an event, not a pop. |
| Water, force fields, energy, terrain blending, special lighting | **A hand-written URP shader** for that one thing | Use a custom shader only when textures and materials cannot do it. |

If a row says yes, do it in this order: textures, then occlusion, then shader effects. Skip a row only for a reason you write down.

## Recipes (all verified in Medieval Chess, under `Assets/Prototypes/MedievalChess/`)
- **Textures.** `Tools/textures/<Name>/make_textures.py`, run headless in Blender (it ships numpy: `/Applications/Blender.app/Contents/MacOS/Blender -b -P <script>`). Tileable noise made in the frequency domain, voronoi for stones, whole-number repeats for wood rings. 512 px albedo ("detail" maps with an average near 0.8, so the palette still sets the colour) and normal maps into `Art/Textures/<name>_albedo.png` and `<name>_normal.png`. No packages, no downloads, so nothing to license. Look at every texture after you generate it (read the PNG).
- **Import.** An `AssetPostprocessor` in the prototype's Editor folder (`SurfaceTextureImporter`) sets `_normal` files to Normal map, others to sRGB, Repeat, trilinear, mipmaps. No clicks, and the `.meta` files carry it.
- **Shader.** `Shaders/<Name>Lit.shader`, hand-written URP (copy `MedievalLit.shader` and rename it; move it to `Assets/Shared/Shaders/` when a second prototype uses it). It maps the textures in world space for the set and in object space as grain for moving things, multiplies in baked occlusion from the vertex colour, and has a dissolve with a glowing rim. It calls `UniversalFragmentPBR`, so lights, shadows, torches and fog behave like the Lit shader. Scripts write materials through one builder helper (`DuelSceneBuilder.Surface`) with strengths from the settings asset.
- **Occlusion.** In the Blender piece script: build a `BVHTree` of all parts in the rest pose, cast about 40 cosine-weighted rays per vertex (0.28 m reach), write a `FLOAT_COLOR` point attribute named `AO`, export with `export_vertex_color="ACTIVE"`. glTFast imports it as `Mesh.colors`. No UV maps are needed. Two alternates that never show together (two shoulder shapes) must not shade each other.
- **Dissolve.** A `Dissolve` channel in the action plan (a pure function of time, tested), and a `MaterialPropertyBlock` with `_Dissolve` set per renderer by the actor, so the side materials stay shared. Stop shrinking the victim and let it burn instead.
- **Settings.** Every strength lives on the settings asset (`look.surface`: texture, normal, grain, occlusion, edge colour and width; `fight.dissolveTime`). A strength of 0 turns each part off.

## Tests to write first
- The shader exists, `ShaderUtil.GetShaderMessages` has no errors and `isSupported` is true.
- Every texture is square, a power of two, not flat (standard deviation), tiles (the seam is no worse than the inside), and each normal map holds unit vectors that point mostly out. Importers have the right type.
- Every exported mesh has a vertex colour per vertex, and the occlusion varies (some dark, some open, not all dark).
- The dissolve rises 0 to 1 with time, stays 0 before the event and ends hidden, and the actor passes it to its renderers.
- The scene's materials use the shader, and the strengths are in range.

## Look at it
Render the board and a close-up of the pieces from play distance (`capture.sh screen` or `play`). Compare with and without the new surface (set the strength to 0). Check that the gameplay objects still read and that the texture does not fight the colour that carries meaning (side colours, highlights). A texture that makes things harder to read is cut.

## When to ask the person
Ask, with a recommendation, only when:
- a **package** is needed (Shader Graph assets are fine to avoid: hand-write the shader; a texture package needs approval),
- a texture, model or sound would be **downloaded or copied** (needs a licence record), 
- the **result is large** (more than about 10 MB of new binary files, or more than 2048 px textures),
- there is a real **style choice** the brief does not settle (gritty or clean, hand-painted or realistic), or
- the change would **cost real performance** on the target (many dynamic lights with shadows, large transparent areas).
Otherwise decide, build it, and say what you chose in the report.

## Limits to remember
- A hand-written shader must be kept compatible with the URP version in use: copy the keyword lists from the installed `Lit.shader` (`Library/PackageCache/com.unity.render-pipelines.universal@*/Shaders/Lit.shader`) and drop only what the game does not use. The Medieval Chess shader has no lightmaps, probes, decals, screen-space shadows or DepthNormals pass (so no SSAO), and says so in its header. Add a pass or a keyword when a feature needs it.
- A material property block makes a renderer skip the SRP Batcher. That is fine for a few actors, not for hundreds of objects.
- Vertex occlusion is coarse on big flat faces and is baked in the rest pose: parts that swing a long way show a little wrong shading. Raise the mesh density or bake a texture if that shows.
- You cannot judge how a surface feels in motion. Say so and put it on the play-test list.
