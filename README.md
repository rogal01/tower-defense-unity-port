# Unity Port

This folder contains a Unity rebuild of the tower-defense game as a code-first project.
The UI, menu, world, and gameplay bootstrap at runtime, so you do not need to recreate scenes by hand.

## Included Variants

- The repository root contains the main code-first Unity port.
- `My project/` is a preserved editor-created Unity project snapshot kept for reference assets and alternate setup work.

## What To Install

Install these from Unity's official tooling:

1. Unity Hub
2. Unity 6.3 LTS
3. Visual Studio Editor support module
4. Windows Build Support if you want Windows builds
5. Android Build Support if you want mobile export

Official references:

- Unity download: https://unity.com/download
- Unity Hub install docs: https://docs.unity.com/hub/install-hub
- Add an Editor in Unity Hub: https://docs.unity.com/hub/add-editor
- Unity 6 release overview: https://unity.com/releases/release-overview

## How To Open

1. Install Unity Hub.
2. Install Unity 6.3 LTS through the Hub.
3. In Unity Hub, choose `Add` and point it at this `unity-port` folder.
4. Open the project in Unity 6.3 LTS.
5. Press Play.

The project uses runtime bootstrap code, so any default scene will work.

## Project Notes

- The port is a polished gameplay foundation, not a byte-for-byte engine translation.
- It keeps the same tower-defense structure: menu, map selection, campaign/endless feel, tower placement, waves, powers, upgrades, and a stronger visual presentation.
- The Unity rebuild is intentionally text-first so Codex can iterate on it cleanly.
