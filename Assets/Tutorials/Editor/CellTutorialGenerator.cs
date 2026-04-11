// ============================================
// Cell Tutorial Generator
// ============================================
// Generates all tutorial assets for Demo 1: Living Cells.
// Menu: Tutorials > Generate Living Cells Tutorials
//
// REQUIRES:
//   1. com.unity.learn.iet-framework (3.1+)
//   2. com.unity.learn.iet-framework.authoring (1.2+)
//
// INSTALL (Package Manager > Add package from git URL):
//   com.unity.learn.iet-framework
//   com.unity.learn.iet-framework.authoring
//
// If property paths don't match your IET version, run:
//   Tutorials > Debug > Dump Tutorial Property Names
// and update the paths in the SetLocalizable helper.
// ============================================

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unity.Tutorials.Core.Editor;

public static class CellTutorialGenerator
{
    private const string ROOT = "Assets/Tutorials/LivingCells";

    // ═══════════════════════════════════════
    // Content Data
    // ═══════════════════════════════════════

    struct PageDef
    {
        public string title;
        public string body;
    }

    struct TutorialDef
    {
        public string title;
        public string description;
        public PageDef[] pages;
    }

    static readonly TutorialDef[] Tutorials = new[]
    {
        // ──────────────────────────────────
        // Tutorial 1: The Cell Prefab
        // ──────────────────────────────────
        new TutorialDef
        {
            title = "1 — The Cell Prefab",
            description = "Build a cell with energy, decay, and a visible health bar.",
            pages = new[]
            {
                new PageDef
                {
                    title = "What We're Building",
                    body =
                        "In this tutorial we create our first living cell — a sphere that has energy, " +
                        "loses it over time, and dies when it runs out.\n\n" +
                        "We'll see the energy drain visually through a fill bar from the very first step.\n\n" +
                        "Two key Unity patterns:\n" +
                        "\u2022 <b>ScriptableObject</b> as a data container (CellData)\n" +
                        "\u2022 <b>MonoBehaviour</b> for logic (CellLifecycle) and presentation (CellView)"
                },
                new PageDef
                {
                    title = "Create the Data Asset",
                    body =
                        "CellData is a ScriptableObject \u2014 a shared data asset that lives in the Project, " +
                        "not on a GameObject. Every cell of the same species points to the same asset. " +
                        "Change the asset, every cell updates. This is the <b>Flyweight pattern</b>.\n\n" +
                        "<b>Steps:</b>\n" +
                        "1. In the Project window: <b>Create > Data > CellData</b>\n" +
                        "2. Name it <b>DefaultCell</b>\n" +
                        "3. In the Inspector, set:\n" +
                        "   \u2022 typeName: \"Cell\"\n" +
                        "   \u2022 typeColor: White\n" +
                        "   \u2022 speed: 3\n" +
                        "   \u2022 startingEnergy: 100\n" +
                        "   \u2022 energyDecayRate: 5"
                },
                new PageDef
                {
                    title = "Create the Cell GameObject",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Hierarchy: <b>3D Object > Sphere</b>. Name it <b>Cell</b>\n" +
                        "2. Add Component: <b>CellLifecycle</b>\n" +
                        "3. Drag the <b>DefaultCell</b> asset into the Cell Data field\n\n" +
                        "CellLifecycle reads startingEnergy and energyDecayRate from CellData. " +
                        "Each frame, energy decreases by energyDecayRate \u00d7 Time.deltaTime. " +
                        "When it hits zero, the cell destroys itself."
                },
                new PageDef
                {
                    title = "Make Energy Visible",
                    body =
                        "The cell dies, but we can only see it in the Inspector. Let's add a visual energy bar.\n\n" +
                        "<b>Steps:</b>\n" +
                        "1. Right-click the Cell in Hierarchy > <b>UI > Canvas</b> (becomes a child)\n" +
                        "2. Set Render Mode to <b>World Space</b>. Scale it down (0.01, 0.01, 0.01)\n" +
                        "3. Inside the Canvas, add <b>UI > Image</b>\n" +
                        "4. Set Image Type to <b>Filled</b>, pick a Fill Method (e.g. Horizontal)\n" +
                        "5. Add <b>CellView</b> to the Cell root\n" +
                        "6. Wire references: CellLifecycle and the fill Image\n\n" +
                        "<b>The View Rule:</b> CellView reads from CellLifecycle, it never writes. " +
                        "Delete every View script and the simulation runs identically. " +
                        "Presentation is separate from logic."
                },
                new PageDef
                {
                    title = "Make It a Prefab",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Drag the Cell from Hierarchy into the Project window to create a <b>Prefab</b>\n" +
                        "2. Hit <b>Play</b>\n\n" +
                        "<b>Observe:</b>\n" +
                        "\u2022 The energy bar drains smoothly\n" +
                        "\u2022 When energy reaches zero, the cell disappears\n" +
                        "\u2022 Change energyDecayRate on the CellData asset during play \u2014 death speed changes\n\n" +
                        "One prefab. One data asset. Same code can drive any species \u2014 just different numbers."
                }
            }
        },

        // ──────────────────────────────────
        // Tutorial 2: Movement
        // ──────────────────────────────────
        new TutorialDef
        {
            title = "2 — Movement",
            description = "Add wander behavior and boundary reflection.",
            pages = new[]
            {
                new PageDef
                {
                    title = "Adding Movement",
                    body =
                        "A living cell should move. We'll add a motor that wanders randomly and " +
                        "reflects off the play area boundary.\n\n" +
                        "The motor uses a <b>reset-per-frame</b> pattern: every frame the direction resets. " +
                        "If nothing calls SetDirection(), the cell wanders. Later, the agent will steer it \u2014 " +
                        "but only for one frame at a time. Next frame, the decision starts fresh."
                },
                new PageDef
                {
                    title = "Attach the Motor",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Open the <b>Cell</b> prefab\n" +
                        "2. Add Component: <b>CellMotor</b>\n" +
                        "3. Assign the <b>DefaultCell</b> CellData asset\n" +
                        "4. Set boundaryCenter to <b>(0, 0, 0)</b>\n" +
                        "5. Set boundaryRadius to <b>20</b>\n\n" +
                        "The motor uses CellData.Speed for movement. " +
                        "The boundary is a spherical wall \u2014 when crossed, the direction reflects like bouncing off a surface."
                },
                new PageDef
                {
                    title = "Add a Rigidbody",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Add Component: <b>Rigidbody</b>\n" +
                        "2. Check <b>Is Kinematic</b>\n" +
                        "3. Uncheck <b>Use Gravity</b>\n\n" +
                        "<b>Why kinematic?</b> We move via transform.position, not forces. " +
                        "Physics-based movement is great for physical interactions, but here cells are " +
                        "logical agents, not physics objects. The Rigidbody exists only so trigger colliders " +
                        "(added later) can detect each other."
                },
                new PageDef
                {
                    title = "Watch It Move",
                    body =
                        "Hit <b>Play</b> and observe:\n\n" +
                        "\u2022 The cell drifts randomly, slowly changing course (wander)\n" +
                        "\u2022 At the boundary edge, it reflects and heads back\n" +
                        "\u2022 It still loses energy and dies\n\n" +
                        "<b>Experiment:</b>\n" +
                        "\u2022 Change <b>speed</b> on CellData \u2014 faster/slower movement\n" +
                        "\u2022 Change <b>wanderDrift</b> on CellMotor \u2014 higher = more direction changes\n\n" +
                        "The cell is now alive in two ways: it has a lifespan and it moves through space."
                }
            }
        },

        // ──────────────────────────────────
        // Tutorial 3: Sensing
        // ──────────────────────────────────
        new TutorialDef
        {
            title = "3 — Sensing",
            description = "Detect nearby objects with the sensor pattern.",
            pages = new[]
            {
                new PageDef
                {
                    title = "The Sensor Pattern",
                    body =
                        "Cells need to know what's around them. We'll build a sensor that " +
                        "detects nearby objects and reports them as <b>Signals</b>.\n\n" +
                        "The key principle: <b>sensors detect, they don't decide.</b> " +
                        "TriggerSensor populates a list of what's nearby. It has no opinion about " +
                        "what to do \u2014 chase? flee? eat? That's the agent's job.\n\n" +
                        "We use a <b>child GameObject</b> for the sensor because each trigger collider " +
                        "needs its own GameObject. Unity fires OnTriggerEnter on the object that owns the collider."
                },
                new PageDef
                {
                    title = "Build the Sense Zone",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Open the Cell prefab\n" +
                        "2. Create an <b>empty child</b>, name it <b>Sense</b>\n" +
                        "3. Add <b>SphereCollider</b> to Sense\n" +
                        "4. Check <b>Is Trigger</b>, set Radius to ~<b>5</b>\n\n" +
                        "This trigger sphere defines the cell's perception range. " +
                        "Anything entering this volume will be detected."
                },
                new PageDef
                {
                    title = "Attach the Sensor",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Add <b>TriggerSensor</b> to the Sense child\n\n" +
                        "TriggerSensor extends the abstract Sensor base class. When objects enter the trigger, " +
                        "it creates a <b>Signal</b> \u2014 a struct containing the detected GameObject and its distance.\n\n" +
                        "<b>Why Signal is a struct:</b>\n" +
                        "A Signal is a small data pair (object + distance). Structs are value types \u2014 " +
                        "they live on the stack, don't allocate heap memory, and avoid garbage collection pressure. " +
                        "For a list that refreshes every frame, this matters."
                },
                new PageDef
                {
                    title = "Configure Physics",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. On the Cell <b>root</b>, add a <b>SphereCollider</b> (non-trigger) \u2014 the body\n" +
                        "2. Set the Layer to a new layer: <b>Cell</b>\n" +
                        "3. Apply to all children when prompted\n" +
                        "4. <b>Edit > Project Settings > Physics</b>\n" +
                        "5. In the Layer Collision Matrix, ensure <b>Cell \u2194 Cell</b> is checked\n\n" +
                        "The body collider gives each cell a physical presence. " +
                        "The layer matrix ensures cells detect each other but not unrelated objects."
                },
                new PageDef
                {
                    title = "Observe the Signals",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Exit the prefab editor\n" +
                        "2. Place <b>3\u20134 cell instances</b> in the scene\n" +
                        "3. Hit <b>Play</b>\n\n" +
                        "<b>Observe:</b>\n" +
                        "\u2022 Select any cell, look at the Sense child's TriggerSensor\n" +
                        "\u2022 Expand the <b>Signals</b> list \u2014 entries appear as cells enter range\n" +
                        "\u2022 Entries disappear as cells drift away\n" +
                        "\u2022 Distances update in real time\n" +
                        "\u2022 The <b>yellow gizmo</b> shows the sense radius\n\n" +
                        "The sensor sees everything. It makes no judgments. Detect, then let someone else decide."
                }
            }
        },

        // ──────────────────────────────────
        // Tutorial 4: Consume Zone
        // ──────────────────────────────────
        new TutorialDef
        {
            title = "4 — Consume Zone",
            description = "Add a smaller inner ring for eating range.",
            pages = new[]
            {
                new PageDef
                {
                    title = "Layered Detection",
                    body =
                        "We have a sense zone (what's nearby). Now we add a <b>consume zone</b> \u2014 " +
                        "a smaller, inner ring that answers: is this close enough to eat?\n\n" +
                        "Two zones, two scripts, two responsibilities. " +
                        "The outer ring (Sensor) detects. The inner ring (CellConsume) tracks what's " +
                        "within eating distance. This is common in game AI \u2014 detection range " +
                        "is always larger than action range."
                },
                new PageDef
                {
                    title = "Build the Consume Zone",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Open the Cell prefab\n" +
                        "2. Create an <b>empty child</b>, name it <b>Consume</b>\n" +
                        "3. Add <b>SphereCollider</b> \u2014 check Is Trigger, Radius ~<b>1.5</b>\n" +
                        "   (must be smaller than the sense radius)\n" +
                        "4. Add Component: <b>CellConsume</b>\n\n" +
                        "CellConsume uses a HashSet to track what's inside. " +
                        "CellAgent will later call IsInRange() to check before eating."
                },
                new PageDef
                {
                    title = "Two Rings",
                    body =
                        "Hit <b>Play</b> and observe:\n\n" +
                        "1. Select a cell in the Scene view\n" +
                        "2. Click the Sense child \u2014 see the <b>yellow</b> gizmo ring\n" +
                        "3. Click the Consume child \u2014 see the <b>red</b> gizmo ring\n\n" +
                        "Two concentric rings: sense (yellow, large) and consume (red, small). " +
                        "Objects can be sensed without being consumable \u2014 " +
                        "the cell knows something is nearby before it's close enough to eat.\n\n" +
                        "Nothing happens yet. There's no brain to act on this information."
                }
            }
        },

        // ──────────────────────────────────
        // Tutorial 5: Interaction Rules
        // ──────────────────────────────────
        new TutorialDef
        {
            title = "5 — Interaction Rules",
            description = "Define cell reactions in data before writing logic.",
            pages = new[]
            {
                new PageDef
                {
                    title = "Data Before Logic",
                    body =
                        "Before writing decision logic, we define the <b>rules</b> \u2014 entirely in data.\n\n" +
                        "Every cell will have a <b>CellBehavior</b> ScriptableObject that maps " +
                        "other cell types to actions: attract, repel, consume, flock, or ignore.\n\n" +
                        "This separation is powerful: change how a species behaves by editing an asset. " +
                        "Add a new species? Create new data assets. The code stays the same."
                },
                new PageDef
                {
                    title = "The Three Pieces",
                    body =
                        "Three small scripts define the rule system:\n\n" +
                        "<b>InteractionAction</b> (enum)\n" +
                        "Five possible reactions: Ignore, Attract, Repel, Consume, Flock.\n\n" +
                        "<b>InteractionRule</b> (struct)\n" +
                        "A pair: target CellData + what action. It's a struct because it's pure data \u2014 " +
                        "no behavior, no identity.\n\n" +
                        "<b>CellBehavior</b> (ScriptableObject)\n" +
                        "Holds a list of InteractionRules. Given a target type, returns the matching action.\n\n" +
                        "CellData = <i>what a cell is</i> (identity, speed, energy).\n" +
                        "CellBehavior = <i>how it reacts</i> (rules toward other types). Different concerns, different assets."
                },
                new PageDef
                {
                    title = "Create a CellBehavior",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. In the Project window: <b>Create > Data > CellBehavior</b>\n" +
                        "2. Name it <b>DefaultBehavior</b>\n" +
                        "3. Expand <b>Interaction Rules</b>, click <b>+</b>\n" +
                        "4. Set:\n" +
                        "   \u2022 Target Type: your <b>DefaultCell</b> CellData asset\n" +
                        "   \u2022 Action: <b>Attract</b>\n\n" +
                        "This means: \"when I see another cell of my type, move toward it.\"\n\n" +
                        "Later, each species will have its own CellBehavior with different rules."
                },
                new PageDef
                {
                    title = "The Data Matrix",
                    body =
                        "Open the CellBehavior asset. The rules are a visible, editable list. " +
                        "Each entry is a relationship: \"when I see <i>this type</i>, I do <i>this action</i>.\"\n\n" +
                        "This is <b>data-driven design</b> at its core. The entire relationship matrix " +
                        "between species will be defined in these assets. No if-statements. " +
                        "No hardcoded species names. Just data.\n\n" +
                        "In Step 7 we'll create three CellBehavior assets with asymmetric rules \u2014 " +
                        "herbivores consume flora, but flora repels herbivores. " +
                        "Same system, different data, emergent dynamics."
                }
            }
        },

        // ──────────────────────────────────
        // Tutorial 6: The Brain
        // ──────────────────────────────────
        new TutorialDef
        {
            title = "6 — The Brain",
            description = "Wire the Sense \u2192 Think \u2192 Act loop.",
            pages = new[]
            {
                new PageDef
                {
                    title = "Sense \u2192 Think \u2192 Act",
                    body =
                        "Every piece is in place: lifecycle, motor, sensor, consume zone, rules. " +
                        "Now we add the <b>brain</b> \u2014 CellAgent.\n\n" +
                        "Every frame, CellAgent runs a three-step loop:\n" +
                        "1. <b>Sense</b> \u2014 read sensor signals (what's nearby?)\n" +
                        "2. <b>Think</b> \u2014 look up the rule for each signal, rank by priority\n" +
                        "3. <b>Act</b> \u2014 steer toward/away from the best target, or consume it\n\n" +
                        "Priority: Consume (2) > Repel (1) > Attract (0). " +
                        "Equal priority? Closer target wins."
                },
                new PageDef
                {
                    title = "Wire the Brain",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Open the Cell prefab\n" +
                        "2. Add Component: <b>CellAgent</b> on the root\n" +
                        "3. Assign all references:\n" +
                        "   \u2022 Cell Data: <b>DefaultCell</b>\n" +
                        "   \u2022 Cell Behavior: <b>DefaultBehavior</b>\n" +
                        "   \u2022 Motor: CellMotor (this root)\n" +
                        "   \u2022 Lifecycle: CellLifecycle (this root)\n" +
                        "   \u2022 Sensor: TriggerSensor (Sense child)\n" +
                        "   \u2022 Consume: CellConsume (Consume child)\n\n" +
                        "Every reference is explicit \u2014 visible in the Inspector, easy to debug. " +
                        "No hidden dependencies."
                },
                new PageDef
                {
                    title = "Populate the Scene",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Exit the prefab editor\n" +
                        "2. Place <b>5\u20136 cell instances</b> in the scene, spread out\n" +
                        "3. All use the same CellData and CellBehavior\n\n" +
                        "With the Attract rule from the previous tutorial, cells should move toward each other " +
                        "when they enter sensing range."
                },
                new PageDef
                {
                    title = "Watch the Cells Think",
                    body =
                        "Hit <b>Play</b> and observe:\n\n" +
                        "\u2022 Cells detect each other and steer toward nearby cells (Attract rule)\n" +
                        "\u2022 They cluster together as the simulation runs\n" +
                        "\u2022 Energy still decays \u2014 cells die even while attracted\n\n" +
                        "<b>Experiment \u2014 change the rule in DefaultBehavior:</b>\n" +
                        "\u2022 <b>Repel</b> \u2014 cells flee from each other\n" +
                        "\u2022 <b>Consume</b> \u2014 cells chase and eat each other, energy transfers\n" +
                        "\u2022 <b>Ignore</b> \u2014 cells wander past each other as if alone\n\n" +
                        "Same code. Same prefab. Different data \u2192 different behavior. " +
                        "This is the full Sense \u2192 Think \u2192 Act loop."
                }
            }
        },

        // ──────────────────────────────────
        // Tutorial 7: The Ecosystem
        // ──────────────────────────────────
        new TutorialDef
        {
            title = "7 — The Ecosystem",
            description = "Three species, zero code changes.",
            pages = new[]
            {
                new PageDef
                {
                    title = "Three Species, Zero Code Changes",
                    body =
                        "Time to build a real ecosystem with three species:\n\n" +
                        "\u2022 <b>Flora</b> (green) \u2014 slow, resilient, repels everything\n" +
                        "\u2022 <b>Herbivore</b> (blue) \u2014 medium, eats flora, flees carnivores\n" +
                        "\u2022 <b>Carnivore</b> (red) \u2014 fast, fragile, hunts herbivores\n\n" +
                        "<b>No code will change.</b> Every species uses the exact same scripts. " +
                        "The only difference is data: different CellData assets (numbers) " +
                        "and different CellBehavior assets (rules)."
                },
                new PageDef
                {
                    title = "Define Three Species",
                    body =
                        "Create three <b>CellData</b> assets (Create > Data > CellData):\n\n" +
                        "<b>Flora:</b>\n" +
                        "\u2022 typeName: Flora, typeColor: Green\n" +
                        "\u2022 speed: 1, startingEnergy: 200, energyDecayRate: 2\n\n" +
                        "<b>Herbivore:</b>\n" +
                        "\u2022 typeName: Herbivore, typeColor: Blue\n" +
                        "\u2022 speed: 3, startingEnergy: 100, energyDecayRate: 5\n\n" +
                        "<b>Carnivore:</b>\n" +
                        "\u2022 typeName: Carnivore, typeColor: Red\n" +
                        "\u2022 speed: 5, startingEnergy: 80, energyDecayRate: 8\n\n" +
                        "Notice the tradeoffs: Flora is slow but lasts long. " +
                        "Carnivores are fast but burn energy quickly."
                },
                new PageDef
                {
                    title = "Define the Rules",
                    body =
                        "Create three <b>CellBehavior</b> assets (Create > Data > CellBehavior):\n\n" +
                        "<b>FloraBehavior:</b>\n" +
                        "\u2022 Herbivore \u2192 Repel\n" +
                        "\u2022 Carnivore \u2192 Repel\n\n" +
                        "<b>HerbivoreBehavior:</b>\n" +
                        "\u2022 Flora \u2192 Consume\n" +
                        "\u2022 Herbivore \u2192 Attract\n" +
                        "\u2022 Carnivore \u2192 Repel\n\n" +
                        "<b>CarnivoreBehavior:</b>\n" +
                        "\u2022 Herbivore \u2192 Consume\n" +
                        "\u2022 Flora \u2192 Ignore\n" +
                        "\u2022 Carnivore \u2192 Repel\n\n" +
                        "Rules are <b>asymmetric</b>: Herbivore\u2192Flora is Consume, " +
                        "Flora\u2192Herbivore is Repel. Each species has its own perspective."
                },
                new PageDef
                {
                    title = "Prefab Variants",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Right-click Cell prefab > <b>Create > Prefab Variant</b>. Name: <b>Flora</b>\n" +
                        "2. Open it, override CellData and CellBehavior on CellAgent, CellLifecycle, CellMotor\n" +
                        "3. Repeat for <b>Herbivore</b> and <b>Carnivore</b>\n\n" +
                        "<b>Prefab Variants</b> inherit everything from the base prefab \u2014 " +
                        "structure, components, children. They only store the differences. " +
                        "Change the base Cell prefab, all variants inherit the change."
                },
                new PageDef
                {
                    title = "Watch Life Unfold",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Clear the scene. Place: <b>~10 Flora, ~5 Herbivores, ~3 Carnivores</b>\n" +
                        "2. Hit <b>Play</b>\n\n" +
                        "<b>Observe:</b>\n" +
                        "\u2022 Flora drifts slowly, fleeing everything\n" +
                        "\u2022 Herbivores chase and eat flora \u2014 energy bar jumps on consume\n" +
                        "\u2022 Carnivores hunt herbivores \u2014 fast but burning energy fast\n" +
                        "\u2022 Carnivores starve if they can't catch herbivores\n\n" +
                        "<b>Experiment:</b> Change a number on any CellData asset during play. " +
                        "Make carnivores faster \u2014 they catch more. Make flora energy higher \u2014 " +
                        "herbivores thrive longer. <b>Behavior emerges from numbers.</b>"
                }
            }
        },

        // ──────────────────────────────────
        // Tutorial 8: Flocking
        // ──────────────────────────────────
        new TutorialDef
        {
            title = "8 — Flocking",
            description = "Emergent group behavior from three simple rules.",
            pages = new[]
            {
                new PageDef
                {
                    title = "Emergent Group Behavior",
                    body =
                        "In 1986, Craig Reynolds showed that realistic flocking emerges from " +
                        "three local rules:\n\n" +
                        "1. <b>Cohesion</b> \u2014 steer toward the average position of nearby flockmates\n" +
                        "2. <b>Alignment</b> \u2014 match the average heading of nearby flockmates\n" +
                        "3. <b>Separation</b> \u2014 steer away from flockmates that are too close\n\n" +
                        "No bird knows the shape of the flock. No central controller exists. " +
                        "Each agent follows three simple rules with local information \u2014 " +
                        "and complex group behavior emerges."
                },
                new PageDef
                {
                    title = "Create the Flocking Module",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Open the Cell base prefab\n" +
                        "2. Create an <b>empty child</b>, name it <b>Flocking</b>\n" +
                        "3. Add Component: <b>CellFlocking</b>\n" +
                        "4. Wire references:\n" +
                        "   \u2022 Cell Behavior: (set per variant later)\n" +
                        "   \u2022 Motor: CellMotor on root\n" +
                        "   \u2022 Sensor: TriggerSensor on Sense child\n" +
                        "   \u2022 Body Collider: SphereCollider on root (non-trigger)\n\n" +
                        "CellFlocking uses [DefaultExecutionOrder(-1)] to run <b>before</b> CellAgent. " +
                        "It sets a flock direction, which the agent can override if something more urgent happens."
                },
                new PageDef
                {
                    title = "Enable Flocking for Herbivores",
                    body =
                        "<b>Steps:</b>\n" +
                        "1. Open <b>HerbivoreBehavior</b>\n" +
                        "2. Change Herbivore \u2192 Herbivore rule from Attract to <b>Flock</b>\n" +
                        "3. Open the <b>Herbivore</b> prefab variant\n" +
                        "4. On the Flocking child, set CellBehavior to <b>HerbivoreBehavior</b>\n" +
                        "5. Tune weights on CellFlocking:\n" +
                        "   \u2022 cohesionWeight: 1\n" +
                        "   \u2022 alignmentWeight: 1\n" +
                        "   \u2022 separationWeight: 1.5\n\n" +
                        "<b>Why separation > 1?</b> Without enough separation, cells pile on top of each other. " +
                        "The body collider radius defines the personal space bubble."
                },
                new PageDef
                {
                    title = "Watch the Flock",
                    body =
                        "Hit <b>Play</b> and observe:\n\n" +
                        "\u2022 Herbivores now <b>move in groups</b> \u2014 cluster, align, maintain spacing\n" +
                        "\u2022 When a carnivore approaches, the nearest herbivore flees (Repel overrides Flock)\n" +
                        "\u2022 Flora still wanders independently. Carnivores hunt alone.\n\n" +
                        "<b>The layering:</b> Every frame, CellFlocking sets a flock direction. " +
                        "Then CellAgent checks for threats. If there's a carnivore (Repel, priority 1), " +
                        "it overrides. If not, the flock direction stands.\n\n" +
                        "<b>Experiment:</b>\n" +
                        "\u2022 Set all species to Flock with each other\n" +
                        "\u2022 Increase cohesionWeight \u2014 tighter groups\n" +
                        "\u2022 Increase separationWeight \u2014 more spread out\n" +
                        "\u2022 Set alignmentWeight to 0 \u2014 cluster without aligning headings"
                }
            }
        }
    };

    // ═══════════════════════════════════════
    // Generator
    // ═══════════════════════════════════════

    [MenuItem("Tutorials/Generate Living Cells Tutorials")]
    public static void Generate()
    {
        if (!EditorUtility.DisplayDialog(
            "Generate Tutorials",
            $"This will create tutorial assets in:\n{ROOT}\n\nExisting assets will be overwritten.",
            "Generate", "Cancel"))
            return;

        CreateFolders();

        var tutorialAssets = new List<Tutorial>();

        for (int i = 0; i < Tutorials.Length; i++)
        {
            var def = Tutorials[i];
            string folder = $"{ROOT}/T{i + 1}";
            EnsureFolder(folder);

            // Create pages
            var pages = new List<TutorialPage>();
            for (int p = 0; p < def.pages.Length; p++)
            {
                var pageDef = def.pages[p];
                string pagePath = $"{folder}/Page_{p + 1:D2}.asset";
                var page = CreatePage(pagePath, pageDef);
                if (page) pages.Add(page);
            }

            // Create tutorial
            string tutorialPath = $"{folder}/Tutorial_{i + 1}.asset";
            var tutorial = CreateTutorial(tutorialPath, def, pages.ToArray());
            if (tutorial) tutorialAssets.Add(tutorial);
        }

        // Create container
        CreateContainer($"{ROOT}/LivingCellsTOC.asset", tutorialAssets.ToArray());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CellTutorials] Generated {tutorialAssets.Count} tutorials in {ROOT}");
    }

    // ═══════════════════════════════════════
    // Asset Creation Helpers
    // ═══════════════════════════════════════

    static TutorialPage CreatePage(string path, PageDef def)
    {
        var page = ScriptableObject.CreateInstance<TutorialPage>();
        page.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(page, path);

        var so = new SerializedObject(page);

        // m_Paragraphs is a CollectionWrapper — inner array is m_Items
        var paragraphs = so.FindProperty("m_Paragraphs.m_Items");
        if (paragraphs == null)
        {
            Debug.LogWarning($"[CellTutorials] Cannot find m_Paragraphs.m_Items on TutorialPage. " +
                             $"Run 'Tutorials > Debug > Dump TutorialPage Properties' to find the correct name.");
            return page;
        }

        paragraphs.arraySize = 1;
        var p0 = paragraphs.GetArrayElementAtIndex(0);

        // Type = Narrative (0)
        SetEnum(p0, 0, "m_Type");

        // Title — public LocalizableString field on TutorialParagraph
        SetLocalizable(p0, def.title, "Title");

        // Text — public LocalizableString field on TutorialParagraph
        SetLocalizable(p0, def.body, "Text");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(page);
        return page;
    }

    static Tutorial CreateTutorial(string path, TutorialDef def, TutorialPage[] pages)
    {
        var tutorial = ScriptableObject.CreateInstance<Tutorial>();
        tutorial.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(tutorial, path);

        var so = new SerializedObject(tutorial);

        // TutorialTitle — public LocalizableString field on Tutorial
        SetLocalizable(so, def.title, "TutorialTitle");

        // Use active scene instead of creating a new one
        var sceneBehavior = so.FindProperty("m_SceneManagementBehavior");
        if (sceneBehavior != null)
            sceneBehavior.enumValueIndex = 1; // UseActiveScene

        // m_Pages is a CollectionWrapper — inner array is m_Items
        var pagesArray = so.FindProperty("m_Pages.m_Items");
        if (pagesArray != null)
        {
            pagesArray.arraySize = pages.Length;
            for (int i = 0; i < pages.Length; i++)
            {
                pagesArray.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            }
        }
        else
        {
            Debug.LogWarning("[CellTutorials] Cannot find m_Pages.m_Items on Tutorial.");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(tutorial);
        return tutorial;
    }

    static TutorialContainer CreateContainer(string path, Tutorial[] tutorials)
    {
        var container = ScriptableObject.CreateInstance<TutorialContainer>();
        container.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(container, path);

        var so = new SerializedObject(container);

        // Container title — public LocalizableString
        SetLocalizable(so, "Demo 1: Living Cells", "Title");

        // Sections — public Section[] field
        var sections = so.FindProperty("Sections");
        if (sections != null)
        {
            sections.arraySize = tutorials.Length;
            for (int i = 0; i < tutorials.Length; i++)
            {
                var section = sections.GetArrayElementAtIndex(i);

                // OrderInView for consistent ordering
                var order = section.FindPropertyRelative("OrderInView");
                if (order != null) order.intValue = i;

                // Tutorial — public field on Section
                var tutRef = section.FindPropertyRelative("Tutorial");
                if (tutRef != null)
                    tutRef.objectReferenceValue = tutorials[i];

                // Heading — public LocalizableString on Section
                SetLocalizable(section, Tutorials[i].title, "Heading");

                // Text — public LocalizableString on Section
                SetLocalizable(section, Tutorials[i].description, "Text");
            }
        }
        else
        {
            Debug.LogWarning("[CellTutorials] Cannot find Sections on TutorialContainer.");
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(container);
        return container;
    }

    // ═══════════════════════════════════════
    // Property Helpers (version-adaptive)
    // ═══════════════════════════════════════

    /// <summary>
    /// Sets a LocalizableString field by name on a parent SerializedProperty.
    /// LocalizableString stores its value in m_Untranslated.
    /// </summary>
    static void SetLocalizable(SerializedProperty parent, string value, string fieldName)
    {
        var field = parent.FindPropertyRelative(fieldName);
        if (field == null)
        {
            Debug.LogWarning($"[CellTutorials] Cannot find property '{fieldName}' on {parent.propertyPath}");
            return;
        }

        // Direct string field (backwards-compat fields)
        if (field.propertyType == SerializedPropertyType.String)
        {
            field.stringValue = value;
            return;
        }

        // LocalizableString — inner field is m_Untranslated
        var inner = field.FindPropertyRelative("m_Untranslated");
        if (inner != null)
        {
            inner.stringValue = value;
            return;
        }

        Debug.LogWarning($"[CellTutorials] Cannot set LocalizableString '{fieldName}' — no m_Untranslated found");
    }

    /// <summary>Overload for setting localizable strings on the root SerializedObject.</summary>
    static void SetLocalizable(SerializedObject so, string value, string fieldName)
    {
        var field = so.FindProperty(fieldName);
        if (field == null)
        {
            Debug.LogWarning($"[CellTutorials] Cannot find property '{fieldName}' on {so.targetObject.GetType().Name}");
            return;
        }

        if (field.propertyType == SerializedPropertyType.String)
        {
            field.stringValue = value;
            return;
        }

        var inner = field.FindPropertyRelative("m_Untranslated");
        if (inner != null)
        {
            inner.stringValue = value;
            return;
        }

        Debug.LogWarning($"[CellTutorials] Cannot set LocalizableString '{fieldName}' — no m_Untranslated found");
    }

    static void SetEnum(SerializedProperty parent, int value, string fieldName)
    {
        var field = parent.FindPropertyRelative(fieldName);
        if (field == null)
        {
            Debug.LogWarning($"[CellTutorials] Cannot find enum property '{fieldName}'");
            return;
        }

        if (field.propertyType == SerializedPropertyType.Enum)
            field.enumValueIndex = value;
        else if (field.propertyType == SerializedPropertyType.Integer)
            field.intValue = value;
    }

    // ═══════════════════════════════════════
    // Folder Helpers
    // ═══════════════════════════════════════

    static void CreateFolders()
    {
        EnsureFolder(ROOT);
        for (int i = 0; i < Tutorials.Length; i++)
            EnsureFolder($"{ROOT}/T{i + 1}");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    // ═══════════════════════════════════════
    // Debug Tools
    // ═══════════════════════════════════════

    [MenuItem("Tutorials/Debug/Dump TutorialPage Properties")]
    public static void DumpPageProperties()
    {
        var page = ScriptableObject.CreateInstance<TutorialPage>();
        DumpProperties("TutorialPage", page);
        Object.DestroyImmediate(page);
    }

    [MenuItem("Tutorials/Debug/Dump Tutorial Properties")]
    public static void DumpTutorialProperties()
    {
        var tutorial = ScriptableObject.CreateInstance<Tutorial>();
        DumpProperties("Tutorial", tutorial);
        Object.DestroyImmediate(tutorial);
    }

    [MenuItem("Tutorials/Debug/Dump TutorialContainer Properties")]
    public static void DumpContainerProperties()
    {
        var container = ScriptableObject.CreateInstance<TutorialContainer>();
        DumpProperties("TutorialContainer", container);
        Object.DestroyImmediate(container);
    }

    static void DumpProperties(string typeName, ScriptableObject obj)
    {
        var so = new SerializedObject(obj);
        var prop = so.GetIterator();
        var lines = new List<string> { $"--- {typeName} Properties ---" };
        while (prop.NextVisible(true))
            lines.Add($"  {prop.propertyPath}  ({prop.propertyType})");
        Debug.Log(string.Join("\n", lines));
    }
}
