﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class PropagatingAudioSourceManager : MonoBehaviour {
  public PropagatingHostRoom[] PredefinedRooms;

  public int AudioClipPoolSize = 50;
  public bool AutoExtendAudioPool = true;
  public GameObject Speaker;

  private PropagatingSpeaker[] audioSourcePool;

  private Dictionary<GameObject, PropagatingSoundGateway[]> RoomToNeighbourPositions;
  private Dictionary<PropagatingAudioSource,PropagatingSpeaker[]> ConnectedSpeakers;
  private Dictionary<PropagatingSpeaker, PropagatingAudioSource> SpeakerToOrigin;
  private int poolIndex = 0;

  public static PropagatingAudioSourceManager Instance;

	private void Awake () {
    if (Instance == null)
    {
      Instance = this;
      DontDestroyOnLoad(this);

      Debug.Assert(AudioClipPoolSize >= 1, "AudioClipSize must at least be 1");
      ExtendPool(AudioClipPoolSize);

      if (PredefinedRooms != null)
      {
        Setup();
      }
    }
    else
    {
      Destroy(this);
    }
	}

  public void Setup(IEnumerable<GameObject> rooms)
  {
    RoomToNeighbourPositions = new Dictionary<GameObject, PropagatingSoundGateway[]>();
    ConnectedSpeakers = new Dictionary<PropagatingAudioSource, PropagatingSpeaker[]>();
    SpeakerToOrigin = new Dictionary<PropagatingSpeaker, PropagatingAudioSource>();

    if (PredefinedRooms != null)
    {
      foreach (PropagatingHostRoom hostRoom in PredefinedRooms)
      {
        PropagatingSoundGateway[] gateways = hostRoom.GetComponentsInChildren<PropagatingSoundGateway>(true);
        RoomToNeighbourPositions[hostRoom.gameObject] = gateways;
      }
    }

    foreach (GameObject gO in rooms)
    {
      PropagatingSoundGateway[] gateways = gO.GetComponentsInChildren<PropagatingSoundGateway>(true);
      RoomToNeighbourPositions[gO] = gateways;
    }
  }

  private void Setup() => Setup(new List<GameObject>());

  public void ForwardPlay(PropagatingAudioSource host, AudioClip clip, float volume, bool loop)
  {
    if (ConnectSpeakers(host))
    {
      PropagatingSpeaker[] speakers = ConnectedSpeakers[host];
      foreach (PropagatingSpeaker speaker in speakers)
      {
        speaker.audioSource.clip = clip;
        speaker.audioSource.volume = volume * speaker.Dampening;
        speaker.audioSource.loop = loop;
        speaker.audioSource.Play();
      }
    }
  }

  public void ForwardPlayOneShot(PropagatingAudioSource host, AudioClip clip, float volume)
  {
    if(ConnectSpeakers(host))
    {
      PropagatingSpeaker[] speakers = ConnectedSpeakers[host];
      foreach(PropagatingSpeaker speaker in speakers)
      {
        speaker.audioSource.PlayOneShot(clip, volume * speaker.Dampening);
      }
    }
  }

  public void Disconnect(PropagatingAudioSource host)
  {
    if (ConnectedSpeakers.ContainsKey(host))
    {
      PropagatingSpeaker[] speakers = ConnectedSpeakers[host];
      FreeSpeakers(speakers);
    }
  }

  public void ForwardStop(PropagatingAudioSource host)
  {
    if(IsConnected(host))
    {
      PropagatingSpeaker[] speakers = ConnectedSpeakers[host];
      foreach(PropagatingSpeaker speaker in speakers)
      {
        speaker.audioSource.Stop();
      }
      FreeSpeakers(speakers);
    }
  }  

  public void UpdateVolume(PropagatingAudioSource host, float volume)
  {
    if(IsConnected(host))
    {
      PropagatingSpeaker[] speakers = ConnectedSpeakers[host];
      foreach (PropagatingSpeaker speaker in speakers)
      {
        speaker.audioSource.volume = volume * speaker.Dampening;
      }
    }
  }

  public void UpdateClip(PropagatingAudioSource host, AudioClip clip)
  {
    if (IsConnected(host))
    {
      PropagatingSpeaker[] speakers = ConnectedSpeakers[host];
      foreach (PropagatingSpeaker speaker in speakers)
      {
        speaker.audioSource.clip = clip;
      }
    }
  }
  
  private bool IsConnected(PropagatingAudioSource host) => ConnectedSpeakers.ContainsKey(host);

  private bool ConnectSpeakers(PropagatingAudioSource host)
  {
    if (!IsConnected(host))
    {
      PropagatingSoundGateway[] gateways = RoomToNeighbourPositions[host.HostRoom];
      PropagatingSpeaker[] speakers = new PropagatingSpeaker[gateways.Length];

      for (int i = 0; i < speakers.Length; i++)
      {
        PropagatingSpeaker speaker = GetNextFree(speakers.Length);
        if (speaker == null)
        {
          FreeSpeakers(speakers);
          return false;
        }
        else
        {
          speaker.Dampening = gateways[i].Dampening;
          speakers[i]= speaker;
        }
      }

      // All required speakers are available - position them correctly
      for(int i  = 0; i < speakers.Length; i++)
      {
        speakers[i].audioSource.transform.position = gateways[i].SoundTarget;
        SpeakerToOrigin[speakers[i]] = host;
      }

      ConnectedSpeakers[host] = speakers;
    }

    return true;
  }


  private PropagatingSpeaker GetNextFree(int required)
  {
    for (int i = 0; i < audioSourcePool.Length; i++)
    {
      if (!audioSourcePool[poolIndex].Connected)
      {
        PropagatingSpeaker ps = audioSourcePool[poolIndex];
        ps.Connected = true;
        return ps;
      }

      poolIndex = (poolIndex + 1) % AudioClipPoolSize;
    }

    // Run "Garbage Collector" before extending pool
    if (GarbageCollect(required))
    {
      poolIndex = 0;
      return GetNextFree(required);
    }
    else
    {
      if (AutoExtendAudioPool)
      {
        Debug.Log("Extending Audio Pool, initial size was not enough");
        ExtendPool(required);
        return GetNextFree(required);
      }
      else
      {
        Debug.Log("Propagating Audio Pool ran out of audio sources");
        return null;
      }
    }
  }

  private bool GarbageCollect(int required)
  {
    int collected = 0;
    for (int i = 0; i < audioSourcePool.Length; i++)
    {
      if (!audioSourcePool[i].IsPlaying)
      {
        PropagatingSpeaker registered = audioSourcePool[i];
        if (SpeakerToOrigin.ContainsKey(registered))
        {
          PropagatingAudioSource origin = SpeakerToOrigin[registered];
          FreeSpeakers(ConnectedSpeakers[origin]);
          collected++;
        }
      }
    }

    return collected >= required;
  }

  private void FreeSpeakers(PropagatingSpeaker[] speakers)
  {
    foreach(PropagatingSpeaker speaker in speakers)
    {
      FreeSpeaker(speaker);
    }
  }

  private void FreeSpeaker(PropagatingSpeaker speaker)
  {
    if(SpeakerToOrigin.ContainsKey(speaker))
    {
      PropagatingAudioSource origin = SpeakerToOrigin[speaker];
      SpeakerToOrigin.Remove(speaker);

      if (ConnectedSpeakers.ContainsKey(origin))
      {
        ConnectedSpeakers.Remove(origin);
      }
    }

    speaker.Connected = false;
  }

  private void ExtendPool(int count)
  {
    if (audioSourcePool == null)
    {
      AudioClipPoolSize = 0;
      audioSourcePool = new PropagatingSpeaker[count];
    }
    else
    {
      PropagatingSpeaker[] newPool = new PropagatingSpeaker[AudioClipPoolSize + count];
      Array.Copy(audioSourcePool, newPool, audioSourcePool.Length);
      audioSourcePool = newPool;
    }
    
    for (int i = AudioClipPoolSize; i < AudioClipPoolSize + count; i++)
    {
      GameObject audioSourceObject = Instantiate(Speaker, transform);
      AudioSource audioSource = audioSourceObject.GetComponent<AudioSource>();
      Debug.Assert(audioSource != null, "Speaker must have AudioSource");

      audioSourcePool[i] = new PropagatingSpeaker(audioSource);
    }

    AudioClipPoolSize = audioSourcePool.Length;
  }
}
