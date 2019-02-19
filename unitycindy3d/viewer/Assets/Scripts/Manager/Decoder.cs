/*
Copyright (C) 2018-2019 https://github.com/tmytokai/CindyJSPlugins

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

using System;
using System.Threading;
using System.Threading.Tasks;

public class Decoder{

	private enum COMMAND : byte
	{
		SetCoordinate = 0,
		SelectObject,

		Begin, 
		End,

		SetColor,
		SetRadius,

		AddPoint,
		AddLine,
		AddPolygon,
		AddSphere,
		AddMesh,

		COMMANDSIZE
	};

	private enum MODIFIERS : byte
	{
		Radius = 0,
		Color,
		Colors,
		Topology,

		MODIFIERSIZE
	};

	private enum COORDINATE : byte
	{
		Zup_RightHand = 0,
		Yup_LeftHand,

		COORDINATESIZE
	};

	private enum TOPOLOGY : byte
	{
		Open = 0,
		Close,

		TOPOLOGYSIZE
	};

	private Manager manager;

	private bool decoding;

	private Dictionary<string,int> dic_gobj;
	private int idx_gobj;
	private List<GeometricObject> gobj;

	private int coordinate = (int)COORDINATE.Zup_RightHand;
	private int MAX_POINTS = 8192;
	private float POINT_SCALE = 0.05f;
	private List<Stack<Appearance>> pointAppearance;
	private List<Stack<Appearance>> lineAppearance;
	private List<Stack<Appearance>> surfaceAppearance;

	private int BufferSize = 256 * 1024; // bytes
	private List<byte[]> command_buffer;
	private List<int> command_head;
	private List<int> command_size;

	private object lockObj;

	public Decoder( Manager _manager ) {

		// manager = _manager;

		dic_gobj = new Dictionary<string,int> ();
		idx_gobj = -1;
		gobj = new List<GeometricObject> ();

		pointAppearance = new List<Stack<Appearance>> ();
		lineAppearance = new List<Stack<Appearance>> ();
		surfaceAppearance = new List<Stack<Appearance>> ();

		command_buffer = new  List<byte[]> ();
		command_head = new List<int> ();
		command_size = new List<int> ();

		decoding = false;

		lockObj = new object ();

		// to use Task.Run(), set Scripting Runtime Version to .NET4.6 or later
		Task.Run (() => {
			Decode ();
		});
	}

	public void Quit ()
	{
		decoding = false;  // stops decode thread
	}

	private void AddGeometricObject( string name ){ // runs on the server thread

		idx_gobj = command_buffer.Count;
		dic_gobj.Add (name, command_buffer.Count);
		command_buffer.Add (new byte[ BufferSize ]);
		command_head.Add(0);
		command_size.Add(0);
/*
		Manager.context.Post (
			(state) => { // runs on the main thread
				var obj = manager.InstantiateGeometricObject();
				obj.name =  (string)state;
				gobj.Add (obj.GetComponent<GeometricObject> ());
			}, name);
*/
		pointAppearance.Add (new Stack<Appearance> ());
		pointAppearance[idx_gobj].Push (new Appearance (Color.red, Color.red, 60f, 1f, 1f));

		lineAppearance.Add (new Stack<Appearance> ());
		lineAppearance[idx_gobj].Push (new Appearance (Color.blue, Color.blue, 60f, 1f, 1f));

		surfaceAppearance.Add (new Stack<Appearance> ());
		surfaceAppearance[idx_gobj].Push (new Appearance (Color.green, Color.grey, 60f, 1f, 1f));
	}

	public void Register( byte[] buffer , int buffer_size ){  // runs on the server thread

		lock (lockObj) {
			
			if( buffer_size >= BufferSize ) {

				Manager.DebugLogError ("Decoder::Register: buffer overflow size =  " + buffer_size );
			} 

			else if ((COMMAND)buffer [0] >= COMMAND.COMMANDSIZE) {
				
				Manager.DebugLogError ("Decoder::Register: unknown command " + buffer [0]);
			} 

			else if ((COMMAND)buffer [0] == COMMAND.SetCoordinate) {

				if ((int)buffer [3] == (int)COORDINATE.Zup_RightHand) {
					coordinate = (int)COORDINATE.Zup_RightHand;
				} else {
					coordinate = (int)COORDINATE.Yup_LeftHand;
				}
			} 

			else if ((COMMAND)buffer [0] == COMMAND.SelectObject) {

				var lng = BitConverter.ToInt32 ( buffer, 1);
				var objname = System.Text.Encoding.ASCII.GetString (buffer, (1+4), lng); 

				if (dic_gobj.ContainsKey (objname)) {
					idx_gobj = dic_gobj [objname];
				} 
				else {					
					AddGeometricObject (objname);
				}
			}

			else if( idx_gobj >= 0 ) {

				// skip unprocessed commands
				if ((COMMAND)buffer [0] == COMMAND.Begin) {
					
					if (command_size [idx_gobj] != 0) {
						Manager.DebugLogWarning ("Decoder::Register: skipped unprocessed commands idx = " + idx_gobj + " head = " + command_head [idx_gobj] + " size = " + command_size [idx_gobj]);
					}

					command_head [idx_gobj] = 0;
					command_size [idx_gobj] = 0;
				} 

				// no commands exist
				if ((COMMAND)buffer [0] == COMMAND.End && command_size [idx_gobj] == 0) {
					buffer_size = 0;
				}

				if (buffer_size > 0) {
					Array.Copy (buffer, 0, command_buffer [idx_gobj], command_size [idx_gobj], buffer_size);
					command_size [idx_gobj] += buffer_size;
				}
			}

		} // lock
	}

	private bool Decode(){  // runs on the decode thread

		Manager.DebugLog ("Decoder::Decode: start");

		var chunksize = 0;
		var radius = 0f;
		var frontColor = Color.white;
		var frontColors = new List<Color32> ();
		var backColor = Color.white;
		var topology = TOPOLOGY.Open;
		var color_r = new float[MAX_POINTS];
		var color_g = new float[MAX_POINTS];
		var color_b = new float[MAX_POINTS];
		var point_x = new float[MAX_POINTS];
		var point_y = new float[MAX_POINTS];
		var point_z = new float[MAX_POINTS];

		System.Action<int,int> readColors = (idx, points) => {

			for (var i = 0; i < points; ++i) {
				
				color_r [i] = BitConverter.ToSingle (command_buffer [idx], command_head [idx]);
				color_g [i] = BitConverter.ToSingle (command_buffer [idx], command_head [idx] + 4);
				color_b [i] = BitConverter.ToSingle (command_buffer [idx], command_head [idx] + 8);

				command_head [idx] += 12;
				chunksize -= 12;
			}
		};

		System.Action<int,int> readPoints_zup_righthand = (idx, points) =>{

			for (var i = 0; i < points; ++i) {

				point_z [i] = -1*BitConverter.ToSingle (command_buffer [idx], command_head [idx] );
				point_x [i] = BitConverter.ToSingle (command_buffer [idx], command_head [idx]+4 );
				point_y [i] = BitConverter.ToSingle (command_buffer [idx], command_head [idx]+8 );

				command_head [idx] += 12;
				chunksize -= 12;
			}
		};
		System.Action<int,int> readPoints_yup_lefthand = (idx, points) =>{

			for (var i = 0; i < points; ++i) {

				point_x [i] = BitConverter.ToSingle (command_buffer [idx], command_head [idx] );
				point_y [i] = BitConverter.ToSingle (command_buffer [idx], command_head [idx]+4 );
				point_z [i] = BitConverter.ToSingle (command_buffer [idx], command_head [idx]+8 );

				command_head [idx] += 12;
				chunksize -= 12;
			}
		};
		var readPoints = new []{readPoints_zup_righthand, readPoints_yup_lefthand};

		System.Action<int> readModifiers = (idx) => {

			while (chunksize > 0) {

				var modifiers = (MODIFIERS)command_buffer [idx] [command_head [idx]];
				command_head [idx]++;
				chunksize--;

				if (modifiers == MODIFIERS.Radius) {

					radius = BitConverter.ToSingle (command_buffer [idx], command_head [idx]);
					command_head [idx] += 4;
					chunksize -= 4;

				} 
				else if (modifiers == MODIFIERS.Color) {

					readColors (idx, 1);
					frontColor = new Color (color_r [0], color_g [0], color_b [0]);

				} 
				else if (modifiers == MODIFIERS.Colors) {
					
					var colorssize = BitConverter.ToInt32 ( command_buffer [idx], command_head[idx]);
					command_head [idx] += 4;
					chunksize -= 4;

					frontColors.Clear();
					readColors (idx, colorssize);
					for (var i = 0; i < colorssize; ++i) {	
						frontColors.Add (new Color (color_r [i], color_g [i], color_b [i]) );
					}
				}
				else if (modifiers == MODIFIERS.Topology) {

					topology = (TOPOLOGY)command_buffer [idx] [command_head [idx]];
					command_head [idx] ++;
					chunksize --;
				} 
				else {

					Manager.DebugLogError ("Decoder::Decode: unknown modifiers " + modifiers);
					break;
				}
			}

			if (chunksize != 0) {
				
				Manager.DebugLogError ("Decoder::Decode: invalid chunk " + chunksize);
				command_head [idx] += chunksize;
				chunksize = 0;
			}
		};

		decoding = true;
		while (decoding) {
		
			var wait = true;
						
			lock (lockObj) {

				for ( var idx = 0; idx < gobj.Count; ++idx) {

					try{

						if (gobj [idx] == null) {
							throw new Exception( "gobj[" + idx + "] is null" );
						}

						if (! gobj [idx].rendering && command_size[idx] - command_head[idx] >= (1 + 4)) {

							var command = (COMMAND)command_buffer [idx] [command_head[idx]];
							chunksize = BitConverter.ToInt32 (command_buffer [idx], command_head[idx] + 1);

							if (command_size[idx] - command_head[idx] >= (1 + 4) + chunksize) {

								wait = false;
								command_head[idx] += (1 + 4);

								switch (command) {

								case COMMAND.Begin:
									gobj [idx].Begin ();
									break;

							
								case COMMAND.End:

									if( gobj[idx].initialized ){

										gobj [idx].rendering = true;
										Manager.context.Post ((state) => gobj[(int)state].End (), idx);

										command_head[idx] = 0;
										command_size[idx] = 0;
									}
									break;
								

								case COMMAND.SetColor:

									if (gobj [idx].initialized) {

										readColors (idx, 1);
										var color = new Color (color_r [0], color_g [0], color_b [0]);
										
										pointAppearance[idx].Peek().frontColor = color;
										pointAppearance[idx].Peek().backColor = color;

										lineAppearance[idx].Peek().frontColor = color;
										lineAppearance[idx].Peek().backColor = color;

										surfaceAppearance[idx].Peek().frontColor = color;
										surfaceAppearance[idx].Peek().backColor = color;
									}
									break;

								
								case COMMAND.SetRadius:

									if (gobj [idx].initialized) {

										radius = BitConverter.ToSingle (command_buffer [idx], command_head [idx]);
										command_head [idx] += 4;
										chunksize -= 4;

										pointAppearance[idx].Peek().radius = radius;

										lineAppearance [idx].Peek ().radius = radius;
									}
									break;
								
								
								case COMMAND.AddPoint:

									if( gobj[idx].initialized ){

										readPoints[coordinate] (idx, 1);

										radius = pointAppearance [idx].Peek ().radius;
										frontColor = pointAppearance [idx].Peek ().frontColor;
										readModifiers (idx);

										gobj [idx].SetPointRadius ( radius * POINT_SCALE);
										gobj [idx].SetPointFrontColor (frontColor);
										gobj [idx].AddPoint (new Vector3 (point_x[0], point_y[0], point_z[0]));
									}
									break;
								
								
								case COMMAND.AddLine:

									if (gobj [idx].initialized) {

										var pointssize = BitConverter.ToInt32 ( command_buffer [idx], command_head[idx]);
										command_head [idx] += 4;
										chunksize -= 4;
							
										if( pointssize >= MAX_POINTS ){
											command_head[idx] = 0;
											command_size[idx] = 0;
											throw new Exception( "too many points size = " + pointssize + " > " + MAX_POINTS );
										}

										readPoints[coordinate] (idx, pointssize);
										var points = new List<Vector3> ();
										for (var i = 0; i < pointssize; ++i) {
											points.Add (new Vector3 (point_x [i], point_y [i], point_z [i]));
										}

										radius   = lineAppearance [idx].Peek ().radius;
										frontColor = lineAppearance [idx].Peek ().frontColor;
										frontColors.Clear ();
										topology = TOPOLOGY.Open;
										readModifiers (idx);

										gobj [idx].SetLineRadius (radius * POINT_SCALE);
										gobj [idx].SetLineFrontColor (frontColor);
										if (topology == TOPOLOGY.Close) {
											gobj [idx].SetLineTopology (Pen.Topology.Close);
										} 
										else {
											gobj [idx].SetLineTopology (Pen.Topology.Open);
										}
										gobj [idx].AddLine ( points, frontColors );
									}
									break;

								
								case COMMAND.AddPolygon:

									if( gobj[idx].initialized ){

										var pointssize = BitConverter.ToInt32 ( command_buffer [idx], command_head[idx]);
										command_head [idx] += 4;
										chunksize -= 4;

										if( pointssize >= MAX_POINTS ){
											command_head[idx] = 0;
											command_size[idx] = 0;
											throw new Exception( "too many points size = " + pointssize + " > " + MAX_POINTS );
										}

										readPoints[coordinate] (idx, pointssize);
										var points = new List<Vector3> ();
										if ( coordinate == (int) COORDINATE.Zup_RightHand) {
											for (var i = pointssize-1; i >=0 ; --i) {
												points.Add (new Vector3 (point_x [i], point_y [i], point_z [i]));
											}
										} 
										else {
											for (var i = 0; i < pointssize; ++i) {
												points.Add (new Vector3 (point_x [i], point_y [i], point_z [i]));
											}
										}
										
										frontColor = surfaceAppearance [idx].Peek ().frontColor;
										readModifiers (idx);

										gobj [idx].SetPolygonFrontColor (frontColor);
										gobj [idx].SetPolygonBackColor (frontColor);
										gobj [idx].AddPolygon (points);
									}
									break;
								
								
								case COMMAND.AddSphere:

									if( gobj[idx].initialized ){

										readPoints[coordinate] (idx, 1);

										radius = surfaceAppearance [idx].Peek ().radius;
										frontColor = surfaceAppearance [idx].Peek ().frontColor;
										readModifiers (idx);

										gobj [idx].SetSphereRadius ( radius );
										gobj [idx].SetSphereFrontColor (frontColor);
										gobj [idx].AddSphere (new Vector3 (point_x[0], point_y[0], point_z[0]));
									}
									break;
								
								
								case COMMAND.AddMesh:

									if( gobj[idx].initialized ){

										var rows = BitConverter.ToInt32 ( command_buffer [idx], command_head[idx]);
										command_head [idx] += 4;
										chunksize -= 4;

										var cols = BitConverter.ToInt32 ( command_buffer [idx], command_head[idx]);
										command_head [idx] += 4;
										chunksize -= 4;

										var pointssize = BitConverter.ToInt32 ( command_buffer [idx], command_head[idx]);
										command_head [idx] += 4;
										chunksize -= 4;

										if( pointssize >= MAX_POINTS ){
											command_head[idx] = 0;
											command_size[idx] = 0;
											throw new Exception( "too many points size = " + pointssize + " > " + MAX_POINTS );
										}

										readPoints[coordinate] (idx, pointssize);
										var idxs = new int[rows, cols];
										var points = new List<Vector3> ();

										var i = 0;
										if (coordinate == (int)COORDINATE.Zup_RightHand) {
										
											for (var r = 0; r < rows; r++) {		
												for (var c = cols-1; c >= 0 ; c--) {
													idxs [r, c] = points.Count;
													points.Add (new Vector3 (point_x [i], point_y [i], point_z [i]));
													++i;
												}
											}
										} 
										else {
											for (var r = 0; r < rows; r++) {		
												for (var c = 0; c < cols; c++) {
													idxs [r, c] = points.Count;
													points.Add (new Vector3 (point_x [i], point_y [i], point_z [i]));
													++i;
												}
											}
										}

										frontColor = surfaceAppearance [idx].Peek ().frontColor;
										readModifiers (idx);

										gobj [idx].SetMeshFrontColor (frontColor);
										gobj [idx].SetMeshBackColor (frontColor);
										gobj [idx].AddMesh( rows, cols, points, idxs );
									}
									break;
								
								default:
									command_head[idx] = 0;
									command_size[idx] = 0;
									throw new Exception( "unknown command " + command );
								
								} // switch
							}
						}
					
					} // try
					catch (Exception e) {
						Manager.DebugLogError ( "Decoder::Decode: " + e.Message );
					}

				} // for

			} // lock

			if(wait) {
				Thread.Sleep (1);
			}

		} // while

		Manager.DebugLog ("Decoder::Decode: finished");

		return true;
	}

}
