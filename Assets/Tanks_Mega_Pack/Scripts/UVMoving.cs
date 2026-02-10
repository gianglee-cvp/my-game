using System;
using UnityEngine;

public class UVMoving : MonoBehaviour {

    public float scrollSpeed;
	// скорость юви анимации
	float offset;
	Renderer rend;
    Vector3 OldPosition;
	Vector3 NewPosition;
	public GameObject MainObject;
	public Transform RotationBase;
	// здесь выбираешь основной, родительский объект - на основе его передвижений работает скрипт
	void Start()

	{
		rend = GetComponent<Renderer>();
	}

	void Update()

	{

		NewPosition = MainObject.transform.position;

		var Direction = NewPosition - OldPosition;
		var Dot = RotationBase.InverseTransformDirection(Direction).z;

		if (Direction.magnitude > 0.01f)

		{
			float offset = rend.material.mainTextureOffset.y + Time.deltaTime * scrollSpeed * Mathf.Sign(Dot);
			rend.material.mainTextureOffset = new Vector2(0, offset);
		}

		OldPosition = NewPosition;

	}
}


	