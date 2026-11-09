# Pitfalls

Each of these happened while building the first prototype in this repository.

1. **Unity 6 has no built-in `.glb` importer.** Without a glTF package the `.meta` says `DefaultImporter` and the file is an unknown asset. Adding `com.unity.cloud.gltfast` (6.20.0 here) fixed it. Adding a package needs approval, and the add fails with "not signed in" until Unity Hub is signed in.
2. **`EditorSceneManager.NewScene` unloads unused assets.** An asset loaded before it becomes an empty reference by the time you assign it, and the scene saves `fileID: 0`. Load assets after `NewScene`. A builder test that checks the reference is not null caught this.
3. **Scene files differ on every save.** Unity writes new object IDs, so two builds are not byte-identical. Test "builds the same scene" by comparing content (objects, transforms, components, references), not the file text. Rebuilding makes a noisy git diff, so rebuild only when needed.
4. **A PlayMode test assembly must cover all platforms.** An Editor-only test assembly is run as EditMode. Code that needs `UnityEditor` (to load a scene by path) goes inside `#if UNITY_EDITOR`.
5. **PlayMode tests need `--async-tests`.** The plain call returns zero results. Poll `test status` until `completed`.
6. **`unity test` refuses to run while the Editor has the project open** (exit 6). Use `unity cmd test run` instead.
7. **`unity recompile` can exit 7 right after a reload.** Wait until `unity status` shows `ready` and retry. `Tools/unity/check.sh` does this.
8. **A new test that passes first time proves little.** Break something on purpose and check that a test fails.
9. **TextMeshPro needs its font resources imported**, and a fresh project doesn't have them. The plain UI `Text` component with the built-in font works without import. Decide in the plan.
10. **The Editor in the background may not advance Play Mode time.** A `play` capture can show the first frame. PlayMode tests are not affected.
11. **`git lfs track` rewrites `.gitattributes` and strips blank lines.** Add the rule by hand to keep the diff small, or check the diff.
12. **Assets and models.** Blender exports are checked on the file (sizes, pivot, colour) and again after Unity imports them (bounds, material count).
13. **Don't leave test scenes behind.** A test that builds a scene to a temp path must delete it in its teardown.
14. **The Editor must be open.** If `unity status` doesn't show a ready Editor for this project, the loop can't run.
15. **The first test run after new test files compile can report zero results**, even though `test list` shows the tests. Wait a few seconds and run again. `Tools/unity/check.sh` retries.
16. **A rules test that plays for many seconds can end the game.** With the win score reached, the paddles freeze. Raise the win score in the test, or set up the state directly.
17. **`capture.sh camera` has no post-processing.** A scene with Bloom and Vignette looks flat in that capture. Use `capture.sh screen` (the Game window) to judge a look. Open the scene first.
18. **Creating URP materials or a Volume profile can make Unity add a line to `ProjectSettings/URPProjectSettings.asset`** (`m_ProjectSettingFolderPath`). Nobody asked for that, so check `git status` and restore the file before committing.
19. **`wasPressedThisFrame` only works in Play Mode.** Test key mapping with `isPressed` in EditMode, and a "pressed this frame" key in a PlayMode test with a test keyboard device (`InputSystem.AddDevice<Keyboard>()`, then `QueueStateEvent` and `yield return null`).
20. **A settings asset created just before `NewScene` is unloaded by it** and the scene saves an empty reference. Create the asset first, then load it again after `NewScene`.
21. **Edit Mode captures can show garbled or missing UI text** (the dynamic font atlas goes stale after a rebuild or a test run). Judge UI on `capture.sh play`, which enters Play Mode.
22. **To get a still frame of a moving moment**, enter Play Mode, set up the state with `unity cmd script eval`, then set `autoStep` off and `Time.timeScale = 0` before `capture.sh screen`. Particles and trails freeze in place.
23. **A test that changes a settings asset must restore it** (use a teardown). A ScriptableObject changed in a PlayMode test can stay changed in the Editor.
24. **`Object.FindFirstObjectByType` is deprecated in this Unity version** (it warns). Use `FindAnyObjectByType` in new code.
25. **Articulated models through Blender and glTFast:** the node hierarchy and names survive, and an empty object with an origin at a joint keeps that pivot. An object scale in Blender becomes a node scale, so apply it to the mesh. **Blender +x arrives as Unity -x**, so write a piece's right side on Blender -x. A root empty merges into the asset's own root node. The piece script in `Tools/blender/MedievalChess/make_pieces.py` shows the pattern.
26. **Resolve a repository path in a Blender script with `parents[3]`** from `Tools/blender/<Name>/script.py`. `parents[2]` silently writes into `Tools/Assets/`.
27. **A camera that moves without re-aiming loses the subject.** Push and lower the camera, then `LookRotation` at the focus point.
28. **For a still frame of an animation, make the animation a pure function of time** and call `ShowAt(t)` from `unity cmd script eval` in Play Mode, then capture. It does not depend on frame timing or on the Editor being in front.
29. **Don't name a type `Key` in code that uses the Input System.** `UnityEngine.InputSystem.Key` clashes with it and gives confusing errors. Use a name like `ChannelKey`.
30. **A rules engine can pass every perft number and still be wrong.** Perft at shallow depth did not reach a rook captured on its corner. Write a direct test for each special rule, and break a rule on purpose to see a test fail.
31. **Test positions must be legal.** The side not to move cannot be in check, so a position with a rook giving check to the wrong king is refused by the FEN reader. Put the kings on squares nothing attacks.
32. **A few games prove nothing about which engine is stronger.** Two games of depth 4 against depth 3 lost, while ten games won 7 to 3. Use a large sample, and keep a slow check as an `[Explicit]` test. Measure speed and depth with `unity cmd script eval` (give it a `--timeout`).
33. **A search with a time cap is not repeatable.** For a test that must give the same answer every run, set a long cap and limit the depth.
34. **`GameObject.Find` skips inactive objects.** A hidden panel (the promotion choice, the result screen) is not found. Use `FindObjectsByType<T>(FindObjectsInactive.Include)` or keep a reference from the builder.
35. **Restoring a scene file under an open Editor can freeze it** behind a modal "reload" dialog. Never `git checkout` a scene that is open. Rebuild it with the menu command instead.
36. **Test runs dirty some files.** `ProjectSettings/URPProjectSettings.asset` and another prototype's materials change after a test run. Run `git checkout` on them before you commit, and check `git status`.
37. **`unity cmd script eval` has limits.** No local functions used as method groups (use `System.Func`). A FEN with a wrong square count throws, so count the ranks.
38. **`parents[3]` again, for audio too.** `Tools/audio/<Name>/make_sounds.py` is three folders below the repository root. `parents[2]` wrote into `Tools/Assets/`. Check where a script wrote before you run the tests.
39. **A still frame of particles:** emit them, then set `Time.timeScale` to about 0.02 (not 0) and capture. At 0 a new particle is never drawn, and at normal speed it is gone before the capture command runs.
40. **Defaults in a settings class do not change a saved asset.** After editing a default, reset the section with `unity cmd script eval` (`s.look = new LookSettings()`, then `SetDirty` and `SaveAssets`) and rebuild the scene.
41. **`Awake` does not run in Edit Mode.** A component whose logic a builder test calls must read its base values on first use, not in `Awake`.
42. **A full-screen UI overlay is far stronger than its alpha.** Blending is in linear light: 30 percent gold covered the board, 3 percent is a gentle tint.
43. **The Game view does not repaint while the Editor is in the background,** so `capture.sh screen` can return a stale frame (the menu, though the game has started). In Play Mode use `capture.sh camera` (it renders the main camera, with no Bloom or Vignette), and drive the game with `c.Step(0.02f)` in a loop. A big `Step(1.2f)` is clamped, and slow motion stretches time around a blow, so count frames, not seconds. Judge Bloom on a `play` capture with the Editor in front.
44. **A `MonoBehaviour` field initializer cannot create a `MaterialPropertyBlock`** (`CreateImpl is not allowed to be called from a MonoBehaviour constructor`). Create it on first use.
45. **Rename or move assets with `AssetDatabase.RenameAsset` or `MoveAsset`** (`unity cmd script eval`), never `mv`. The GUID and the `.meta` follow, so scenes and materials keep their references. Do it before changing the builder code that names them, and rebuild the scenes after.
46. **A hand-written URP shader needs URP's own keyword list.** Copy the pragmas from the installed `Library/PackageCache/com.unity.render-pipelines.universal@*/Shaders/Lit.shader` (Forward+ is `_CLUSTER_LIGHT_LOOP`, not `_FORWARD_PLUS`) and call `UniversalFragmentPBR`. `ShaderUtil.GetShaderMessages` returning nothing does not prove every pass compiles, so render the result and check for pink.
47. **The first `check.sh` after new textures, models or a shader can take ten minutes** (import, texture compression, shader variants). Run it in the background with the output in a file and read the file. Later runs take two or three minutes.
48. **A sampled seam test can mistake a designed edge for a seam.** Plank gaps in the wood texture sit on the wrap line. Compare the seam with the texture's own average and biggest step, not with a few sampled columns.
49. **An Editor left in Play Mode blocks the test runner** (`This cannot be used during play mode`) and `check.sh` waits for ten minutes. Stop Play Mode (`unity cmd editor.playmode stop`) before a test run.
50. **Blender writes vertex colours into the `.glb` only with `export_vertex_color="ACTIVE"`.** They are normalised unsigned shorts, and glTFast reads them into `Mesh.colors`. Use a `FLOAT_COLOR` point attribute so the values are linear and untouched.
