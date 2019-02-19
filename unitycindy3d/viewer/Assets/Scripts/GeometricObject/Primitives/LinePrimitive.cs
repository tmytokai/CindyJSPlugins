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

using System.Linq;

public class LinePrimitive : PrimitiveBase{

	public override void Begin()
	{
		base.Begin ();

		name = "LinePrimitive";

		pen.topology = Pen.Topology.Open;
		pen.radius = 0.1f;
		pen.stacks = 1;
		pen.slices = 12;
		pen.shaderType = Pen.ShaderType.VertexColors;
	}

	public void Add ( List<Vector3> points, List<Color32> frontColors )
	{
		var frontColor_backuped = pen.frontColor;

		var idx_base = vertices.Count;
		var loop_offset = 0;
		System.Func<int,int> GetFirstSectionIdx = (i) => {
			return idx_base + (pen.slices + i + loop_offset) % pen.slices;
		};

		if ( pen.topology == Pen.Topology.Close ) {
			points.Add (points [0]);
			points.Add (points [1]);
			if ( frontColors != null && frontColors.Count > 2) {
				frontColors.Add ( frontColors [0] );
				frontColors.Add ( frontColors [1] );		
			}
		}
		var count = points.Count-1;

		/*
		Manager.DebugLog ("----------");
		Manager.DebugLog ("LinePrimitive::Add:");
		Manager.DebugLog ("topology = " + pen.topology );
		Manager.DebugLog ("count = " + count );
		if ( frontColors != null ) {
			Manager.DebugLog ("multicolored");
		}
		*/

		var template = base.GetSection (1);
		var margin0 = new float[pen.slices];
		var margin1 = new float[pen.slices];
		var idx_section0 = new int[pen.slices];
		var idx_section1 = new int[pen.slices];
		var idx_joint0 = new int[pen.slices];
		var idx_joint1 = new int[pen.slices];
		var lng = new float[pen.slices];

		var p0 = points [0];
		var p1 = points [1];
		var p2 = Vector3.zero;
		var unit_01 = (p1 - p0); 
		unit_01.Normalize ();
		var unit_12 = Vector3.zero;
		var unit_joint = Vector3.zero;
		var rotation = Quaternion.FromToRotation (Vector3.up, unit_01);
		var unit_forward = rotation * Vector3.forward;
		var idx_forward = 0;

		for (var n = 0; n < count; ++n) {

			/*
			var SmallNumber = 0.01f;
			if ( Mathf.Abs ( Vector3.Angle ( unit_forward, unit_01 ) - 90f ) > SmallNumber ) {
				Manager.DebugLogError ( "LinePrimitive::Add: illegal section0 n = " + n + ", angle = " + Vector3.Angle( unit_forward, unit_01 ) );
			}
			*/

			// multicolored line
			if ( frontColors != null && frontColors.Count > n ) {
				
				base.SetFrontColor ( frontColors [n] );

				if (n > 0) {		
					
					for (var i = 0; i < pen.slices; ++i) {						
						vertices.Add (vertices [idx_joint0 [i]]);
						vcolors.Add (pen.frontColor);
						idx_joint0 [i] = vertices.Count - 1;
					}
				}
			}

			// caluculate the coordinates of the section0
			rotation = Quaternion.LookRotation (unit_forward, unit_01);
			var section0_tmp = template.Select (v => rotation * v + p0).ToList ();
			var section0 = section0_tmp.GetRange (pen.slices - idx_forward, idx_forward);
			section0.AddRange (section0_tmp.GetRange (0, pen.slices - idx_forward));
			for (var i = 0; i < pen.slices; ++i) {
				
				if (margin0 [i] > 0) {
					idx_section0 [i] = idx_joint0 [i];
				} else {

					if ( pen.topology == Pen.Topology.Close && n == count - 1) {
						idx_section0 [i] = GetFirstSectionIdx (i);
						vertices [ idx_section0 [i] ] = section0 [i];
					} else {
						vertices.Add (section0 [i]);
						vcolors.Add (pen.frontColor);
						idx_section0 [i] = vertices.Count - 1;
					}
				}
			}

			// caluculate the coordinates of the section1
			var section1_tmp = template.Select (v => rotation * v + p1).ToList ();
			var section1 = section1_tmp.GetRange (pen.slices - idx_forward, idx_forward);
			section1.AddRange (section1_tmp.GetRange (0, pen.slices - idx_forward));
			if (n < count-1) {

				p2 = points [n + 2];
				unit_12 = (p2 - p1);
				unit_12.Normalize ();
				unit_joint = (unit_01 + unit_12);
				unit_joint.Normalize ();

				for (var i = 0; i < pen.slices; ++i) {
					
					var v = vertices [ idx_section0 [i] ];
					lng [i] = Vector3.Dot (unit_joint, (p1 - v)) / Vector3.Dot (unit_joint, unit_01);

					/*
					if( lng [i] <= 0 ){
						Manager.DebugLogError( "LinePrimitive::Add: lng[i] <= 0: " + lng [i]);
					}
					*/

					margin1[i] = Vector3.Distance( v, section1 [i] ) - lng[i];
					if (margin1 [i] < 0) {
						margin1 [i] = 0;
					} else {
						section1[i] = vertices [idx_section0 [i]] + lng[i] * unit_01;
					}
				}
				idx_forward = margin1.ToList ().IndexOf (margin1.Max ());
			}

			System.Action<int> addSection1 = (i) => {
				vertices.Add (section1 [i]);
				vcolors.Add (pen.frontColor);
				idx_section1 [i] = vertices.Count - 1;
			};

			if ( pen.topology == Pen.Topology.Close && n == count - 2) { // reuses the vertices of the first section

				loop_offset = 0; // used in GetFirstSectionIdx()
				var min_i = 0;
				var min_dist = 1000f;
				for (var i = 0; i < pen.slices; ++i) {
					var dist = Vector3.Distance (section1 [idx_forward], vertices [GetFirstSectionIdx(i)]);
					if (dist < min_dist) {
						min_i = i;
						min_dist = dist;
					}
				}
				loop_offset = min_i - idx_forward;

				for (var i = 0; i < pen.slices; ++i) {
					
					if (margin1 [i] > 0) {

						vertices [GetFirstSectionIdx (i)] = section1 [i];

						if ((Color)vcolors [GetFirstSectionIdx (i)] == pen.frontColor) {
							idx_section1 [i] = GetFirstSectionIdx (i);
						} else {
							addSection1 (i);
						}
					} else {
						addSection1 (i);
					}
				}

			} else {
				
				for (var i = 0; i < pen.slices; ++i) {
					addSection1 (i);
				}
			}

			// caluculate the coordinates of the joint1
			if (n < count -1) {

				unit_forward = vertices[ idx_section1[idx_forward] ] - p1;
				unit_forward.Normalize ();

				/*
				if ( Mathf.Abs ( Vector3.Angle (unit_forward, unit_joint) - 90f ) > SmallNumber ) {
					Manager.DebugLogError( "LinePrimitive::Add: illegal joint1 n = " + n + ", angle = " + Vector3.Angle (unit_forward, unit_joint) );
				}
				*/

				rotation = Quaternion.LookRotation (unit_forward, unit_joint);
				var joint_tmp = template.Select (v => rotation * v + p1).ToList ();
				var joint1 = joint_tmp.GetRange (pen.slices - idx_forward, idx_forward);
				joint1.AddRange (joint_tmp.GetRange (0, pen.slices - idx_forward));
				for (var i = 0; i < pen.slices; ++i) {
					
					if (margin1 [i] > 0) {
						idx_joint1 [i] = idx_section1 [i];
					} else {
						
						vertices.Add (joint1 [i]);
						vcolors.Add (pen.frontColor);
						idx_joint1 [i] = vertices.Count - 1;
					}
				}
			}

			//
			// draw polygons
			//

			// draw first cap
			if ( pen.topology == Pen.Topology.Open && n == 0) {
				base.DrawPolygon (new List<int>(idx_section0), false );
			}

			// draw joint0
			if (n > 0) {

				for (var i = 0; i < pen.slices; ++i) {

					base.DrawQuad (
						idx_section0 [i], // Left Up
						idx_joint0 [i],  // Left Down
						idx_section0 [(i + 1) % pen.slices],  // Right Up
						idx_joint0 [(i + 1) % pen.slices] // Right Down
					);
				}
			}
				
			if ( pen.topology == Pen.Topology.Close && n == count - 1) {
				break;
			}

			// draw sides
			for (var i = 0; i < pen.slices; ++i) { 

				base.DrawQuad (
					idx_section1 [i], // Left Up
					idx_section0 [i],  // Left Down
					idx_section1 [(i + 1) % pen.slices],  // Right Up
					idx_section0 [(i + 1) % pen.slices] // Right Down
				);
			}

			// draw joint1
			if (n < count -1) {
				
				for (var i = 0; i < pen.slices; ++i) {

					base.DrawQuad (
						idx_joint1 [i], // Left Up
						idx_section1 [i],  // Left Down
						idx_joint1 [(i + 1) % pen.slices],  // Right Up
						idx_section1 [(i + 1) % pen.slices] // Right Down
					);
				}
			}

			// draw last cap
			if ( pen.topology == Pen.Topology.Open && n == count -1 ) {
				base.DrawPolygon (new List<int>(idx_section1),true);
			}

			p0 = p1;
			p1 = p2;
			unit_01 = unit_12;
			System.Array.Copy (margin1, margin0, pen.slices);
			System.Array.Copy (idx_joint1, idx_joint0, pen.slices);
			unit_forward = vertices[ idx_section1 [idx_forward]] - (p0 + margin0 [idx_forward] * unit_01);
			unit_forward.Normalize ();
		}

		base.SetFrontColor (frontColor_backuped);
	}
		
}
