using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer masterMixer;
    public List<GameObject> audioSources;

    private List<float> startingVolumes = new List<float>();

    public void InitializeAudio()
    {
        SetMasterVolume(0.75f);

        foreach (GameObject audioSource in audioSources)
        {
            startingVolumes.Add(audioSource.GetComponent<AudioSource>().volume);
            audioSource.transform.GetComponent<AudioSource>().Play();
        }
    }

    public void SetMasterVolume(float volume)
    {
        float dB = Mathf.Log10(volume) * 20;
        masterMixer.SetFloat("MasterVolume", dB);
    }

    public void MuteAudio()
    {
        foreach (GameObject audioSource in audioSources)
        {
            audioSource.transform.GetComponent<AudioSource>().volume = 0.0f;
        }
    }

    public void UnmuteAudio()
    {
        foreach (GameObject audioSource in audioSources)
        {
            audioSource.transform.GetComponent<AudioSource>().volume = startingVolumes[audioSources.IndexOf(audioSource)];
        }
    }
}