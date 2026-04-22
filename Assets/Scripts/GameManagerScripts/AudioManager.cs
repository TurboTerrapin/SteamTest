using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer masterMixer;
    public List<GameObject> audioSources;

    public void InitializeAudio()
    {
        SetMasterVolume(0.75f);

        audioSources[0].GetComponent<AudioSource>().Play(); //play ambient noise
        audioSources[1].GetComponent<AudioSource>().Play(); //play ship beeps
    }

    public void SetMasterVolume(float volume)
    {
        float dB = Mathf.Log10(volume) * 20;
        masterMixer.SetFloat("MasterVolume", dB);
    }

    public void MuteAudio()
    {
        masterMixer.SetFloat("SFXVolume", -80.0f);
    }

    public void UnmuteAudio()
    {
        masterMixer.SetFloat("SFXVolume", 0.0f);
    }
}