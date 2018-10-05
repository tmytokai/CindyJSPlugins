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

public class SpherePrimitive : PrimitiveBase{

	public override void Begin()
	{
		base.Begin ();

		name = "SpherePrimitive";

		pen.topology = Pen.Topology.CloseColumns;
		pen.radius = 1f;
		pen.stacks = 9;
		pen.slices = pen.stacks * 2;
		pen.shaderType = Pen.ShaderType.SingleSided;
	}

	public void Add ( Vector3 center )
	{
		var idx_base = vertices.Count;
		var idxs = new int[pen.stacks, pen.slices];

		for (var st = 0; st < pen.stacks; st++) {
			var section = base.GetSection ( st+1 );
			for (var sl = 0; sl < pen.slices; sl++) {
				idxs [st, pen.slices-1-sl] = vertices.Count;
				vertices.Add (center+section [sl]);
				vcolors.Add (pen.frontColor);
			}
		}

		// north pole
		var idx_pole = vertices.Count;
		vertices.Add (new Vector3 (center.x, center.y+pen.radius, center.z));
		vcolors.Add (pen.frontColor);
		for (var sl = 0; sl < pen.slices; sl++) {
			base.DrawTriangle (idx_base + sl, idx_pole, idx_base + ( sl + 1 ) % pen.slices);
		}

		base.DrawMesh (pen.stacks, pen.slices, idxs);

		// south pole
		idx_pole++;
		vertices.Add (new Vector3 (center.x, center.y-pen.radius, center.z));
		vcolors.Add (pen.frontColor);
		for (var sl = 0; sl < pen.slices; sl++) {
			base.DrawTriangle ( (idx_pole-1-pen.slices) + ( sl + 1 ) % pen.slices, idx_pole, (idx_pole-1-pen.slices) + sl );
		}
	}

}
