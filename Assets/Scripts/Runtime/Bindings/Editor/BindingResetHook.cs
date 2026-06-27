// ============================================
// Binding Reset Hook
// ============================================
// Editor glue for BindingPlayer.KeepChangesOnPlayExit. When the global toggle is OFF (the default),
// every BindingPlayer in the scene reverts its stateful outputs as play mode exits — so lights,
// materials, particle modules, and especially SHARED SO VARIABLES the outputs drove return to their
// authored state instead of polluting the project. Mirror of FeedbackResetHook.
// Toggle from: Tools > Ludocore > Bindings > Keep Changes On Play Exit.
// ============================================

using UnityEditor;
using UnityEngine;

namespace Ludocore
{
    [InitializeOnLoad]
    internal static class BindingResetHook
    {
        private const string MenuPath = "Tools/Ludocore/Bindings/Keep Changes On Play Exit";
        private const string PrefKey = "Ludocore.Bindings.KeepChangesOnPlayExit";

        static BindingResetHook()
        {
            BindingPlayer.KeepChangesOnPlayExit = EditorPrefs.GetBool(PrefKey, false);
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;
            if (BindingPlayer.KeepChangesOnPlayExit) return;

            // Include inactive — a player that captured state and was then disabled before play-exit must
            // still be reverted, or its lights/materials/SO variables leak into the saved scene/assets.
            foreach (BindingPlayer player in Object.FindObjectsByType<BindingPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                player.ResetTargets();
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            bool value = !BindingPlayer.KeepChangesOnPlayExit;
            BindingPlayer.KeepChangesOnPlayExit = value;
            EditorPrefs.SetBool(PrefKey, value);
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, BindingPlayer.KeepChangesOnPlayExit);
            return true;
        }
    }
}
