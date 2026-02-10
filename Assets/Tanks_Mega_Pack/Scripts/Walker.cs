using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Walker : MonoBehaviour {

	Animator _animator;
	public float speed;

	// Use this for initialization
	void Start () {

		_animator = GetComponent<Animator>();
		
	}
	
	// Update is called once per frame
	void Update () {

		bool isWalk = Input.GetKey("space");
		_animator.SetBool("isWalking", isWalk);

		if (_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "Armature|Walk")
		{
		 transform.position += transform.TransformDirection(Vector3.forward)*Time.deltaTime*speed;
		}
		
	}
}
