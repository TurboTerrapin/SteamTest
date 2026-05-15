using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer masterMixer;
    public List<GameObject> audioSources;
    public List <AudioSource> computerVoiceSpeakers;

    private List<AudioClip> high_priority_notifications = new List<AudioClip>();
    private List<AudioClip> low_priority_notifications = new List<AudioClip>();

    private bool computer_voice_active = false;
    private Coroutine computer_voice_player_coroutine = null;

    public void InitializeAudio()
    {
        SetMasterVolume(0.75f);

        audioSources[0].GetComponent<AudioSource>().Play(); //play ambient noise
        audioSources[1].GetComponent<AudioSource>().Play(); //play ship beeps
    }

    public void ActivateComputerVoice()
    {
        computer_voice_active = true;
        StartComputerVoicePlayer();
    }

    public void DeactivateComputerVoice()
    {
        high_priority_notifications.Clear();
        low_priority_notifications.Clear();
        if (computer_voice_player_coroutine != null)
        {
            StopCoroutine(computer_voice_player_coroutine);
            computer_voice_player_coroutine = null;
        }
        computer_voice_active = false;
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

    IEnumerator ComputerVoicePlayer()
    {
        while (high_priority_notifications.Count > 0 || low_priority_notifications.Count > 0)
        {
            AudioClip notificationToPlay; 
            if (high_priority_notifications.Count > 0)
            {
                notificationToPlay = high_priority_notifications[0];
                high_priority_notifications.RemoveAt(0);
            }
            else
            {
                notificationToPlay = low_priority_notifications[0];
                low_priority_notifications.RemoveAt(0);
            }

            foreach (AudioSource speaker in computerVoiceSpeakers)
            {
                speaker.clip = notificationToPlay;
                speaker.Play();
            }

            while (computerVoiceSpeakers[0].isPlaying == true)
            {
                yield return null;
            }
        }

        computer_voice_player_coroutine = null;
    }

    private void StartComputerVoicePlayer()
    {
        if (computer_voice_player_coroutine != null || computer_voice_active == false)
        {
            return;
        }

        computer_voice_player_coroutine = StartCoroutine(ComputerVoicePlayer());
    }

    public void AddHighPriorityNotification(AudioClip notification)
    {
        high_priority_notifications.Add(notification);
        StartComputerVoicePlayer();
    }

    public void AddLowPriorityNotification(AudioClip notification)
    {
        low_priority_notifications.Add(notification);
        StartComputerVoicePlayer();
    }

    public bool NotificationInQueue(AudioClip to_test)
    {
        if (computerVoiceSpeakers[0].isPlaying == true && computerVoiceSpeakers[0].clip.name.Equals(to_test.name) == true)
        {
            return true;
        }

        for (int i = 0; i < high_priority_notifications.Count; i++)
        {
            if (high_priority_notifications[i].name.Equals(to_test.name) == true)
            {
                return true;
            }
        }

        for (int i = 0; i < low_priority_notifications.Count; i++)
        {
            if (low_priority_notifications[i].name.Equals(to_test.name) == true)
            {
                return true;
            }
        }

        return false;
    }
}