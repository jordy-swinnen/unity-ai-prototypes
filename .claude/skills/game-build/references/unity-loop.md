# Unity loop: commands

All of these were run in this repository with Unity 6000.6.8f1 and Unity CLI 1.0.0-beta.13. The Editor is open on the project. The CLI binary is `unity`.

## Scripts in this repository
| Command | What it does |
|---|---|
| `Tools/unity/check.sh compile` | Refresh assets, wait for the Editor, recompile scripts. Exit 0 means no compile errors. |
| `Tools/unity/check.sh editmode` | Compile, then run EditMode tests and print `tests: passed/total`. |
| `Tools/unity/check.sh playmode` | Compile, then run PlayMode tests (started in the background and polled). |
| `Tools/unity/check.sh all` | Compile, then both test modes. |
| `Tools/unity/capture.sh camera <out.png> [w h]` | Render the main camera. No post-processing (no Bloom or Vignette). |
| `Tools/unity/capture.sh screen <out.png>` | Capture the Game window, including UI and post-processing. Use this to judge a look. |
| `Tools/unity/capture.sh play <out.png> [seconds]` | Enter Play Mode, wait, capture, leave Play Mode. |

Write captures outside the repository, for example into a temp folder, then open the image to look at it.

## Commands behind them
- Status: `unity status`. The state must be `ready`.
- Refresh after writing files: `unity cmd script eval --code 'UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport); return "ok";' --result-only`
- Compile: `unity recompile --project-path .` (exit 0 and no output means it compiled).
- EditMode tests: `unity cmd test run --mode EditMode --result-only` prints JSON with `Summary` and `Results`.
- PlayMode tests: `unity cmd test run --mode PlayMode --async-tests --result-only`, then poll `unity cmd test status --result-only` until `status` is `completed`. Without `--async-tests` it returns zero results.
- List tests: `unity cmd test list --mode all --result-only`.
- Run a menu item: `unity cmd editor invoke_menu --path "Prototypes/<Name>/Build Scene"`.
- Run a small piece of C# in the Editor: `unity cmd script eval --code '...' --result-only`.
- Enter or leave Play Mode: `unity cmd editor.playmode start` and `unity cmd editor.playmode stop`.
- Find commands: `unity command --query <word> --detail compact`, and `--detail full --format json` for parameters.
- Add a package (needs approval): `unity cmd package add --name <id>@<version> --dry-run` to preview, then `--confirm --wait`. It fails if Unity Hub is not signed in.
- Search packages: `unity cmd package search <id>`.

## With the Editor closed
`unity test . --mode EditMode --output Logs/test-results.xml` starts its own batch-mode Editor. With the Editor open on the project it refuses and exits 6. This form has not been run in this repository.
