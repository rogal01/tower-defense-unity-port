# Tower Defense Unity Port

**The TD Ecosystem:** [Kotlin/Android (Flagship)](https://github.com/rogal01/tower-defense-android) | [Godot (Fast Iteration)](https://github.com/rogal01/tower-defense-godot) | [Unity (Code-First)](https://github.com/rogal01/tower-defense-unity-port)

A code-first Unity rebuild of my tower-defense project, focused on runtime bootstrap, maintainable gameplay code, and engine-port experimentation.

Instead of relying entirely on a scene-heavy workflow, this version emphasizes code-driven setup for menus, gameplay bootstrap, and world initialization. That makes it a useful portfolio piece for showing how I approach Unity from a systems and architecture perspective rather than only from an editor workflow.

> Portfolio note: add a short gameplay GIF or screenshot strip near the top before making the repo public.

## Why This Matters To Clients

- It shows that I can build in Unity with an engineering-first mindset rather than relying only on editor assembly.
- It demonstrates maintainable runtime structure for projects that will keep growing.
- It proves I can port the same product space into a different engine without copying the same architecture blindly.
- It translates well to Unity projects that need stronger code structure, clearer bootstrap flows, or easier long-term maintenance.

## Quick Ecosystem Comparison

| Repo | Primary Strength | Best Use Case |
| --- | --- | --- |
| Kotlin/Android | Cross-platform shared architecture | Native-feeling mobile game or app logic across platforms |
| Godot | Fast iteration and UX polish | Prototypes, gameplay validation, onboarding, HUD work |
| Unity | Code-first runtime structure | Larger engine-based products that need maintainable architecture |

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
- maintainable separation between startup flow and gameplay systems

## What The Port Covers

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

### Open The Project

1. Open Unity Hub.
2. Install Unity 6.3 LTS.
3. Add this repository folder as a project.
4. Open it in Unity 6.3 LTS.
5. Press Play.

Because the project uses runtime bootstrap code, the setup is intentionally less scene-dependent than a typical Unity project.

## Engineering Takeaways

- code-first Unity project organization
- runtime bootstrap instead of purely manual scene composition
- architecture-minded porting of an existing game concept
- adaptation of the same gameplay space to a different engine and toolchain

## Best-Fit Client Work

This repo is a strong fit for clients who need:

- Unity gameplay systems that can keep growing without becoming scene spaghetti
- a developer who is comfortable structuring startup and runtime logic in code
- ports, rebuilds, or refactors of existing game concepts into Unity
- a more maintainable engineering layer behind a Unity project

## What I Would Improve Next

- add visual proof near the top showing the runtime result, not just the architecture story
- clarify the long-term role of `My project/` so the repo feels cleaner to first-time visitors
- add a short architecture callout showing how bootstrap and gameplay systems are separated

## Part Of The Multi-Engine Ecosystem

This repository complements:

- the **Kotlin/Android flagship**, which emphasizes cross-platform native gameplay architecture
- the **Godot version**, which emphasizes iteration speed, UI flow, and gameplay feel

Together, the three versions show that I can move between toolchains quickly, choose the right architectural tradeoffs for the stack, and keep the same product space understandable across very different environments.
