using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Put on the Settings panel (next to MenuPanel). Connects the four sliders to GameSettings.
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider sensitivitySlider;

    void Awake()
    {
        masterSlider.minValue = 0f;  masterSlider.maxValue = 1f;
        musicSlider.minValue = 0f;   musicSlider.maxValue = 1f;
        sfxSlider.minValue = 0f;     sfxSlider.maxValue = 1f;
        sensitivitySlider.minValue = GameSettings.MinSensitivity;
        sensitivitySlider.maxValue = GameSettings.MaxSensitivity;

        masterSlider.onValueChanged.AddListener(v => GameSettings.SetVolume(mixer, GameSettings.MasterKey, GameSettings.MasterParam, v));
        musicSlider.onValueChanged.AddListener(v => GameSettings.SetVolume(mixer, GameSettings.MusicKey, GameSettings.MusicParam, v));
        sfxSlider.onValueChanged.AddListener(v => GameSettings.SetVolume(mixer, GameSettings.SfxKey, GameSettings.SfxParam, v));
        sensitivitySlider.onValueChanged.AddListener(v => GameSettings.SetSensitivity(v));
    }

    // Show the saved values every time the panel opens (without triggering the listeners)
    void OnEnable()
    {
        masterSlider.SetValueWithoutNotify(GameSettings.GetVolume(GameSettings.MasterKey));
        musicSlider.SetValueWithoutNotify(GameSettings.GetVolume(GameSettings.MusicKey));
        sfxSlider.SetValueWithoutNotify(GameSettings.GetVolume(GameSettings.SfxKey));
        sensitivitySlider.SetValueWithoutNotify(GameSettings.GetSensitivity());
    }

    // Write to disk when the panel closes
    void OnDisable()
    {
        PlayerPrefs.Save();
    }
}
