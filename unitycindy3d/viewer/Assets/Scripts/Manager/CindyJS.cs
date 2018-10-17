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
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class CindyJS : MonoBehaviour {

#if UNITY_WEBGL

	private enum MODIFIERS : byte
	{
		Radius = 0,
		Color,
		Colors,
		Topology,

		MODIFIERSIZE
	};

	private enum TOPOLOGY : byte
	{
		Open = 0,
		Close,

		TOPOLOGYSIZE
	};
	
	[DllImport("__Internal")]
	private static extern void InitBufferCS( float[] offset, int length );

	[DllImport("__Internal")]
	private static extern void StartCS();

	[DllImport("__Internal")]
	private static extern void OnDestroyCS( int id );

	[DllImport("__Internal")]
	private static extern void CollisionEnterCS( int objid1, string classname1, int objid2, string classname2 );

	[SerializeField] private GameObject GeometricObjectPrefab;

	[SerializeField] private Text statusText = null;
	private float counter_status = 0;

	private Dictionary<int,GeometricObject> gobjs;
	private int id_head = 0;
	private GeometricObject gobj_drawing = null;

	private List<Vector3> points;
	private List<Vector3> angles;
	private float radius;
	private Color32 frontColor;
	private List<Color32> frontColors ;
	private TOPOLOGY topology;

	private float POINT_SCALE = 0.05f;

	private int uc3dBufferSize = 256; // kbyte
	private float[] uc3dBuffer;
	private int idx_uc3dBuffer;

	private bool testmode = false;
	private float[] id_test = {-1,-1};

	void Start () {

		gobjs = new Dictionary<int, GeometricObject>();

		points = new List<Vector3> ();
		angles = new List<Vector3> ();
		frontColors = new List<Color32> ();

		uc3dBuffer = new float[ uc3dBufferSize * 1024 /4 ];

		Clear3D();

		#if !UNITY_EDITOR
		WebGLInput.captureAllKeyboardInput = false;
		InitBufferCS( uc3dBuffer, uc3dBuffer.Length );
		StartCS ();
		#endif
	}
	
	void Update () {
		if (Input.GetKeyDown (KeyCode.F3)) {
			testmode = !testmode;
			statusText.enabled = testmode;
			counter_status = 0;
		}
		if (Input.GetKeyDown (KeyCode.F4)) {
			statusText.enabled = !statusText.enabled;
			counter_status = 0;
		}
		
		if( testmode ) TestMode();
		if( statusText.enabled ) ShowStatus();
	}

	private void TestMode(){

		if ( Input.GetKeyDown (KeyCode.S) ) {
			if( id_test[0] == -1f ){

				idx_uc3dBuffer = 0;
				uc3dBuffer[idx_uc3dBuffer++] = 1f; // active
				Begin3D( "Test_Sphere" );
				id_test[0] = uc3dBuffer[0];

				idx_uc3dBuffer = 0;
				uc3dBuffer[idx_uc3dBuffer++] = 1f;
				uc3dBuffer[idx_uc3dBuffer++] = 2f; //x
				uc3dBuffer[idx_uc3dBuffer++] = 0f; //y
				uc3dBuffer[idx_uc3dBuffer++] = 0f; //z

				uc3dBuffer[idx_uc3dBuffer++] = (float)MODIFIERS.Radius;
				uc3dBuffer[idx_uc3dBuffer++] = 0.5f; 

				uc3dBuffer[idx_uc3dBuffer++] = (float)MODIFIERS.Color;
				uc3dBuffer[idx_uc3dBuffer++] = 1f; // r
				uc3dBuffer[idx_uc3dBuffer++] = 1f; // g
				uc3dBuffer[idx_uc3dBuffer++] = 1f; // b

				uc3dBuffer[idx_uc3dBuffer++] = -1f;
				AddSphere3D();

				End3D();				
			}
			else{
				idx_uc3dBuffer = 0;
				uc3dBuffer[idx_uc3dBuffer++] = id_test[0]; // id
				uc3dBuffer[idx_uc3dBuffer++] = 0f; // sec
				Destroy3D();
				id_test[0] = -1;
			}
		}
		if ( Input.GetKeyDown (KeyCode.T) ) {
			if( id_test[1] == -1f ){

				idx_uc3dBuffer = 0;
				uc3dBuffer[idx_uc3dBuffer++] = 1f; // active
				Begin3D( "Test_Torus" );
				id_test[1] = uc3dBuffer[0];

				var n = 600;
				var p = 3f;
				var q = 8f;

				idx_uc3dBuffer = 0;
				uc3dBuffer[idx_uc3dBuffer++] = (float)n;
				for( var i = 0; i< n; ++i ){
					var w = 2f * Mathf.PI * (float)i / n;
					var r = Mathf.Cos (q * w) + 2f;
					uc3dBuffer[idx_uc3dBuffer++] = Mathf.Sin (q * w); //x
					uc3dBuffer[idx_uc3dBuffer++] = r * Mathf.Cos (p * w); //y
					uc3dBuffer[idx_uc3dBuffer++] = r * Mathf.Sin (p * w); //z
				}

				uc3dBuffer[idx_uc3dBuffer++] = (float)MODIFIERS.Colors;
				uc3dBuffer[idx_uc3dBuffer++] = (float)(n+1);
				for (var i = 0; i < n+1; ++i) {
					var	hue = (float)i / n;
					var cl = Color.HSVToRGB (hue, 1f, 1f);
					uc3dBuffer[idx_uc3dBuffer++] = cl.r; // r
					uc3dBuffer[idx_uc3dBuffer++] = cl.g; // g
					uc3dBuffer[idx_uc3dBuffer++] = cl.b; // b
				}

				uc3dBuffer[idx_uc3dBuffer++] = (float)MODIFIERS.Radius;
				uc3dBuffer[idx_uc3dBuffer++] = 4f; 

				uc3dBuffer[idx_uc3dBuffer++] = (float)MODIFIERS.Topology;
				uc3dBuffer[idx_uc3dBuffer++] = (float)TOPOLOGY.Close;

				uc3dBuffer[idx_uc3dBuffer++] = -1f;

				AddLine3D();

				End3D();				
			}
			else{
				idx_uc3dBuffer = 0;
				uc3dBuffer[idx_uc3dBuffer++] = id_test[1]; // id
				uc3dBuffer[idx_uc3dBuffer++] = 0f; // sec
				Destroy3D();
				id_test[1] = -1;
			}
		}
	}

	private void ShowStatus(){

		counter_status -= Time.deltaTime;
		if( counter_status < 0 ){
			var fps = 1f/Time.deltaTime;
			statusText.text =  (testmode ? "- Test Mode -\n\n": "") + "fps: " + fps + "\nobjects: " + gobjs.Count;
			counter_status = 1f;
		}
	}

	public void FocusCanvas3D( string focus ) {

    	if ( focus == "false" ) {
    	    WebGLInput.captureAllKeyboardInput = false;
    	} else if( focus == "true" ){
     	   	WebGLInput.captureAllKeyboardInput = true;
    	}
	}

	private GeometricObject init_drawing(){
		idx_uc3dBuffer = 0;
		if( gobj_drawing != null && ! gobj_drawing.initialized ) gobj_drawing.Begin ();
		return gobj_drawing;
	}

	private GeometricObject init_operation(){
		idx_uc3dBuffer = 0;
		var id = (int)uc3dBuffer[idx_uc3dBuffer++];
		return gobjs[id];
	}

	private void readPoints_zup_righthand(){
		points.Clear();
		var pointssize = (int)uc3dBuffer[idx_uc3dBuffer++];
		for (var i = 0; i < pointssize; ++i) {
			var z = -1 * uc3dBuffer[idx_uc3dBuffer++];
			var x = uc3dBuffer[idx_uc3dBuffer++];
			var y = uc3dBuffer[idx_uc3dBuffer++];
			points.Add( new Vector3( x, y, z ) );
		}
	}

	private void readAngles_zup_righthand(){
		angles.Clear();
		var anglessize = (int)uc3dBuffer[idx_uc3dBuffer++];
		for (var i = 0; i < anglessize; ++i) {
			var z = uc3dBuffer[idx_uc3dBuffer++];
			var x = -1 * uc3dBuffer[idx_uc3dBuffer++];
			var y = -1 * uc3dBuffer[idx_uc3dBuffer++];
			angles.Add( new Vector3( x, y, z ) );
		}
	}

	private float readFloat(){
		return uc3dBuffer[idx_uc3dBuffer++];
	}

	private bool readBoolean(){
		var ret = false;
		if( uc3dBuffer[idx_uc3dBuffer++] > 0 ) ret = true;
		return ret;
	}

	private void readModifiers(){

		while( true ){

			var modifiers = (MODIFIERS)((int)uc3dBuffer[idx_uc3dBuffer++]);
			if( modifiers== MODIFIERS.Color ){
				var r = uc3dBuffer[idx_uc3dBuffer++];
				var g = uc3dBuffer[idx_uc3dBuffer++];
				var b = uc3dBuffer[idx_uc3dBuffer++];
				frontColor = new Color ( r, g, b );
			}
			else if( modifiers == MODIFIERS.Radius  ){
				radius = uc3dBuffer[idx_uc3dBuffer++];
			}
			else if( modifiers == MODIFIERS.Colors  ){
				frontColors.Clear();
				var colorssize = (int)uc3dBuffer[idx_uc3dBuffer++];
				for (var i = 0; i < colorssize; ++i) {
					var r = uc3dBuffer[idx_uc3dBuffer++];
					var g = uc3dBuffer[idx_uc3dBuffer++];
					var b = uc3dBuffer[idx_uc3dBuffer++];
					frontColors.Add( new Color ( r, g, b ) );
				}
			}
			else if( modifiers == MODIFIERS.Topology  ){
				topology = (TOPOLOGY)( (int) uc3dBuffer[idx_uc3dBuffer++] );
			}
			else break;
		}
	}

    //////////////////////////////////////////////////////////////////////
    // Object handling functions
	
	public void Clear3D(){

		foreach( var o in gobjs.Values ){
			if( testmode ) Manager.DebugLog( "CindyJS::Clear3D : Destroy " + o.name );
			o.destroyCallback = null;
			Destroy( o.gameObject );
		}
		gobjs.Clear();
		id_head = 0;
		for( var i = 0 ; i<  id_test.Length; ++i ) id_test[i] = -1;
		counter_status = 0;
	}

	private GeometricObject registerGobj( GameObject obj ){
		
		var gobj = obj.GetComponent<GeometricObject>();
		gobj.id = id_head;
		gobj.destroyCallback = (id) =>{
       		if (gobjs[id] != null){
				if( testmode ) Manager.DebugLog( "CindyJS::registerGobj : destroyCallback " + id );	
				gobjs.Remove(id);
				#if !UNITY_EDITOR
            	OnDestroyCS(id);
				#endif				
				counter_status = 0;
        	}
		};
		gobjs[id_head] = gobj;
		++id_head;
		counter_status = 0;

		return gobj;
	}

	public void Begin3D ( string name ){

		if( gobj_drawing != null ) End3D();

		gobj_drawing = gobjs.FirstOrDefault( item => item.Value.name == name ).Value;
		if( gobj_drawing == null ){
	
			idx_uc3dBuffer = 0;
			var active = readBoolean();

			var o = Instantiate ( GeometricObjectPrefab );
			o.name = name;
			o.SetActive( active );
			gobj_drawing = registerGobj( o );

			if( testmode ) Manager.DebugLog( "CindyJS::Begin3D : Instantiate " + o.name + " id = " + gobj_drawing.id );
		}

		uc3dBuffer[0] = gobj_drawing.id;
	}

	public void Instantiate3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		var active = readBoolean();

		if( active ) readPoints_zup_righthand();
		else points[0] = gobj.gameObject.transform.position;

		var o = Instantiate ( gobj.gameObject, points[0], Quaternion.identity );
		o.SetActive( active );
		var gobj_copied = registerGobj( o );	

		if( testmode ) Manager.DebugLog( "CindyJS::Instantiate3D : Instantiate " + o.name + " id = " + gobj_copied.id);	

		uc3dBuffer[0] = gobj_copied.id;
	}

	public void End3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		gobj.End ();

		// recalculates the bound of collider
		var cl = gobj.gameObject.GetComponent< BoxCollider >();
		if( cl != null ){
			var mr = gobj.gameObject.GetComponent< MeshRenderer >();
 			cl.center = mr.bounds.center;
 			cl.size = mr.bounds.size;

			if( testmode ) Manager.DebugLog( "CindyJS::End3D : collider size = " + cl.size.x + " / " + cl.size.y + " / " + cl.size.z );
		}

		gobj_drawing = null;
	}

	public void Destroy3D (){

		var gobj = init_operation();
		if( gobj == null ) return;
		
		var sec = readFloat();

		Destroy( gobj.gameObject, sec );
	}

    //////////////////////////////////////////////////////////////////////
    // Appearance handling functions

	public void AddPoint3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		radius = gobj.pointAppearance.Peek ().radius;
		frontColor = gobj.pointAppearance.Peek ().frontColor;
		readModifiers ();

		gobj.SetPointRadius ( radius * POINT_SCALE);
		gobj.SetPointFrontColor (frontColor);
		gobj.AddPoint ( points[0] );
	}

	public void AddLine3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		radius = gobj.lineAppearance.Peek ().radius;
		frontColor = gobj.lineAppearance.Peek ().frontColor;
		frontColors.Clear ();
		topology = TOPOLOGY.Open;
		readModifiers();

		gobj.SetLineRadius (radius * POINT_SCALE);
		gobj.SetLineFrontColor (frontColor);
		if (topology == TOPOLOGY.Close) {
			gobj.SetLineTopology (Pen.Topology.Close);
		} 
		else {
			gobj.SetLineTopology (Pen.Topology.Open);
		}
		gobj.AddLine ( points, frontColors );
	}

	public void AddPolygon3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		frontColor = gobj.surfaceAppearance.Peek ().frontColor;
		readModifiers();

		gobj.SetPolygonFrontColor (frontColor);
		gobj.SetPolygonBackColor (frontColor);
		gobj.AddPolygon( points );
	}

	public void AddSphere3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		radius = gobj.surfaceAppearance.Peek ().radius;
		frontColor = gobj.surfaceAppearance.Peek().frontColor;
		readModifiers();

		gobj.SetSphereRadius ( radius );
		gobj.SetSphereFrontColor ( frontColor );
		gobj.AddSphere ( points[0] );
	}

    //////////////////////////////////////////////////////////////////////
    // Property handling functions
	
	public void SetActive3D (){

		var gobj = init_operation();
		if( gobj == null ) return;
		
		var active = readBoolean();

		gobj.gameObject.SetActive( active );
	}

	public void UseGravity3D (){

		var gobj = init_operation();
		if( gobj == null ) return;
		
		var usegravity = readBoolean();

		var rb = gobj.gameObject.GetComponent< Rigidbody >();
		if( rb == null ) rb = gobj.gameObject.AddComponent< Rigidbody >();
		rb.useGravity = usegravity;
	}

	public void AddCollider3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		var cl = gobj.gameObject.GetComponent< BoxCollider >();
		if( cl == null ) cl = gobj.gameObject.AddComponent< BoxCollider >();
	}

    //////////////////////////////////////////////////////////////////////
    // Move functions

	public void SetPosition3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		gobj.gameObject.transform.position = points[0];
	}

	public void GetPosition3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		var point = gobj.gameObject.transform.position;

		uc3dBuffer[0] = -1 * point.z;
		uc3dBuffer[1] = point.x;
		uc3dBuffer[2] = point.y;
	}

	public void SetRotation3D (){		

		var gobj = init_operation();
		if( gobj == null ) return;

		readAngles_zup_righthand();

		gobj.gameObject.transform.rotation = Quaternion.Euler( angles[0] );
	}

	public void SetVelocity3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		var rb = gobj.gameObject.GetComponent< Rigidbody >();
		if( rb == null ){
			rb = gobj.gameObject.AddComponent< Rigidbody >();
			rb.useGravity = false;
		}
		rb.velocity = points[0];
	}

	//////////////////////////////////////////////////////////////////////
    // Input functions

	public void GetKey3D( string name ){
		uc3dBuffer[0] = 0;
		if ( Input.GetKey(name) ){
			uc3dBuffer[0] = 1;
		}
	}

#endif
}
