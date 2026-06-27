# Backrooms Walk — Design Document

A short worked example of how interaction atoms compose into a finished experience.

---

## 1. Experience vision

A 4–5 minute walk through six rooms. You enter somewhere that seems almost normal. Room by room, the space drifts further from normal. Things respond to you. Then they respond wrongly. Then they respond to things you did rooms ago. Eventually you walk out.

No monsters, no deaths, no fail states. The register is the **uncanny** — a quiet sense that the space is aware of you, and that you are not entirely welcome.

The experience is not a game. It is a **composition of interaction atoms placed spatially**, authored so each room feels a little further from the last.

---

## 2. Design principles

1. **Placement over logic.** Escalation is authored into profiles per room, not computed at runtime.
2. **One concept per room.** Each room showcases a single placement strategy (threshold, hotspot, zone, behavioral, ambient) as its spine.
3. **State is small, local, and visible.** Two flags tied to specific interactions, read by later rooms.
4. **Managers only where outputs are shared.** Lighting and post-processing are arbitrated. Nothing else.

---

## 3. Input and output palette

### Inputs
| Input | Sensor | Signal type |
|---|---|---|
| Contact | TriggerSensor | Binary |
| Proximity | ProximitySensor | Continuous |
| Stillness | Velocity threshold | Threshold |
| Gaze | GazeSensor *(new for L7)* | Binary |
| Grab | Pickup *(new for L7, minimal)* | Binary, one-shot |

### Outputs
| Output | Channel | Arbitrated by |
|---|---|---|
| Material emission | Per-object or shared ceiling | LightManager (when shared) |
| Scene lighting | Ambient, reflections | LightManager |
| Post-processing | Saturation, vignette, temperature | PostProcessManager |
| Fog | RenderSettings | Direct (one writer per moment) |
| Geometry | Wall transforms, tile inversion | Direct |
| Material | Wallpaper swap | Direct |
| Sound | Per-room ambient bed, per-object SFX | Direct |

---

## 4. Room-by-room progression

### Room 1 — Foyer *(normal)*
> The space is alive but gentle. Curiosity.

- **Threshold:** Room1Entry at the door — Cinematic profile.
- **Hotspot:** Two columns flanking the center — Gentle profile.
- **Behavioral:** StillnessDim — slow dim, slow recovery.
- **Ambient:** One breathing ceiling light — Sleeping profile.

State: none.

---

### Room 2 — First Corridor *(bending)*
> Transition. The space narrows.

- **Hotspot:** CompactingCorridor — walls lean inward as you approach the exit.
- **Ambient:** Proximity Fog — Reluctant profile.
- **Behavioral:** GazeGlow on a wall stain — Shy profile.

State: none.

---

### Room 3 — Gallery *(attention)*
> The space begins to stare back.

- **Threshold:** Room entry — Sudden profile.
- **Zone:** Zone Presence Hum — Clingy profile.
- **Hotspots:** Four wall stains with GazeGlow — Sticky profile.
- **Behavioral + state:** Mannequin in the corner with GazeSensor. Looking at it sets `mannequinSeen = true`.
- **Behavioral:** GazeDrain fires while gazing at the mannequin — Creeping profile.

State: **writes `mannequinSeen`**.

---

### Room 4 — Strange Corridor *(memory)*
> The room before you left something behind.

- **Threshold:** DoorwayFogPulse on entry — Tide profile.
- **Hotspot:** FloorLight per tile — Warm Trail profile.
- **State-driven:** ProximityFog reads `mannequinSeen`. If true, fog stays dense and the corridor remains dim. If false, fog is light and the corridor breathes. **Same atoms, different atmosphere based on what you did two rooms ago.**

State: **reads `mannequinSeen`**.

---

### Room 5 — Oscillating Room *(disorientation)*
> You cannot trust what you see.

- **Ambient:** BreathingLight, Anxious profile tuned fast — oscillates bright/dark every 2–3 seconds.
- **Dark-moment events:** While the room is dark, hidden controllers fire:
  - Mannequin teleports to a new position
  - One wallpaper material swaps
  - A distant object shifts
- When light returns, the player sees the changes but did not witness them happening.
- **Optional:** A grabbable object on a pedestal. Pickup sets `objectTaken = true`.

State: **writes `objectTaken`** *(optional)*.

---

### Room 6 — Exit *(release)*
> It lets you go.

- **Threshold:** Exit trigger fires a closing one-shot — slow audio swell, white fade, all lights settle to a single warm glow, scene returns to menu.
- **Ambient:** One breathing light, slowest profile.
- No hotspots, no behaviors. Deliberate stillness.
- If `objectTaken` is true, the closing audio has an extra sub-bass layer. Small payoff — the space noticed.

State: **reads `objectTaken`**.

---

## 5. State

One ScriptableObject, `ExperienceMemory.asset`, holding two booleans:

| Flag | Written by | Read by |
|---|---|---|
| `mannequinSeen` | GazeSensor in Room 3 | ProximityFog in Room 4 |
| `objectTaken` | Pickup in Room 5 | Exit audio bed in Room 6 |

Reset on scene reload. No other state anywhere in the experience.

---

## 6. Architecture (brief)

The architecture is the one already taught:

- **Sensors** emit signals. One new sensor in L7: **GazeSensor**.
- **Controllers** map signals to outputs via swappable Profiles.
- **Managers** arbitrate shared outputs: **LightManager**, **PostProcessManager**.
- **State** is a small ScriptableObject, read and written directly by the controllers that need it.
- **No flow manager, no director, no narrative tracker.** The scene is linear; rooms are physical spaces, not states.

---

## 7. What students do with this

1. Play through with the shipped profiles.
2. Classify each interaction by matrix quadrant and placement type.
3. Swap profiles between rooms — make the Gallery gentle, make the Foyer dreadful. Notice the experience re-voice itself with no layout change.
4. Create one new profile variant for a room of their choice.
5. *(Stretch)* Add one new interaction atom from the L6 catalog. Defend the choice of placement.
