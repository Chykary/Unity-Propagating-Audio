using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(PropagatingAudioSource))]
public class NetworkedPropagatingAudioSource : NetworkBehaviour {
  public PropagatingAudioSource NetworkedAudioSource;

  private static Dictionary<int, AudioClip> IdToAudioClip;
  private static Dictionary<AudioClip, int> AudioClipToId;
  public static bool Initialised = false;

  private Coroutine loopRoutine;

  public static void Initialise(List<AudioClip> clips)
  {
    Debug.Assert(IdToAudioClip == null, "Must only initialise NetworkedPropagatingAudioSource once");
    Initialised = true;

    clips.Sort(delegate (AudioClip x, AudioClip y)
    {
      if (x.GetHashCode() > y.GetHashCode())
      {
        return 1;
      }
      else
      {
        return -1;
      }
    });

    IdToAudioClip = new Dictionary<int, AudioClip>();
    AudioClipToId = new Dictionary<AudioClip, int>();

    foreach (AudioClip audioClip in clips)
    {
      Debug.Assert(!AudioClipToId.ContainsKey(audioClip), "Hash Collision! You can fix this by renaming the audioclip with name " + audioClip.name);

      IdToAudioClip.Add(audioClip.name.GetHashCode(), audioClip);
      AudioClipToId.Add(audioClip, audioClip.name.GetHashCode());
    }
  }

  [Server]
  public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
  {
    int id = AudioClipToId[clip];
    RpcPlayOneShot(id, volumeScale);
  }

  [ClientRpc]
  private void RpcPlayOneShot(int clipId, float volumeScale)
  {
    AudioClip clip = IdToAudioClip[clipId];
    NetworkedAudioSource.PlayOneShot(clip, volumeScale);
  }

  [Server]
  public void Stop()
  {
    RpcStop();
  }

  [ClientRpc]
  private void RpcStop()
  {
    NetworkedAudioSource.Stop();
  }

  [Server]
  public void Play()
  {
    RpcPlay();
  }

  [ClientRpc]
  private void RpcPlay()
  {
    NetworkedAudioSource.Play();
  }

  [Server]
  public void PlayAndLoop(AudioClip clip, float volume = 1)
  {
    int id = AudioClipToId[clip];
    RpcPlayAndLoop(id, volume);
  }

  [ClientRpc]
  private void RpcPlayAndLoop(int clipId, float volume)
  {
    AudioClip clip = IdToAudioClip[clipId];
    NetworkedAudioSource.clip = clip;
    NetworkedAudioSource.loop = true;
    NetworkedAudioSource.volume = volume;
  }

  /// <summary>
  /// Randomly and endlessly plays one of the provided clips with a random time distance specified.
  /// </summary>
  /// <param name="minTime">minimum time until next clip</param>
  /// <param name="maxTime">maximum time until next clip</param>
  /// <param name="clips">clips to choose from</param>
  [Server]
  public void LoopRandomClips(float minTime, float maxTime, params AudioClip[] clips)
  {
    Debug.Assert(loopRoutine == null, "Starting Coroutine, but another one is still running which can not be stopped anymore");
    loopRoutine = StartCoroutine(LoopRandomClips(clips, minTime, maxTime));
  }

  private IEnumerator LoopRandomClips(AudioClip[] clips, float minTime, float maxTime)
  {
    while (true)
    {
      PlayOneShot(clips[Random.Range(0, clips.Length)]);
      yield return new WaitForSeconds(Random.Range(minTime, maxTime));
    }
  }

  /// <summary>
  /// Stops the playing of random and endless clips.
  /// </summary>
  [Server]
  public void StopLoopRandomClips()
  {
    if (loopRoutine != null)
    {
      StopCoroutine(loopRoutine);
      loopRoutine = null;
    }
  }

  [Server]
  public void FadeOut(float fadeTime)
  {
    RpcFadeOut(fadeTime);
  }

  [ClientRpc]
  private void RpcFadeOut(float fadeTime)
  {
    NetworkedAudioSource.FadeOut(fadeTime);
  }

  [Server]
  public void FadeIn(float targetVolume, float fadeTime)
  {
    RpcFadeIn(targetVolume, fadeTime);
  }

  [ClientRpc]
  private void RpcFadeIn(float targetVolume, float fadeTime)
  {
    NetworkedAudioSource.FadeIn(targetVolume, fadeTime);
  }  

  [Server]
  public void PlayDelayed(float delay)
  {
    Invoke(nameof(Play), delay);
  }
}
