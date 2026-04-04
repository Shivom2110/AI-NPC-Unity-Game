# 🎮 AI-Powered NPC PC Game

A Unity-based PC game featuring **adaptive NPC dialogue** and **real-time boss AI** that learns from player behavior.

This project focuses on **low-latency, local AI systems** without using cloud APIs.

---

# 📌 Project Overview

This game demonstrates:
- Adaptive NPC conversations based on memory
- Bosses that analyze player attack patterns
- Real-time combo detection and counter strategies
- Fully local architecture (no Firebase / OpenAI)

---

# 🧠 Core Features

## 1. NPC System (Adaptive Dialogue)
- NPC remembers player interactions
- Dialogue changes based on:
  - relationship score
  - previous actions
  - personality
- Uses variation + memory callbacks to feel human-like

---

## 2. Boss AI System (Adaptive Combat)

### Boss Difficulty Scaling

| Boss | Health | Combo Read Depth |
|------|--------|------------------|
| Boss 1 | 2000 | 2 |
| Boss 2 | 3000 | 3 |
| Boss 3 | 4000 | 4 |

### Behavior
- Tracks player attack history
- Detects combos using timing
- Selects counter strategies dynamically

---

## 3. Player Combat System

| Attack | Damage | Cooldown |
|--------|--------|----------|
| Auto Attack | 10 | 0 sec |
| Attack 2 | 50 | 3 sec |
| Attack 3 | 100 | 5 sec |
| Attack 4 | 150 | 7 sec |
| Ultimate | 300 | 10 sec |

---

## 4. Combo Detection System

- Combos are based on time gap between attacks
- If attacks occur within threshold → same combo
- If delay is large → combo resets

Example:
Fast attacks → combo  
Delayed attack → new combo  

---

## 5. Counter System

Boss selects counters based on:
- last N attacks in combo
- predefined mapping

Example:
- Attack2 → Dodge  
- Attack3 → HeavyCounter  
- Ultimate → SpecialCounter  

---

# 🏗️ Project Structure

Assets/
│
├── Scripts/
│   ├── Core/
│   │   ├── GameInitializer.cs
│   │   └── NPCMemoryManager.cs
│   │
│   ├── NPC/
│   │   ├── NPCController.cs
│   │   ├── BossAIController.cs
│   │   └── NPCMemory.cs
│   │
│   ├── Player/
│   │   ├── PlayerCombatController.cs
│   │   └── PlayerInteractionManager.cs
│   │
│   ├── Systems/
│   │   └── Learning/
│   │       ├── PlayerAttackType.cs
│   │       ├── AttackEvent.cs
│   │       ├── ComboTracker.cs
│   │       └── BossCounterLibrary.cs
│   │
│   └── UI/
│       ├── DialogueUIController.cs
│       └── BossDebugUIController.cs
│
├── Scenes/
│   ├── HubArea.unity
│   ├── Boss1Arena.unity
│   ├── Boss2Arena.unity
│   └── Boss3Arena.unity

---

# ⚙️ Setup Instructions

## Requirements

### Windows
- Unity Hub
- Unity 2021.3 LTS
- Visual Studio
- Git

### Mac
- Unity Hub
- Unity 2021.3 LTS
- VS Code
- .NET SDK
- Git

---

## Clone Project

git clone https://github.com/Shivom2110/AI-NPC-Unity-Game.git  
cd AI-NPC-Unity-Game  

---

## Open in Unity

1. Open Unity Hub  
2. Add project folder  
3. Open project  
4. Wait for compilation  

---

# 🧩 Scene Setup (IMPORTANT)

## Systems Object
Create empty GameObject → Systems  

Add:
- GameInitializer  

---

## Player Setup
- Capsule (or model)
- Add:
  - PlayerCombatController
  - PlayerInteractionManager
- Tag = Player  

---

## NPC Setup
Add NPCController  

Values:
npcId = merchant_01  
personality = merchant  
isBoss = false  

---

## Boss Setup
Add BossAIController  

Example (Boss 1):
health = 2000  
comboReadDepth = 2  

---

# 🎮 Controls

| Key | Action |
|-----|-------|
| Left Click | Auto Attack |
| Right Click | Attack 2 |
| Q | Attack 3 |
| R | Attack 4 |
| F | Ultimate |
| E | Interact |

---

# 🧪 How to Run

1. Open HubArea  
2. Press Play  
3. Interact with NPC  
4. Attack boss  
5. Observe combo + counter system  

---

# ⚡ Design Decisions

## Why no OpenAI / Firebase?
- avoids latency  
- ensures real-time gameplay  
- simplifies debugging  
- keeps system reliable  

---

## AI Approach Used
- rule-based adaptive system  
- memory-driven dialogue  
- combo pattern recognition  

---

# 📊 Current Status

## Completed
- NPC memory system  
- adaptive dialogue  
- player combat system  
- combo tracking  
- boss AI system  

## Remaining
- scene setup  
- UI polish  
- testing  

---

# 👥 Team

- Shivom → System architecture, memory, integration  
- Ayush → AI logic, combat balancing  
- Shaan → UI, scene design  

---

# 🔐 Security Notes

Do NOT commit:
- API keys  
- credentials  
- private configs  

---

# 🚀 Summary

This project demonstrates:
- real-time adaptive gameplay  
- AI-like NPC interaction without APIs  
- scalable system design in Unity  

Built for performance, adaptability, and simplicity.
