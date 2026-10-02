# evak

A browser FPS mechanics project: CoD-calibrated gunplay and movement, built natively in Unity, playable on the web.

> Original implementation of common FPS mechanics. No Activision assets, trademarks, or copyrighted material.

## v0.1 — Feel Range (in progress)

**Question v0.1 answers:** does it feel like a real FPS within 60 seconds of picking up the rifle?

**Branch:** `v0.1-feel-range` — sprint board: `evak-sprint-board-v0.1.md`.

One scene, one rifle, static humanoid target dummies with full player-identical hitboxes, one wall-pattern target at 25 m, movement timing gates. Scope is frozen: movement + deterministic recoil + bloom + hitmarker + WebGL deploy.

### Architecture non-negotiables
- `IInputProvider` / `PlayerInputProvider` — movement/weapon code never touches InputSystem directly.
- `WeaponData` ScriptableObject — the rifle is a data asset, not a subclass.
- `IDamageable` + `DamageResolver` — all hits route through one choke point.
- `DamageInfo` struct — no per-shot allocation on the fire path.
- Gameplay tick at 60 Hz fixed timestep.
- Deterministic recoil via seeded xorshift PRNG — never `UnityEngine.Random` in gameplay.

### How to open
Unity 2022.3.62f3. Open the folder, let Unity generate `Library/`, then follow `Assets/_Project/Scenes/TestArena.notes.md` to build the scene.

### Roadmap
v0.1 (feel range) → v0.2 (second weapon class, score-attack loop) → later (vertical slice).
