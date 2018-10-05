/*
Copyright (C) 2018 https://github.com/tmytokai/CindyJSPlugins

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandAloneSample : MonoBehaviour {

	public GameObject GeometricObjectPrefab;

	private GameObject sphere = null;
	private GameObject torus = null;
	private GameObject enneper = null;

	void Start () {		
	}

	void Update () {

		if (Input.GetKeyDown (KeyCode.F1)) {

			if (sphere == null) {
				sphere = Instantiate (GeometricObjectPrefab, new Vector3( 0f,0f,0f), Quaternion.identity );
				sphere.name = "Sphere";
				Sphere (sphere.GetComponent<GeometricObject> ());
			} else {
				Destroy (sphere);
				sphere = null;
			}
		}

		if (Input.GetKeyDown (KeyCode.F2)) {

			if (torus == null) {
				torus = Instantiate (GeometricObjectPrefab, new Vector3( 0f,0f,1.5f), Quaternion.identity );
				torus.name = "TorusKnot";
				TorusKnot (torus.GetComponent<GeometricObject> ());
			} else {
				Destroy (torus);
				torus = null;
			}
		}

		if (Input.GetKeyDown (KeyCode.F3)) {

			if (enneper == null) {
				enneper = Instantiate (GeometricObjectPrefab, new Vector3( 0f,0f,-1.5f), Quaternion.identity );
				enneper.name = "Enneper";
				Enneper (enneper.GetComponent<GeometricObject> ());
			} else {
				Destroy (enneper);
				enneper = null;
			}
		}
	}

	void Sphere ( GeometricObject obj ){

		var fcl = new Color (0.5f, 0.5f, 0.5f, 1.0f);
		var stacks = 20;
		var slices = 2 * stacks;
		var radius = 1.0f;

		obj.Begin ();
		obj.SetSphereStacks ( stacks );
		obj.SetSphereSlices ( slices );
		obj.SetSphereRadius ( radius );
		obj.SetSphereFrontColor ( fcl );
		obj.AddSphere ( Vector3.zero );
		obj.End ();
	}

	// see https://en.wikipedia.org/wiki/Torus_knot
	void TorusKnot ( GeometricObject obj ){

		System.Func<float, Vector3> f = (phi) => {
			var o = new Vector3 ();
			var p = 3f;
			var q = 8f;
			var r = Mathf.Cos (q * phi) + 2f;
			o.x = r * Mathf.Sin (p * phi);
			o.y = r * Mathf.Cos (p * phi);
			o.z = -Mathf.Sin (q * phi);
			return o;
		};

		var alpha = 1.0f;
		var n = 1200;
		var frontColors = new List<Color32> ();
		var points = new List<Vector3> ();

		frontColors.Clear ();
		for (var i = 0; i < n; ++i) {
			var	hue = (float)i / n;
			var cl = Color.HSVToRGB (hue, 1f, 1f);
			cl.a = alpha;
			frontColors.Add( cl );
		}

		points.Clear();
		for (var i = 0; i < n; ++i) {
			var w = 2f * Mathf.PI * (float)i / n;
			points.Add ( f(w) );
		}

		obj.Begin ();
		obj.SetLineRadius (0.3f);
		obj.SetLineTopology (Pen.Topology.Close);
		obj.AddLine( points, frontColors );
		obj.End ();
	}

	// see https://en.wikipedia.org/wiki/Enneper_surface
	void Enneper ( GeometricObject obj ){

		System.Func<int, int, Vector3> f = (r, c) => {
			var o = new Vector3 ();
			var u = (float)r / 10f;
			var v = (float)c / 10f;
			o.x = u * (1f - u * u + v * v) / 3f; 
			o.y = v * (1f - v * v + u * u) / 3f;
			o.z = (u * u - v * v) / 3f;
			return o*0.5f;
		};

		var alpha = 1.0f;
		var pcl = new Color (1.0f, 1.0f, 1.0f, alpha);
		var fcl = new Color (0.5f, 0.5f, 1.0f, alpha);
		var bcl = new Color (0.5f, 0.5f, 0.0f, alpha);
		var rows = 51;
		var cols = 51;
		var idxs = new int[rows, cols];
		var points = new List<Vector3> ();

		points.Clear();
		for ( var r = -rows/2; r <= rows/2; r++) {			
			for (var c = -cols/2; c <= cols/2; c++) {
				idxs [rows/2+r, cols/2+c] = points.Count;
				points.Add (f(r,c));
			}
		}

		obj.Begin ();
		obj.SetPointFrontColor( pcl );
		for (var r = 0; r < rows; r++) {			
			for (var c = 0; c < cols; c++) {
				obj.AddPoint (points [idxs[r,c]]);
			}
		}
		obj.SetMeshFrontColor ( fcl );
		obj.SetMeshBackColor ( bcl );
		obj.AddMesh( rows, cols, points, idxs );
		obj.End ();
	}

}
