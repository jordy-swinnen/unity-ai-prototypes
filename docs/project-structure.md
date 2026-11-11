# Project structure

How the Unity project is organised so that it stays easy to work in as prototypes are added, by people and by coding agents. The short rules are in `AGENTS.md` ("Project structure"). This page gives the reasons, and says what we did not adopt. `Assets/Shared/Tests/EditMode/ProjectStructureTests.cs` enforces the rules that a test can check.

Source: Unity's guide [Organizing your project](https://unity.com/how-to/organizing-your-project) `[read]`. The guide covers folder structure and naming, empty folders, `.meta` files, splitting scenes and prefabs, Presets and code standards. It does not cover assembly definitions, `Resources`, `Plugins` or `StreamingAssets`, so nothing below claims it does. The two folder-tree examples and the naming table in the guide are images, which I could not read.

## What the guide says, and what we do

| Guide's tip | What we do | Enforced by |
|---|---|---|
| Document naming conventions and folder structure | `AGENTS.md` (short) and this page (reasons) | review |
| Be consistent with naming | PascalCase everywhere; materials are `<Name><Thing>` | test |
| No spaces in file and folder names (command line tools trip on them) | Renamed the 20 Medieval Chess materials from `MedievalChess Tile Light` to `MedievalChessTileLight`, and the 8 Neon Pong materials the same way. | test |
| Separate sandbox areas | `Assets/Sandbox/<user>/`, ignored by git, created when needed | `.gitignore` |
| Avoid extra folders at the root of `Assets` | Only `Prototypes`, `Shared`, `ThirdParty`, `Sandbox` and `Settings` | test |
| Keep internal assets apart from third-party ones | Anything we did not make goes in `Assets/ThirdParty/<Vendor>/` | test (root rule), review |
| Split assets by type in subfolders | Per prototype: `Scripts`, `Editor`, `Tests`, `Art/Exports`, `Art/Textures`, `Art/Sprites`, `Audio`, `Materials`, `Shaders`, `Scenes`, `Settings` | test (art, textures and sprites, shaders) |
| Define the structure early: moving assets looks like delete plus add in version control | Moves and renames go through Unity (`AssetDatabase.MoveAsset` or `RenameAsset`), so `.meta` and GUIDs follow. Git shows them as renames. | rule in `AGENTS.md` |
| Avoid empty folders | None exist | test |
| Commit `.meta` files | Always, with their assets | test |
| Split big scenes, load additively; use prefabs and nested prefabs | Scenes are built by code, one per game. More levels get one scene each, loaded additively. | rule |
| Use namespaces, match folders | `Prototypes.<Name>`, `Shared` | test |
| Consistent code standards and templates | One conventional style, enforced by review and the build. We did not add Unity script templates: the agents write the headers. | not enforced |
| Presets | Not used. Tuning values live in settings assets (ScriptableObjects), which serve the same purpose. | n/a |
| Create the same folder structure for every project with an Editor script | Each prototype's folders come from `AGENTS.md` and the `game-plan` skill, and a test checks them. A script was not worth it. | test |

## Our own rules that the guide does not cover
- **Assemblies:** one per folder part, named like the folder (`<Name>`, `<Name>.Editor`, `<Name>.Tests.EditMode`, `<Name>.Tests.PlayMode`). Prototypes never reference each other, and `Shared` never references a prototype. Move code into `Assets/Shared` only when a second prototype needs it.
- **Generated files:** scenes, materials, models, textures and sounds come from scripts and editor commands, never by hand, so they can be rebuilt and reviewed as code.
- **Large files:** `.png`, `.glb`, `.wav`, `.blend`, `.aseprite` and similar are in Git LFS (`.gitattributes`).
- **Pixel art:** sprites come from Aseprite Lua scripts (`Tools/aseprite/`) and land in `Art/Sprites/`, apart from generated textures in `Art/Textures/`, because they need different import settings: a Sprite, Point filter, no compression, no mipmaps. A path-based `AssetPostprocessor` sets them. It lives in `Assets/Shared/Editor/` as its own editor-only assembly `Shared.Editor`, not in `Shared`, because `Shared` also builds into players and editor code would break that build. It is in Shared, not in one prototype, because the rule is by path and applies to all of them.

## Known leftovers
- **Visible Meta Files:** a Unity project setting (`ProjectSettings/`). We did not change it. `.meta` files exist on disk and are committed for every asset, which `ProjectStructureTests` checks, so the setting does not need to change.
