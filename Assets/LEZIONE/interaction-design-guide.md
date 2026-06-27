# Interaction Design Guide

> A guided conversation with an AI assistant to design and build a Unity interaction.
> You describe what you want; the AI asks questions, teaches you the underlying model, and produces a spec + Profile + Controller at the end.
> No coding required.

---

## What this is

This is the **conversation script** for designing one interaction. You and an AI assistant walk through eight steps. By the end, the assistant produces three files for your scene:

- A **spec** (a plain-text description of the interaction)
- A **Profile** (a ScriptableObject — the design surface you tune in the inspector)
- A **Controller** (the script that drives the behavior; you place it on a GameObject)

You will *not* write code. You will choose, describe, and decide. The AI translates your decisions into the spec and the code.

Before you start, it helps to skim:
- `transformation-model-v2.md` — the underlying model (input → relationship → output, with curve, range, envelope)
- `primitives.md` — the catalog of inputs and outputs you can choose from

You can also start cold and let the AI explain as you go.

---

## How the conversation works

There are **eight steps**, in order. At any step you can say:

| Say | The AI does |
|---|---|
| **continue** | confirm what you have and move to the next step |
| **skip** | leave the step open and come back later — the AI tracks what is still missing |
| **explain** | give you a deeper teaching aside on the current concept |
| **show me options** | list the choices available (with examples from `primitives.md`) |
| **back** | return to a previous step to change something |
| **explore together** | walk through the choices interactively — the AI proposes, you react |
| **I don't know** | the AI offers two or three plausible defaults and you pick |

The AI keeps a running list of what you have decided and what is still open. At the end of every step it tells you both. You are never stuck.

If a step is unclear, the AI asks "**is this clear, or would you like me to explain more?**" — say no and it moves on, say yes and it teaches you the underlying concept before continuing.

---

## When the interaction is bigger than one atom

The model is built around the **atom**: one input, one relationship, one output. Many interactions are exactly that. But some are not. Two cases come up:

**Stateful** — the interaction remembers something. A counter that builds up over visits. A toggle that persists. A threshold that changes the behavior after it is crossed. State is added at Step 6 as a condition or as a wrapper around the input.

**Multi-component** — the interaction is several atoms composed together. Walking close brightens the column *and* lowers the fog *and* plays a sound. Or: pressing a button starts a swell that lasts five seconds and changes the room's state.

If your interaction is multi-component, the AI will help you **break it into atoms at Step 1**, and then walk through Steps 2–5 once per atom. You can also share an input across multiple outputs (one proximity reading drives three things) — the AI will recognize this and keep the proximity reading single.

You do not need to decide this up front. Describe the interaction; the AI will ask "is this one atom or several?" and help you break it down.

---

## The eight steps

### Step 1 — Description

You describe the interaction in your own words. One or two sentences as if telling a friend.

The AI asks:
- What happens? (a short description of cause and effect)
- What should it *feel* like? (eager, reluctant, attentive, indifferent, precise, forgiving, sticky, crisp, alive, still — pick one or two)
- Is this one moment, or does it have multiple stages or memory?

This step sets the tone for everything that follows. The feeling word becomes the touchstone the AI returns to when you are choosing curves and envelope values later: *does this feel reluctant enough? does it feel alive?*

If the interaction is multi-component, the AI proposes a breakdown into atoms here. You confirm or adjust.

### Step 2 — Input

You decide what the system reads about the participant or the world. The AI offers options from the input palette:

- **Spatial** — proximity, gaze direction, gaze duration, presence in a zone, contact, stillness, velocity
- **Direct manipulation** — grab/hold, throw/release, push/force
- **Workspace UI** — slider, button, toggle, dropdown
- **Autonomous** — time, random

Each input produces either a **continuous** signal (a flowing value, like distance) or a **binary** signal (on/off, like inside a zone). The AI fills in the signal type for you once you choose the input — you don't have to know it in advance.

The AI also asks for the **input range of interest**: a proximity sensor sees 0 to 20 meters, but maybe you only care about 0 to 5. Narrow the window so small movements feel meaningful.

### Step 3 — Output

You decide what changes in the world. The AI offers options from the output palette by perceptual domain:

- **Lighting** — light intensity, color, emission
- **Material & surface** — color, smoothness, transparency, dissolve, fresnel
- **Transform & geometry** — scale, position, rotation, deformation
- **Spawning** — show/hide, spawn, destroy
- **Particles & VFX** — emission rate, size, speed, color
- **Sound** — volume, pitch, low-pass filter, reverb
- **Environment** — fog, ambient, skybox
- **Post-processing** — bloom, saturation, vignette, depth of field
- **Camera** — field of view, shake, position

You can choose **more than one output** for a single input. The AI will ask whether each output uses the same shape (curve, range, envelope) or a different one — that's the *one-to-many* case the next step handles cleanly.

You also give the AI a reference to the **scene object** the output applies to: which light, which material, which renderer. You don't have to know the exact names — describe ("the ceiling lamp", "the pillar in the corner") and the AI lists what you need to assign in the inspector later.

### Step 4 — Relationship

You decide whether the output is **bound** to the input or **unbound** from it.

- **Bound** — the output depends on the input continuously. Walk closer, it brightens; walk away, it dims. *The system follows you.*
- **Unbound** — the input triggers the output, and then the output runs on its own. Press a button, a swell rises and falls over three seconds regardless of what you do next. *The system continues on its own.*

If you have more than one output, you can choose a different relationship per output. Sometimes the light is bound (tracks proximity) and the sound is unbound (a sting that plays when you cross a threshold). The AI tracks this per pair.

If the relationship is unclear, the AI asks: "**does the output need the input to keep going, or can it finish on its own?**"

### Step 5 — Shape

This is the design surface. Every relationship has a shape, defined by three sequential stages:

**Curve** — the *form* of the transformation. For a bound relationship, the curve maps input value to output value (X = input, Y = output). For an unbound relationship, the curve maps time to output (X = time, Y = output). The AI offers presets — linear, eased-in, eased-out, s-curve, stepped, inverted — or you can draw a custom curve later in the Unity inspector.

**Range** — the *boundaries*. Input range (which slice of the signal you care about) and output range (how strong the response can be). A narrow input range with a wide output range feels amplified. A wide input range with a narrow output range feels subtle.

**Envelope** — the *temporal feel*. How fast the output reaches its target.
- **Attack** (essential) — how fast it turns on / catches up
- **Release** (essential) — how fast it returns to rest
- **Decay** (advanced) — does it overshoot and settle?
- **Sustain** (advanced) — does it drift, breathe, oscillate while held?

For most interactions, **attack and release are enough**. The AI offers decay and sustain only if you ask for an overshooting or breathing feel.

If you said "reluctant" at Step 1, the AI will suggest slow attack. If you said "sticky" the AI will suggest slow release. The feeling word at Step 1 becomes a guide here.

### Step 6 — Condition (optional)

You decide whether the relationship is always active, or only active under specific rules.

- **Unconditional** — the relationship is always on. The default.
- **Conditional** — the relationship only activates when a variable is true, a counter has reached a threshold, the participant has visited another location first, the time of day matches, etc.

This is where **state** enters the system. The AI offers options:
- *Toggle* — the interaction only runs after a switch has been flipped
- *Counter* — the interaction changes after N occurrences
- *Threshold* — the interaction crosses into a new mode once a value is reached
- *Phase* — a global variable (day/night, calm/tense) gates the interaction
- *Cooldown* — after firing, the interaction is dormant for N seconds

You can skip this step entirely if the interaction is stateless. Most first interactions are.

### Step 7 — Spec

The AI produces the spec by filling in the template (`interaction-spec-template.md`). You see the whole interaction as one structured document — input, output, relationship per pair, shape per pair, conditions, scene references.

You read it back. If something is wrong, say so and the AI revises. This step is review and confirmation.

### Step 8 — Generate

The AI generates the three files into your project:

- `<Name>Spec.md` (or `.txt`) — the spec, saved alongside the script
- `<Name>Profile.cs` — the ScriptableObject with curves, ranges, and envelope parameters
- `<Name>Controller.cs` — the MonoBehaviour that reads the sensor and applies the output

The AI tells you exactly what to do in Unity:
1. Right-click in the Project window → Create → Ludocore → `<Name>` Profile (creates the SO asset)
2. Put the Controller component on a GameObject in your scene
3. Assign the scene references (sensor, renderer, transform, etc.) in the inspector
4. Assign the Profile asset to the Controller
5. Press Play and tune the Profile values while running

---

## A short worked example

> **You**: "When the player walks close to the column, it glows. When they walk away, it dims. It should feel reluctant — the column should take a moment to notice."
>
> **AI** (Step 1): Description = proximity makes a column glow. Feeling = reluctant. Single atom, stateless. Moving to Step 2.
>
> **AI** (Step 2): What's the input? — *Proximity is the natural fit. Anything else?* — Use ProximitySensor, max range 5 meters. Signal type: continuous.
>
> **AI** (Step 3): What changes? — *Emission intensity on the column's material.* One output. Renderer + material index to be assigned in the inspector.
>
> **AI** (Step 4): Bound or unbound? — *Bound. The column follows the player.*
>
> **AI** (Step 5): Curve = eased-in (so the response is small until the player is close, then ramps up — matches "reluctant"). Input range = 0–5m (handled at Step 2). Output range = 0 (min intensity) to 2 (max intensity). Envelope: slow attack (1.0), faster release (3.0) — reluctant to brighten, normal to fade.
>
> **AI** (Step 6): No conditions. Skip.
>
> **AI** (Step 7): Spec produced (see template). One Profile, one Controller.
>
> **AI** (Step 8): Generates `ColumnGlowSpec.md`, `ColumnGlowProfile.cs`, `ColumnGlowController.cs`. Tells you to assign the ProximitySensor reference, the renderer, and the Profile asset.

---

## What you are learning

Every interaction you design teaches you the same model, deeper.

After a few interactions you will notice:
- **Signal type** is decided by the input, not by your choice — proximity is always continuous, a button is always binary
- **Bound vs. unbound** is a question about whether the output needs the input to keep going
- **Curve, range, envelope** are three independent dials — change any one and the interaction feels different
- **Feeling words** map to concrete envelope settings — "reluctant" is slow attack, "sticky" is slow release, "alive" is overshoot or oscillation

You are not memorizing options. You are building intuition for the model. The AI is the scaffolding; the model is the thing you keep.

---

## Reference

- `transformation-model-v2.md` — the underlying model
- `primitives.md` — full catalog of inputs and outputs
- `interaction-spec-template.md` — the structured output produced by this guide
- `interaction-agent-guide.md` — the instructions the AI follows (you don't need to read this, but it's there)
