using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public class JatoSettings
{
    public float Thrust = 100000f;
    public KeyCode KeyCode = KeyCode.LeftShift;
    public bool ForceOn = false;
    // offsets
    public float PositionX = 0;
    public float PositionY = 3;
    public float PositionZ = -4;
    public float RotationX = 0;
    public float RotationY = 0;
    public float RotationZ = 0;
    public float SoundVolume = 1f;
    public bool RequireSittingInside = true;
    public float Scale = 1f;
    public bool HideBody = false;
    public JatoSettings Clone()
    {
        return new JatoSettings()
        {
            Thrust = Thrust,
            KeyCode = KeyCode,
            ForceOn = ForceOn,
            PositionX = PositionX,
            PositionY = PositionY,
            PositionZ = PositionZ,
            RotationX = RotationX,
            RotationY = RotationY,
            RotationZ = RotationZ,
            SoundVolume = SoundVolume,
            RequireSittingInside = RequireSittingInside,
            Scale = Scale,
            HideBody = HideBody
        };
    }
}

public static class JatoManager
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private static AssetBundle? _rocketAssetBundle;

    private static AssetBundle LoadBundle(string pathInsideAssetBundles)
    {
        var bundlePath = Path.Combine(Main.ModEntry.Path, "Dependencies/AssetBundles", pathInsideAssetBundles);

        Logger.Log($"Loading bundle from: {bundlePath}");

        if (!File.Exists(bundlePath))
            throw new Exception($"Asset bundle not found at {bundlePath}");

        return AssetBundle.LoadFromFile(bundlePath);
    }

    public static GameObject? RocketPrefab;

    private static GameObject GetRocketPrefab()
    {
        if (RocketPrefab != null)
            return RocketPrefab;

        _rocketAssetBundle = LoadBundle("rocket");

        var all = _rocketAssetBundle.LoadAllAssets<GameObject>();

        var newPrefab = all[0];

        Logger.Log($"Found rocket prefab: {newPrefab}");

        RocketPrefab = newPrefab;

        return RocketPrefab;
    }

    public static int GetJatoCount(Transform target)
    {
        return GetJatos(target).Count;
    }

    public static bool GetDoesTargetHaveJato(Transform target)
    {
        return GetJatoCount(target) > 0;
    }

    public static void ApplyOffsetsToRocket(Transform target, JatoSettings newSettings)
    {
        var pos = new Vector3(0 + newSettings.PositionX, 0 + newSettings.PositionY, 0 + newSettings.PositionZ);
        var rot = Quaternion.Euler(180f + newSettings.RotationX, newSettings.RotationY, newSettings.RotationZ);

        target.localPosition = pos;
        target.localRotation = rot;
        target.localScale = new Vector3(newSettings.Scale, newSettings.Scale, newSettings.Scale);
    }

    public static UniversalJato AddJato(Transform target, TrainCar trainCar, Rigidbody rigidbody, JatoSettings newSettings)
    {
        var existingComponents = GetJatos(target);

        if (existingComponents.Count > 0)
        {
            Logger.Log("Warning - JATO already exists (adding anyway)");
        }

        var prefab = GetRocketPrefab();

        var newObj = GameObject.Instantiate(prefab, target);

        var settingsToApply = newSettings.Clone();

        ApplyOffsetsToRocket(newObj.transform, settingsToApply);

        var component = newObj.gameObject.AddComponent<UniversalJato>();
        component.trainCar = trainCar;
        component.settings = settingsToApply;
        component.trainRigidbody = rigidbody;

        Logger.Log($"Added JATO as object {newObj} (with component {component}) to {target} (rigidbody {rigidbody})");

        return component;
    }

    public static void UpdateJato(Transform target, JatoSettings newSettings, int? componentIndex = null)
    {
        var components = GetJatos(target);

        if (components.Count == 0)
        {
            Logger.Log("Cannot update JATO - No components");
            return;
        }

        if (componentIndex != null)
        {
            var component = components[(int)componentIndex];
            components = [component];
        }

        var settingsToApply = newSettings.Clone();

        foreach (var component in components)
        {
            component.settings = settingsToApply;
        }

        ApplyOffsetsToRocket(target, settingsToApply);
    }
    public static void UpdateJato(UniversalJato component, JatoSettings newSettings)
    {
        var settingsToApply = newSettings.Clone();

        component.settings = settingsToApply;

        ApplyOffsetsToRocket(component.transform, settingsToApply);
    }

    public static void RemoveJato(Transform target, int? componentIndex = null)
    {
        var components = GetJatos(target);

        if (components.Count == 0)
        {
            Logger.Log("Cannot remove JATO - No components");
            return;
        }

        if (componentIndex != null)
        {
            var component = components[(int)componentIndex];
            components = [component];
        }

        var count = components.Count;

        foreach (var component in components)
            GameObject.Destroy(component.gameObject);

        Logger.Log($"Removed {count} JATO components from {target} (index={componentIndex})");
    }

    public static List<UniversalJato> GetAllJatos()
    {
        var jatos = SingletonBehaviour<CarSpawner>.Instance.AllCars
            .SelectMany(x => x.GetComponentsInChildren<UniversalJato>())
            .ToList();

        return jatos;
    }

    public static void RemoveAllJatos(Transform? target = null)
    {
        if (target != null)
        {
            RemoveJato(target);
            return;
        }

        var allJatos = GetAllJatos();

        Logger.Log($"Removing all JATOs ({allJatos.Count})...");

        // fix InvalidOperationException
        var jatosToRemove = allJatos.ToList();

        foreach (var jatoComponent in jatosToRemove)
        {
            allJatos.Remove(jatoComponent);

            GameObject.Destroy(jatoComponent.gameObject);
        }

        Logger.Log("All removed");
    }

    public static List<UniversalJato> GetJatos(Transform target)
    {
        return target.GetComponentsInChildren<UniversalJato>().ToList();
    }

    public static void Unload()
    {
        Logger.Log("Unload manager");
        RemoveAllJatos();
        _rocketAssetBundle?.Unload(unloadAllLoadedObjects: true);
    }
}