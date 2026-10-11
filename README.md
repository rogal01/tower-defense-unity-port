# Tower Defense Unity Port

Unity/C# implementation of my Tower Defense case study.

This version explores a code-first Unity workflow: most of the game world, UI, campaign data, and runtime behavior is created or controlled from C# scripts rather than relying only on editor setup.

## Portfolio Role

This repository is part of the larger **Tower Defense Case Study**:

- Android/Kotlin version: native rendering and original gameplay reference.
- Godot/GDScript version: engine workflow, scenes, signals, UI/audio iteration.
- Unity/C# version: code-first engine port and C# systems.

## Features

- Runtime setup for gameplay and UI.
- Shared tower defense systems: towers, enemies, waves, campaign data, economy, and win/loss flow.
- C# scripts for gameplay controller, runtime actors, visuals, data, and art/library tooling.
- Unity editor workflow for reviewing the scene and testing gameplay.

## Key Files

- `Assets/TowerDefensePort/Scripts/TDGameController.cs`
- `Assets/TowerDefensePort/Scripts/TDRuntimeActors.cs`
- `Assets/TowerDefensePort/Scripts/TDData.cs`
- `Assets/TowerDefensePort/Scripts/TDVisuals.cs`
- `Assets/TowerDefensePort/Scripts/TDGameBootstrap.cs`

## Useme Screenshot Plan

1. Unity editor with scene/game view.
2. Runtime gameplay.
3. `TDGameController` or related C# script.
4. Asset/art library view if useful.

## Run

1. Open the folder in Unity Hub.
2. Use Unity 6 or a compatible version.
3. Open the main scene and press Play.

## Before Publishing

- Keep generated Unity folders out of Git: `Library`, `Temp`, `Logs`, `Obj`, `Build`, `Builds`, `UserSettings`.
- Commit only project settings, assets, scripts, scenes, and documentation.
- Add screenshots to the main Tower Defense case study.
