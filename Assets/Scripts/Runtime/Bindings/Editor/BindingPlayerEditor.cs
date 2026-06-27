// ============================================
// BindingPlayer Editor
// ============================================
// Custom inspector for BindingPlayer. Draws the inherited Binding pipeline (source, master shape,
// shared envelope, trigger, registry) then the BindTarget list via the shared CardListDrawer.
//
// The host's own output range is intentionally NOT drawn — it stays at its 0..1 default so the host
// emits a normalized "master signal" and each card owns its own range. Must live in an Editor folder.
// ============================================

using System;
using UnityEditor;
using UnityEngine;

namespace Ludocore
{
    [CustomEditor(typeof(BindingPlayer))]
    public class BindingPlayerEditor : Editor
    {
        private SerializedProperty _triggerEvent;
        private SerializedProperty _registry;
        private SerializedProperty _source;
        private SerializedProperty _fallback;
        private SerializedProperty _fallbackPeriod;
        private SerializedProperty _curve;
        private SerializedProperty _inputRange;
        private SerializedProperty _attack;
        private SerializedProperty _release;
        private SerializedProperty _current;
        private SerializedProperty _activatedEvent;
        private SerializedProperty _deactivatedEvent;
        private SerializedProperty _targets;

        private bool _eventsFoldout;
        private CardListDrawer _cards;

        private void OnEnable()
        {
            _triggerEvent = serializedObject.FindProperty("triggerEvent");
            _registry = serializedObject.FindProperty("registry");
            _source = serializedObject.FindProperty("source");
            _fallback = serializedObject.FindProperty("fallback");
            _fallbackPeriod = serializedObject.FindProperty("fallbackPeriod");
            _curve = serializedObject.FindProperty("curve");
            _inputRange = serializedObject.FindProperty("inputRange");
            _attack = serializedObject.FindProperty("attack");
            _release = serializedObject.FindProperty("release");
            _current = serializedObject.FindProperty("current");
            _activatedEvent = serializedObject.FindProperty("activatedEvent");
            _deactivatedEvent = serializedObject.FindProperty("deactivatedEvent");
            _targets = serializedObject.FindProperty("targets");

            _cards = new CardListDrawer(
                serializedObject, _targets, typeof(BindTarget),
                menuPath: MenuPath,
                sectionTitle: "Outputs",
                addLabel: "+  Add Output",
                cardNoun: "Output");
        }

        private static string MenuPath(Type t)
        {
            var attr = (BindMenuAttribute)Attribute.GetCustomAttribute(t, typeof(BindMenuAttribute));
            return attr != null ? attr.Path : t.Name;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_source);
            if (_source.objectReferenceValue == null)
            {
                EditorGUILayout.PropertyField(_fallback);
                EditorGUILayout.PropertyField(_fallbackPeriod);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shape (master → 0..1)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_inputRange);
            EditorGUILayout.PropertyField(_curve);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Envelope (shared)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_attack);
            EditorGUILayout.PropertyField(_release);

            EditorGUILayout.Space();
            _cards.Draw();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Trigger / Registry", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_triggerEvent);
            EditorGUILayout.PropertyField(_registry);

            EditorGUILayout.Space();
            _eventsFoldout = EditorGUILayout.Foldout(_eventsFoldout, "Events", true);
            if (_eventsFoldout)
            {
                EditorGUILayout.PropertyField(_activatedEvent);
                EditorGUILayout.PropertyField(_deactivatedEvent);
            }

            if (Application.isPlaying && _current != null)
            {
                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Slider("Master Signal", _current.floatValue, 0f, 1f);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
