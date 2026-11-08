# Appendix A: Research (as of 8 November 2026)

Confidence tags: [verified] means checked against a source during the research session. [unverified] means prior knowledge or my own inference, to check before relying on it.

## A1. Platform support

* [verified] Unity provides Linux Editor support only for Ubuntu. Supported versions are Ubuntu 22.04 and 24.04 on x64, with GNOME on X11.
* [verified] Wayland is supported only with AMD graphics on Ubuntu 22.04 and 24.04, and with Nvidia proprietary driver 550 or newer on 24.04.
* [verified] The Editor must run without emulation, a container, or a compatibility layer.
* [verified] Linux Editor limitations: video import is limited to VP8, and the file system is case-sensitive.
* [verified] macOS and Windows are supported platforms for the Editor.
* Consequence: the CachyOS machine is not a supported Editor host.

## A2. Unity licence

* [verified] Unity Personal is free while total revenue and funding over the last 12 months stay under $200,000.
* [verified] Above that, Unity Pro is required. Pro costs $2,310 per seat per year, prepaid, or $210 per month (2026 pricing page).
* [verified] When you build for a third party, the client's revenue and funding count toward the threshold. Personal can't be used for internal or prototyping work once the threshold is exceeded.
* [verified] With Unity 6, the Personal splash screen is optional.

## A3. Unity to agent bridge

* [verified] Unity's in-editor MCP server, shipped in the `com.unity.ai.assistant` package, is deprecated. Unity now points to the Unity CLI. The CLI includes its own MCP server (`unity mcp`), built on the Unity Pipeline package.
* [verified] The CLI is free, needs no Unity AI subscription, and works with an agent or with no agent at all.
* [verified] From Unity's migration table, third-party MCP packages installed from GitHub are not affected by the deprecation. This is my reading of the table, so check it.
* [verified] Install on macOS and Linux with `curl -fsSL https://unity.com/install.sh | bash`, or with Homebrew: `brew install --cask unity-cli`. Update with `unity self-update`.
* [verified] Commands mentioned in the docs: `unity install <version or lts>`, `unity editors -r | -i | -a`, `unity install-modules`, `unity pipeline install`, `unity status`, `unity command` (alias `cmd`), `unity list`, `unity doctor`, `unity skill`, `unity mcp`.
* [verified] The Pipeline package needs Unity 6.0 LTS or later to control a running Editor.
* [verified] Claude Code users are told to install the Unity plugin, which includes the `unity-cli` skill.
* [verified] The earlier Unity AI MCP open beta (announced 11 May 2026) required Unity 6, the AI Assistant package, a Cloud-connected project, and a Unity AI trial or subscription. It's superseded by the CLI.

## A4. Third-party bridge: AnkleBreaker (not used in this setup)

* [verified] Two parts: `unity-mcp-plugin` (a C# Unity package that exposes an HTTP bridge, port 7890 in its install instructions) and `unity-mcp-server` (Node, `npx anklebreaker-unity-mcp@latest`).
* [verified] Tool count: 268 in the README headline and 288 in other descriptions. Either way, the count is large.
* [verified] Its two-tier design loads core tools immediately and advanced tools on demand, to limit context use.
* [verified] Licence: AnkleBreaker Open License v1.0. It requires attribution (waived for personal and educational use) and prohibits reselling, sublicensing, and commercial distribution. It is not MIT.
* [verified] Its screenshot tool is Windows only.
* Decision: not included. Choose one bridge, and avoid a large tool list in every request.

## A5. Blender

* [verified] Blender 5.2 LTS was released 14 July 2026 and is supported until July 2028.
* [verified] Blender 5.1 was released 17 March 2026 and uses Python 3.13 under the VFX Reference Platform 2026.
* [unverified] A third-party article says Blender 5.3 is due in November 2026.
* [verified] Use an LTS release for a stable pipeline, and pin the version in `AGENTS.md`.
* [unverified] Headless run: `blender -b -P script.py`. This is standard Blender CLI usage, not run in this session.
* [verified] blender-mcp (ahujasid): a Blender addon (`addon.py`) opens a socket server, by default on port 9876. The MCP server runs as `uvx blender-mcp`. For Claude Code: `claude mcp add blender uvx blender-mcp`.
* [verified] Its `execute_blender_code` tool runs arbitrary Python. The action can't be undone, so save first. The addon must be running before the client connects.
* [unverified] Whether blender-mcp works with Blender 5.2.
* [verified] mcp-for-blender is a newer alternative: `uvx mcp-for-blender setup`, then `uvx mcp-for-blender install-addon`. Its server starts when Blender opens. Not compared in depth.

## A6. JetBrains Rider

* [verified] Rider has been free for non-commercial use since 24 October 2024. That covers learning, open source, hobby work, and content creation.
* [verified] The non-commercial licence lasts one year and renews automatically if the IDE was used in the previous six months.
* [verified] If you plan to release a product for commercial benefit, now or later, you need a commercial licence.
* [verified] Activation: Help > Manage Licenses > Non-commercial use.

## A7. Design reasoning (my analysis)

* Text serialization (Force Text) lets agents read and diff scenes and prefabs. Binary files can't be diffed. The menu setting is standard Unity practice, but I haven't checked it in this session.
* Build scenes in code through an editor menu command. An agent can't reproduce a hand-placed scene.
* Tests live in `Assets/`. Unity only compiles test assemblies inside `Assets/` or a package.
* Verification loop: compile check, then EditMode tests, then PlayMode tests, then a rendered image for visual checks.
* One bridge. Every MCP tool adds to the context of each request.
* Blender scripts first. They are reproducible, versioned, and need no live session. Live MCP control is optional.
* Git LFS for binaries, plain text for everything the agent edits.
* Pin versions for Unity and Blender, and record them in `AGENTS.md`.
* Model cost (reasoning, not tested): keep planning on the strongest model, and send narrow tasks to cheaper models if cost matters.
* One spec per mechanic, matching the SDD workflow.

## A8. Unverified items

* The exact menu location and `EditorSettings.asset` field names for Force Text and Visible Meta Files.
* Whether `unity pipeline install` changes the project's manifest.
* The agent names accepted by `unity skill install`.
* The `.mcp.json` syntax for `unity mcp`.
* The compile-check and test commands in the CLI (`unity --help`).
* Whether `git lfs install --local` is accepted by the installed git-lfs.
* Whether the Arch package is named `blender` and what version pacman provides.
* Whether `bpy.ops.export_scene.gltf` runs unchanged on Blender 5.2.
* How Unity Hub behaves when the target folder isn't empty.
* Whether the CLI and Pipeline work with a path that contains spaces.
* Which package Unity uses for glTF import (glTFast was my assumption).
* Blender 5.3's release date.
