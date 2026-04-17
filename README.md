# Tower Defense Unity Port

A code-first Unity rebuild of my tower-defense project, focused on runtime bootstrap, maintainable gameplay code, and engine-port experimentation.

Instead of relying on a scene-heavy workflow, this version emphasizes code-driven setup for menus, gameplay bootstrap, and world initialization. That makes it a useful portfolio piece for showing how I approach Unity from a systems and architecture perspective rather than only from an editor workflow.

## Why This Project Is Interesting

- It ports the same game space into **Unity** while keeping a strong engineering focus.
- It uses a **code-first** setup rather than depending entirely on manually assembled scenes.
- It shows how I adapt the same product idea across different engines and frameworks.

## Included Variants

- The repository root contains the main code-first Unity port.
- `My project/` contains a preserved editor-created Unity project snapshot used for reference assets and alternate setup work.

## Technical Focus

- runtime-driven gameplay bootstrap
- text-first, reviewable project structure
- tower-defense systems adapted for Unity workflows
- cleaner iteration for code generation and automated assistance

## What the Port Covers

- menu and game startup flow
- map and gameplay bootstrap
- tower placement and waves
- powers, upgrades, and progression hooks
- a stronger visual and structural foundation for future polish

## Getting Started

### Requirements

Install these with Unity's official tools:

1. Unity Hub
2. Unity 6.3 LTS
3. Visual Studio Editor support
4. Windows Build Support if you want desktop builds
5. Android Build Support if you want mobile export

### Open the Project

1. Open Unity Hub.
2. Install Unity 6.3 LTS.
3. Add this repository folder as a project.
4. Open it in Unity 6.3 LTS.
5. Press Play.

Because the project uses runtime bootstrap code, the setup is intentionally less scene-dependent than a typical Unity project.

## Portfolio Value

This repo demonstrates:

- Unity development beyond editor-only workflows
- code-first project organization
- engine adaptation of an existing game concept
- willingness to explore different technical tradeoffs for the same product
