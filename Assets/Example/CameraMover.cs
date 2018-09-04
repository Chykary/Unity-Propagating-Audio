using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour {
  public Vector3 left;
  public Vector3 right;
  public float time = 3f;

  private void Start()
  {
    StartCoroutine(Mover());
  }

  private IEnumerator Mover()
  {
    while(true)
    {
      yield return StartCoroutine(Mover(true));
      yield return StartCoroutine(Mover(false));
    }
  }

  private IEnumerator Mover(bool dirRight)
  {
    Vector3 start = dirRight ? left : right;
    Vector3 end = dirRight ? right : left;

    float passed = 0f;
    while(passed < time)
    {
      passed += Time.deltaTime;
      transform.position = Vector3.Lerp(start, end, passed / time);
      yield return null;
    }

    yield return new WaitForSeconds(1.5f);
  }
}
