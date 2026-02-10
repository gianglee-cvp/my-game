using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropellersRotate : MonoBehaviour {

	public float SpeedRotate;

	// Update is called once per frame
	void Update () {
		transform.Rotate(new Vector3(0, SpeedRotate, 0) * Time.deltaTime);
	}
}
