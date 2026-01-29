# 🎮 AI-NPC Unity Game

A single-player Unity RPG prototype focused on **adaptive NPCs and bosses** that learn player behavior **locally**, without cloud AI or external services.

✔ Runs fully offline  
✔ Cross-platform (Windows & macOS)  
✔ Deterministic, debuggable systems  

---

## 🎯 Project Goal

Build a game where:
- NPCs remember player interactions
- Relationships evolve over time
- Bosses adapt to player combat patterns
- All learning happens **locally in-memory**

No OpenAI. No Firebase. No paid APIs.

---

## 🧠 Core Systems (Current)

### NPC Memory (Local)
- Each NPC has an `NPCMemory`
- Tracks:
  - Relationship score
  - Interaction history
  - Learned patterns
- Stored locally via `NPCMemoryManager`

### Dialogue (Rule-Based)
- Dialogue generated locally
- Influenced by:
  - NPC personality
  - Relationship level
  - Player actions
- Deterministic and easy to extend

### Boss Adaptive AI
- Boss tracks player combat actions
- Learns frequency patterns (attack, dodge, block)
- Predicts next move using simple statistics
- Chooses counter-actions dynamically

---

## 🗂️ Key Folder Structure

Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameInitializer.cs
│   │   └── NPCMemoryManager.cs
│   ├── NPC/
│   │   ├── NPCController.cs
│   │   ├── BossAIController.cs
│   │   └── NPCMemory.cs
│   ├── Player/
│   │   ├── PlayerInteractionManager.cs
│   │   └── PlayerCombatController.cs
│
├── Scenes/
│   └── HubArea.unity
│
└── Documentation/
└── README.md

---

## ⚙️ Setup (Windows & macOS)

### Requirements
- **Unity 2021.3 LTS**
- **Git**
- No external services required

### Clone Repository

**Windows (PowerShell / Git Bash)**
```powershell
git clone https://github.com/Shivom2110/AI-NPC-Unity-Game.git

macOS (Terminal)

git clone https://github.com/Shivom2110/AI-NPC-Unity-Game.git


⸻

Open in Unity
	1.	Open Unity Hub
	2.	Click Add Project
	3.	Select the cloned folder
	4.	Open Assets/Scenes/HubArea.unity

⸻

Scene Initialization (Required)

In HubArea:
	1.	Create empty GameObject → Systems
	2.	Add components:
	•	GameInitializer
	•	NPCMemoryManager

This must exist for NPCs and bosses to function.

⸻

🎮 Controls (Current)
	•	WASD — Move
	•	Mouse — Look
	•	E — Interact
	•	Left Click — Light attack
	•	Right Click — Heavy attack
	•	Space — Dodge
	•	Shift — Block

(Same on Windows & macOS)

⸻

👥 Team Roles
	•	Shivom — Architecture & Core Systems
	•	Shaan — UI / Scene / Visual Design
	•	Ayush — NPC & Boss Behavior Logic

⸻

🌿 Git Workflow (Important)
	•	❌ Do NOT commit directly to main
	•	✅ Create a feature branch
	•	✅ Push to your branch
	•	✅ Open a Pull Request

Example:

git checkout -b your-name/feature
git push -u origin your-name/feature


⸻

🚧 Current Status
	•	✅ Compile-stable
	•	✅ Local memory + learning
	•	✅ Boss AI functional
	•	🚧 UI polish pending
	•	🚧 Visual assets pending

⸻

🚀 Immediate Next Steps
	1.	Minimal dialogue UI
	2.	One NPC fully playable
	3.	One boss arena prototype
	4.	Balance learning speed
	5.	Visual + animation pass

⸻

🧠 Design Principles
	•	Local-first logic
	•	Simple > complex
	•	Predictable behavior
	•	Easy debugging
	•	Extend only when needed

⸻

Status: Stable foundation ✅
Platform: Windows & macOS
Branch: main
Last Updated: Jan 2026

---