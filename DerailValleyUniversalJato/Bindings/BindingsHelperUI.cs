using System;
using System.Collections.Generic;
using Rewired;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

[Serializable]
public static class BindingsHelperUI
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    private static int? _bindingIndexPressed;
    private static float _isPressedHintTimer = 0;
    private static int? _bindingIndexRecording;
    private static float _failsafeTimeLeft = 0f;

    public static void DrawBindings(List<BindingInfo> bindings, int? actionIdToAdd = null)
    {
        var controllers = BindingsHelper.GetAllControllers();

        if (controllers == null)
            return;

        GUILayout.BeginHorizontal();
        for (var i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];

            GUILayout.BeginVertical(GUILayout.Width(400f));

            DrawBinding(binding, index: i);

            bindings[i] = binding;

            if (binding.Removable)
                if (GUILayout.Button("Remove Binding"))
                    bindings.RemoveAt(i);

            GUILayout.EndVertical();

            if ((i + 1) % 3 == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }
        GUILayout.EndHorizontal();

        if (actionIdToAdd != null)
            if (GUILayout.Button("Add Binding"))
            {
                bindings.Add(
                    new BindingInfo()
                    {
                        ActionId = actionIdToAdd.Value,
                        ControllerId = 0,
                        ControllerType = ControllerType.Keyboard,
                        ControllerName = BindingsHelper.GetControllerNameFromType(ControllerType.Keyboard) ?? "",
                        ButtonName = "Space",
                        ButtonId = BindingsHelper.GetButtonId(ControllerType.Keyboard, 0, "Space")
                    }
                );
            }
    }

    public static void DrawBinding(BindingInfo binding, Dictionary<int, string>? labels = null, int? index = null, Action? OnUpdated = null)
    {
        var controllers = BindingsHelper.GetAllControllers();

        if (controllers == null)
            return;

        var bold = new GUIStyle(GUI.skin.label);
        bold.fontStyle = FontStyle.Bold;

        if (labels != null && index != null)
            GUILayout.Label($"{labels[binding.ActionId]}", bold);

        GUILayout.Label($"{binding.Label}", bold);

        GUILayout.Label($"  Controller: {binding.ControllerName} ({binding.ControllerType})");

        ControllerType newControllerType = binding.ControllerType;
        string? newControllerName = binding.ControllerName;
        int newControllerId = binding.ControllerId;

        foreach (var controller in controllers)
        {
            // TODO: why is controller ID always 0
            var isNowChecked = GUILayout.Toggle(binding.ControllerName == controller.name, controller.name);

            if (isNowChecked && binding.ControllerName != controller.name)
            {
                Logger.Log($"  Binding select controller {binding.ControllerName} => {controller.name}");
                newControllerType = controller.type;
                newControllerName = controller.name;
                newControllerId = controller.id;
            }
        }

        GUILayout.Label($"  Button name: {binding.ButtonName} ({binding.ButtonId})");
        // var newButtonName = GUILayout.TextField(binding.ButtonName);
        // var newButtonId = BindingsHelper.GetButtonId(newControllerType, newControllerId, newButtonName);

        string? newButtonName = null;
        int? newButtonId = null;

        if (_bindingIndexRecording != null && _bindingIndexRecording == index)
        {
            if (GUILayout.Button("Stop Recording"))
            {
                _bindingIndexRecording = null;
            }

            GUILayout.Label($"  Waiting for a button press ({Mathf.CeilToInt(_failsafeTimeLeft)})...");

            if (_failsafeTimeLeft > 0f)
            {
                _failsafeTimeLeft -= Time.deltaTime * 0.5f; // OnGUI much faster
                if (_failsafeTimeLeft < 0f)
                {
                    Logger.Log("Binding timer ended");
                    _failsafeTimeLeft = 0f;
                    _bindingIndexRecording = null;
                }
            }

            var result = BindingsHelper.GetAnyButtonPressedInfo(newControllerType);

            if (result != null)
            {
                var (pressedButtonName, pressedButtonId) = result.Value;
                Logger.Log($"User pressed controllerType={newControllerType} controllerName={newControllerName} name={pressedButtonName} id={pressedButtonId}");
                newButtonName = pressedButtonName;
                newButtonId = pressedButtonId;

                _bindingIndexRecording = null;
            }
        }
        else
        {
            if (GUILayout.Button("Record"))
            {
                Logger.Log("Binding timer started");
                _bindingIndexRecording = index;
                _failsafeTimeLeft = 5f;
            }
        }

        if (index != null && _bindingIndexRecording == null)
        {
            if (BindingsHelper.GetIsPressed(binding.ControllerType, binding.ControllerId, binding.ButtonId))
            {
                _bindingIndexPressed = index;
                _isPressedHintTimer = Time.time + 1f;
            }

            if (_bindingIndexPressed != null && Time.time >= _isPressedHintTimer)
            {
                _bindingIndexPressed = null;
            }

            if (_bindingIndexPressed == index)
            {
                GUILayout.Label("Pressed!");
            }
        }

        if (
            OnUpdated != null && (
                newControllerType != binding.ControllerType ||
                newControllerName != binding.ControllerName ||
                newControllerId != binding.ControllerId ||
                (newButtonName != null && newButtonName != binding.ButtonName) ||
                (newButtonId != null && newButtonId != binding.ButtonId)
            ))
        {
            //             Logger.Log($"CHANGED!!! " +
            // $"  {binding.ControllerType} => {newControllerType} --- " +
            // $"  {binding.ControllerName} => {newControllerName} --- " +
            // $"  {binding.ControllerId} => {newControllerId} --- " +
            // $"  {binding.ButtonName} => {newButtonName} --- " +
            // $"  {binding.ButtonId} => {newButtonId}");
            OnUpdated.Invoke();
        }

        binding.ControllerType = newControllerType;
        binding.ControllerName = newControllerName;
        binding.ControllerId = newControllerId;

        if (newButtonName != null)
            binding.ButtonName = newButtonName;
        if (newButtonId != null)
            binding.ButtonId = newButtonId.Value;
    }
}
