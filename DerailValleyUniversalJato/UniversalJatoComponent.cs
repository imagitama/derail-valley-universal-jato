using System;
using System.Collections.Generic;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyUniversalJato;

public class UniversalJatoComponent : MonoBehaviour
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public TrainCar TrainCar;
    public Rigidbody TrainRigidbody;
    public JatoSettings Settings;
    public Transform OffStuff;
    public Transform OnStuff;
    // public AudioSource[] audioSources;
    // public Dictionary<Transform, LayeredAudio> LayeredAudios;
    public Dictionary<Vector3, AudioClip> AudioClips = [];
    public bool IsOn = false;
    // public AudioManager AudioManager;

    void Start()
    {
        Logger.Log("UniversalJatoComponent.Start");
        OffStuff = transform.Find("Off");
        OnStuff = transform.Find("On");

        // AudioManager = SingletonBehaviour<AudioManager>.Instance;

        if (OffStuff == null)
            throw new Exception("No 'off' stuff");
        if (OnStuff == null)
            throw new Exception("No 'on' stuff");

        var audioSources = GetComponentsInChildren<AudioSource>();

        Logger.Log($"Found {audioSources.Length} audio sources");

        foreach (var audioSource in audioSources)
        {
            var transform = audioSource.transform;

            // var layeredAudio = AudioHelper.ConvertAudioSourceToLayeredAudio(audioSource);
            // LayeredAudios[transform] = layeredAudio;

            AudioClips[transform.position] = audioSource.clip;

            GameObject.Destroy(audioSource);
        }

        OnStuff.gameObject.SetActive(false);
    }

    private bool GetIsSittingInside()
    {
        if (TrainCar == null)
            throw new Exception("Need a train car");

        return PlayerManager.Car == TrainCar;
    }

    void Update()
    {
        var originallyIsOn = IsOn;

        if (Settings == null)
        {
            Logger.Log($"Waiting for required data Settings={Settings}");
            return;
        }

        if (Input.GetKey(Settings.KeyCode) && (Settings.RequireSittingInside == false || GetIsSittingInside()))
        {
            IsOn = true;
        }
        else
        {
            IsOn = false;
        }

        if (Settings.ForceOn)
            IsOn = true;

        if (IsOn != originallyIsOn)
            Logger.Log("Boost!");

        OffStuff.gameObject.SetActive(IsOn!);
        OnStuff.gameObject.SetActive(IsOn);

        if (IsOn)
        {
            ApplyBoosterForce();
        }

        if (IsOn && IsOn != originallyIsOn)
        {
            PlayAudio();
        }
    }

    void PlayAudio()
    {
        foreach (var item in AudioClips)
        {
            var position = item.Key;
            var audioClip = item.Value;

            if (audioClip == null)
                throw new Exception($"Audio clip from transform '{transform}' is null");

            // TODO: get this working
            audioClip.Play(TrainCar.transform.position, minDistance: 10f, parent: TrainCar.transform, volume: Settings.SoundVolume);
        }
    }

    void ApplyBoosterForce()
    {
        if (TrainRigidbody == null)
        {
            Logger.Log("Waiting for rigidbody");
            return;
        }

        Vector3 thrustDir = transform.TransformDirection(Vector3.back);

        Vector3 force = thrustDir * Settings.Thrust;

        TrainRigidbody.AddForceAtPosition(force, transform.position, ForceMode.Force);
    }
}