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
