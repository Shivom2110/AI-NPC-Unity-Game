#!/usr/bin/env bash
set -euo pipefail

mkdir -p Assets/Documentation

cat > Assets/Documentation/README.md <<'MD'
# AI-Powered NPC PC Game

A Unity-based PC game featuring one adaptive NPC and three adaptive bosses. The system is fully local, uses C#, and avoids cloud services for low-latency gameplay.

## Project Scope
- 1 interactive NPC with memory-based dialogue
- 3 bosses with increasing adaptive difficulty
- 5 player attacks with cooldowns and damage values
- Combo detection based on timing between attacks
- Boss counters determined in real time from recent combo patterns
- Local-only architecture for fast response and simpler debugging

## Core Gameplay Systems

### NPC System
The NPC remembers past player interactions and changes its tone based on relationship state and recent history. Dialogue is local, adaptive, and designed to feel human-like without using external AI APIs.

### Boss System
Bosses track the player’s recent attacks and identify combos based on time gaps between actions.

- Boss 1 reads the last 2 combo attacks
- Boss 2 reads the last 3 combo attacks
- Boss 3 reads the last 4 combo attacks

Each boss selects a predefined counter in real time.

### Player Combat System
The player has 5 attacks:

- Auto Attack: 10 damage, 0s cooldown
- Attack 2: 50 damage, 3s cooldown
- Attack 3: 100 damage, 5s cooldown
- Attack 4: 150 damage, 7s cooldown
- Ultimate: 300 damage, 10s cooldown

### Boss Health
- Boss 1: 2000 HP
- Boss 2: 3000 HP
- Boss 3: 4000 HP

### Combo Logic
A combo is determined by the time gap between consecutive attacks. If attacks happen within the combo threshold, they are grouped as one combo. If the delay is too large, the combo resets.

## Project Structure

### Important Script Folders
- `Assets/Scripts/Core` → initializers and memory manager
- `Assets/Scripts/NPC` → NPC controller, boss AI, NPC memory
- `Assets/Scripts/Player` → player combat and player interaction
- `Assets/Scripts/Systems/Learning` → attack types, combo tracking, counter logic
- `Assets/Scripts/UI` → dialogue and boss debug UI

### Important Scenes
- `HubArea` → player + NPC interaction
- `Boss1Arena` → Boss 1 fight
- `Boss2Arena` → Boss 2 fight
- `Boss3Arena` → Boss 3 fight

## Scene Setup Checklist

### In every playable scene
Create a `Systems` GameObject and add:
- `GameInitializer`

This will initialize:
- `NPCMemoryManager`
- `ComboTracker`

### Player setup
Player should have:
- `CharacterController`
- `PlayerCombatController`
- `PlayerInteractionManager`

Tag the player as:
- `Player`

### NPC setup
NPC should have:
- `NPCController`

Recommended values:
- `npcId = merchant_01`
- `personality = merchant`
- `isBoss = false`

### Boss setup
Boss should have:
- `BossAIController`

Recommended values:

#### Boss 1
- `bossName = Boss 1`
- `maxHealth = 2000`
- `comboReadDepth = 2`
- `baseCounterCooldown = 1.2`

#### Boss 2
- `bossName = Boss 2`
- `maxHealth = 3000`
- `comboReadDepth = 3`
- `baseCounterCooldown = 1.0`

#### Boss 3
- `bossName = Boss 3`
- `maxHealth = 4000`
- `comboReadDepth = 4`
- `baseCounterCooldown = 0.8`

## Controls
- Left Click → Auto Attack
- Right Click → Attack 2
- Q → Attack 3
- R → Attack 4
- F → Ultimate
- E → Interact with NPC

## Local Architecture
This project is fully local:
- no Firebase
- no OpenAI
- no AWS
- no online dependency

This keeps the system fast, simple, and suitable for real-time adaptive gameplay.

## Current Status
Completed:
- local NPC memory system
- adaptive NPC dialogue
- player combat system
- combo tracking
- adaptive boss counter logic
- modular script structure

Remaining:
- scene wiring
- object placement
- testing
- minor polish

## Team Roles
- Shivom → system integration, local memory, gameplay architecture
- Ayush → combat logic, balancing, adaptive behavior tuning
- Shaan → UI, scene setup, presentation layer

## How to Run
1. Open the project in Unity
2. Open `HubArea`
3. Make sure the required objects are in the scene
4. Press Play

## Notes
This project prioritizes functional gameplay systems over final art polish. The main innovation is adaptive behavior using lightweight local logic rather than cloud-based AI.
MD

cat > README.md <<'MD'
# AI-Powered NPC PC Game

See the main project documentation here:

`Assets/Documentation/README.md`

This Unity project contains:
- adaptive NPC dialogue with local memory
- 3 adaptive bosses with combo-based counters
- 5 player attacks with cooldown and damage logic
- local-only gameplay systems for low-latency response
MD

rm -f Assets/Documentation/ARCHITECTURE.md
rm -f Assets/Documentation/PROJECT_TIMELINE.md
rm -f Assets/Documentation/QUICK_REFERENCE.md
rm -f Assets/Documentation/SETUP_GUIDE.md

echo "Documentation refreshed."
echo "Kept:"
echo "  Assets/Documentation/README.md"
echo "  README.md"
echo "Deleted:"
echo "  Assets/Documentation/ARCHITECTURE.md"
echo "  Assets/Documentation/PROJECT_TIMELINE.md"
echo "  Assets/Documentation/QUICK_REFERENCE.md"
echo "  Assets/Documentation/SETUP_GUIDE.md"
