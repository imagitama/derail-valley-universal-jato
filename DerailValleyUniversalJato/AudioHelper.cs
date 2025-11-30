using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DerailValleyUniversalJato;

public static class AudioHelper
{
    public static LayeredAudio ConvertAudioSourceToLayeredAudio(
        AudioSource source,
        AudioMixerGroup mixer = null,
        bool randomizeStartTime = false)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        var go = source.gameObject;

        // Disable normal behaviour
        source.enabled = false;

        // Create new component
        var layered = go.AddComponent<LayeredAudio>();
        layered.audioMixerGroup = mixer ?? source.outputAudioMixerGroup;
        layered.randomizeStartTime = randomizeStartTime;

        // Choose type based on looping behaviour
        layered.type = source.loop ? LayeredAudio.Type.Continuous : LayeredAudio.Type.OneTime;

        // Create 1 layer
        layered.layers = new LayeredAudio.Layer[1];
        var layer = new LayeredAudio.Layer();
        layered.layers[0] = layer;

        layer.name = source.clip != null ? source.clip.name : "Layer";
        layer.source = source;

        // Default volume curve (linear)
        layer.volumeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 1f)
        );

        // Default pitch curve
        layer.pitchCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 1f)
        );

        layer.usePitchCurve = false;

        // Preserve pitch
        layer.startPitch = source.pitch;

        // One-shot clip support
        if (layered.type == LayeredAudio.Type.OneTime && source.clip != null)
            layer.clips = new[] { source.clip };

        // Pitch range default (no variation)
        layer.pitchRange = new Vector2(1f, 1f);

        // Inertia disabled by default
        layer.inertia = 0f;
        layer.inertialPitch = false;

        // Doppler if present
        // layer.doppler = source.GetComponent<Doppler>();
        // layer.useDoppler = layer.doppler != null;

        // Preserve original distances & spread
        // (LayeredAudio reads these automatically when playing)
        // so we do nothing here.

        // Initialize the component
        layered.Reset();

        return layered;
    }
}