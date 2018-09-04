using System;
using UnityEngine;

public class PropagatingSoundGateway : MonoBehaviour {
  public Transform soundTarget;
  public float Dampening = 0.3f;

  private Func<Vector3> getTarget;
  public Vector3 SoundTarget
  {
    get
    {
      if(soundTarget == null)
      {
        return getTarget();
      }
      else
      {
        return soundTarget.position;
      }
    }
  }

  public void NotifyTargetDeferred(Func<Vector3> target)
  {
    getTarget = target;
  }
}
