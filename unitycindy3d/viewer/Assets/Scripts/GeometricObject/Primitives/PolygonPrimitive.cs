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

using System.Linq;

public class PolygonPrimitive : PrimitiveBase{

	public override void Begin()
	{
		base.Begin ();

		name = "PolygonPrimitive";

		pen.shaderType = Pen.ShaderType.DoubleSided;
	}

	public void Add ( List<Vector3> points ) 
	{
		var idx_base = vertices.Count;
		var N = points.Count;

		vertices.AddRange (points);
		vcolors.AddRange (Enumerable.Repeat (pen.frontColor, N).ToList () );

		var idxs = new List<int> ();
		for (var i = 0; i < N; ++i) {
			idxs.Add (idx_base + i);
		}
		base.DrawPolygon (idxs,false);
	}

}
