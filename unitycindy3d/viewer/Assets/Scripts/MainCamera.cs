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

public class MainCamera : MonoBehaviour {

	private Camera cam;
	private Vector3 angle;
	private Vector3 prePosition;
	private bool button_down;

	void Start () {
		cam = GetComponent<Camera> ();	
		angle = Vector3.zero;
		prePosition = Vector3.zero;
		button_down = false;
	}

	void Update () {

		Vector3 position = Input.mousePosition;

		if( Input.GetMouseButtonDown(0) ){
			prePosition = position;
			button_down = true;
		}
		else if( Input.GetMouseButtonUp(0) ){
			button_down = false;
		}
		else if( button_down ){
			angle = GetAngle (prePosition) - GetAngle (position);
			transform.RotateAround (Vector3.zero, Vector3.up, angle.x);
			transform.RotateAround (Vector3.zero, transform.right, -angle.y);
			prePosition = position;
		}
	}

	private Vector3 GetAngle (Vector3 pos)
	{
		var a = 200f;
		var origin = cam.WorldToScreenPoint( Vector3.zero );
		var diff = pos - origin;

		var angle_x = 0f;
		var angle_y = 0f;
		if (diff.magnitude > Mathf.Epsilon) {
			angle_x = Mathf.Atan2 (a, diff.x);
			angle_y = Mathf.Atan2 (a, diff.y);
		}

		return new Vector3 (angle_x, angle_y, 0 ) * Mathf.Rad2Deg;
	}
}
