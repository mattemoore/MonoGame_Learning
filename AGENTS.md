# MonoGame Learning Project

## Project Overview

This is a **C# MonoGame** project designed for learning game development concepts, with the primary goal of building a side-scrolling beat 'em up game in the vein of 90s classics like *Streets of Rage*, *Final Fight*, and *Double Dragon*. As part of this effort, we are developing a generic, reusable core engine library (`MonoGameLearning.Core`) that will be useful for bootstrapping future game projects.

The project targets **.NET 10.0** and utilizes **MonoGame.Framework.DesktopGL** for cross-platform desktop support, relying heavily on **MonoGame.Extended** for utilities like cameras, sprites, and input handling.

The solution is structured into two main projects:

1. **`MonoGameLearning.Core`**: A library containing reusable engine-level components, base classes, and utilities.
2. **`MonoGameLearning.Game`**: The main executable project containing specific game logic, assets, and the game loop.

## Architecture

* **Core Library (`MonoGameLearning.Core`)**
  * **`GameCore`**: A base class (inheriting from `Microsoft.Xna.Framework.Game`) that handles boilerplate setup: `GraphicsDeviceManager`, `SpriteBatch`, `ContentManager`, and an `OrthographicCamera` with a `BoxingViewportAdapter` for resolution independence.
  * **`Input`**: Contains `InputManager` to abstract raw input into game actions (e.g., `Action1Pressed`).
  * **`Entities`**: Defines base entity classes.
  * **`Drawing`**: Basic shape drawing utilities.

* **Game Project (`MonoGameLearning.Game`)**
  * **`GameLoop`**: Inherits from `GameCore`. Implements the specific game logic (`Update`, `Draw`), manages entities (like the player), and handles the main application lifecycle.
  * **`Program.cs`**: The entry point, using C# top-level statements to bootstrap `GameLoop`.
  * **`Entities`**: Game-specific entities, such as `PlayerEntity`.
  * **`Sprites`**: Sprite management and animation logic (e.g., `PlayerSprite`).
  * **`Content`**: Contains game assets (images, fonts, etc.) processed by the MonoGame Content Pipeline (`.mgcb`).

## Key Technologies

* **Language**: C# (NET 10.0)
* **Framework**: MonoGame (DesktopGL 3.8.*)
* **Extensions**: MonoGame.Extended (5.3.1), Stateless (5.20.0)

## Building and Running

### Prerequisites

* .NET 10.0 SDK
* MonoGame Content Builder (if modifying assets)

### Commands

**Run the Game:**

```bash
dotnet run --project MonoGameLearning.Game/MonoGameLearning.Game.csproj
```

**Build the Solution:**

```bash
dotnet build
```

**Run Tests:**

```bash
dotnet test
```

## Development Conventions

* **Separation of Concerns**: Keep generic, reusable logic in `Core` and specific game implementation in `Game`.
* **State Management**: The project uses the `Stateless` library for managing entity states.
* **Input**: Input is decoupled from logic via `InputManager` events.
* **Resolution**: The game uses a virtual resolution (`GAME_WIDTH`, `GAME_HEIGHT`) scaled to the window size using `BoxingViewportAdapter`.
* **Conciseness**: Responses and suggestions should include code that is as concise and terse as possible.
* **Modern C#**: Always use the latest C# features (e.g., primary constructors, collection expressions, raw string literals) to ensure the codebase remains modern and idiomatic.
* **Solution Simplification**: Before proposing a solution, ALWAYS include a consideration step to see if the proposed architecture or implementation can be further simplified, refactored, or streamlined.
* **Build Verification**: Always run `dotnet build` to ensure the project compiles successfully after any code modifications.
* **Testing**: Always run `dotnet test` to execute unit tests after making any changes to verify no regressions were introduced.
* **Preventing Game-Breaking Bugs (Test Suggestion)**: Always proactively suggest or write new unit/integration tests when modifying logic to prevent game-breaking bugs. Focus tests on critical gameplay failure modes such as:
  * **Out-of-Bounds**: Characters or entities slipping outside of screen, level, or walkable boundaries.
  * **Connectivity & Seams**: Disconnected backgrounds or levels that trap players or break scrolling.
  * **State Machine Deadlocks**: Entities getting stuck in non-interruptible states (e.g., infinite attacking or falling) without recovery.
  * **Collision Failures**: Entities passing through solid boundaries or failing to register collision responses.
  * **Camera Tracking**: The camera tracking system losing track of player coordinates or clamping to incorrect screen areas.
