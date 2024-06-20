﻿using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Staple.Internal;
using System;
using System.Reflection;
using System.IO;
using System.Threading;

namespace Staple.Editor;

[CustomEditor(typeof(AudioClipMetadata))]
internal class AudioClipEditor : Editor
{
    private AudioClip clip;
    private bool triedLoad = false;
    private IAudioSource audioSource;
    private IAudioClip audioClip;
    private CancellationTokenSource cancellation;
    private object lockObject = new();

    public override void Destroy()
    {
        base.Destroy();

        lock (lockObject)
        {
            cancellation.Cancel();
            audioSource?.Destroy();
            audioClip?.Destroy();

            clip = null;
            cancellation = null;
            audioSource = null;
            audioClip = null;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var metadata = (AudioClipMetadata)target;
        var originalMetadata = (AudioClipMetadata)original;

        lock (lockObject)
        {
            if (triedLoad == false && clip == null)
            {
                triedLoad = true;

                clip = ResourceManager.instance.LoadAudioClip(cachePath);
                cancellation = AudioSystem.Instance.LoadAudioClip(clip, (samples, channels, bits, sampleRate) =>
                {
                    if (samples == default)
                    {
                        return;
                    }

                    lock (lockObject)
                    {
                        audioSource = (IAudioSource)Activator.CreateInstance(AudioSystem.AudioSourceImpl);

                        if (audioSource.Init())
                        {
                            audioClip = (IAudioClip)Activator.CreateInstance(AudioSystem.AudioClipImpl);

                            if (audioClip.Init(samples, channels, bits, sampleRate))
                            {
                                if (audioSource.Bind(audioClip) == false)
                                {
                                    audioClip.Destroy();

                                    audioSource.Destroy();

                                    audioClip = null;
                                    audioSource = null;
                                }
                            }
                            else
                            {
                                audioClip.Destroy();

                                audioSource.Destroy();

                                audioClip = null;
                                audioSource = null;
                            }
                        }
                        else
                        {
                            audioSource = null;
                        }
                    }
                });
            }

            if (clip == null && triedLoad)
            {
                EditorGUI.Label("Audio data is corrupted");

                return;
            }

            var hours = 0;
            var minutes = 0;
            var seconds = clip.duration;

            while (seconds > 60)
            {
                seconds -= 60;

                minutes++;
            }

            while (minutes > 60)
            {
                minutes -= 60;

                hours++;
            }

            EditorGUI.Label($"Channels: {clip.channels} ({clip.bitsPerSample} bits, {clip.sampleRate}Hz)");
            EditorGUI.Label($"Duration: {hours}:{minutes}:{seconds.ToString("0.00")}");

            if (audioSource != null)
            {
                EditorGUI.Button("Play", "AudioSourcePlay", () =>
                {
                    audioSource.Play();
                });

                EditorGUI.SameLine();

                if (audioSource.Playing)
                {
                    EditorGUI.Button("Pause", "AudioSourcePause", () =>
                    {
                        audioSource.Pause();
                    });
                }
                else if (audioSource.Paused)
                {
                    EditorGUI.Button("Resume", "AudioSourceResume", () =>
                    {
                        audioSource.Play();
                    });
                }
                else
                {
                    EditorGUI.ButtonDisabled("Pause", "AudioSourcePause", null);
                }

                EditorGUI.SameLine();

                if (audioSource.Playing)
                {
                    EditorGUI.Button("Stop", "AudioSourceStop", () =>
                    {
                        audioSource.Stop();
                    });
                }
                else
                {
                    EditorGUI.ButtonDisabled("Stop", "AudioSourceStop", null);
                }
            }

            var hasChanges = metadata != originalMetadata;

            if (hasChanges)
            {
                EditorGUI.Button("Apply", "AudioSourceApply", () =>
                {
                    try
                    {
                        var text = JsonConvert.SerializeObject(metadata, Formatting.Indented, new JsonSerializerSettings()
                        {
                            Converters =
                            {
                                new StringEnumConverter(),
                            }
                        });

                        File.WriteAllText(path, text);
                    }
                    catch (Exception)
                    {
                    }

                    var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        field.SetValue(original, field.GetValue(metadata));
                    }

                    EditorUtils.RefreshAssets(false, null);
                });

                EditorGUI.SameLine();

                EditorGUI.Button("Revert", "AudioSourceRevert", () =>
                {
                    var fields = metadata.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        field.SetValue(metadata, field.GetValue(original));
                    }
                });
            }
            else
            {
                EditorGUI.ButtonDisabled("Apply", "AudioSourceApply", null);

                EditorGUI.SameLine();

                EditorGUI.ButtonDisabled("Revert", "AudioSourceRevert", null);
            }
        }
    }
}
