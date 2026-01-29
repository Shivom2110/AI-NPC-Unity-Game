# Architecture Overview (Local Learning)

## Core Principles
- No cloud services
- No ML training
- Deterministic, local statistical learning

## Event Flow
Player Action
→ EventBus
→ MarkovPredictor
→ Boss / NPC Decision
→ Combat / Dialogue Output

## Systems
- Systems/Events: PlayerEvent, EventBus
- Systems/Learning: MarkovPredictor
- Systems/Persistence: LocalSaveService
- Systems/Dialogue: Rule + template dialogue (stubbed)
- NPC: Local memory + relationship tracking

## Persistence
- Saved locally via JSON
- Path: Application.persistentDataPath/saves/

## Status
- Architecture locked
- Gameplay, UI, and visuals intentionally deferred
