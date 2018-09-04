using UnityEngine;

public class PropagatingSpeaker {
  public AudioSource audioSource;
  public bool Connected;
  public float Dampening;

  public PropagatingSpeaker(AudioSource source)
  {
    audioSource = source;
  }
}
