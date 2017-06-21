using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine;

public class PlayerMovementController : MonoBehaviour {

	public float speed = 6.0F;
    public float jumpSpeed = 8.0F;
    public float gravity = 20.0F;
	public float grabDistance = 2.0F;
	public float throwForce = 7.0F;
	public Transform holdLocation;

	[SerializeField] MouseLook mouseLook;
	CharacterController controller;
	Vector3 moveDirection = Vector3.zero;
	float lastY;
	Camera mainCamera;
	bool holdingItem = false;
	HoldableObject holdableObject = null;
	FlattenableObject flattenableObject = null;

	// Use this for initialization
	void Start () {
		controller = GetComponent<CharacterController>();
		mainCamera = Camera.main;
		mouseLook.Init(transform, mainCamera.transform);
	}
	
	// Update is called once per frame
	void Update () {
        mouseLook.LookRotation(transform, mainCamera.transform);

		// Holding & dropping items
		if (Input.GetButtonUp("Fire1") || Input.GetButtonUp("Fire2")) {
			// Grabbing
			if (!holdingItem) {
				holdableObject = GrabHoldableObject();
				if (holdableObject != null) {
					holdableObject.Grab(gameObject, holdLocation);
					holdingItem = true;
				}
			// Dropping
			} else {
				if (Input.GetButtonUp("Fire1")) {
					holdableObject.Drop();
				} else if (Input.GetButtonUp("Fire2")) {
					holdableObject.Drop(new Vector3(transform.forward.x, mainCamera.ScreenPointToRay(Input.mousePosition).direction.y, transform.forward.z) * throwForce);
				}
				holdableObject = null;
				holdingItem = false;
			}
		}

		// Flatten object
		if (Input.GetButtonUp("Fire3")) {
			flattenableObject = GetFlattenableObject();
			if (flattenableObject != null)
				flattenableObject.Flatten(mainCamera);
		}
	}

	HoldableObject GrabHoldableObject() {
		HoldableObject holdableObject = null;

		RaycastHit hit;
		Ray camRay = mainCamera.ScreenPointToRay(Input.mousePosition);
		Debug.DrawRay(camRay.origin, camRay.direction, Color.green, 1.0f);
		if (Physics.Raycast(camRay.origin, camRay.direction, out hit, grabDistance)) {
			if (hit.collider.gameObject.layer == LayerMask.NameToLayer("HoldObject")) {
				holdableObject = hit.collider.gameObject.GetComponent<HoldableObject>();
			}
		}

		return holdableObject;
	}

	FlattenableObject GetFlattenableObject() {
		FlattenableObject holdableObject = null;

		RaycastHit hit;
		Ray camRay = mainCamera.ScreenPointToRay(Input.mousePosition);
		Debug.DrawRay(camRay.origin, camRay.direction, Color.green, 1.0f);
		if (Physics.Raycast(camRay.origin, camRay.direction, out hit, grabDistance)) {
			// TODO: HoldObjects shouldn't == FlattenableObjects
			if (hit.collider.gameObject.layer == LayerMask.NameToLayer("HoldObject")) {
				holdableObject = hit.collider.gameObject.GetComponent<FlattenableObject>();
			}
		}

		return holdableObject;
	}


	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate() {
		if (controller.isGrounded) {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
			moveDirection = transform.TransformDirection(moveDirection);
			moveDirection *= speed;
            if (Input.GetButton("Jump"))
                moveDirection.y = jumpSpeed;
        } else {
			// Allow lateral movement while jumping
			moveDirection = new Vector3(Input.GetAxis("Horizontal"), lastY, Input.GetAxis("Vertical"));
			moveDirection = transform.TransformDirection(moveDirection);
			moveDirection.x *= speed;
			moveDirection.z *= speed;
		}
        moveDirection.y -= gravity * Time.deltaTime;
		lastY = moveDirection.y;
        controller.Move(moveDirection * Time.deltaTime);
	}
}
