# cod-mobile
optimize mobile server based first person shooter

## v0 — Movement & Gunplay Prototype (in progress)

**Question v0 answers:** does moving and shooting feel good?

**Branch:** `v0-foundation` — scaffold per `workspace/user/files/cod-mobile-clone-v0-plan.md`.

### What's in this commit
- Unity `.gitignore` + Git LFS `.gitattributes` (set up before any binaries land).
- Folder structure under `Assets/_Project/` (Core/Player/Weapons/UI/Data/Utils, Prefabs, Scenes, Data/Weapons, Art, Audio).
- `Packages/manifest.json` pinning Input System 1.7.0 + URP 17.0.3.
- Architecture non-negotiables, built from the first script:
  - `IInputProvider` / `PlayerInputProvider` — movement/weapon code never touches InputSystem directly (bots + net players later drive the same code).
  - `WeaponData` ScriptableObject — the one v0 rifle is a data asset, not a subclass.
  - `IDamageable` + `DamageResolver` — all hits route through one choke point.
  - Movement physics in `FixedUpdate`.
- Systems: `PlayerMovement` (idle/walk/sprint/crouch/slide/airborne, coyote time, slide cooldown), `PlayerCamera` (yaw-on-body/pitch-on-pivot, head bob, ADS FOV blend, recoil kick), `WeaponController` (hitscan from camera center, hip/ADS spread, semi/auto, tactical + empty reload, muzzle flash, impact decals), `TargetDummy`, `HUDController` (crosshair, ammo, hitmarker).
- `TestArena.notes.md` — gray-box scene build checklist (scene itself must be built in-editor).

### Non-goals (explicitly not in v0)
Weapon variety, attachments, perks, bots/AI, art pass, menus/progression, multiplayer code, mobile touch input (the `IInputProvider` abstraction keeps that door open).

### How to open
Unity 2022 LTS+ with URP template. Open the folder, let Unity generate `Library/`, then follow `Assets/_Project/Scenes/TestArena.notes.md` to build the scene.

### Roadmap
See `cod-mobile-clone-roadmap.md` (v0 → v1.0: gunplay framework → content → AI → modes).
