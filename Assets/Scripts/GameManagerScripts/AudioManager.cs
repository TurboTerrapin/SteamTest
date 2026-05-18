using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer masterMixer;
    public List<GameObject> audioSources;
    public AudioSource computerVoiceSpeaker;

    private List<AudioClip>[] notificationQueues = new List<AudioClip>[] { 
        new List<AudioClip>(), //0: low-priority notifications
        new List<AudioClip>(), //1: high-priority notifications only
        new List<AudioClip>(), //2: boundary departure only
        new List<AudioClip>(), //3: boundary countdown only
        new List<AudioClip>(), //4: self-destruct only
    };

    private bool computerVoiceActive = false;
    private int currentClipPriority = -1;
    private Coroutine computerVoicePlayerCoroutine = null;

    public void InitializeAudio()
    {
        SetMasterVolume(0.75f);

        audioSources[0].GetComponent<AudioSource>().Play(); //play ambient noise
        audioSources[1].GetComponent<AudioSource>().Play(); //play ship beeps
    }

    public void ActivateComputerVoice()
    {
        computerVoiceActive = true;
        StartComputerVoicePlayer();
    }

    public void DeactivateComputerVoice()
    {
        foreach (List<AudioClip> notificationQueue in notificationQueues)
        {
            notificationQueue.Clear();
        }
        if (computerVoicePlayerCoroutine != null)
        {
            StopCoroutine(computerVoicePlayerCoroutine);
            computerVoiceSpeaker.Stop();
            computerVoicePlayerCoroutine = null;
        }
        computerVoiceActive = false;
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

    private void ClearNotificationQueuesBelowPriority(int priority)
    {
        for (int i = 0; i < priority; i++)
        {
            notificationQueues[i].Clear();
        }
    }

    private AudioClip GetClipToPlay()
    {
        AudioClip selectedClip = null;
        for (int i = 4; i >= 2; i--)
        {
            if (notificationQueues[i].Count > 0)
            {
                selectedClip = notificationQueues[i][0];
                notificationQueues[i].RemoveAt(0);
                ClearNotificationQueuesBelowPriority(i);
                currentClipPriority = i;
                return selectedClip;
            }
        }
        for (int i = 1; i >= 0; i--)
        {
            if (notificationQueues[i].Count > 0)
            {
                selectedClip = notificationQueues[i][0];
                notificationQueues[i].RemoveAt(0);
                currentClipPriority = i;
                return selectedClip;
            }
        }

        return selectedClip;
    }

    IEnumerator ComputerVoicePlayer()
    {
        AudioClip notificationToPlay = GetClipToPlay();
        while (notificationToPlay != null)
        {
            computerVoiceSpeaker.clip = notificationToPlay;
            computerVoiceSpeaker.Play();

            while (computerVoiceSpeaker.isPlaying == true)
            {
                yield return null;
            }
            notificationToPlay = GetClipToPlay();
        }

        computerVoicePlayerCoroutine = null;
    }

    private void StartComputerVoicePlayer()
    {
        if (computerVoicePlayerCoroutine != null || computerVoiceActive == false)
        {
            return;
        }

        computerVoicePlayerCoroutine = StartCoroutine(ComputerVoicePlayer());
    }

    public void AddNotification(int priority, AudioClip notification)
    {
        if (priority < 0 || priority > 5)
        {
            return;
        }

        if (NotificationInQueue(notification) == true)
        {
            return;
        }

        if (computerVoicePlayerCoroutine != null)
        {
            if (priority >= 2 && priority > currentClipPriority)
            {
                StopCoroutine(computerVoicePlayerCoroutine);
                computerVoicePlayerCoroutine = null;
                computerVoiceSpeaker.Stop();
            }
        }

        notificationQueues[priority].Add(notification);
        StartComputerVoicePlayer();
    }

    public bool NotificationInQueue(AudioClip to_test)
    {
        if (computerVoiceSpeaker.isPlaying == true && computerVoiceSpeaker.clip.name.Equals(to_test.name) == true)
        {
            return true;
        }

        foreach (List<AudioClip> notificationQueue in notificationQueues)
        {
            foreach (AudioClip notification in notificationQueue)
            {
                if (notification.name.Equals(to_test.name) == true)
                {
                    return true;
                }
            }
        }

        return false;
    }
}