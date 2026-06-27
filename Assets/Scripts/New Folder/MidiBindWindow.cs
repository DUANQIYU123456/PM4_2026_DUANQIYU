// ============================================
// MIDI Bind Window
// ============================================
// PURPOSE: Editor utility that listens to live MIDI input (in Edit Mode, via
//          Minis' direct event API), captures recent notes / CCs, and lets you
//          one-click create the full scaffolding for a binding:
//            - SO(s): GameEvent for notes (pressed/released) or FloatVariable for CCs
//            - Optionally: a new InputAction + binding in a chosen .inputactions asset
//            - A GameObject in the open scene, under a "_MidiBindings" root, with
//              the appropriate raiser component pre-wired to the SOs / action.
//
// USAGE: Tools → Ludocore → MIDI Bind. Press "Start Listening". Twist a knob or
//        hit a pad — it appears in the captures list. Click "Use" on a capture,
//        fill the name, pick a mode, hit Create.
//
// MODES:
//        Direct (events) — uses MidiNoteEventRaiser / MidiCcEventRaiser. Always
//          works; no PlayerInput pairing required.
//        Input Action     — uses MidiNoteActionRaiser / MidiCcActionRaiser.
//          Creates an action + binding in the selected InputActionAsset, then
//          auto-assigns the generated InputActionReference to the raiser.
//          REQUIRES that the asset is NOT owned by a PlayerInput with mismatched
//          control schemes (or that you have MidiDeviceAssigner solving pairing).
// ============================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minis;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Ludocore;

namespace Ludocore.EditorTools
{
    public class MidiBindWindow : EditorWindow
    {
        //==================== TYPES =====================
        private enum CaptureType { Note, Cc }
        private enum BindingMode { Direct, InputAction }

        private struct Capture
        {
            public CaptureType type;
            public int number;
            public int channel;
            public string deviceName;
            public DateTime time;
            public float lastValue; // for CCs: latest value; for Notes: last velocity
        }

        private const string SceneRootName = "_MidiBindings";
        private const int MaxCaptures = 50;

        //==================== STATE =====================
        // Capture/listen
        private bool _listening;
        private readonly List<Capture> _captures = new();
        private readonly HashSet<MidiDevice> _hooked = new();

        // Selected capture + form state
        private int _selectedIndex = -1;
        private string _bindingName = "";
        private BindingMode _mode = BindingMode.Direct;
        private bool _createReleasedEvent = true;

        // Action-mode targets
        private InputActionAsset _targetAsset;
        private string _mapName = "MIDI";

        // Folder
        private string _baseFolder = "Assets/MidiBindings";

        // Scroll
        private Vector2 _capturesScroll;

        //==================== MENU =====================
        [MenuItem("Tools/Ludocore/MIDI Bind...")]
        public static void Open() => GetWindow<MidiBindWindow>("MIDI Bind");

        //==================== LIFECYCLE =====================
        private void OnDisable() => StopListening();

        //==================== GUI =====================
        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space();
            DrawCaptures();
            EditorGUILayout.Space();
            DrawBindingForm();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MIDI Bind", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_listening)
                {
                    if (GUILayout.Button("Stop Listening", GUILayout.Height(22))) StopListening();
                    GUILayout.Label("● live", GUILayout.Width(60));
                }
                else
                {
                    if (GUILayout.Button("Start Listening", GUILayout.Height(22))) StartListening();
                }

                if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(22)))
                {
                    _captures.Clear();
                    _selectedIndex = -1;
                }
            }
            EditorGUILayout.HelpBox(
                _listening
                    ? "Listening live in the Editor. Twist a knob / hit a pad to capture it."
                    : "Press Start Listening, then twist or tap to capture.",
                MessageType.None);
        }

        private void DrawCaptures()
        {
            EditorGUILayout.LabelField($"Captured ({_captures.Count})", EditorStyles.miniBoldLabel);

            using (var scroll = new EditorGUILayout.ScrollViewScope(_capturesScroll, GUILayout.Height(180)))
            {
                _capturesScroll = scroll.scrollPosition;

                if (_captures.Count == 0)
                {
                    EditorGUILayout.LabelField("(empty)", EditorStyles.miniLabel);
                }
                else
                {
                    // Newest first
                    for (int i = _captures.Count - 1; i >= 0; i--)
                    {
                        var c = _captures[i];
                        bool selected = i == _selectedIndex;

                        GUIStyle rowStyle = selected ? (GUIStyle)"selectionRect" : GUIStyle.none;
                        using (new EditorGUILayout.HorizontalScope(rowStyle))
                        {
                            string label = c.type == CaptureType.Note
                                ? $"Note {c.number:D3} ({NoteName(c.number)})  ch{c.channel}  vel={c.lastValue:F2}"
                                : $"CC   {c.number:D3}                ch{c.channel}  val={c.lastValue:F2}";

                            GUILayout.Label(label, GUILayout.ExpandWidth(true));
                            GUILayout.Label(c.deviceName, EditorStyles.miniLabel, GUILayout.Width(170));

                            if (GUILayout.Button("Use", GUILayout.Width(50)))
                            {
                                _selectedIndex = i;
                                _bindingName = DefaultName(c);
                            }
                        }
                    }
                }
            }
        }

        private void DrawBindingForm()
        {
            EditorGUILayout.LabelField("New Binding", EditorStyles.miniBoldLabel);

            if (_selectedIndex < 0 || _selectedIndex >= _captures.Count)
            {
                EditorGUILayout.HelpBox("Click 'Use' on a captured control above to start a binding.", MessageType.Info);
                return;
            }

            var cap = _captures[_selectedIndex];
            EditorGUILayout.LabelField("Source",
                cap.type == CaptureType.Note
                    ? $"Note {cap.number} ({NoteName(cap.number)}) on ch{cap.channel}"
                    : $"CC {cap.number} on ch{cap.channel}");

            _bindingName = EditorGUILayout.TextField("Name", _bindingName);
            _baseFolder  = EditorGUILayout.TextField("Folder", _baseFolder);

            _mode = (BindingMode)EditorGUILayout.EnumPopup("Mode", _mode);

            if (cap.type == CaptureType.Note)
                _createReleasedEvent = EditorGUILayout.Toggle("Create Released SO", _createReleasedEvent);

            if (_mode == BindingMode.InputAction)
            {
                _targetAsset = (InputActionAsset)EditorGUILayout.ObjectField(
                    "Action Asset", _targetAsset, typeof(InputActionAsset), false);
                _mapName = EditorGUILayout.TextField("Action Map", _mapName);
                if (!_targetAsset)
                    EditorGUILayout.HelpBox(
                        "Pick an InputActionAsset. If you're using Starter Assets' InputSystem_Actions.inputactions, " +
                        "be sure the GameObject with PlayerInput has MidiDeviceAssigner — otherwise bindings resolve to 0 controls.",
                        MessageType.Warning);
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!CanCreate(cap)))
            {
                if (GUILayout.Button("Create binding", GUILayout.Height(28)))
                    CreateBinding(cap);
            }
        }

        private bool CanCreate(Capture cap)
        {
            if (string.IsNullOrWhiteSpace(_bindingName)) return false;
            if (string.IsNullOrWhiteSpace(_baseFolder)) return false;
            if (_mode == BindingMode.InputAction && !_targetAsset) return false;
            if (!EditorSceneManager.GetActiveScene().IsValid()) return false;
            return true;
        }

        //==================== LISTEN =====================
        private void StartListening()
        {
            if (_listening) return;
            foreach (var d in InputSystem.devices) TryHook(d);
            InputSystem.onDeviceChange += OnDeviceChange;
            _listening = true;
        }

        private void StopListening()
        {
            if (!_listening) return;
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var m in _hooked) Unhook(m);
            _hooked.Clear();
            _listening = false;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)        TryHook(device);
            else if (change == InputDeviceChange.Removed) TryUnhook(device);
        }

        private void TryHook(InputDevice device)
        {
            if (device is not MidiDevice midi) return;
            if (!_hooked.Add(midi)) return;
            midi.onWillNoteOn += OnNoteOn;
            midi.onWillControlChange += OnCc;
        }

        private void TryUnhook(InputDevice device)
        {
            if (device is MidiDevice midi && _hooked.Remove(midi)) Unhook(midi);
        }

        private void Unhook(MidiDevice midi)
        {
            midi.onWillNoteOn -= OnNoteOn;
            midi.onWillControlChange -= OnCc;
        }

        private void OnNoteOn(MidiNoteControl note, float velocity)
        {
            var midi = note.device as MidiDevice;
            AddOrUpdateCapture(new Capture
            {
                type = CaptureType.Note,
                number = note.noteNumber,
                channel = midi?.channel ?? -1,
                deviceName = midi?.description.product ?? "?",
                lastValue = velocity,
                time = DateTime.Now,
            });
        }

        private void OnCc(MidiValueControl cc, float value)
        {
            var midi = cc.device as MidiDevice;
            AddOrUpdateCapture(new Capture
            {
                type = CaptureType.Cc,
                number = cc.controlNumber,
                channel = midi?.channel ?? -1,
                deviceName = midi?.description.product ?? "?",
                lastValue = value,
                time = DateTime.Now,
            });
        }

        private void AddOrUpdateCapture(Capture c)
        {
            // Coalesce repeats — same type+number+channel+device gets its value updated instead
            // of spamming the list (especially for knob streams).
            for (int i = 0; i < _captures.Count; i++)
            {
                var existing = _captures[i];
                if (existing.type == c.type && existing.number == c.number
                    && existing.channel == c.channel && existing.deviceName == c.deviceName)
                {
                    existing.lastValue = c.lastValue;
                    existing.time = c.time;
                    _captures[i] = existing;
                    Repaint();
                    return;
                }
            }

            _captures.Add(c);
            if (_captures.Count > MaxCaptures) _captures.RemoveAt(0);
            Repaint();
        }

        //==================== CREATE =====================
        private void CreateBinding(Capture cap)
        {
            try
            {
                string deviceFolder = SanitizeFolderName(cap.deviceName);
                string folder = $"{_baseFolder.TrimEnd('/')}/{deviceFolder}";
                EnsureFolder(folder);

                // 1) Create SOs.
                ScriptableObject pressedOrValueSo;
                ScriptableObject releasedSo = null;

                if (cap.type == CaptureType.Note)
                {
                    pressedOrValueSo = CreateAsset<GameEvent>($"{folder}/{_bindingName}_Pressed.asset");
                    if (_createReleasedEvent)
                        releasedSo = CreateAsset<GameEvent>($"{folder}/{_bindingName}_Released.asset");
                }
                else
                {
                    pressedOrValueSo = CreateAsset<FloatVariable>($"{folder}/{_bindingName}.asset");
                }

                AssetDatabase.SaveAssets();

                // 2) If Input Action mode, add the action + binding to the chosen asset
                //    and find the auto-generated InputActionReference sub-asset.
                InputActionReference actionRef = null;
                if (_mode == BindingMode.InputAction)
                    actionRef = AddActionAndGetReference(cap, _targetAsset, _mapName, _bindingName);

                // 3) Create the GameObject in the active scene under _MidiBindings.
                var root = EnsureSceneRoot();
                var go = new GameObject($"Bind_{_bindingName}");
                Undo.RegisterCreatedObjectUndo(go, "Create MIDI Binding");
                go.transform.SetParent(root.transform, false);

                // 4) Attach the right raiser and wire references via SerializedObject.
                if (cap.type == CaptureType.Note)
                {
                    if (_mode == BindingMode.Direct)
                        WireDirectNoteRaiser(go, cap, (GameEvent)pressedOrValueSo, (GameEvent)releasedSo);
                    else
                        WireActionNoteRaiser(go, actionRef, (GameEvent)pressedOrValueSo, (GameEvent)releasedSo);
                }
                else
                {
                    if (_mode == BindingMode.Direct)
                        WireDirectCcRaiser(go, cap, (FloatVariable)pressedOrValueSo);
                    else
                        WireActionCcRaiser(go, actionRef, (FloatVariable)pressedOrValueSo);
                }

                EditorSceneManager.MarkSceneDirty(go.scene);
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);

                Debug.Log($"[MidiBind] Created '{_bindingName}' under {folder} ({_mode}, {cap.type}).", go);
            }
            catch (Exception e)
            {
                Debug.LogError($"[MidiBind] Create failed: {e}");
            }
        }

        //==================== ASSETS / FOLDER HELPERS =====================
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parts = folder.Split('/');
            string acc = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = acc + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(acc, parts[i]);
                acc = next;
            }
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            return so;
        }

        private static string SanitizeFolderName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Unknown";
            foreach (var ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
            return s.Trim();
        }

        //==================== SCENE HELPERS =====================
        private static GameObject EnsureSceneRoot()
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == SceneRootName) return root;

            var go = new GameObject(SceneRootName);
            Undo.RegisterCreatedObjectUndo(go, "Create MIDI Bindings root");
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        //==================== INPUT ACTION ASSET HELPERS =====================
        private static InputActionReference AddActionAndGetReference(
            Capture cap, InputActionAsset asset, string mapName, string actionName)
        {
            // Add/find the map.
            var map = asset.FindActionMap(mapName);
            if (map == null) map = asset.AddActionMap(mapName);

            // Generate the binding path with zero-padded 3-digit number.
            string path = cap.type == CaptureType.Note
                ? $"<MidiDevice>/note{cap.number:D3}"
                : $"<MidiDevice>/control{cap.number:D3}";

            // Notes → Button (discrete on/off).
            // CCs   → Value (continuous; .performed fires on every value change above the
            //                actuation threshold). expectedControlType = "Axis" matches
            //                Minis' MidiValueControl, which derives from AxisControl.
            InputActionType actionType = cap.type == CaptureType.Note
                ? InputActionType.Button
                : InputActionType.Value;
            string expectedControlType = cap.type == CaptureType.Note ? "Button" : "Axis";

            var action = map.FindAction(actionName);
            if (action == null)
            {
                action = map.AddAction(actionName, actionType);
                action.expectedControlType = expectedControlType;
            }
            else
            {
                // Catch mismatched-name reuse early — would otherwise silently produce
                // a Button action with an Axis binding (or vice versa).
                if (action.type != actionType)
                    throw new InvalidOperationException(
                        $"Action '{actionName}' already exists in map '{mapName}' as type {action.type}, " +
                        $"but this capture needs {actionType}. Pick a different name.");

                action.expectedControlType = expectedControlType;
            }

            // Avoid duplicate-binding spam on re-runs.
            bool alreadyBound = false;
            foreach (var b in action.bindings)
                if (b.path == path) { alreadyBound = true; break; }

            if (!alreadyBound) action.AddBinding(path);
            else Debug.Log($"[MidiBind] Action '{actionName}' already has binding {path} — skipping duplicate.");

            // Persist via JSON → reimport (this triggers InputActionImporter to regenerate
            // InputActionReference sub-assets).
            string assetPath = AssetDatabase.GetAssetPath(asset);
            File.WriteAllText(assetPath, asset.ToJson());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Find the regenerated reference for this action.
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var sub in subAssets)
            {
                if (sub is InputActionReference iar
                    && iar.action != null
                    && iar.action.actionMap != null
                    && iar.action.actionMap.name == mapName
                    && iar.action.name == actionName)
                    return iar;
            }

            Debug.LogWarning("[MidiBind] Created the action+binding but couldn't locate the generated InputActionReference. " +
                             "Drag it onto the raiser's 'Input Action' field manually.");
            return null;
        }

        //==================== WIRING (SerializedObject so undo + dirty work) =====================
        private static void WireDirectNoteRaiser(GameObject go, Capture cap, GameEvent pressed, GameEvent released)
        {
            var raiser = Undo.AddComponent<MidiNoteEventRaiser>(go);
            var so = new SerializedObject(raiser);
            so.FindProperty("noteNumber").intValue = cap.number;
            so.FindProperty("channel").intValue = -1;
            so.FindProperty("pressedGameEvent").objectReferenceValue = pressed;
            so.FindProperty("releasedGameEvent").objectReferenceValue = released;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireDirectCcRaiser(GameObject go, Capture cap, FloatVariable variable)
        {
            var raiser = Undo.AddComponent<MidiCcEventRaiser>(go);
            var so = new SerializedObject(raiser);
            so.FindProperty("ccNumber").intValue = cap.number;
            so.FindProperty("channel").intValue = -1;
            so.FindProperty("floatVariable").objectReferenceValue = variable;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireActionNoteRaiser(GameObject go, InputActionReference action, GameEvent pressed, GameEvent released)
        {
            var raiser = Undo.AddComponent<MidiNoteActionRaiser>(go);
            var so = new SerializedObject(raiser);
            so.FindProperty("inputAction").objectReferenceValue = action;
            so.FindProperty("pressedGameEvent").objectReferenceValue = pressed;
            so.FindProperty("releasedGameEvent").objectReferenceValue = released;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireActionCcRaiser(GameObject go, InputActionReference action, FloatVariable variable)
        {
            var raiser = Undo.AddComponent<MidiCcActionRaiser>(go);
            var so = new SerializedObject(raiser);
            so.FindProperty("inputAction").objectReferenceValue = action;
            so.FindProperty("floatVariable").objectReferenceValue = variable;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        //==================== NAMING =====================
        private static string DefaultName(Capture c)
            => c.type == CaptureType.Note
                ? $"Note_{NoteName(c.number)}"
                : $"CC{c.number:D3}";

        private static readonly string[] NoteNames = { "C","C#","D","D#","E","F","F#","G","G#","A","A#","B" };
        private static string NoteName(int n)
        {
            int octave = (n / 12) - 1;
            int idx = n % 12;
            return $"{NoteNames[idx]}{octave}";
        }
    }
}
