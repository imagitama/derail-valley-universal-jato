using UnityEngine;
using DerailValleyModToolbar;

namespace DerailValleyUniversalJato;

public class BindingPanel : MonoBehaviour, IModToolbarPanel
{
    public void Window(Rect rect)
    {
        foreach (var kv in BindingsAPI.AllBindings)
        {
            var modEntry = kv.Key;
            var bindings = kv.Value;

            GUILayout.Label($"Mod: {modEntry.Info.DisplayName}");

            for (var i = 0; i < bindings.Count; i++)
                BindingsHelperUI.DrawBinding(bindings[i], index: i, OnUpdated: () => modEntry.OnSaveGUI(modEntry));
        }
    }
}