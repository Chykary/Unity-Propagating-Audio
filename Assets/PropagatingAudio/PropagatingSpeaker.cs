using UnityEngine;

public class PropagatingSpeaker {
  public AudioSource audioSource;

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
      audioSource.gameObject.SetActive(connected);
    }
  }
  public float Dampening;

  public PropagatingSpeaker(AudioSource source)
  {
    audioSource = source;
    Connected = false;
  }
}
