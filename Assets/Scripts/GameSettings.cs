using UnityEngine;
using UnityEngine.Audio;

// Saves settings with PlayerPrefs. PlayerPrefs live on each computer (per user account),
// not inside the Unity project, so they are never committed to GitHub.
public static class GameSettings
{
    // PlayerPrefs keys
    public const string MasterKey = "vol_master";
    public const string MusicKey = "vol_music";
    public const string SfxKey = "vol_sfx";
    public const string SensitivityKey = "mouse_sensitivity";

    // Exposed parameter names in the Audio Mixer (must match exactly)
    public const string MasterParam = "MasterVolume";
    public const string MusicParam = "MusicVolume";
    public const string SfxParam = "SFXVolume";

    public const float DefaultVolume = 0.8f;
    public const float DefaultSensitivity = 0.1f;   // keep in sync with PlayerMovement's default
    public const float MinSensitivity = 0.02f;
    public const float MaxSensitivity = 0.3f;

    public static float GetVolume(string key)
    {
        return PlayerPrefs.GetFloat(key, DefaultVolume);
    }

    public static float GetSensitivity()
    {
        return PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
    }

    public static void SetVolume(AudioMixer mixer, string key, string param, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        mixer.SetFloat(param, ToDecibels(value));
    }

    public static void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(SensitivityKey, value);
        ApplySensitivity(value);
    }

    // Call once when a scene starts
    public static void ApplyAll(AudioMixer mixer)
    {
        if (mixer != null)
        {
            mixer.SetFloat(MasterParam, ToDecibels(GetVolume(MasterKey)));
            mixer.SetFloat(MusicParam, ToDecibels(GetVolume(MusicKey)));
            mixer.SetFloat(SfxParam, ToDecibels(GetVolume(SfxKey)));
        }

        // Only override the player's sensitivity if the user has actually changed it
        if (PlayerPrefs.HasKey(SensitivityKey)) ApplySensitivity(GetSensitivity());
    }

    static void ApplySensitivity(float value)
    {
        var player = Object.FindFirstObjectByType<PlayerMovement>();
        if (player != null) player.mouseSensitivity = value;
    }

    // Slider 0..1 -> decibels (0 = silent, 1 = full volume)
    static float ToDecibels(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }
}
