// ============================================
// Card List Drawer
// ============================================
// Reusable inspector renderer for a [SerializeReference] list of ICard objects — compact, colored,
// collapsible cards with a categorized "Add" menu. Shared by FeedbackPlayerEditor (Feedback cards)
// and BindingPlayerEditor (BindTarget cards) so the authoring surface stays identical and lives in
// one place.
//
// Card anatomy: a neutral card frame with a single color-tinted header strip (the category cue)
// carrying a mute toggle, "<n>. Name" ordinal title, an optional right-aligned badge, and the
// reorder/delete controls. The body (only when expanded) holds the label then the card's own fields.
// ============================================

using System;
using UnityEditor;
using UnityEngine;

namespace Ludocore
{
    public class CardListDrawer
    {
        private static readonly Color FallbackTint = new Color(0.45f, 0.45f, 0.5f);

        private readonly SerializedObject _so;
        private readonly SerializedProperty _list;
        private readonly Type _cardBaseType;
        private readonly Func<Type, string> _menuPath;
        private readonly Func<SerializedProperty, string> _badge;
        private readonly string _sectionTitle;
        private readonly string _addLabel;
        private readonly string _cardNoun;

        private GUIStyle _cardTitle;
        private GUIStyle _headerStrip;
        private GUIStyle _badgeStyle;

        private GUIStyle CardTitle =>
            _cardTitle ??= new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

        private GUIStyle HeaderStrip =>
            _headerStrip ??= new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(4, 4, 2, 2),
                margin = new RectOffset(0, 0, 0, 0)
            };

        private GUIStyle BadgeStyle =>
            _badgeStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };

        /// <param name="so">Owning serialized object (used to apply reorder/delete/add edits).</param>
        /// <param name="list">The [SerializeReference] list property to render.</param>
        /// <param name="cardBaseType">Base type whose concrete subclasses populate the Add menu.</param>
        /// <param name="menuPath">Maps a card type to its Add-menu path (e.g. via its menu attribute).</param>
        /// <param name="sectionTitle">Header label, e.g. "What happens" / "Outputs".</param>
        /// <param name="addLabel">Add-button label, e.g. "+  Add Feedback".</param>
        /// <param name="cardNoun">Singular noun for empty/missing text, e.g. "Feedback" / "Output".</param>
        /// <param name="badge">Optional per-card right-aligned mini-label (e.g. a delay). Return null/empty for none.</param>
        public CardListDrawer(SerializedObject so, SerializedProperty list, Type cardBaseType,
                              Func<Type, string> menuPath, string sectionTitle, string addLabel,
                              string cardNoun, Func<SerializedProperty, string> badge = null)
        {
            _so = so;
            _list = list;
            _cardBaseType = cardBaseType;
            _menuPath = menuPath;
            _sectionTitle = sectionTitle;
            _addLabel = addLabel;
            _cardNoun = cardNoun;
            _badge = badge;
        }

        public void Draw()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{_sectionTitle}  ({_list.arraySize})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(_list.arraySize == 0))
            {
                if (GUILayout.Button("Expand", EditorStyles.miniButtonLeft, GUILayout.Width(58)))
                    SetAllExpanded(true);
                if (GUILayout.Button("Collapse", EditorStyles.miniButtonRight, GUILayout.Width(64)))
                    SetAllExpanded(false);
            }
            EditorGUILayout.EndHorizontal();

            if (_list.arraySize == 0)
                EditorGUILayout.HelpBox($"No {_cardNoun.ToLowerInvariant()}s yet — add one below.", MessageType.None);

            for (int i = 0; i < _list.arraySize; i++)
                DrawCard(_list.GetArrayElementAtIndex(i), i);

            EditorGUILayout.Space(2);
            if (GUILayout.Button(_addLabel, GUILayout.Height(24)))
                ShowAddMenu();
        }

        private void DrawCard(SerializedProperty element, int index)
        {
            // A [SerializeReference] element whose type was renamed or removed deserializes to a null
            // managed reference, so its relative properties resolve to null. Render a small recoverable
            // card instead of dereferencing them — otherwise the whole inspector throws.
            SerializedProperty activeProp = element.FindPropertyRelative("active");
            if (activeProp == null)
            {
                DrawMissingCard(index);
                return;
            }

            var card = element.managedReferenceValue as ICard;
            SerializedProperty labelProp = element.FindPropertyRelative("label");

            Color prevColor = GUI.color;
            Color prevBg = GUI.backgroundColor;

            // Neutral card frame; only the header strip carries the category color.
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.backgroundColor = card != null ? card.CardColor : FallbackTint;
            EditorGUILayout.BeginHorizontal(HeaderStrip);
            GUI.backgroundColor = prevBg;

            activeProp.boolValue = EditorGUILayout.Toggle(activeProp.boolValue, GUILayout.Width(16));

            // Dim everything after the mute toggle when the card is muted.
            if (!activeProp.boolValue)
                GUI.color = new Color(prevColor.r, prevColor.g, prevColor.b, 0.5f);

            string name = card != null ? card.DisplayName : _cardNoun;
            string shown = labelProp != null && !string.IsNullOrWhiteSpace(labelProp.stringValue)
                ? labelProp.stringValue
                : name;
            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, $"{index + 1}.  {shown}", true, CardTitle);

            string badge = _badge?.Invoke(element);
            if (!string.IsNullOrEmpty(badge))
                GUILayout.Label(badge, BadgeStyle, GUILayout.Width(48));

            using (new EditorGUI.DisabledScope(index == 0))
                if (GUILayout.Button("▲", EditorStyles.miniButtonLeft, GUILayout.Width(24)))
                    Move(index, index - 1);

            using (new EditorGUI.DisabledScope(index == _list.arraySize - 1))
                if (GUILayout.Button("▼", EditorStyles.miniButtonMid, GUILayout.Width(24)))
                    Move(index, index + 1);

            if (GUILayout.Button("✕", EditorStyles.miniButtonRight, GUILayout.Width(24)))
                Delete(index);

            EditorGUILayout.EndHorizontal();

            if (element.isExpanded)
            {
                EditorGUILayout.Space(2);
                EditorGUI.indentLevel++;
                if (labelProp != null) EditorGUILayout.PropertyField(labelProp);
                DrawChildren(element);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            GUI.color = prevColor;
        }

        // Recovery card for an element whose card type can no longer be resolved (renamed, removed, or
        // moved between assemblies). Lets the user see and delete it without the inspector erroring.
        private void DrawMissingCard(int index)
        {
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.5f);
            EditorGUILayout.BeginHorizontal(HeaderStrip);
            GUI.backgroundColor = prevBg;

            EditorGUILayout.LabelField($"{index + 1}.  ⚠ Missing {_cardNoun.ToLowerInvariant()} type", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(24)))
                Delete(index);

            EditorGUILayout.EndHorizontal();
        }

        // Draws every serialized child of the card except the ones already surfaced on the header
        // (active) or above (label). Base-class fields come first, then the card's own fields —
        // matching their declaration order.
        private static void DrawChildren(SerializedProperty element)
        {
            SerializedProperty end = element.GetEndProperty();
            SerializedProperty child = element.Copy();
            bool enter = true;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                if (child.name == "active" || child.name == "label") continue;
                EditorGUILayout.PropertyField(child, true);
            }
        }

        private void SetAllExpanded(bool expanded)
        {
            for (int i = 0; i < _list.arraySize; i++)
                _list.GetArrayElementAtIndex(i).isExpanded = expanded;
        }

        // Reorder/delete mutate the array then bail out of the current GUI pass via ExitGUI — the
        // pending layout groups are unwound by the ExitGUIException, same as the original editor.
        private void Move(int from, int to)
        {
            _list.MoveArrayElement(from, to);
            _so.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        private void Delete(int index)
        {
            _list.DeleteArrayElementAtIndex(index);
            _so.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        private void ShowAddMenu()
        {
            var menu = new GenericMenu();
            foreach (Type t in TypeCache.GetTypesDerivedFrom(_cardBaseType))
            {
                if (t.IsAbstract) continue;
                string path = _menuPath != null ? _menuPath(t) : t.Name;
                Type captured = t;
                menu.AddItem(new GUIContent(path), false, () => AddCard(captured));
            }
            menu.ShowAsContext();
        }

        private void AddCard(Type t)
        {
            _so.Update();
            int idx = _list.arraySize;
            _list.InsertArrayElementAtIndex(idx);
            _list.GetArrayElementAtIndex(idx).managedReferenceValue = Activator.CreateInstance(t);
            _so.ApplyModifiedProperties();
        }
    }
}
