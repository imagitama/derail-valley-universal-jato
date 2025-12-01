using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using DerailValleyModToolbar;

namespace DerailValleyUniversalJato;

public static class Main
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings settings;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;

        Harmony? harmony = null;
        try
        {
            settings = Settings.Load<Settings>(modEntry);
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            if (settings.LastJatoSettings != null)
                UniversalJatoPanel.NewSettings = settings.LastJatoSettings.Clone();

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            ModToolbarAPI
                .Register(modEntry)
                .AddPanelControl(
                    label: "Universal JATO",
                    icon: "icon.png",
                    tooltip: "Configure Universal JATO",
                    type: typeof(UniversalJatoPanel),
                    title: "Universal JATO",
                    width: 400)
                .Finish();

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

    static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Draw(modEntry);
    }

    static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Save(modEntry);
    }

    private static bool Unload(UnityModManager.ModEntry modEntry)
    {
        ModToolbarAPI.Unregister(modEntry);

        JatoManager.RemoveAllJatos();

        ModEntry.Logger.Log("DerailValleyUniversalJato stopped");
        return true;
    }
}
