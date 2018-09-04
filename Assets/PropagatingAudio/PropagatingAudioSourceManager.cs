using System.Collections.Generic;
using UnityEngine;

public class PropagatingAudioSourceManager : MonoBehaviour {
  public uint AudioClipPoolSize = 50;
  public GameObject Speaker;

  private PropagatingSpeaker[] audioSourcePool;

  private Dictionary<GameObject, PropagatingSoundGateway[]> RoomToNeighbourPositions;
  private Dictionary<PropagatingAudioSource, List<PropagatingSpeaker>> ConnectedSpeakers;
  private int poolIndex = 0;

  public static PropagatingAudioSourceManager Instance;

	private void Awake () {
    Debug.Assert(Instance == null, "More than one PropagatingAudioSourceManager is not allowed");
    Instance = this;
    DontDestroyOnLoad(this);

    Debug.Assert(AudioClipPoolSize >= 10, "AudioClipSize should at least be 10");

    audioSourcePool = new PropagatingSpeaker[AudioClipPoolSize];

    for(int i = 0; i < AudioClipPoolSize; i++)
    {
      GameObject audioSourceObject = Instantiate(Speaker, transform);
      AudioSource audioSource = audioSourceObject.GetComponent<AudioSource>();
      Debug.Assert(audioSource != null, "Speaker must have AudioSource");
      audioSourcePool[i] = new PropagatingSpeaker(audioSource);
    }
	}

  public void Setup(IEnumerable<GameObject> rooms)
  {
    RoomToNeighbourPositions = new Dictionary<GameObject, PropagatingSoundGateway[]>();
    ConnectedSpeakers = new Dictionary<PropagatingAudioSource, List<PropagatingSpeaker>>();    

    foreach(GameObject gO in rooms)
    {
      PropagatingSoundGateway[] gateways = gO.GetComponentsInChildren<PropagatingSoundGateway>(true);
      RoomToNeighbourPositions[gO] = gateways;
    }
  }

  private bool IsConnected(PropagatingAudioSource host) => ConnectedSpeakers.ContainsKey(host);

  public void ForwardPlay(PropagatingAudioSource host, AudioClip clip, float volume, bool loop)
  {
    if (ConnectSpeakers(host))
    {
      List<PropagatingSpeaker> speakers = ConnectedSpeakers[host];
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
      List<PropagatingSpeaker> speakers = ConnectedSpeakers[host];
      foreach(PropagatingSpeaker speaker in speakers)
      {
        speaker.audioSource.PlayOneShot(clip, volume * speaker.Dampening);
      }
    }
  }

  public void ForwardStop(PropagatingAudioSource host)
  {
    if(IsConnected(host))
    {
      List<PropagatingSpeaker> speakers = ConnectedSpeakers[host];
      foreach(PropagatingSpeaker speaker in speakers)
      {
        speaker.audioSource.Stop();
      }
      FreeSpeakers(speakers);
      ConnectedSpeakers.Remove(host);
    }
  }  

  public void UpdateVolume(PropagatingAudioSource host, float volume)
  {
    if(IsConnected(host))
    {
      List<PropagatingSpeaker> speakers = ConnectedSpeakers[host];
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
      List<PropagatingSpeaker> speakers = ConnectedSpeakers[host];
      foreach (PropagatingSpeaker speaker in speakers)
      {
        speaker.audioSource.clip = clip;
      }
    }
  }

  private bool ConnectSpeakers(PropagatingAudioSource host)
  {
    if (!IsConnected(host))
    {
      PropagatingSoundGateway[] gateways = RoomToNeighbourPositions[host.HostRoom];

      List<PropagatingSpeaker> speakers = new List<PropagatingSpeaker>();

      for (int i = 0; i < gateways.Length; i++)
      {
        PropagatingSpeaker speaker = GetNextFree();
        if (speaker == null)
        {
          FreeSpeakers(speakers);
          return false;
        }
        else
        {
          speaker.Dampening = gateways[i].Dampening;
          speakers.Add(speaker);
        }
      }

      // All required speakers are available - position them correctly
      for(int i  = 0; i < speakers.Count; i++)
      {
        speakers[i].audioSource.transform.position = gateways[i].SoundTarget;
      }

      ConnectedSpeakers[host] = speakers;
    }

    return true;
  }

  private PropagatingSpeaker GetNextFree()
  {
    for(int i = 0; i < 50; i++)
    {
      if(!audioSourcePool[poolIndex].Connected)
      {
        audioSourcePool[poolIndex].Connected = true;
        poolIndex++;
        return audioSourcePool[poolIndex - 1];
      }
      else
      {
        poolIndex = (int)((poolIndex + 1) % AudioClipPoolSize);
      }
    }
    Debug.Log("Propagating Audio Pool ran out of audio sources");
    return null;
  }

  private void FreeSpeakers(List<PropagatingSpeaker> speakers)
  {
    foreach(PropagatingSpeaker speaker in speakers)
    {
      speaker.Connected = false;
    }
  }
}
