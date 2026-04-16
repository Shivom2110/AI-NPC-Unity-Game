# AI-NPC-Unity-Game

This repository contains a Unity 6 third-person prototype focused on adaptive gameplay. The current project combines memory-driven NPC interactions, boss combat that reacts to player behavior, and locally persisted difficulty tuning. Everything runs locally in C# without cloud services or external AI APIs.

## What The Project Currently Includes

- A main gameplay scene at `Assets/Scenes/HubArea.unity`
- Relationship-based NPC dialogue using in-memory interaction history
- Adaptive boss combat with combo reading, parry and dodge windows, and phase pressure
- Player combat with light/heavy attacks, parry, flashy attack, ultimate, and roll
- Runtime combat analytics that adjust difficulty based on player performance
- Local persistence for the player's adaptive combat profile
- Runtime-generated HUD elements for player and boss combat feedback

## Main Tech Stack

- Unity Editor `6000.3.10f1`
- Universal Render Pipeline
- Unity Input System
- Unity AI Navigation
- C# gameplay scripts

## Repo Structure

```text
AI-NPC-Unity-Game/
├── Assets/
│   ├── Animations/              # Combat and character animation assets
│   ├── Audio/                   # Audio content
│   ├── BrokenVector/            # Environment and imported dungeon assets
│   ├── Materials/               # Materials and rendering assets
│   ├── Models/                  # Character, weapon, and environment models
│   ├── Prefabs/                 # Player, camera, and gameplay prefabs
│   ├── Rallba/                  # Imported boss/character asset pack
│   ├── Resources/               # Runtime-loadable configs
│   ├── Scenes/                  # Main Unity scenes
│   ├── Scripts/
│   │   ├── Core/                # Initializers and memory manager
│   │   ├── NPC/                 # NPC dialogue and boss entry logic
│   │   ├── Player/              # Movement, combat, health, weapons
│   │   ├── Systems/Combat/      # Adaptive combat, analytics, difficulty
│   │   ├── Systems/Dialogue/    # Dialogue data and rule helpers
│   │   ├── Systems/Events/      # Event bus and combat event types
│   │   ├── Systems/Learning/    # Combo tracking and counter logic
│   │   ├── Systems/Persistence/ # Local save helpers
│   │   └── UI/                  # Dialogue, HUD, and death screen UI
│   ├── Settings/                # URP and rendering settings
│   └── Textures/                # Texture assets
├── Packages/                    # Unity package manifest and lockfile
├── ProjectSettings/             # Unity project settings
├── Library/                     # Unity-generated local cache
├── Logs/                        # Unity-generated logs
└── UserSettings/                # Unity editor user settings
```

## Key Gameplay Systems

### NPC memory and dialogue

- `Assets/Scripts/Core/GameInitializer.cs` keeps shared systems alive across scenes
- `Assets/Scripts/Core/NPCMemoryManager.cs` stores relationship score and interaction history per NPC
- `Assets/Scripts/NPC/NPCController.cs` changes responses based on personality, relationship state, and recent interactions
- `Assets/Scripts/UI/DialogueUIController.cs` displays prompts and dialogue lines

### Adaptive combat

- `Assets/Scripts/Systems/Combat/CombatSystemBootstrap.cs` auto-creates core combat systems at runtime
- `Assets/Scripts/NPC/BossAIController.cs` handles boss movement, counters, attack windows, and adaptive scaling
- `Assets/Scripts/Systems/Learning/ComboTracker.cs` tracks current combos and estimates player skill from combat timing
- `Assets/Scripts/Systems/Combat/CombatTracker.cs` records combat performance for live analytics
- `Assets/Scripts/Systems/Combat/FightProgressionManager.cs` adjusts difficulty and saves the player's evolving profile
- `Assets/Scripts/Systems/Combat/PlayerSkillProfile.cs` persists adaptive combat stats between sessions

### Player systems

- `Assets/Scripts/Player/PlayerMovement.cs` handles exploration and combat movement
- `Assets/Scripts/Player/SwordManager.cs` toggles drawn vs. sheathed weapons
- `Assets/Scripts/Player/PlayerCombatController.cs` handles attacks, parry, roll, and boss targeting
- `Assets/Scripts/Player/PlayerHealth.cs` manages HP, damage, death, and temporary invulnerability
- `Assets/Scripts/UI/PlayerHUD.cs` builds the player and boss HUD automatically at runtime

## Setup Instructions

### Requirements

- Unity Hub
- Unity Editor `6000.3.10f1`
- Git
- A code editor such as VS Code, Rider, or Visual Studio

### Clone and open

```bash
git clone https://github.com/Shivom2110/AI-NPC-Unity-Game.git
cd AI-NPC-Unity-Game
```

1. Open Unity Hub.
2. Add this repository as a project.
3. Use Unity Editor `6000.3.10f1`.
4. Let Unity finish importing packages and compiling scripts.
5. Open `Assets/Scenes/HubArea.unity`.
6. Press Play.

## User Guide

### Basic controls

- `W`, `A`, `S`, `D`: Move
- `Left Shift`: Run
- `Space`: Jump while weapons are sheathed
- `F`: Draw or sheathe weapons

### Combat controls

These actions are available when weapons are drawn.

- `Left Mouse`: Light attack
- `Right Mouse`: Heavy attack
- `Q`: Parry
- `E`: Flashy attack
- `R`: Ultimate
- Double tap `Space`: Roll

### Interaction controls

- `E`: Interact with nearby NPCs when you are not using it for combat

### Play flow

1. Open `HubArea`.
2. Move with `WASD` and approach the playable area.
3. Draw your weapons with `F` before testing combat.
4. Use parries, dodges, and combo variation to influence adaptive difficulty.
5. Approach NPCs and press `E` to cycle through dialogue interactions.

## Working In New Scenes

When creating or wiring a new scene, make sure it includes:

- A player object tagged `Player`
- `PlayerMovement`, `PlayerCombatController`, `SwordManager`, and `PlayerHealth` on the player setup
- NPCs with `NPCController` for dialogue testing
- Bosses with `BossAIController` for adaptive combat testing
- A trigger using `BossEntranceTrigger` if you want a staged boss entrance

Combat support systems are auto-bootstrapped at runtime, and `GameInitializer` ensures shared memory and combo tracking stay available across scene loads.

## Local Data And Persistence

- The adaptive combat profile is saved locally through `PlayerSkillProfile`
- Save data is written under Unity's `Application.persistentDataPath`
- `LocalSaveService` also supports JSON-based local saves under a `saves` folder inside that persistent data path

## Important Notes

- This project is currently centered around the authored gameplay code and the `HubArea` scene
- Imported art and animation packs remain under `Assets/BrokenVector`, `Assets/Kevin Iglesias`, and `Assets/Rallba`
- `Library`, `Logs`, and `UserSettings` are local Unity-generated folders rather than core gameplay source
- This `README.md` is the single maintained documentation file for the repository
