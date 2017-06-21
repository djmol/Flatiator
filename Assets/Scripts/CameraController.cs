using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public Transform character;
	Vector3 offset;

	// Use this for initialization
	void Start () {
		offset = transform.position - character.position;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	/// <summary>
	/// LateUpdate is called every frame, if the Behaviour is enabled.
	/// It is called after all Update functions have been called.
	/// </summary>
	void LateUpdate() {
		transform.position = character.position + offset;
	}

	/// <summary>
	/// OnGUI is called for rendering and handling GUI events.
	/// This function can be called multiple times per frame (one call per event).
	/// </summary>
	void OnGUI() {
		Vector3 p = new Vector3();
		Camera c = Camera.main;
		Vector2 mousePos = new Vector2();
		mousePos.x = Input.mousePosition.x;
        mousePos.y = c.pixelHeight - Input.mousePosition.y;

		p = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane));
		
		GUILayout.BeginArea(new Rect(20, 20, 250, 120));
        GUILayout.Label("Screen pixels: " + c.pixelWidth + ":" + c.pixelHeight);
        GUILayout.Label("Mouse position: " + mousePos);
        GUILayout.Label("World position: " + p.ToString("F3"));
        GUILayout.EndArea();
	}
}
