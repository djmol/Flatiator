using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HoldableObject : MonoBehaviour {

	GameObject holder;
	Transform holdLocation;
	Rigidbody rb;
	Collider cd;
	List<Collider> otherCd;
	bool isHeld;
	int numCollisions;

	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody>();
		cd = GetComponent<Collider>();
		otherCd = new List<Collider>();
		isHeld = false;
	}
	
	// Update is called once per frame
	void Update () {

	}

	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate() {
		// Return item to hold position
		if (isHeld && numCollisions == 0) {
			transform.position = Vector3.Lerp(transform.position, holdLocation.position, Time.deltaTime * 7);
		}
	}

	public void Grab (GameObject holder, Transform holdLocation) {
		rb.velocity = Vector3.zero;
		this.holder = holder;
		this.holdLocation = holdLocation;
		otherCd.AddRange(holder.GetComponentsInChildren<Collider>());
		isHeld = true;
		rb.useGravity = false;
		rb.freezeRotation = true;
		transform.position = holdLocation.position;


		foreach (Collider oCd in otherCd) {
			Physics.IgnoreCollision(cd, oCd, true);
		}

		transform.SetParent(holdLocation);
	}

	public void Drop (Vector3 direction = default(Vector3)) {
		this.holder = null;
		this.holdLocation = null;

		foreach (Collider oCd in otherCd) {
			Physics.IgnoreCollision(cd, oCd, false);
		}

		otherCd.Clear();
		isHeld = false;
		rb.useGravity = true;
		rb.freezeRotation = false;
		transform.SetParent(null);
		rb.AddForce(direction, ForceMode.Impulse);
	}

	/// <summary>
	/// OnCollisionEnter is called when this collider/rigidbody has begun
	/// touching another rigidbody/collider.
	/// </summary>
	/// <param name="other">The Collision data associated with this collision.</param>
	void OnCollisionEnter(Collision other) {
		numCollisions++;
	}

	/// <summary>
	/// OnCollisionExit is called when this collider/rigidbody has
	/// stopped touching another rigidbody/collider.
	/// </summary>
	/// <param name="other">The Collision data associated with this collision.</param>
	void OnCollisionExit(Collision other) {
		numCollisions--;
	}

	/// <summary>
	/// OnGUI is called for rendering and handling GUI events.
	/// This function can be called multiple times per frame (one call per event).
	/// </summary>
	void OnGUI()
	{
		// FOR DEBUGGING: Tracking number of collisions
		GUI.Label(new Rect(0,0,130,30),numCollisions.ToString());
	}
}
