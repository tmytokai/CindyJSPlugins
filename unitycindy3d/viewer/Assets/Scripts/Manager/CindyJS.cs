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
using System.Runtime.InteropServices;

public class CindyJS : MonoBehaviour {

#if UNITY_WEBGL

	private class GEOOBJECT {

		public string className;
		public GeometricObject obj = null;
		public Stack<Appearance> pointAppearance = null;
		public Stack<Appearance> lineAppearance = null;
		public Stack<Appearance> surfaceAppearance = null;

		public GEOOBJECT(  ){
		} 
		
		public void CreateObject( Manager manager, string _name ){

			className = _name;

			var o = manager.InstantiateGeometricObject();
			o.name = className;
			obj = o.GetComponent<GeometricObject>();

			pointAppearance = new Stack<Appearance> ();
			pointAppearance.Push (new Appearance (Color.red, Color.red, 60f, 1f, 1f));

			lineAppearance = new Stack<Appearance> ();
			lineAppearance.Push (new Appearance (Color.blue, Color.blue, 60f, 1f, 1f));

			surfaceAppearance = new Stack<Appearance> ();			
			surfaceAppearance.Push (new Appearance (Color.green, Color.grey, 60f, 1f, 1f));
		}

		public void CopyObject( Manager manager, GEOOBJECT srcobj, Vector3 pos ){

			className = srcobj.className;

			var o = Instantiate ( srcobj.obj.gameObject, pos, Quaternion.identity );
			o.name = className;
			obj = o.GetComponent<GeometricObject>();

			pointAppearance = new Stack<Appearance> ( srcobj.pointAppearance.Reverse() );

			lineAppearance = new Stack<Appearance> ( srcobj.lineAppearance.Reverse() );

			surfaceAppearance = new Stack<Appearance>( srcobj.surfaceAppearance.Reverse() );
		}

		public void Clear(){
			if( obj != null ) Destroy( obj.gameObject );
			if( pointAppearance != null ) pointAppearance.Clear();
			if( lineAppearance != null ) lineAppearance.Clear();
			if( surfaceAppearance != null ) surfaceAppearance.Clear();
		}
	};

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
	private static extern void CollisionEnterCS( int objid1, string classname1, int objid2, string classname2 );

	private Manager manager = null;
	private float counter_status = 0;

	private int MAX_OBJECTS = 256;
	private Dictionary<string,int> dic_gobj;
	private GEOOBJECT[] gObj;
	private int idx_drawing = -1;
	private int head_gobj = 0;
	private int count_gobj = 0;

	private List<Vector3> points;
	private List<Vector3> angles;
	private float radius;
	private Color32 frontColor;
	private List<Color32> frontColors ;
	private Color32 backColor;
	private TOPOLOGY topology;

	private float POINT_SCALE = 0.05f;

	private int uc3dBufferSize = 256; // kbyte
	private float[] uc3dBuffer;
	private int idx_uc3dBuffer;

	void Start () {
		manager = gameObject.GetComponent<Manager> ();

		dic_gobj = new Dictionary<string,int> ();
		gObj = new GEOOBJECT[MAX_OBJECTS];

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
		if (Input.GetKeyDown (KeyCode.F4)) {
			manager.StatusTextEnabled = !manager.StatusTextEnabled;
		}
		ShowStatus();
	}

	private void ShowStatus(){
		if( !manager.StatusTextEnabled ) return;
		counter_status -= Time.deltaTime;
		if( counter_status < 0 ){
			var fps = 1f/Time.deltaTime;
			manager.StatusText = "fps: " + fps + "\nobjects: " + count_gobj + "/"+MAX_OBJECTS;
			counter_status = 1f;
		}
	}

	private GEOOBJECT init_drawing(){
		if( idx_drawing < 0 ) return null;
		var gobj = gObj[idx_drawing];
		if( gobj == null || gobj.obj == null ) return null;
		if( ! gobj.obj.initialized ) gobj.obj.Begin ();
		idx_uc3dBuffer = 0;
		return gobj;
	}

	private GEOOBJECT init_operation(){
		idx_uc3dBuffer = 0;
		var idx = (int)uc3dBuffer[idx_uc3dBuffer++];
		if( idx < 0 || idx >= MAX_OBJECTS ) return null;
		var gobj = gObj[idx];
		if( gobj == null || gobj.obj == null ) return null;
		return gobj;
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

	public void Clear3D(){

		for( var i =0; i < MAX_OBJECTS; ++i ){
			if( gObj[i] != null ) gObj[i].Clear();
			gObj[i] = null;
		}

		dic_gobj.Clear();

		idx_drawing = -1;
		head_gobj = 0;
		count_gobj = 0;
	}

	public void Begin3D ( string name ){

		idx_drawing = -1;
		if (dic_gobj.ContainsKey ( name )) {
			idx_drawing = dic_gobj [name];
		} 
		else if( count_gobj < MAX_OBJECTS ){

			idx_uc3dBuffer = 0;
			var active = readBoolean();

			var idx = head_gobj;
			++head_gobj;
			++count_gobj;

			gObj[idx] = new GEOOBJECT();
			gObj[idx].CreateObject( manager, name );
			gObj[idx].obj.gameObject.SetActive( active );
			dic_gobj.Add ( name, idx );

			idx_drawing = idx;
		}
		else{
			Manager.DebugLogError ( "CindyJS::Begin3D : Cannot create a new object. Please destroy unused objects.");
		}

		uc3dBuffer[0] = idx_drawing;
	}

	public void End3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		gobj.obj.End ();

		// recalculates the bound of collider
		var cl = gobj.obj.gameObject.GetComponent< BoxCollider >();
		if( cl != null ){
			var mr = gobj.obj.gameObject.GetComponent< MeshRenderer >();
 			cl.center = mr.bounds.center;
 			cl.size = mr.bounds.size;
			Manager.DebugLog( "CindyJS::End3D : collider size = " + cl.size.x + " / " + cl.size.y + " / " + cl.size.z );
		}

		idx_drawing = -1;
	}

	public void AddPoint3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		radius = gobj.pointAppearance.Peek ().radius;
		frontColor = gobj.pointAppearance.Peek ().frontColor;
		readModifiers ();

		gobj.obj.SetPointRadius ( radius * POINT_SCALE);
		gobj.obj.SetPointFrontColor (frontColor);
		gobj.obj.AddPoint ( points[0] );
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

		gobj.obj.SetLineRadius (radius * POINT_SCALE);
		gobj.obj.SetLineFrontColor (frontColor);
		if (topology == TOPOLOGY.Close) {
			gobj.obj.SetLineTopology (Pen.Topology.Close);
		} 
		else {
			gobj.obj.SetLineTopology (Pen.Topology.Open);
		}
		gobj.obj.AddLine ( points, frontColors );
	}

	public void AddPolygon3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		frontColor = gobj.surfaceAppearance.Peek ().frontColor;
		readModifiers();

		gobj.obj.SetPolygonFrontColor (frontColor);
		gobj.obj.SetPolygonBackColor (frontColor);
		gobj.obj.AddPolygon( points );
	}

	public void AddSphere3D (){

		var gobj = init_drawing();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		radius = gobj.surfaceAppearance.Peek ().radius;
		frontColor = gobj.surfaceAppearance.Peek().frontColor;
		readModifiers();

		gobj.obj.SetSphereRadius ( radius );
		gobj.obj.SetSphereFrontColor ( frontColor );
		gobj.obj.AddSphere ( points[0] );
	}

	public void FocusCanvas3D( string focus ) {

    	if ( focus == "false" ) {
//			Manager.DebugLog("unfocused");
    	    WebGLInput.captureAllKeyboardInput = false;
    	} else if( focus == "true" ){
//			Manager.DebugLog("focused");
     	   	WebGLInput.captureAllKeyboardInput = true;
    	}
	}

	public void Instantiate3D (){

		if( count_gobj >= MAX_OBJECTS ){
			Manager.DebugLogError ( "CindyJS::Instantiate3D : Cannot create a new object. Please destroy unused objects.");
			return;
		}

		var gobj = init_operation();
		if( gobj == null ) return;

		var active = readBoolean();

		if( active ) readPoints_zup_righthand();
		else points[0] = gobj.obj.gameObject.transform.position;

		var idx = head_gobj;
		++head_gobj;
		++count_gobj;

		gObj[idx] = new GEOOBJECT();
		gObj[idx].CopyObject( manager, gobj, points[0] );
		gObj[idx].obj.gameObject.SetActive( active );

		uc3dBuffer[0] = idx;
	}

	public void SetActive3D (){

		var gobj = init_operation();
		if( gobj == null ) return;
		
		var active = readBoolean();

		gobj.obj.gameObject.SetActive( active );
	}

	public void UseGravity3D (){

		var gobj = init_operation();
		if( gobj == null ) return;
		
		var usegravity = readBoolean();

		var rb = gobj.obj.gameObject.GetComponent< Rigidbody >();
		if( rb == null ) rb = gobj.obj.gameObject.AddComponent< Rigidbody >();
		rb.useGravity = usegravity;
	}

	public void AddCollider3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		var cl = gobj.obj.gameObject.GetComponent< BoxCollider >();
		if( cl == null ) cl = gobj.obj.gameObject.AddComponent< BoxCollider >();
	}

	public void SetPosition3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		gobj.obj.gameObject.transform.position = points[0];
	}

	public void GetPosition3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		var point = gobj.obj.gameObject.transform.position;

		uc3dBuffer[0] = -1 * point.z;
		uc3dBuffer[1] = point.x;
		uc3dBuffer[2] = point.y;
	}

	public void SetRotation3D (){		

		var gobj = init_operation();
		if( gobj == null ) return;

		readAngles_zup_righthand();

		gobj.obj.gameObject.transform.rotation = Quaternion.Euler( angles[0] );
	}

	public void SetVelocity3D (){

		var gobj = init_operation();
		if( gobj == null ) return;

		readPoints_zup_righthand();

		var rb = gobj.obj.gameObject.GetComponent< Rigidbody >();
		if( rb == null ){
			rb = gobj.obj.gameObject.AddComponent< Rigidbody >();
			rb.useGravity = false;
		}
		rb.velocity = points[0];
	}
	
	public void GetKey3D( string name ){
		uc3dBuffer[0] = 0;
		if ( Input.GetKey(name) ){
			uc3dBuffer[0] = 1;
		}
	}

#endif
}
