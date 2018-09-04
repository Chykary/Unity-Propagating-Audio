using System.Collections.Generic;
using UnityEngine;

public class Setup : MonoBehaviour {
  public List<GameObject> Rooms;

	void Awake () {
    PropagatingAudioSourceManager.Instance.Setup(Rooms);
	}
}
