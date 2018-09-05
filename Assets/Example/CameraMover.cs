using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour {
  public Vector3 left;
  public Vector3 right;

  public PropagatingAudioSource Radio;

  public GameObject Room;
  public GameObject Boom;

  private bool isLeft;

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
      transform.position = isLeft ? left : right;
      isLeft = !isLeft;
    }

    if (Input.GetKeyDown(KeyCode.Alpha2))
    {
      if (Radio.isPlaying)
      {
        Radio.Stop();
      }
      else
      {
        Radio.Play();
      }
    }

    if (Input.GetKeyDown(KeyCode.Alpha3))
    {
      GameObject boom = Instantiate(Boom, Room.transform);
      StartCoroutine(DisposeBoom(boom));
    }
    }

  private IEnumerator DisposeBoom(GameObject boom)
  {
    yield return new WaitForSeconds(5f);
    Destroy(boom);
  }
}
