using System.Collections;
using UnityEngine;

public class PropagatingAudioSource : MonoBehaviour {
  //Set this to false if the object is moving host rooms, in which case the host room is determined on-demand (causes more load)
  public bool ObjectStationary = true;
  //If this is set to true, the host room must be set outside and will not be fetched on demand. This can cause a null exception if set to true and the dev forgets to assign the room.
  public bool HostRoomManualSet = false;

  public AudioRolloffMode RollOfMode = AudioRolloffMode.Linear;
  public float MaxDistance = 20f;
  public AudioClip Clip;

  public float Volume = 1f;
  public bool PlayOnAwake;
  public bool Loop;

  private AudioSource WrappedAudioSource;

  private GameObject hostRoom;
  public GameObject HostRoom
  {
    get
    {
      if (ObjectStationary || HostRoomManualSet)
      {
        // Must have been set in Start() or outside
        return hostRoom;
      }
      else
      {
        return GetComponentInParent<PropagatingHostRoom>().gameObject;
      }
    }
    set
    {
      hostRoom = value;
    }
  }

  public PropagatingAudioSourceManager Manager => PropagatingAudioSourceManager.Instance;

  public bool isPlaying => WrappedAudioSource.isPlaying;

  public float volume
  {
    get
    {
      return WrappedAudioSource.volume;
    }
    set
    {
      WrappedAudioSource.volume = value;
      Manager.UpdateVolume(this, value);
    }
  }

  public AudioClip clip
  {
    get
    {
      return WrappedAudioSource.clip;
    }
    set
    {
      WrappedAudioSource.clip = value;
      Manager.UpdateClip(this, value);
    }
  }

  public bool loop
  {
    get
    {
      return WrappedAudioSource.loop;
    }
    set
    {
      WrappedAudioSource.loop = value;
    }
  }

  public float pitch
  {
    get
    {
      return WrappedAudioSource.pitch;
    }
    set
    {
      WrappedAudioSource.pitch = value;
    }
  }


  private void Start () {
    if (ObjectStationary)
    {
      PropagatingHostRoom phr = GetComponentInParent<PropagatingHostRoom>();
      Debug.Assert(phr != null, $"Propagating Audio Source of {gameObject.name} not a child of host room");
      HostRoom = phr.gameObject;
    }   

    WrappedAudioSource = gameObject.AddComponent<AudioSource>();
    WrappedAudioSource.spatialBlend = 1f;
    WrappedAudioSource.rolloffMode = RollOfMode;
    WrappedAudioSource.maxDistance = MaxDistance;
    WrappedAudioSource.clip = Clip;
    WrappedAudioSource.loop = Loop;
    WrappedAudioSource.volume = Volume;

    if(PlayOnAwake)
    {
      Play();
    }
  }


  public void Stop()
  {
    WrappedAudioSource.Stop();
    Manager.ForwardStop(this);
  }

  public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
  {
    WrappedAudioSource.PlayOneShot(clip, volumeScale);
    Manager.ForwardPlayOneShot(this, clip, volumeScale);
  }

  public void Play()
  {    
    WrappedAudioSource.Play();
    Manager.ForwardPlay(this, WrappedAudioSource.clip, WrappedAudioSource.volume, WrappedAudioSource.loop);
  }

  public void PlayDelayed(float delay)
  {
    Invoke(nameof(Play), delay);
  }

  /// <summary>
  /// Fades out the AudioSource given a specified time.
  /// </summary>
  /// <param name="fadeTime">Time to fade out clip.</param>
  public void FadeOut(float fadeTime) => StartCoroutine(FadeOutRoutine(fadeTime, WrappedAudioSource.volume));

  /// <summary>
  /// Fades the AudioSource from the current volume to a specified target volume given a specified time.
  /// </summary>
  /// <param name="fadeTime">Target volume to fade in to.</param>
  /// <param name="targetVolume">Time to fade in clip.</param>
  public void FadeIn(float targetVolume, float fadeTime) => StartCoroutine(FadeInRoutine(fadeTime, targetVolume));


  private IEnumerator FadeOutRoutine(float fadeTime, float startVolume)
  {
    while (volume > 0)
    {
      volume -= startVolume * Time.deltaTime / fadeTime;

      yield return null;
    }

    Stop();
    volume = startVolume;
  }

  private IEnumerator FadeInRoutine(float targetVolume, float fadeTime)
  {
    while (volume < targetVolume)
    {
      volume += targetVolume * Time.deltaTime / fadeTime;

      yield return null;
    }

    volume = targetVolume;
  }

  private void OnDestroy()
  {
    Manager.Disconnect(this);
  }
}
