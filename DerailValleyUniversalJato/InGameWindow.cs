using System;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public class InGameWindow : MonoBehaviour
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private bool showGui = false;
    private Rect buttonRect = new Rect(60, 30, 20, 20); // TODO: avoid conflict with other mods (currently just DV Utilities mod)
    private Rect windowRect = new Rect(20, 30, 0, 0);
    private Rect scrollRect;
    private Vector2 scrollPosition;
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
    private string _volumeText = NewSettings.SoundVolume.ToString();
    private int? _selectedComponentIndex = null;

    public void Show()
    {

    }

    void OnGUI()
    {
        if (PlayerManager.PlayerTransform == null)
        {
            showGui = false;
            return;
        }

        if (!VRManager.IsVREnabled() && ScreenspaceMouse.Instance && !ScreenspaceMouse.Instance.on) return;

        if (GUI.Button(buttonRect, "UJ", new GUIStyle(GUI.skin.button) { fontSize = 16, clipping = TextClipping.Overflow })) showGui = !showGui;

        if (showGui)
        {
            float scale = 1.5f;
            Vector2 pivot = Vector2.zero; // top-left corner

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            windowRect = GUILayout.Window(700, windowRect, Window, "JATO");

            GUI.matrix = oldMatrix;
        }
    }

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
    }

    void StopTrainMoving()
    {
        var target = GetJatoTargetInfo();

        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        Logger.Log("Stopping train moving");

        trainCar.StopMovement();
    }

    void Window(int windowId)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(270 + GUI.skin.verticalScrollbar.fixedWidth), GUILayout.Height(scrollRect.height + GUI.skin.box.margin.vertical), GUILayout.MaxHeight(Screen.height - 130));
        GUILayout.BeginVertical();

        var target = GetJatoTargetInfo();

        GUILayout.Label($"Target: {(target != null ? $"{target.Value.trainCar.carType} {target.Value.trainCar.carLivery.id}" : "(none)")}");

        GUILayout.Label($"Key:");
        _keyCodeText = GUILayout.TextField(_keyCodeText);

        if (Enum.TryParse(_keyCodeText, out KeyCode keyCodeResult))
        {
            NewSettings.KeyCode = keyCodeResult;
        }
        GUILayout.Label($"(any Unity keycode eg. 'LeftShift', 'E', 'Enter', 'F12')");

        GUILayout.Label($"Thrust:");
        _thrustText = GUILayout.TextField(_thrustText);

        if (float.TryParse(_thrustText, out float thrustResult))
        {
            NewSettings.Thrust = thrustResult;
        }

        string[] forces = ["50,000", "100,000", "250,000", "500,000", "1,000,000"];

        foreach (var force in forces)
        {
            if (GUILayout.Button(force))
            {
                _thrustText = force;
            }
        }

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

        GUILayout.Label($"Scale:");

        _scaleText = GUILayout.TextField(_scaleText, GUILayout.Width(40f));
        if (float.TryParse(_scaleText, out float scaleResult))
        {
            NewSettings.Scale = scaleResult;
        }

        NewSettings.ForceOn = GUILayout.Toggle(NewSettings.ForceOn, "Force on");
        NewSettings.RequireSittingInside = GUILayout.Toggle(NewSettings.RequireSittingInside, "Must sit inside to use");

        GUILayout.Label($"Volume (percent):");
        _volumeText = GUILayout.TextField(_volumeText);
        if (float.TryParse(_rotationZText, out float volumeResult))
        {
            NewSettings.SoundVolume = volumeResult;
        }

        bool? alreadyHasJato = target != null ? JatoManager.GetDoesTargetHaveJato(target.Value.transform) : null;

        if (target != null)
        {
            var count = JatoManager.GetJatoCount(target.Value.transform);

            if (count > 1)
            {
                for (var i = 0; i < count; i++)
                {
                    if (GUILayout.Button($"Rocket #{i + 1}{(i == _selectedComponentIndex ? " - selected" : "")}"))
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

        if (GUILayout.Button("Add"))
        {
            AddJato();
        }
        GUI.enabled = alreadyHasJato == true;
        if (GUILayout.Button("Update Without Offsets"))
        {
            UpdateJato(applyOffsets: false);
        }
        if (GUILayout.Button("Update Everything"))
        {
            UpdateJato();
        }
        if (GUILayout.Button("Remove"))
        {
            RemoveJato();
        }
        GUI.enabled = true;

        GUILayout.Label($"");

        GUILayout.Label("Make position X greater than 0 and you can add two JATOs one on either side:");
        GUI.enabled = NewSettings.PositionX != 0;
        if (GUILayout.Button("Add 2 Mirrored"))
        {
            AddMirroredJato();
        }
        GUI.enabled = true;

        GUILayout.Label($"");

        if (GUILayout.Button("Stop Train Moving"))
        {
            StopTrainMoving();
        }
        if (GUILayout.Button("Save Settings"))
        {
            Main.Settings.LastJatoSettings = NewSettings.Clone();
        }

        GUILayout.EndVertical();
        if (Event.current.type == EventType.Repaint)
        {
            scrollRect = GUILayoutUtility.GetLastRect();
        }
        GUILayout.EndScrollView();
    }
}