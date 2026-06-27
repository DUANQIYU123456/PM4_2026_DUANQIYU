# Backrooms Walk — Production Plan

Practical build list for the Lecture 7 example scene.

---

## 1. Scripts

### Reused from L5 / L6
- Room1EntryControllerV2 + Room1EntryProfile
- ColumnControllerV2 + ColumnProfile
- StillnessDimController + StillnessDimProfile
- CompactingCorridorController + CompactingCorridorProfile
- GazeDrainController + GazeDrainProfile
- ManequinController + ManequinProfile
- RoomInversion / TileInversion (optional, for Room 5 disorientation)

### New for L7
- **GazeSensor** — the new input primitive.
- **GazeGlowController + Profile** — first non-drain use of gaze (L6 catalog #7).
- **Pickup** — minimal grab interaction. On trigger + button press, sets a flag and hides the object.
- **WallpaperSwap** — swaps a material on a contact or timed event.
- **OscillatingLight** — or a BreathingLight profile tuned fast enough to oscillate.
- **LightManager** — owns ambient, reflection, shared ceiling emission.
- **PostProcessManager** — owns URP Volume overrides.
- **ExperienceMemory** — ScriptableObject, 2 booleans.

---

## 2. Art assets

### Environment
- 2–3 wallpaper materials: baseline, faded, subtly wrong
- 1–2 floor tile materials: baseline + emissive variant
- 1 stain decal or material (wall hotspots)
- Ceiling emission material
- 1 fog color palette (neutral → cold shift)

### Objects
- Column prefab *(existing)*
- Mannequin model *(existing)*
- One grabbable object — photograph, journal, small prop
- Door frame prefab

### UI
- Menu: black screen, "ENTER" text fades in, click/key to start
- Exit: fade to white, return to menu

---

## 3. Audio assets

### Ambient beds (one per room)
| Room | Character |
|---|---|
| 1 — Foyer | Neutral hum |
| 2 — Corridor | Corridor air, slight drone |
| 3 — Gallery | Quiet with subtle pitch wobble |
| 4 — Strange Corridor | Low drone *(lower pitch if `mannequinSeen`)* |
| 5 — Oscillating | Bed pulsing with the light |
| 6 — Exit | Warm closure tone |

### SFX
- Column proximity hum *(existing)*
- Stain glow tone
- Fog pulse whoosh
- Mannequin teleport thud
- Wallpaper swap *(near-inaudible)*
- Pickup tone
- Exit swell

Source from free ambient packs (freesound.org, zapsplat) or reuse from L4/L5 assets.

---

## 4. Scene layout

Six rooms connected linearly via corridors or direct doorways. Grid-aligned for Backrooms aesthetic. Rooms roughly 4×4m to 6×6m. Total walkable distance ~30m.

```
[Menu]
   │
   ▼
[Room 1: Foyer] ─► [Room 2: Corridor] ─► [Room 3: Gallery]
                                               │
                                               ▼
[Room 6: Exit] ◄─ [Room 5: Oscillating] ◄─ [Room 4: Strange Corridor]
```

---

## 5. Build order

### Pass 1 — Structure *(~2h)*
1. Move Backrooms-specific scripts from `Modules/Glue/` to `Experiences/Backrooms/` subfolders (Controllers, Managers, Profiles).
2. Build the six rooms as a linear greybox with doorways. Add player controller and basic lighting.
3. Verify the scene is walkable menu-to-exit with no interactions yet.

### Pass 2 — Core atoms *(~3h)*
1. Drop reused controllers into each room per the Design doc. Assign existing profiles.
2. Build `GazeSensor` and the first `GazeGlowController`.
3. Build `LightManager` and `PostProcessManager`. Refactor the two or three controllers that touch shared lighting to go through the manager.
4. Verify each room responds correctly in isolation.

### Pass 3 — Polish and state *(~2h)*
1. Oscillating Room: wallpaper swap, mannequin teleport, object shift during dark moments.
2. Wire `ExperienceMemory`: Room 3 gaze writes, Room 4 fog reads.
3. Author the per-room profile sets. Aim for a Gentle → Dread → Arctic arc across the sequence.
4. Menu, exit fade, per-room audio beds.
5. Playtest and tune.

**Total estimate: ~7 hours of focused build time.**

---

## 6. Folder structure after the move

```
Assets/Scripts/
├── Modules/
│   └── Glue/
│       ├── GateResponse.cs
│       ├── SensorResponse.cs
│       └── ThresholdResponse.cs
└── Experiences/
    └── Backrooms/
        ├── Controllers/
        │   ├── ColumnControllerV2.cs
        │   ├── Room1EntryControllerV2.cs
        │   ├── StillnessDimController.cs
        │   ├── CompactingCorridorController.cs
        │   ├── GazeDrainController.cs
        │   ├── GazeGlowController.cs        (new)
        │   ├── ManequinController.cs
        │   ├── RoomInversionController.cs
        │   ├── TileInversionController.cs
        │   ├── WallpaperSwap.cs             (new)
        │   ├── OscillatingLight.cs          (new)
        │   └── Pickup.cs                    (new)
        ├── Managers/
        │   ├── LightManager.cs              (new)
        │   └── PostProcessManager.cs        (new)
        ├── Profiles/
        │   └── (matching ScriptableObject classes)
        └── State/
            └── ExperienceMemory.cs          (new)
```

---

## 7. Deliverables for the class session

- The finished scene file in `Assets/_Lectures ENG/Lecture 7/Lecture 7.unity`
- This Design doc + Production Plan as the example brief
- Two authored profile sets per room (one gentle, one aggressive) so students can swap and compare
- Homework: modify one room's profiles, or add one new interaction from the L6 catalog, with a one-sentence justification of the placement choice
