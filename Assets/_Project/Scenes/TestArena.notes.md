# TestArena — gray-box scene build notes (v0)

Scene: `Assets/_Project/Scenes/TestArena.unity` (build in-editor; not committable from here).

## Layout
- Ground: 60x60 plane, grid material, origin at (0,0,0).
- Ramps: 2 wedges (slide testing) — one gentle (15°), one steep (30°).
- Stairs: 1 staircase, 8 steps, 0.25m rise each.
- Low walls: 3 walls at 1.0m height (crouch testing; mantle-adjacent, full mantle is v0.1).
- Raised platforms: 2 platforms at 1.2m and 2.0m (jump testing).
- Scale reference: default capsule (1.8m) at spawn.

## Targets
- 4–6 cubes/capsules tagged `Target`, each with `TargetDummy` component.
- Place at varied ranges (10m / 25m / 50m) and elevations.

## Player prefab wiring
1. Capsule + `CharacterController` + `PlayerMovement` + `PlayerInputProvider` (with `PlayerInput` component + Input Actions asset: Move/Look/Jump/Sprint/Crouch/Fire/Reload/ADS).
2. Child camera pivot at 1.6m with `PlayerCamera`; Camera child of pivot.
3. Weapon child with `WeaponController`, muzzle socket transform; assign `WeaponData` instance (create via Create > Evak > Weapon Data).
4. Canvas with `HUDController` refs: crosshair RectTransform, ammo TMP_Text, hitmarker Image, reload TMP_Text.

## Exit criteria
- 60+ fps locked, no input lag, no camera roll/tilt artifacts.
- Walk/sprint/crouch/slide/jump all feel tight; slide has min duration + cooldown.
- Hip-fire + ADS hitscan on targets with crosshair, ammo, hitmarker, decals.
- Manual + empty-mag reload both work.
