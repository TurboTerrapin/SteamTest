using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer masterMixer;

    public void initializeAudio()
    {
        FindAnyObjectByType<AudioManager>()?.SetMasterVolume(0.75f);
    }

    public void SetMasterVolume(float volume)
    {
        float dB = Mathf.Log10(volume) * 20;
        masterMixer.SetFloat("MasterVolume", dB);
    }

}