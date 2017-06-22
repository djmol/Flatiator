using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlattenableObject : MonoBehaviour {

	Collider cd;
	Renderer rend;

	bool showPolygonPoints = false;
	List<Vector2> polygonPoints;

	// Use this for initialization
	void Start () {
		rend = GetComponent<Renderer>();
		cd = GetComponent<Collider>();
		polygonPoints = new List<Vector2>();
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void Flatten(Camera camera) {
		if (showPolygonPoints) {
			showPolygonPoints = false;
			polygonPoints.Clear();
		} else {
			// Get convex hull of object
			Dictionary<Vector2, Vector3> objectPoints = new Dictionary<Vector2, Vector3>();
			List<Vector2> points = GetScreenPositionsFromMeshVertices(camera, ref objectPoints);
			polygonPoints = GetConvexHull(points, ref objectPoints);
			//showPolygonPoints = true;

			// Convert convex hull to a list of coplanar points
			Poly2Mesh.Polygon flatShape = new Poly2Mesh.Polygon();
			List<Vector3> hullPoints = GetHullPoints(polygonPoints, objectPoints);
			Vector3 closest = GetClosestPoint(hullPoints, camera.transform.position);
			List<Vector3> planePoints = MovePointsToPlane(hullPoints, closest, camera.transform.position - closest);

			// Attempt to "thicken" flat object
			Vector3 objDistance = (closest - camera.transform.position);
			Vector3 farPoint = camera.transform.position + objDistance + (objDistance.normalized * .1f);
			List<Vector3> farPlanePoints = MovePointsToPlane(planePoints, farPoint, objDistance);

			// Link pairs in planePoints and farPlanePoints
			List<List<Vector3>> allPlanePoints = new List<List<Vector3>>() { planePoints, farPlanePoints };
			for (int i = 0; i < planePoints.Count(); i++) {
				int j = (i == planePoints.Count() - 1) ? 0 : i + 1;
				allPlanePoints.Add(new List<Vector3>() { 
					planePoints[i], planePoints[j], farPlanePoints[i], farPlanePoints[j] 
				});
			}

			// Create meshes from all plane points


			flatShape.outside = planePoints;
			GameObject flatObject = Poly2Mesh.CreateGameObject(flatShape, "Flat" + name);
			flatObject.AddComponent<MeshCollider>();
			//Rigidbody flatRb = flatObject.AddComponent<Rigidbody>();
			//flatRb.AddForce(new Vector3(0f, 3f, 0f), ForceMode.Impulse);
			Renderer flatRend = flatObject.GetComponent<Renderer>();
			flatRend.material = rend.material;

			foreach (Vector3 ppt in planePoints) {
				GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				sphere.transform.localScale -= new Vector3(0.95f, 0.95f, 0.95f);
				sphere.transform.position = ppt;
			}

			// 6/6: Can't add MeshCollider... 3d-ify object a bit? Use move point to plane to make a dupe set a slight bit away?
			//		Make backside visible? http://answers.unity3d.com/questions/280741/how-make-visible-the-back-face-of-a-mesh.html

			/*string polyV = "";
			foreach (Vector3 v in flatObject.outside) {
				polyV += v + "\n";
				GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				sphere.transform.localScale -= new Vector3(0.95f, 0.95f, 0.95f);
        		sphere.transform.position = v;
			}
			Debug.Log(polyV);*/

			

			Destroy(gameObject, 0);
			
		}
	}

	float TurnOrientation (Vector2 v1, Vector2 v2, Vector2 v3) {
		// https://en.wikipedia.org/wiki/Graham_scan
		// A turn from v1 to v3 is counter-clockwise if return value is > 1,
		// clockwise if < 1, or collinear if = 0

		return (v2.x - v1.x)*(v3.y - v1.y) - (v2.y - v1.y)*(v3.x - v1.x);
	} 

	Vector2 PeekNext(Stack<Vector2> stack) {
		Vector2 next;

		if (stack.Count >= 2) {
			Vector2 top = stack.Pop();
			next = stack.Peek();
			stack.Push(top);
			return next;
		} else {
			// This is a really bad and dumb way of handling this error.
			Debug.Log("Cannot PeekNext!");
			return Vector2.zero;
		}
	}

	void Swap<T> (ref List<T> list, int a, int b) {
		var temp = list[a];
		list[a] = list[b];
		list[b] = temp;
	}

	public static List<Vector2> HeapSortByPolarAngle(List<Vector2> input, Vector2 v) {
		// Modified from https://begeeben.wordpress.com/2012/08/21/heap-sort-in-c/
		// Build max heap
		int heapSize = input.Count;
		for (int p = (heapSize - 1) / 2; p >= 0; p--)
			MaxHeapify(ref input, heapSize, p, v);
	
		for (int i = input.Count - 1; i > 0; i--)
		{
			// Swap
			var temp = input[i];
			input[i] = input[0];
			input[0] = temp;
	
			heapSize--;
			MaxHeapify(ref input, heapSize, 0, v);
		}

		return input;
	}

	private static void MaxHeapify(ref List<Vector2> input, int heapSize, int index, Vector2 v) {
		// Modified from https://begeeben.wordpress.com/2012/08/21/heap-sort-in-c/
		int left = (index + 1) * 2 - 1;
		int right = (index + 1) * 2;
		int largest = 0;
	
		if (left < heapSize && -Mathf.Atan2(input[left].y - v.y, input[left].x - v.x) > -Mathf.Atan2(input[index].y - v.y, input[index].x - v.x))
			largest = left;
		else
			largest = index;
			
		if (right < heapSize && -Mathf.Atan2(input[right].y - v.y, input[right].x - v.x) > -Mathf.Atan2(input[largest].y - v.y, input[largest].x - v.x))
			largest = right;
	
		if (largest != index)
		{
			var temp = input[index];
			input[index] = input[largest];
			input[largest] = temp;
	
			MaxHeapify(ref input, heapSize, largest, v);
		}
	}

	List<Vector2> GetConvexHull(List<Vector2> vectors, ref Dictionary<Vector2, Vector3> objectPoints) {
		int n = vectors.Count;

		// Get vector with lowest y-coordinate
		float minY = float.MaxValue;
		int minYIndex = 1;
		for (int i = 0; i < n; i++) {
			if (vectors[i].y < minY) {
				minY = vectors[i].y;
				minYIndex = i;
			}
		}
		Vector2 keyVector = vectors[minYIndex];
		vectors.Remove(keyVector);

		// Sort vectors by polar angle to X with keyVector
		var beforestr = "";
		foreach (var v in vectors) {
			var comp = -(v.x - keyVector.x) / (v.y - keyVector.y);
			beforestr += v + " " + comp + "\n";
		}
		Debug.Log("Before: " + beforestr);

		vectors = HeapSortByPolarAngle(vectors, keyVector);

		vectors.Insert(0, keyVector);

		var afterstr = "";
		var q = 0;
		foreach (var v in vectors) {
			var comp = -(Mathf.Atan2(v.y - keyVector.y,v.x - keyVector.x)) * 180 / Mathf.PI;
			afterstr += q + ": " + v + " " + comp + "\n";
			q++;
		}
		Debug.Log("After: " + afterstr);

		
		// If multiple points lie at the same angle from the starting point,
		// remove all but the outermost.
		// (http://www.geeksforgeeks.org/convex-hull-set-2-graham-scan/)
		int m = 1; //vectors.Count;
		for (int i = 1; i < n; i++) {
			// Keep removing i while angle of i and i+1 is same with respect to keyVector
			while (i < n - 1 && TurnOrientation(keyVector, vectors[i], vectors[i + 1]) == 0) {
				i++;
				Debug.Log("Removing collinear point!");
			}

			vectors[m] = vectors[i];
			m++;  // Update size of modified array
		}

		// Find convex hull
		// BUG: Sometimes fails 3 PeekNexts and exceptions out?
		Stack<Vector2> convexHull = new Stack<Vector2>();
		convexHull.Push(vectors[0]);
		convexHull.Push(vectors[1]);
		convexHull.Push(vectors[2]);
		for (int i = 3; i < m; i++) {
			while (TurnOrientation(PeekNext(convexHull), convexHull.Peek(), vectors[i]) >= 0) {
				convexHull.Pop();
			}
			convexHull.Push(vectors[i]);
		}

		return convexHull.ToList();
	}

	List<Vector3> GetHullPoints(List<Vector2> hull, Dictionary<Vector2, Vector3> objectPoints) {
		List<Vector3> hullPoints = new List<Vector3>();
		foreach (Vector2 point in hull) {
			Vector3 v3;
			objectPoints.TryGetValue(point, out v3);
			hullPoints.Add(v3);
		}

		return hullPoints;
	}

	List<Vector3> MovePointsToPlane(List<Vector3> points, Vector3 planeOrigin, Vector3 planeNormal) {
		List<Vector3> planePoints = new List<Vector3>();

		foreach (Vector3 v in points) {
			Vector3 vec = v - planeOrigin;
			Vector3 d = Vector3.Project(vec, planeNormal.normalized);
			Vector3 projectedPoint = v - d;
			planePoints.Add(projectedPoint);

			/*GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
			cube.transform.localScale -= new Vector3(0.95f, 0.95f, 0.95f);
			cube.transform.position = projectedPoint;*/
		}

		return planePoints;
	}

	List<Vector2> GetScreenPositionsFromRendererBounds(Camera camera) {
		var screenPositions = new List<Vector2>();
		
		var renderers = gameObject.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
		for (var i = 1; i < renderers.Length; i++) {
            bounds.Encapsulate(renderers[i].bounds);
        }
		var ext = bounds.extents;
		for (var x = -1; x < 2; x+=2)
        {
            for (var y = -1; y < 2; y+=2)
            {
                for (var z = -1; z < 2; z+=2)
                {
                    Vector3 vect = bounds.center + new Vector3(ext.x*x, ext.y*y, ext.z*z);
					vect = Quaternion.Euler(transform.rotation.eulerAngles) * (vect - bounds.center) + bounds.center;
					Debug.DrawRay(Camera.main.transform.position, vect - Camera.main.transform.position, Color.green, 1*Time.smoothDeltaTime);
                    var wts = camera.WorldToScreenPoint(vect);
                    wts.y = Screen.height - wts.y;
                    screenPositions.Add(wts);
                }
            }
        }

		return screenPositions;
	}

	List<Vector2> GetScreenPositionsFromMeshBounds(Camera camera, ref Dictionary<Vector2, Vector3> dict) {
        List<Vector2> screenPositions = new List<Vector2>();

        var mFilters = new List<MeshFilter>(gameObject.GetComponentsInChildren<MeshFilter>());
        mFilters.RemoveAll(filter => filter.sharedMesh == null);
        var bounds = mFilters[0].sharedMesh.bounds;
        bounds.center = gameObject.transform.TransformPoint(bounds.center);
        for (var i = 1; i < mFilters.Count; i++)
        {
            var b = mFilters[i].sharedMesh.bounds;
            b.center = gameObject.transform.TransformPoint(b.center);
            bounds.Encapsulate(b);
        }
        var ext = bounds.extents;
        ext.Scale(gameObject.transform.localScale);
        
        for (var x = -1; x < 2; x += 2)
        {
            for (var y = -1; y < 2; y += 2)
            {
                for (var z = -1; z < 2; z += 2)
                {
                    var vect = bounds.center + new Vector3(ext.x * x, ext.y * y, ext.z * z);
                    var wts = camera.WorldToScreenPoint(vect);
                    wts.y = Screen.height - wts.y;
					// Add vector pair to our dictionary of object points
					if (dict.ContainsKey(wts)) {
						Vector3 otherVect = new Vector3();
						dict.TryGetValue(wts, out otherVect);
						// Keep vector with shorter distance to camera/player
						vect = ShorterDistance(vect, otherVect, camera.transform.position);
					}
					
					dict.Add(wts, vect);
                    screenPositions.Add(wts);
                }
            }
        }
        
		return screenPositions;
    }

	List<Vector2> GetScreenPositionsFromMeshVertices(Camera camera, ref Dictionary<Vector2, Vector3> dict) {
        List<Vector2> screenPositions = new List<Vector2>();

		Mesh mesh = GetComponent<MeshFilter>().mesh;
		foreach (Vector3 v in mesh.vertices)
		{
			Vector3 vect = transform.TransformPoint(v); //v + transform.position;
			var wts = camera.WorldToScreenPoint(vect);
			wts.y = Screen.height - wts.y;

			// Add vector pair to our dictionary of object points
			if (dict.ContainsKey(wts)) {
				Vector3 otherVect = new Vector3();
				dict.TryGetValue(wts, out otherVect);
				// Keep vector with shorter distance to camera/player
				vect = ShorterDistance(vect, otherVect, camera.transform.position);
				dict.Remove(wts);
				screenPositions.Remove(wts);
			}

			dict.Add(wts, vect);
			screenPositions.Add(wts);
		}

		return screenPositions;
    }

	Vector3 GetClosestPoint(List<Vector3> points, Vector3 pos) {
		Vector3 closest = new Vector3();
		float minDist = float.MaxValue;

		foreach (Vector3 point in points) {
			float dist = Vector3.Distance(point, pos);
			if (dist < minDist) {
				closest = point;
				minDist = dist;
			}
		}

		return closest;
	}

	Vector3 ShorterDistance(Vector3 v1, Vector3 v2, Vector3 pos) {
		return (Vector3.Distance(v1,pos) > Vector3.Distance(v2, pos)) ? v2 : v1;
	}

	/// <summary>
	/// OnGUI is called for rendering and handling GUI events.
	/// This function can be called multiple times per frame (one call per event).
	/// </summary>
	void OnGUI()
	{
		List<Vector2> screenPositions = new List<Vector2>();
		if (showPolygonPoints) {
			int i = 0;
			foreach (Vector2 pos in polygonPoints) {
				//GUI.Box(new Rect(pos, new Vector2(5,5)), "" + i);
				GUI.TextArea(new Rect(pos, new Vector2(50,20)), "" + pos.x);
				i++;
			}
		} else {
			foreach (Vector2 pos in screenPositions) {
				GUI.Box(new Rect(pos, Vector2.one), "");
			}
		}
	}
}
