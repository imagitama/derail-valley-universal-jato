using System;
using UnityEngine;
using UnityModManagerNet;
using DerailValleyModToolbar;
using DV;
using DV.ThingTypes;
using System.Linq;
using System.Collections.Generic;

namespace DerailValleyUniversalJato;

public class JatoSettingsText
{
    public string Thrust = "";
    public string PositionX = "";
    public string PositionY = "";
    public string PositionZ = "";
    public string RotationX = "";
    public string RotationY = "";
    public string RotationZ = "";
    public string SoundVolume = "";
    public string Scale = "";
    public JatoSettingsText(JatoSettings inputSettings)
    {
        Thrust = inputSettings.Thrust.ToString();
        PositionX = inputSettings.PositionX.ToString();
        PositionY = inputSettings.PositionY.ToString();
        PositionZ = inputSettings.PositionZ.ToString();
        RotationX = inputSettings.RotationX.ToString();
        RotationY = inputSettings.RotationY.ToString();
        RotationZ = inputSettings.RotationZ.ToString();
        SoundVolume = inputSettings.SoundVolume.ToString();
        Scale = inputSettings.Scale.ToString();
    }
}

public class UniversalJatoPanel : MonoBehaviour, IModToolbarPanel
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private JatoSettings? _settingsEditingDraft = null;
    private JatoSettingsText _settingsEditingText = new JatoSettingsText(new JatoSettings());
    private int? _selectedComponentIndex = null;
    private bool _showBasics = true;
    private bool _showAdvanced = false;
    private bool _showSettings = true;
    private bool _snapping = true;
    // standard values
    private float _standardThrust = 100000f;
    private float _frontPositionX = 0f;
    private float _frontPositionY = 0f;
    private float _frontPositionZ = 0f;
    private float _rearPositionX = 0f;
    private float _rearPositionY = 0f;
    private float _rearPositionZ = 0f;

    void Start()
    {
        Logger.Log("[Panel] Start");
        PlayerManager.CarChanged += OnCarChanged;

        if (Main.settings.PreventDerail)
            TrainCarHelper.EnableNoDerail();
        else
            TrainCarHelper.DisableNoDerail();
    }

    void OnDestroy()
    {
        Logger.Log("[Panel] Destroy");
        PlayerManager.CarChanged -= OnCarChanged;
    }

    void OnCarChanged(TrainCar newRrainCar)
    {
        Logger.Log("[Panel] Car changed - clearing");
        UnselectJatos();

        if (Main.settings.PreventDerail)
            TrainCarHelper.EnableNoDerail();
        else
            TrainCarHelper.DisableNoDerail();
    }

    (Transform transform, Rigidbody rigidbody, TrainCar trainCar)? GetJatoTargetInfo()
    {
        if (PlayerManager.Car == null)
            return null;

        return (PlayerManager.Car.transform, PlayerManager.Car.rb, PlayerManager.Car);
    }

    Transform? GetJatoTargetTransform()
    {
        if (PlayerManager.Car == null)
            return null;

        return PlayerManager.Car.transform;
    }

    void AddJato()
    {
        Logger.Log("[Panel] Add jato");

        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var (transform, rigidbody, trainCar) = target.Value;

        JatoManager.AddJato(transform, trainCar, rigidbody, new JatoSettings()
        {
            KeyCode = KeyCode.LeftShift
        });

        _selectedComponentIndex = null;
    }

    void RemoveJato(int jatoIndex)
    {
        Logger.Log($"[Panel] Remove jato #{jatoIndex}");

        var target = GetJatoTargetTransform();
        if (target == null)
            return;

        JatoManager.RemoveJato(target, jatoIndex);
    }

    void StopTrainMoving()
    {
        Logger.Log("[Panel] Stopping train moving");

        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        target.Value.trainCar.StopMovement();
    }

    void DerailTrain()
    {
        Logger.Log("[Panel] Derail train");

        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        target.Value.trainCar.Derail();
    }

    private void UpdateDebugTexts()
    {
        var target = GetJatoTargetInfo();
        var comps = JatoManager.GetAllJatos();

        if (target == null || _selectedComponentIndex == null)
        {
            foreach (var comp in comps)
                comp.DebugText = null;

            return;
        }

        var compsOnTarget = JatoManager.GetJatos(target!.Value.transform);

        for (var i = 0; i < compsOnTarget.Count; i++)
        {
            var comp = compsOnTarget[i];

            comp.DebugText = i == _selectedComponentIndex ? $"{i + 1}" : null;
        }
    }

    public Vector3? GetStandardRearJatoPosition(TrainCar trainCar)
    {
        switch (trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                return new Vector3(1.125f, 1.51f, -3f);
            case TrainCarType.LocoDH4:
                return new Vector3(1.122f, 1.55f, -5.07f);
        }

        return null;
    }

    public Vector3? GetStandardFrontJatoPosition(TrainCar trainCar)
    {
        switch (trainCar.carType)
        {
            case TrainCarType.LocoShunter:
                return new Vector3(1.125f, 1.51f, 3f);
            case TrainCarType.LocoDH4:
                return new Vector3(1.122f, 1.55f, 5f);
        }

        return null;
    }

    public void Window(Rect rect)
    {
        UpdateDebugTexts();

        var target = GetJatoTargetInfo();
        var speed = target.HasValue ? TrainCarHelper.GetForwardSpeed(target.Value.trainCar) : null;

        GUILayout.Label($"Train Car: {(target != null ?
                $"{target.Value.trainCar.carType}{(speed != null ? $" ({speed:F1} kph)" : "")}" : "(none)")}"); ;

        if (GUILayout.Button("<b>Basics</b>", GUI.skin.label)) _showBasics = !_showBasics;
        if (_showBasics) DrawBasics(target != null ? target.Value.transform : null);

        if (GUILayout.Button("<b>Advanced</b>", GUI.skin.label)) _showAdvanced = !_showAdvanced;
        if (_showAdvanced) DrawAdvanced(rect);

        if (GUILayout.Button("<b>Settings</b>", GUI.skin.label)) _showSettings = !_showSettings;
        if (_showSettings) DrawSettings();
    }

    void DrawBasics(Transform? target)
    {
        GUI.enabled = target != null;

        void OnChange() => HydrateStandardJatos(target);

        GUILayout.Label($"Thrust (newtons): {_standardThrust}");

        GUILayout.BeginHorizontal();
        GUILayout.Label("10000", GUILayout.Width(40));
        float thrustStepped = _snapping ? Mathf.Round(_standardThrust / 10000f) * 10000f : _standardThrust;
        var newThrust = GUILayout.HorizontalSlider(thrustStepped, 10000f, 1000000f);
        GUILayout.Label("1000000", GUILayout.Width(70));
        GUILayout.EndHorizontal();

        if (newThrust != _standardThrust)
        {
            _standardThrust = newThrust;
            OnChange();
        }

        if (GUILayout.Button("Add 2 Rear JATOs (Shift)"))
        {
            AddStandardRearJatos();
        }

        DrawStandardJatoOffsetSlider("X", ref _rearPositionX, 0f, 5f, OnChange);
        DrawStandardJatoOffsetSlider("Y", ref _rearPositionY, 0f, 5f, OnChange);
        DrawStandardJatoOffsetSlider("Z", ref _rearPositionZ, -10f, 10f, OnChange);

        if (GUILayout.Button("Add 2 Front JATOs (Ctrl)"))
        {
            AddStandardFrontJatos();
        }

        DrawStandardJatoOffsetSlider("X", ref _frontPositionX, 0f, 5f, OnChange);
        DrawStandardJatoOffsetSlider("Y", ref _frontPositionY, 0f, 5f, OnChange);
        DrawStandardJatoOffsetSlider("Z", ref _frontPositionZ, -10f, 10f, OnChange);

        if (GUILayout.Button("Remove All JATOs From Train"))
        {
            RemoveAllJatosFromCurrentTrain();
        }

        GUI.enabled = true;
    }

    void DrawStandardJatoOffsetSlider(string label, ref float value, float min, float max, Action onChange)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: ", GUILayout.Width(50));
        GUILayout.Label($"{min}m", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / 0.05f) * 0.05f : newValueRaw;
        GUILayout.Label($"{max}m", GUILayout.Width(40));
        GUILayout.EndHorizontal();

        if (newValue != value)
        {
            value = newValue;
            onChange();
        }
    }

    void DrawAdvanced(Rect rect)
    {
        DrawJatosEditor(rect);
    }

    void DrawJatosEditor(Rect rect)
    {
        var target = GetJatoTargetInfo();

        if (target == null)
        {
            GUILayout.Label("Must select a train car first");
            _selectedComponentIndex = -1;
            return;
        }

        var jatos = JatoManager.GetJatos(target.Value.transform);

        var bold = new GUIStyle(GUI.skin.label);
        bold.fontStyle = FontStyle.Bold;

        if (jatos.Count > 0)
        {
            for (var i = 0; i < jatos.Count; i++)
            {
                var jato = jatos[i];

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Jato {i + 1}{(jato.side != null ? $" ({jato.side})" : "")}{(i == _selectedComponentIndex ? " ✓" : "")}", bold);
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    if (_selectedComponentIndex == i)
                        UnselectJatos();
                    else
                        SelectJato(i, jato);
                }

                if (GUILayout.Button("Mirror", GUILayout.Width(50)))
                {
                    MirrorJato(i, jato);
                }

                if (GUILayout.Button("Delete", GUILayout.Width(50)))
                {
                    RemoveJato(i);
                    UnselectJatos();
                }
                GUILayout.EndHorizontal();

                if (_selectedComponentIndex == i)
                {
                    DrawJatoEditor(i, jato);
                }
                else
                {
                    GUIStyle small = new GUIStyle(GUI.skin.label);
                    small.fontSize = 10;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Thrust: {jato.settings.Thrust}", small, GUILayout.Width(rect.width * 0.3f));
                    GUILayout.Label($"Position: {jato.settings.PositionX:F1}, {jato.settings.PositionY:F1}, {jato.settings.PositionZ:F1}", small, GUILayout.Width(rect.width * 0.3f));
                    GUILayout.Label($"Key: {jato.settings.KeyCode}", small, GUILayout.Width(rect.width * 0.3f));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("", small, GUILayout.Width(rect.width * 0.3f));
                    GUILayout.Label($"Rotation: {jato.settings.RotationX:F1}, {jato.settings.RotationY:F1}, {jato.settings.RotationZ:F1}", small, GUILayout.Width(rect.width * 0.3f));
                    GUILayout.Label("", small, GUILayout.Width(rect.width * 0.3f));
                    GUILayout.EndHorizontal();
                }
            }
        }
        else
        {
            GUILayout.Label("No JATOs found on this train car");

            _selectedComponentIndex = null;
        }

        if (GUILayout.Button("ADD NEW JATO"))
        {
            AddBasicJato();
        }
    }

    void SelectJato(int jatoIndex, UniversalJato jato)
    {
        Logger.Log($"[Panel] Select jato #{jatoIndex}");

        _selectedComponentIndex = jatoIndex;

        _settingsEditingDraft = jato.settings.Clone();

        _settingsEditingText = new JatoSettingsText(_settingsEditingDraft);
    }

    void UnselectJatos()
    {
        _settingsEditingDraft = null;
        _selectedComponentIndex = null;
    }

    void DrawJatoPositionInput(string label, ref string textValue, ref float value, float min, float max, Action onChange)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: ", GUILayout.Width(50));
        GUILayout.Label($"{min}m", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / 0.05f) * 0.05f : newValueRaw;
        GUILayout.Label($"{max}m", GUILayout.Width(40));
        var newTextValue = GUILayout.TextField(textValue, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        if (newValue != value)
        {
            value = newValue;
            textValue = newValue.ToString();
            onChange();
            return;
        }
        if (newTextValue != textValue)
        {
            textValue = newTextValue;
            if (float.TryParse(newTextValue, out float floatResult))
            {
                if (floatResult != value)
                {
                    value = floatResult;
                    onChange();
                }
            }
        }
    }

    void DrawJatoRotationInput(string label, ref string textValue, ref float value, Action onChange)
    {
        var min = 0;
        var max = 360;
        var snapAmount = 5f;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: ", GUILayout.Width(50));
        GUILayout.Label($"0", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / snapAmount) * snapAmount : newValueRaw;
        GUILayout.Label($"360", GUILayout.Width(40));
        var newTextValue = GUILayout.TextField(textValue, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        if (newValue != value)
        {
            value = newValue;
            textValue = newValue.ToString();
            onChange();
            return;
        }
        if (newTextValue != textValue)
        {
            textValue = newTextValue;
            if (float.TryParse(newTextValue, out float floatResult))
            {
                if (floatResult != value)
                {
                    value = floatResult;
                    onChange();
                }
            }
        }
    }

    void DrawJatoScaleInput(string label, ref string textValue, ref float value, Action onChange)
    {
        var min = 0.1f;
        var max = 5;
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: ", GUILayout.Width(50));
        GUILayout.Label($"{min}m", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / 0.05f) * 0.05f : newValueRaw;
        GUILayout.Label($"{max}m", GUILayout.Width(40));
        var newTextValue = GUILayout.TextField(textValue, GUILayout.Width(50));
        GUILayout.EndHorizontal();

        if (newValue != value)
        {
            value = newValue;
            textValue = newValue.ToString();
            onChange();
            return;
        }
        if (newTextValue != textValue)
        {
            textValue = newTextValue;
            if (float.TryParse(newTextValue, out float floatResult))
            {
                if (floatResult != value)
                {
                    value = floatResult;
                    onChange();
                }
            }
        }
    }

    void MirrorJato(int jatoIndex, UniversalJato jato)
    {
        Logger.Log($"Mirroring #{jatoIndex}...");

        var target = GetJatoTargetInfo();

        if (target == null)
            return;

        var existingJato = JatoManager.GetJatos(target.Value.transform)[jatoIndex];

        if (existingJato == null)
            throw new Exception($"Could not find jato at index {jatoIndex} on transform {target.Value.transform} (count: {JatoManager.GetJatoCount(target.Value.transform)})");

        var newSettings = existingJato.settings.Clone();
        newSettings.PositionX *= -1;
        newSettings.RotationY *= -1;

        JatoManager.AddJato(target.Value.transform, target.Value.trainCar, target.Value.rigidbody, newSettings);
    }

    void AddBasicJato()
    {
        Logger.Log("[Panel] Adding basic rocket...");

        var target = GetJatoTargetInfo();

        if (target == null)
            return;

        var newSettings = new JatoSettings();

        JatoManager.AddJato(target.Value.transform, target.Value.trainCar, target.Value.rigidbody, newSettings);
    }

    private bool _showKeys;
    private KeyCode[] _allKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

    void DrawJatoKeybindingEditor(int jatoIndex, UniversalJato jato)
    {
        if (_settingsEditingDraft == null)
            return;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Key:");

        if (GUILayout.Button(_settingsEditingDraft.KeyCode.ToString(), GUILayout.Width(100)))
            _showKeys = !_showKeys;

        GUILayout.EndHorizontal();

        if (_showKeys)
        {
            int columns = 4;
            int count = 0;

            GUILayout.BeginHorizontal();
            foreach (var k in _allKeys)
            {
                if (GUILayout.Button(k.ToString(), GUILayout.Width(100)))
                {
                    _settingsEditingDraft.KeyCode = k;
                    _showKeys = false;
                    HydrateJato(jatoIndex, jato);
                }

                count++;
                if (count % columns == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }
            GUILayout.EndHorizontal();
        }
    }

    void DrawJatoThrustEditor(int jatoIndex, UniversalJato jato)
    {
        if (_settingsEditingDraft == null)
            return;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Thrust:", GUILayout.Width(75));
        _settingsEditingText.Thrust = GUILayout.TextField(_settingsEditingText.Thrust, GUILayout.Width(100));
        if (float.TryParse(_settingsEditingText.Thrust, out float thrustResult))
        {
            var needsApplying = thrustResult != _settingsEditingDraft.Thrust;

            _settingsEditingDraft.Thrust = thrustResult;

            if (needsApplying)
                HydrateJato(jatoIndex, jato);
        }

        Dictionary<string, float> forces = new Dictionary<string, float>()
        {
            { "100k", 100000f },
            { "250k", 250000f },
            { "500k", 500000f },
            { "1m", 1000000f },
        };

        foreach (var kv in forces)
        {
            if (GUILayout.Button(kv.Key, GUILayout.Width(50)))
            {
                _settingsEditingText.Thrust = kv.Value.ToString();
                // is automatically parsed and applied
            }
        }
        GUILayout.EndHorizontal();
    }

    void DrawJatoEditor(int jatoIndex, UniversalJato jato)
    {
        if (_settingsEditingDraft == null)
            return;

        DrawJatoKeybindingEditor(jatoIndex, jato);
        DrawJatoThrustEditor(jatoIndex, jato);

        GUILayout.Label("Position:");

        DrawJatoPositionInput("X", ref _settingsEditingText.PositionX, ref _settingsEditingDraft.PositionX, 0f, 5f, () => HydrateJato(jatoIndex, jato));
        DrawJatoPositionInput("Y", ref _settingsEditingText.PositionY, ref _settingsEditingDraft.PositionY, 0f, 5f, () => HydrateJato(jatoIndex, jato));
        DrawJatoPositionInput("Z", ref _settingsEditingText.PositionZ, ref _settingsEditingDraft.PositionZ, -10f, 10f, () => HydrateJato(jatoIndex, jato));

        GUILayout.Label("Rotation:");

        DrawJatoRotationInput("X", ref _settingsEditingText.RotationX, ref _settingsEditingDraft.RotationX, () => HydrateJato(jatoIndex, jato));
        DrawJatoRotationInput("Y", ref _settingsEditingText.RotationY, ref _settingsEditingDraft.RotationY, () => HydrateJato(jatoIndex, jato));
        DrawJatoRotationInput("Z", ref _settingsEditingText.RotationZ, ref _settingsEditingDraft.RotationZ, () => HydrateJato(jatoIndex, jato));

        DrawJatoScaleInput("Scale", ref _settingsEditingText.Scale, ref _settingsEditingDraft.Scale, () => HydrateJato(jatoIndex, jato));

        var newForceOn = GUILayout.Toggle(_settingsEditingDraft.ForceOn, "Force on");
        var newRequireSittingInside = GUILayout.Toggle(_settingsEditingDraft.RequireSittingInside, "Require on train for keybinding to work");
        var newHideBody = GUILayout.Toggle(_settingsEditingDraft.HideBody, "Only show flame");

        if (
            newForceOn != _settingsEditingDraft.ForceOn ||
            newRequireSittingInside != _settingsEditingDraft.ForceOn ||
            newHideBody != _settingsEditingDraft.HideBody
        )
        {
            _settingsEditingDraft.ForceOn = newForceOn;
            _settingsEditingDraft.RequireSittingInside = newRequireSittingInside;
            _settingsEditingDraft.HideBody = newHideBody;
            HydrateJato(jatoIndex, jato);
        }

        DrawJatoVolumeSlider(jatoIndex, jato);
    }

    void HydrateJato(int jatoIndex, UniversalJato jato)
    {
        if (_settingsEditingDraft == null)
            return;

        JatoManager.UpdateJato(jato, _settingsEditingDraft);
    }

    void DrawJatoVolumeSlider(int jatoIndex, UniversalJato jato)
    {
        if (_settingsEditingDraft == null)
            return;

        var min = 0;
        var max = 1f;
        var value = _settingsEditingDraft.SoundVolume;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Volume: ", GUILayout.Width(50));
        GUILayout.Label("0", GUILayout.Width(40));
        var newValueRaw = GUILayout.HorizontalSlider(value, min, max);
        var newValue = _snapping ? Mathf.Round(newValueRaw / 0.05f) * 0.05f : newValueRaw;
        GUILayout.Label("100", GUILayout.Width(40));
        GUILayout.EndHorizontal();
        if (newValue != _settingsEditingDraft.SoundVolume)
        {
            _settingsEditingDraft.SoundVolume = newValue;
            HydrateJato(jatoIndex, jato);
        }
    }

    void DrawSettings()
    {
        var target = GetJatoTargetInfo();

        _snapping = GUILayout.Toggle(_snapping, "Snapping");

        var newPreventDerail = GUILayout.Toggle(Main.settings.PreventDerail, "Prevent derail");
        if (newPreventDerail != Main.settings.PreventDerail)
        {
            Main.settings.PreventDerail = newPreventDerail;
            Main.settings.Save(Main.ModEntry);
        }

        GUI.enabled = target != null;
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Stop"))
        {
            StopTrainMoving();
        }
        if (GUILayout.Button("Repair"))
        {
            RepairTrain();
        }
        if (GUILayout.Button("Derail"))
        {
            DerailTrain();
        }
        if (GUILayout.Button("Rerail"))
        {
            RerailTrain();
        }
        if (GUILayout.Button("Rerail (back)"))
        {
            RerailTrain(isReverse: true);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Remove ALL JATOs"))
        {
            RemoveAllJatosFromAllTrains();
        }
    }

    public void RerailTrain(bool isReverse = false)
    {
        Logger.Log("[Panel] Rerailing train...");

        TrainCarHelper.RerailTrain(PlayerManager.Car, isReverse);
    }

    public void RepairTrain()
    {
        Logger.Log("[Panel] Repairing train...");

        TrainCarHelper.RepairTrain(PlayerManager.Car);
    }

    public void HydrateStandardJatos(Transform? target)
    {
        if (target == null)
            return;

        // TODO: move to manager

        var jatos = JatoManager.GetJatos(target);

        var standardJatos = jatos.Where(x => x.side != null);

        foreach (var jato in standardJatos)
        {
            jato.settings.Thrust = _standardThrust;

            switch (jato.side)
            {
                case StandardSide.FrontLeft:
                    jato.settings.PositionX = _frontPositionX * -1;
                    jato.settings.PositionY = _frontPositionY;
                    jato.settings.PositionZ = _frontPositionZ;
                    break;
                case StandardSide.FrontRight:
                    jato.settings.PositionX = _frontPositionX;
                    jato.settings.PositionY = _frontPositionY;
                    jato.settings.PositionZ = _frontPositionZ;
                    break;
                case StandardSide.RearLeft:
                    Logger.Log($"REAR LEFT {jato.settings.PositionX} => {_rearPositionX * -1}");

                    jato.settings.PositionX = _rearPositionX * -1;
                    jato.settings.PositionY = _rearPositionY;
                    jato.settings.PositionZ = _rearPositionZ;
                    break;
                case StandardSide.RearRight:
                    jato.settings.PositionX = _rearPositionX;
                    jato.settings.PositionY = _rearPositionY;
                    jato.settings.PositionZ = _rearPositionZ;
                    break;
            }

            JatoManager.ApplyOffsetsToRocket(jato.transform, jato.settings);
        }
    }

    public void AddStandardRearJatos()
    {
        Logger.Log("[Panel] Adding standard rear jatos...");

        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var position = GetStandardRearJatoPosition(target.Value.trainCar);

        if (position == null)
            position = TrainCarHelper.GetApproxStandardRearJatoPosition(target.Value.trainCar);

        if (position == null)
            position = new Vector3(2f, 0, 3f);

        var settingsRight = new JatoSettings()
        {
            Thrust = _standardThrust,
            KeyCode = KeyCode.LeftShift,
            PositionX = position.Value.x,
            PositionY = position.Value.y,
            PositionZ = position.Value.z
        };

        _rearPositionX = settingsRight.PositionX;
        _rearPositionY = settingsRight.PositionY;
        _rearPositionZ = settingsRight.PositionZ;

        var addedComponentRight = JatoManager.AddJato(target.Value.transform, target.Value.trainCar, target.Value.rigidbody, settingsRight);
        addedComponentRight.side = StandardSide.RearRight;

        var settingsLeft = new JatoSettings()
        {
            Thrust = _standardThrust,
            KeyCode = KeyCode.LeftShift,
            PositionX = position.Value.x * -1,
            PositionY = position.Value.y,
            PositionZ = position.Value.z
        };

        var addedComponentLeft = JatoManager.AddJato(target.Value.transform, target.Value.trainCar, target.Value.rigidbody, settingsLeft);
        addedComponentLeft.side = StandardSide.RearLeft;
    }

    public void AddStandardFrontJatos()
    {
        Logger.Log("[Panel] Adding standard front jatos...");

        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        var position = GetStandardFrontJatoPosition(target.Value.trainCar);

        if (position == null)
            position = TrainCarHelper.GetApproxStandardFrontJatoPosition(target.Value.trainCar);

        if (position == null)
            position = new Vector3(2f, 0, 3f);

        var settingsRight = new JatoSettings()
        {
            Thrust = _standardThrust,
            KeyCode = KeyCode.LeftControl,
            PositionX = position.Value.x,
            PositionY = position.Value.y,
            PositionZ = position.Value.z,
            RotationX = 180
        };

        _frontPositionX = settingsRight.PositionX;
        _frontPositionY = settingsRight.PositionY;
        _frontPositionZ = settingsRight.PositionZ;

        var addedComponentRight = JatoManager.AddJato(target.Value.transform, target.Value.trainCar, target.Value.rigidbody, settingsRight);
        addedComponentRight.side = StandardSide.FrontRight;

        var settingsLeft = new JatoSettings()
        {
            Thrust = _standardThrust,
            KeyCode = KeyCode.LeftControl,
            PositionX = position.Value.x * -1,
            PositionY = position.Value.y,
            PositionZ = position.Value.z,
            RotationX = 180
        };

        var addedComponentLeft = JatoManager.AddJato(target.Value.transform, target.Value.trainCar, target.Value.rigidbody, settingsLeft);
        addedComponentLeft.side = StandardSide.FrontLeft;
    }

    public void RemoveAllJatosFromCurrentTrain()
    {
        Logger.Log("[Panel] Removing all jatos from current train...");

        var target = GetJatoTargetInfo();
        if (target == null)
            return;

        JatoManager.RemoveAllJatos(target.Value.transform);
    }

    public void RemoveAllJatosFromAllTrains()
    {
        Logger.Log("[Panel] Removing all jatos from ALL...");

        JatoManager.RemoveAllJatos();
    }
}