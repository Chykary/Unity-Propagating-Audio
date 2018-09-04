using System.Collections.Generic;
using UnityEngine;

public class Setup : MonoBehaviour {
  public List<GameObject> Rooms;

	void Start () {
    PropagatingAudioSourceManager.Instance.Setup(Rooms);
	}
}
