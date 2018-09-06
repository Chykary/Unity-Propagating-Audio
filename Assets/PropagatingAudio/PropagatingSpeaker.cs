using UnityEngine;

public class PropagatingSpeaker {
  public AudioSource audioSource;

  public bool IsPlaying => audioSource.isPlaying;

  private bool connected;
  public bool Connected
  {
    get
    {
      return connected;
    }
    set
    {
      connected = value;
      // Required for Game Shutdown (AudioSource will be destroyed)
      if (audioSource != null)
      {
        audioSource.gameObject.SetActive(connected);
      }
    }
  }
  public float Dampening;

  public PropagatingSpeaker(AudioSource source)
  {
    audioSource = source;
    Connected = false;
  }
}
