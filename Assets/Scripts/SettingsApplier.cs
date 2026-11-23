using UnityEngine;
using UnityEngine.Audio;

// Put on an object that is always active in every scene (MenuController in the main menu,
// PauseCanvas in the gameplay scene). Applies the saved volume and sensitivity when the scene starts.
public class SettingsApplier : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;

    void Start()
    {
        GameSettings.ApplyAll(mixer);
    }
}
