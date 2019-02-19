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

public class MeshPrimitive : PrimitiveBase{

	public override void Begin()
	{
		base.Begin ();

		name = "MeshPrimitive";

		pen.topology = Pen.Topology.Open;
		pen.shaderType = Pen.ShaderType.DoubleSided;
	}

	public void Add ( int rows, int cols, List<Vector3> points, int[,] idxs )
	{
		var idx_base = vertices.Count;
		var N = points.Count;

		vertices.AddRange (points);
		vcolors.AddRange (Enumerable.Repeat (pen.frontColor, N).ToList ());

		for (var r = 0; r < rows; r++) {
			for (var c = 0; c < cols; c++) {
				if (idxs [r, c] >= 0) {
					idxs [r, c] += idx_base;
				}
			}
		}				
		base.DrawMesh (rows, cols, idxs);
	}

}
