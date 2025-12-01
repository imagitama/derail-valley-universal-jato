using System;
using UnityEngine;
using UnityModManagerNet;
using DerailValleyModToolbar;
using DV;
using DV.ThingTypes;

namespace DerailValleyUniversalJato;

public class UniversalJatoPanel : MonoBehaviour, IModToolbarPanel
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public static JatoSettings NewSettings = new JatoSettings();
    private string _keyCodeText = NewSettings.KeyCode.ToString();
    private string _thrustText = NewSettings.Thrust.ToString();
    private string _positionXText = NewSettings.PositionX.ToString();
    private string _positionYText = NewSettings.PositionY.ToString();
    private string _positionZText = NewSettings.PositionZ.ToString();
    private string _rotationXText = NewSettings.RotationX.ToString();
    private string _rotationYText = NewSettings.RotationY.ToString();
    private string _rotationZText = NewSettings.RotationZ.ToString();
    private string _scaleText = NewSettings.Scale.ToString();
    private int? _selectedComponentIndex = null;

    (Transform transform, Rigidbody Rigidbody, TrainCar trainCar)? GetJatoTargetInfo()
    {
        if (PlayerManager.Car == null)
            return null;

        return (PlayerManager.Car.transform, PlayerManager.Car.rb, PlayerManager.Car);
    }

    void AddJato()
    {
        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        JatoManager.AddJato(transform, trainCar, rigidbody, NewSettings);

        _selectedComponentIndex = null;
    }

    void AddMirroredJato()
    {
        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        JatoManager.AddJato(transform, trainCar, rigidbody, NewSettings);

        var oldPosX = NewSettings.PositionX;

        NewSettings.PositionX *= -1;

        JatoManager.AddJato(transform, trainCar, rigidbody, NewSettings);

        NewSettings.PositionX = oldPosX;

        _selectedComponentIndex = null;
    }

    void UpdateJato(bool applyOffsets = true)
    {
        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        JatoManager.UpdateJato(transform, NewSettings, _selectedComponentIndex, applyOffsets);
    }

    void RemoveJato()
    {
        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        JatoManager.RemoveJato(transform, _selectedComponentIndex);

        _selectedComponentIndex = null;
    }

    void StopTrainMoving()
    {
        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        // fix null error from the game
        if (trainCar == null)
            return;

        Logger.Log("Stopping train moving");

        trainCar.StopMovement();
    }

    void DerailTrain()
    {
        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        Logger.Log("Derail train");

        trainCar.Derail();
    }

    private void EnableNoDerail()
    {
        Globals.G.GameParams.DerailStressThreshold = float.PositiveInfinity;
    }

    private void DisableNoDerail()
    {
        Globals.G.GameParams.DerailStressThreshold = Globals.G.GameParams.defaultStressThreshold;
    }

    private void UpdateDebugTexts()
    {
        var target = GetJatoTargetInfo();
        var comps = JatoManager.allJatos;

        if (target == null || _selectedComponentIndex == null)
        {
            foreach (var comp in comps)
                comp.DebugText = null;

            return;
        }

        var compsOnTarget = JatoManager.GetJatos(target!.Value.transform);

        for (var i = 0; i < compsOnTarget.Length; i++)
        {
            var comp = compsOnTarget[i];

            comp.DebugText = i == _selectedComponentIndex ? $"{i + 1}" : null;
        }
    }

    private void AutoPosition()
    {
        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        var positionX = 0f;
        var positionY = 0f;
        var positionZ = 0f;

        switch (trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                positionX = 1.125f;
                positionY = 1.51f;
                positionZ = -3f;
                break;
                // TODO
        }

        _positionXText = positionX.ToString();
        _positionYText = positionY.ToString();
        _positionZText = positionZ.ToString();

        NewSettings.PositionX = positionX;
        NewSettings.PositionY = positionY;
        NewSettings.PositionZ = positionZ;
    }

    private bool _isRecordingKey = false;

    public void Window(Rect rect)
    {
        var target = GetJatoTargetInfo();

        bool? alreadyHasJato = target != null ? JatoManager.GetDoesTargetHaveJato(target.Value.transform) : null;

        GUILayout.Label($"Target: {(target != null ? $"{target.Value.trainCar.carType} {target.Value.trainCar.carLivery.id}" : "(none)")}");

        if (target != null)
        {
            var count = JatoManager.GetJatoCount(target.Value.transform);

            if (count > 1)
            {
                for (var i = 0; i < count; i++)
                {
                    if (GUILayout.Button($"#{i + 1}{(i == _selectedComponentIndex ? " ✓" : "")}", GUILayout.Width(50f)))
                    {
                        if (_selectedComponentIndex == i)
                            _selectedComponentIndex = null;
                        else
                            _selectedComponentIndex = i;
                    }
                }
            }
            else
            {
                _selectedComponentIndex = null;
            }
        }
        else
        {
            _selectedComponentIndex = null;
        }

        UpdateDebugTexts();

        GUILayout.Label($"Key:");
        _keyCodeText = GUILayout.TextField(_keyCodeText);

        if (Enum.TryParse(_keyCodeText, out KeyCode keyCodeResult))
        {
            NewSettings.KeyCode = keyCodeResult;
        }
        GUILayout.Label($"(any Unity keycode eg. 'LeftShift', 'E', 'Enter', 'F12', 'UpArrow')");

        if (GUILayout.Button("Record Key"))
        {
            _isRecordingKey = true;
        }

        if (_isRecordingKey)
        {
            GUILayout.Label("Waiting for you to press a key...");

            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                NewSettings.KeyCode = e.keyCode;
                _keyCodeText = e.keyCode.ToString();
                _isRecordingKey = false;
            }
        }

        GUILayout.Label($"Thrust (newtons):");
        _thrustText = GUILayout.TextField(_thrustText);
        if (float.TryParse(_thrustText, out float thrustResult))
        {
            NewSettings.Thrust = thrustResult;
        }

        GUILayout.BeginHorizontal();
        string[] forces = ["50,000", "100,000", "250,000", "500,000", "1,000,000"];
        foreach (var force in forces)
        {
            if (GUILayout.Button(force))
            {
                _thrustText = force;
            }
        }
        GUILayout.EndHorizontal();

        NewSettings.ForceOn = GUILayout.Toggle(NewSettings.ForceOn, "Force on");
        NewSettings.RequireSittingInside = GUILayout.Toggle(NewSettings.RequireSittingInside, "Must be on train to use");
        NewSettings.HideBody = GUILayout.Toggle(NewSettings.HideBody, "Only show flame");

        GUILayout.Label($"Volume:");
        NewSettings.SoundVolume = GUILayout.HorizontalSlider(NewSettings.SoundVolume, 0f, 1f);

        GUI.enabled = alreadyHasJato == true;
        if (GUILayout.Button(_selectedComponentIndex != null ? $"Update #{_selectedComponentIndex + 1}" : "Update All"))
        {
            UpdateJato(applyOffsets: false);
        }
        GUI.enabled = true;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Position:");

        _positionXText = GUILayout.TextField(_positionXText, GUILayout.Width(40f));
        if (float.TryParse(_positionXText, out float positionXResult))
        {
            NewSettings.PositionX = positionXResult;
        }

        _positionYText = GUILayout.TextField(_positionYText, GUILayout.Width(40f));
        if (float.TryParse(_positionYText, out float positionYResult))
        {
            NewSettings.PositionY = positionYResult;
        }

        _positionZText = GUILayout.TextField(_positionZText, GUILayout.Width(40f));
        if (float.TryParse(_positionZText, out float positionZResult))
        {
            NewSettings.PositionZ = positionZResult;
        }

        if (GUILayout.Button("Auto", GUILayout.Width(50f)))
        {
            AutoPosition();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Rotation:");

        _rotationXText = GUILayout.TextField(_rotationXText, GUILayout.Width(40f));
        if (float.TryParse(_rotationXText, out float rotationXResult))
        {
            NewSettings.RotationX = rotationXResult;
        }

        _rotationYText = GUILayout.TextField(_rotationYText, GUILayout.Width(40f));
        if (float.TryParse(_rotationYText, out float rotationYResult))
        {
            NewSettings.RotationY = rotationYResult;
        }

        _rotationZText = GUILayout.TextField(_rotationZText, GUILayout.Width(40f));
        if (float.TryParse(_rotationZText, out float rotationZResult))
        {
            NewSettings.RotationZ = rotationZResult;
        }

        GUILayout.Label("", GUILayout.Width(50f));
        GUILayout.EndHorizontal();

        GUILayout.Label($"Scale:");
        _scaleText = GUILayout.TextField(_scaleText, GUILayout.Width(40f));
        if (float.TryParse(_scaleText, out float scaleResult))
        {
            NewSettings.Scale = scaleResult;
        }

        if (GUILayout.Button("Add"))
        {
            AddJato();
        }
        GUI.enabled = alreadyHasJato == true;
        if (GUILayout.Button(_selectedComponentIndex != null ? $"Replace #{_selectedComponentIndex + 1}" : "Replace All"))
        {
            UpdateJato();
        }
        if (GUILayout.Button(_selectedComponentIndex != null ? $"Remove #{_selectedComponentIndex + 1}" : "Remove All"))
        {
            RemoveJato();
        }
        GUI.enabled = true;

        GUILayout.Label("If position X is greater than 0:");
        GUI.enabled = NewSettings.PositionX != 0;
        if (GUILayout.Button("Add 2 Mirrored"))
        {
            AddMirroredJato();
        }
        GUI.enabled = true;

        GUILayout.Label($"");

        Main.settings.PreventDerail = GUILayout.Toggle(Main.settings.PreventDerail, "Prevent derail");

        if (Main.settings.PreventDerail)
            EnableNoDerail();
        else
            DisableNoDerail();

        if (GUILayout.Button("Stop Train Moving"))
        {
            StopTrainMoving();
        }
        if (GUILayout.Button("Derail Train"))
        {
            DerailTrain();
        }
        if (GUILayout.Button("Save Settings"))
        {
            Main.settings.LastJatoSettings = NewSettings.Clone();
        }
    }
}