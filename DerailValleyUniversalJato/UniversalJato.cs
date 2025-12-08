using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public enum StandardSide
{
    FrontLeft,
    FrontRight,
    RearLeft,
    RearRight
}

public class UniversalJato : MonoBehaviour
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public TrainCar trainCar;
    public Rigidbody trainRigidbody;
    public JatoSettings settings;
    public Transform? alwaysStuff;
    public Transform? offStuff;
    public Transform? onStuff;
    public Transform? keepAliveStuff;
    public AudioSource[] audioSources = [];
    public ParticleSystem[] onParticleSystems = [];
    public bool IsOn = false;
    public string? DebugText;
    private UniversalJatoDebugText? _debugText;
    public StandardSide? side;

    void Start()
    {
        Logger.Log("UniversalJato.Start");
        alwaysStuff = transform.Find("Always");
        offStuff = transform.Find("Off");
        onStuff = transform.Find("On");

        if (alwaysStuff == null)
            Logger.Log("No 'always' stuff found");
        if (offStuff == null)
            Logger.Log("No 'off' stuff found");
        if (onStuff == null)
            Logger.Log("No 'on' stuff found");

        audioSources = GetComponentsInChildren<AudioSource>();

        Logger.Log($"Found {audioSources.Length} audio sources");

        onParticleSystems = onStuff?.GetComponentsInChildren<ParticleSystem>() ?? [];

        Logger.Log($"Found {onParticleSystems.Length} particle systems");

        SetupPaticleSystems();

        onStuff?.gameObject.SetActive(false);
    }

    void SetupPaticleSystems()
    {
        var newObj = new GameObject("KeepAlive");
        newObj.transform.parent = this.transform;
        keepAliveStuff = newObj.transform;

        foreach (var particleSystem in onParticleSystems)
        {
            particleSystem.transform.parent = keepAliveStuff;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private bool? GetIsSittingInside()
    {
        if (trainCar == null)
            return null;

        return PlayerManager.Car == trainCar;
    }

    void Update()
    {
        var originallyIsOn = IsOn;

        if (settings == null)
        {
            Logger.Log($"Waiting for required data settings={settings}");
            return;
        }

        alwaysStuff?.gameObject.SetActive(!settings.HideBody);

        if (Input.GetKey(settings.KeyCode))
        {
            if (settings.RequireSittingInside && GetIsSittingInside() != true)
            {
                // ignore
            }
            else
            {
                IsOn = true;
            }
        }
        else
        {
            IsOn = false;
        }

        if (settings.ForceOn)
            IsOn = true;

        offStuff?.gameObject.SetActive(IsOn!);
        onStuff?.gameObject.SetActive(IsOn);

        if (IsOn)
        {
            ApplyBoosterForce();
            SetAudioVolume();

            if (IsOn != originallyIsOn)
                StartParticleSystems();
        }
        else
        {
            if (IsOn != originallyIsOn)
                StopParticleSystems();
        }

        DrawDebug();
    }

    void StartParticleSystems()
    {
        // do this to let particles keep playing so less abrupt
        foreach (var particleSystem in onParticleSystems)
            particleSystem.Play();
    }

    void StopParticleSystems()
    {
        foreach (var particleSystem in onParticleSystems)
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void SetAudioVolume()
    {
        foreach (var audio in audioSources)
            audio.volume = settings.SoundVolume;
    }

    void DrawDebug()
    {
        if (DebugText != null)
        {
            if (_debugText == null)
            {
                var newObj = new GameObject("Debug");
                newObj.transform.SetParent(this.transform, false);
                newObj.transform.localPosition = new Vector3(0, -0.5f, 0);
                newObj.transform.localScale = Vector3.one * 0.5f;
                newObj.transform.localRotation = Quaternion.identity;
                _debugText = newObj.AddComponent<UniversalJatoDebugText>();
            }

            _debugText.Text = DebugText;
        }
        else if (_debugText != null)
        {
            GameObject.Destroy(_debugText.gameObject);
        }
    }

    void ApplyBoosterForce()
    {
        if (trainRigidbody == null)
        {
            Logger.Log("Waiting for rigidbody");
            return;
        }

        Vector3 thrustDir = transform.TransformDirection(Vector3.back);

        Vector3 force = thrustDir * settings.Thrust;

        trainRigidbody.AddForceAtPosition(force, transform.position, ForceMode.Force);
    }
}