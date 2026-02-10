using System;
using UnityEngine;

public class WheelsRotate : MonoBehaviour {

    public float SpeedRotate;
    Vector3 OldPosition;
	Vector3 NewPosition;
	public GameObject Wheel_Front_L;
	public GameObject Wheel_Front_R;
	public GameObject Wheel_Back_L;
	public GameObject Wheel_Back_R;
	public Transform RotationBase;
	void Update()
	{

		NewPosition = transform.position;

		var Direction = NewPosition - OldPosition;
		var Dot = RotationBase.InverseTransformDirection(Direction).z;

		if (Direction.magnitude > 0.01f)

		{
			Wheel_Front_L.transform.Rotate(new Vector3(Mathf.Sign(Dot)* SpeedRotate, 0, 0) * Time.deltaTime);
			Wheel_Front_R.transform.Rotate(new Vector3(Mathf.Sign(Dot) * SpeedRotate, 0, 0) * Time.deltaTime);
			Wheel_Back_L.transform.Rotate(new Vector3(Mathf.Sign(Dot) * SpeedRotate, 0, 0) * Time.deltaTime);
			Wheel_Back_R.transform.Rotate(new Vector3(Mathf.Sign(Dot) * SpeedRotate, 0, 0) * Time.deltaTime);
		}

		OldPosition = NewPosition;
	}
}