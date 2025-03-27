using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SettingSO", menuName = "Scriptable Objects/SettingSO")]
public class SettingSO : ScriptableObject
{
    [SerializeField] private AudioMixer mainMixer;

    [Header("volumeKey")]
    [SerializeField] private string masterKey;
    [SerializeField] private string musicKey;
    [SerializeField] private string sfxKey;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float masterVolume;
    [Range(0f, 1f)][SerializeField] private float musicVolume;
    [Range(0f, 1f)][SerializeField] private float sfxVolume;

    [Header("Sensitivity")]
    [Range(0f, 1f)][SerializeField] private float Sensibility;

    public void LoadSetting()
    {
        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
        SetSensibility(Sensibility);
    }
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        mainMixer.SetFloat(masterKey, Mathf.Log10(volume) * 20);

    }
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        mainMixer.SetFloat(musicKey, Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        mainMixer.SetFloat(sfxKey, Mathf.Log10(volume) * 20);
    }

    public void SetSensibility(float sensibility)
    {
        this.Sensibility = Mathf.Clamp(sensibility, 0.0001f, 1f);

    }
    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    public float GetSensibility()
    {
        return Sensibility;
    }
}
