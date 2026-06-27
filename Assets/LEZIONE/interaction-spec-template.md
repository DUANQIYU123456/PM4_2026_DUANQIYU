# Interaction Spec Template

> The structured output the AI fills as it walks the student through `interaction-design-guide.md`.
> Soft formal: each step has a field set. Missing fields are marked `OPEN`.
> At Step 7 the filled template becomes the spec doc; at Step 8 it drives code generation.

---

## How to read this template

Each step below has a **schema block** (the fields to fill) and a **worked example** (the same fields filled for a real interaction). The AI fills one copy of this template per interaction. For multi-component interactions, Steps 2–6 are repeated per atom inside the same template.

Fields marked `[required]` must be filled before code generation. Fields marked `[optional]` can be left blank. Any field can be marked `OPEN` if the student says skip.

---

## Step 0 — Header

```yaml
name:                 # PascalCase, no spaces — used for file names and class names
folder:               # destination folder under Assets/Scripts/ (e.g. "Backrooms")
one_line:             # one sentence: signal in → what changes
```

**Example:**

```yaml
name:    ColumnGlow
folder:  Backrooms
one_line: Proximity to a column drives its emission intensity, reluctantly.
```

---

## Step 1 — Description

```yaml
description:          # 2–4 sentences in the student's words
feeling:              # one or two: eager | reluctant | attentive | indifferent
                      #           | precise | forgiving | sticky | crisp
                      #           | alive | still
complexity:           # simple | stateful | multi-component
atoms:                # only if multi-component — list of named atoms
                      # each atom gets its own Steps 2–6 block below
```

**Example (simple):**

```yaml
description: When the player walks close to the column, it glows more
             strongly. When they walk away, it dims. The column should
             be a little slow to respond — like it needs a moment to
             notice the participant.
feeling:     reluctant
complexity:  simple
atoms:       [main]
```

**Example (multi-component):**

```yaml
description: Entering the room dims the global lights, plays a low drone,
             and after 3 seconds the ceiling tiles begin to slowly
             invert their orientation while the player is still inside.
feeling:     ominous, alive
complexity:  multi-component
atoms:
  - light-dim       # binary trigger → unbound dim, owns LightManager.Dim
  - drone-play      # binary trigger → unbound audio swell
  - tile-invert     # binary presence → bound rotation, delayed 3s
```

---

## Step 2 — Input

Repeat per atom. One input block per atom.

```yaml
atom: <name>
input:
  name:               # e.g. Proximity, Gaze, Presence, Button, Time
  signal_type:        # continuous | binary  (derived from input)
  sensor_class:       # ProximitySensor | GazeSensor | TriggerSensor |
                      # RaycastSensor | CollisionSensor | (custom / none)
  input_range:        # for continuous signals — the slice of interest
                      # e.g. "0 to 5 meters" or "n/a" for binary
  source_description: # what the sensor watches — for inspector hints
                      # e.g. "tagged Player" or "looking at the panel"
```

**Example:**

```yaml
atom: main
input:
  name:               Proximity
  signal_type:        continuous
  sensor_class:       ProximitySensor
  input_range:        0 to 5 meters
  source_description: ProximitySensor on the column, tagged Player, radius 5
```

---

## Step 3 — Output

Repeat per atom. One or more output blocks per atom.

```yaml
atom: <name>
outputs:
  - name:                  # short, used for Profile field naming
    domain:                # lighting | material | transform | spawning |
                           # particles | sound | environment | post | camera
    property:              # specific property changed (e.g. Light.intensity,
                           # Material._EmissionColor, Transform.localPosition)
    scene_reference:       # description of the object — becomes a Controller
                           # [SerializeField] field
    output_range:          # min → max  (e.g. "0 to 2", "0.2 to 0.8")
    notes:                 # [optional] anything that affects how the
                           # Controller applies the value
```

**Example (one output):**

```yaml
atom: main
outputs:
  - name:            emission
    domain:          material
    property:        Material._EmissionColor (intensity)
    scene_reference: target renderer + material index on the column
    output_range:    0 to 2
    notes:           color is also a Profile field (HDR), defaults to white
```

**Example (multiple outputs sharing one input):**

```yaml
atom: main
outputs:
  - name:            compaction
    domain:          transform
    property:        wallA.localPosition, wallB.localPosition (along axis)
    scene_reference: wallA, wallB transforms; axis Vector3 in Controller
    output_range:    0 to 1 unit inward
  - name:            scroll
    domain:          material
    property:        Material._MainTex.offset.y
    scene_reference: shared wall material
    output_range:    0 to 1 units/sec
  - name:            vignette
    domain:          post
    property:        Vignette.intensity
    scene_reference: URP Volume in scene
    output_range:    0 to 0.4
```

---

## Step 4 — Relationship

Repeat per input→output pair within an atom.

```yaml
atom: <name>
relationships:
  - pair:             # <input_name> → <output_name>
    type:             # bound | unbound
    reason:           # one sentence — why this is bound or unbound
```

**Example:**

```yaml
atom: main
relationships:
  - pair:   Proximity → emission
    type:   bound
    reason: the glow should follow the participant continuously,
            not run on its own
```

**Example (unbound):**

```yaml
atom: tile-invert
relationships:
  - pair:   Presence → tile-rotation
    type:   unbound
    reason: once the player enters, the tiles run their inversion
            sequence regardless of whether the player stays
```

---

## Step 5 — Shape

Repeat per relationship. Curve, range, envelope per pair.

```yaml
atom: <name>
shape:
  - pair: <input → output>
    curve:
      preset:         # linear | ease-in | ease-out | s-curve | stepped |
                      # inverted | custom
      meaning:        # for bound: "input X → output Y";
                      # for unbound: "time X → output Y"
      notes:          # [optional] e.g. "stepped at 0.5 threshold"
    range:
      input:          # e.g. 0..5 m  (or "n/a" for binary input)
      output:         # e.g. 0..2 intensity units
    envelope:
      attack:         # seconds OR units/sec  (label which)
      release:        # seconds OR units/sec
      decay:          # [optional, advanced] overshoot percent + settle time
      sustain:        # [optional, advanced] hold behavior — steady | drift |
                      # oscillate (with frequency/amplitude)
```

**Example (bound, simple):**

```yaml
atom: main
shape:
  - pair: Proximity → emission
    curve:
      preset:  ease-in
      meaning: small response while far, ramps up close — matches "reluctant"
    range:
      input:   0..5 m
      output:  0..2 intensity
    envelope:
      attack:  1.0 units/sec  # reluctant — slow to brighten
      release: 3.0 units/sec  # normal fade
```

**Example (unbound, designed timeline):**

```yaml
atom: drone-play
shape:
  - pair: Presence → drone-volume
    curve:
      preset:  custom
      meaning: time → volume, slow swell to 1.0 over 2s, sustain, fade over 4s
    range:
      input:   n/a (event trigger)
      output:  0..1 volume
    envelope:
      attack:  2.0 s
      release: 4.0 s
      decay:   none
      sustain: steady
```

---

## Step 6 — Condition (optional)

Skip if the relationship is always active.

```yaml
atom: <name>
condition:
  active:               # always | conditional
  gate_type:            # [if conditional] toggle | counter | threshold |
                        #                   phase | cooldown
  gate_variable:        # name of a BoolVariable / IntVariable / FloatVariable
                        # ScriptableObject — created separately if missing
  comparison:           # equals, greater-than, less-than, between, etc.
  value:                # the comparison target
  when:                 # always | only-while-true | only-once
  notes:                # [optional]
```

**Example (counter):**

```yaml
atom: main
condition:
  active:        conditional
  gate_type:     counter
  gate_variable: visitCount (IntVariable)
  comparison:    greater-than-or-equal
  value:         3
  when:          only-while-true
  notes:         The column only responds to proximity after the
                 player has visited it three times.
```

---

## Step 7 — Spec (assembled output)

The AI renders this section as a plain-text spec doc matching the style of existing specs in `Docs/Specs/`. Format:

```
---
Implement only what is specified in this document. If the component already
exists, evaluate the diff and update or add the new features. If something
is unclear, ask me clarifying questions explicitly before implementing.
---

<one-paragraph description: what the interaction does, signal in, what
changes, the feeling word, and which sensor / pattern it follows>

========== PROFILE (ScriptableObject) — <Name>Profile ==========

  Input — <Sensor>
  - <field>  : <type>  — <tooltip>
  - ...

  <Output A> — <Bound|Unbound> Interaction
  - <field>  : <type>  — <tooltip>
  - ...

  <Output B> — <Bound|Unbound> Interaction
  - ...

========== CONTROLLER (MonoBehaviour) — <Name>Controller ==========

  Scene References
  - sensor       : <SensorClass>
  - <reference>  : <type>
  - ...

  Profile
  - profile      : <Name>Profile

  Runtime behavior (Update)
  1. <step>
  2. <step>
  ...

  On Start
  - <cache / initial setup>

  On <event>
  - <e.g. sensor first-detected → start unbound timeline>
  - <e.g. sensor losing all detections → returns to rest>
```

The AI fills this from the structured fields above. It is the artifact saved next to the generated scripts. See `Docs/Specs/CompactingCorridorSpec.txt` and `Docs/Specs/Room1EntryController.txt` for two reference specs to match in style.

---

## Step 8 — Generation manifest

The final action list. The AI prints this back to the student before writing files.

```yaml
files_to_create:
  - path:    Assets/Scripts/<folder>/<Name>Spec.md
    kind:    spec
  - path:    Assets/Scripts/<folder>/<Name>Profile.cs
    kind:    profile (ScriptableObject)
    menu:    Create → Ludocore → <Name> Profile
  - path:    Assets/Scripts/<folder>/<Name>Controller.cs
    kind:    controller (MonoBehaviour)

extra_assets_needed:
  - <e.g. an IntVariable asset for the visitCount counter>
  - <e.g. a Profile asset instance, created via the menu above>

inspector_setup:
  - Add the <Name>Controller component to: <which GameObject>
  - Assign these references on the component:
      - sensor:           <which sensor in scene>
      - targetRenderer:   <which renderer>
      - <other>:          <description>
  - Assign the Profile asset to the component's `profile` field.

first_play_tips:
  - Press Play and walk into / out of the sensor range.
  - Tune <key Profile field> first to feel the response.
  - Try <feeling word> by changing <attack/release/curve>.
```

---

## Multi-component interactions

When `complexity = multi-component`, the template carries one **Step 1 header** for the whole interaction, then repeats **Steps 2–6** under each atom name. Step 7 assembles a single spec doc with a Profile section per output and the Controller's Runtime behavior listing each atom's contribution.

If atoms are truly independent (different sensors, different objects, no shared state), the AI can split them into separate `<Name>` units — one Profile + Controller per atom. Ask the student before splitting.

If atoms share an input (e.g., one ProximitySensor drives three outputs), the input is defined once and the relationships, shapes, and conditions list one entry per output. This is the `CompactingCorridor` pattern.

---

## Stateful interactions

When `complexity = stateful`, state enters in one of three places:

1. **As a condition** at Step 6 (the relationship is gated on a variable).
2. **Wrapping the input** — the Profile / Controller reads accumulated input instead of raw input (accumulated proximity, accumulated gaze duration). The Controller maintains the accumulator; the Profile exposes its decay rate.
3. **Mutating the parameters** — a variable shifts the curve, the range, or the envelope. The Profile holds two curve fields (e.g. `curveCalm`, `curveTense`) and the Controller picks based on a variable.

The AI picks the simplest of these that fits the student's description. If unclear, offer the three and let the student choose.

---

## Open-field tracking

While the conversation is in progress, the AI maintains a running list at the end of each step:

```yaml
decided:
  - Step 1: name, feeling, complexity
  - Step 2: input chosen, sensor, range
  - Step 3: 2 of 3 outputs decided
open:
  - Step 3 output #3: scene_reference and output_range still OPEN
  - Step 5 shape for output #2: envelope.release still OPEN
```

At Step 7, every `open` item must be resolved or explicitly marked `[STUDENT TO TUNE]` in the generated Profile defaults.
