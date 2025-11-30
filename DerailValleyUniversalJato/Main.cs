using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

namespace DerailValleyUniversalJato;

public static class Main
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings Settings;
    private static GameObject _inGameWindow;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;

        Harmony? harmony = null;
        try
        {
            Settings = Settings.Load<Settings>(modEntry);

            if (Settings.LastJatoSettings != null)
                InGameWindow.NewSettings = Settings.LastJatoSettings.Clone();

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _inGameWindow = new GameObject("DerailValleyUniversalJato_CustomWindow");
            var windowComp = _inGameWindow.AddComponent<InGameWindow>();
            UnityEngine.Object.DontDestroyOnLoad(_inGameWindow);

            ModEntry.Logger.Log("DerailValleyUniversalJato started");
        }
        catch (Exception ex)
        {
            ModEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            harmony?.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        modEntry.OnUnload = Unload;
        return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        GUILayout.Label("Mod Settings", GUI.skin.label);

        // GUILayout.Label("How often to check if we need to emit (in seconds):");
        // Settings.CheckIntervalSeconds = float.Parse(GUILayout.TextField(Settings.CheckIntervalSeconds.ToString()));
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        Settings.Save(modEntry);
    }

    private static bool Unload(UnityModManager.ModEntry entry)
    {
        if (_inGameWindow != null)
            GameObject.Destroy(_inGameWindow);

        ModEntry.Logger.Log("DerailValleyUniversalJato stopped");
        return true;
    }
}
