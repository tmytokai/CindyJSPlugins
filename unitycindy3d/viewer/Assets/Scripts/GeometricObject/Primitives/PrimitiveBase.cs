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

public class PrimitiveBase
{
	public string name{ get; protected set; }
	public List<Vector3> vertices{ get; protected set; }
	public List<int> triangles{ get; protected set; }
	public List<Color32> vcolors{ get; protected set; }
	public List<SubMesh> submeshes{ get; protected set; }
	public Pen pen{ get; protected set; }

	private int idx_triangle_base;

	public PrimitiveBase ()
	{
		vertices = new List<Vector3> ();
		triangles = new List<int> ();
		vcolors = new List<Color32> ();
		submeshes = new List<SubMesh>();
		pen = new Pen ();

		idx_triangle_base = 0;
	}

	public virtual void Begin()
	{
		vertices.Clear ();
		triangles.Clear (); 
		vcolors.Clear ();
		submeshes.Clear ();
		pen.Init ();

		idx_triangle_base = 0;
	}

	public void End()
	{
		AddSubMesh ();
	}

	private void ReplacePen( Pen p )
	{
		if (p != pen) {
			if ( pen.shaderType != Pen.ShaderType.VertexColors ) {
				AddSubMesh ();
			}
			pen = p;
		}
	}

	public void SetFrontColor( Color32 frontColor )
	{
		Pen p = new Pen (pen);
		p.frontColor = frontColor;
		ReplacePen (p);
	}

	public void SetBackColor( Color32 backColor )
	{
		Pen p = new Pen (pen);
		p.backColor = backColor;
		ReplacePen (p);
	}
		
	public void SetTopology( Pen.Topology topology )
	{
		Pen p = new Pen (pen);
		p.topology = topology;
		ReplacePen (p);
	}

	public void SetRadius( float radius )
	{
		Pen p = new Pen (pen);
		p.radius = radius;
		ReplacePen (p);
	}

	public void SetStacks( int stacks )
	{
		Pen p = new Pen (pen);
		p.stacks = stacks;
		ReplacePen (p);
	}

	public void SetSlices( int slices )
	{
		Pen p = new Pen (pen);
		p.slices = slices;
		ReplacePen (p);
	}

	protected void AddSubMesh()
	{
		if (triangles.Count - idx_triangle_base > 0) {
			var idx = submeshes.Count;
			submeshes.Add (new SubMesh ());
			submeshes [idx].triangles = triangles.GetRange (idx_triangle_base, triangles.Count - idx_triangle_base);
			submeshes [idx].pen.frontColor = pen.frontColor;
			submeshes [idx].pen.backColor = pen.backColor;
			submeshes [idx].pen.shaderType = pen.shaderType;

			idx_triangle_base = triangles.Count;
		}
	}

	protected void DrawTriangle(int idx_left, int idx_top, int idx_right)
	{
		triangles.Add (idx_left);
		triangles.Add (idx_top);
		triangles.Add (idx_right);
	}

	protected void DrawQuad(int idx_lu, int idx_ld, int idx_ru, int idx_rd)
	{
		if ( vertices[idx_ru] != vertices[idx_rd]) {
			triangles.Add (idx_ld);
			triangles.Add (idx_ru);
			triangles.Add (idx_rd);
		}
		if (vertices[idx_lu] != vertices[idx_ld]) {
			triangles.Add (idx_ld);
			triangles.Add (idx_lu);
			triangles.Add (idx_ru);
		}
	}

	protected void DrawPolygon( List<int> idxs, bool counter )
	{
		var N = idxs.Count;

		// triangle strip
		var i0 = 0;
		var i1 = 1;
		var i2 = N-1;
		for( var i = 0; i < N-2 ; ++i ){

			if (!counter) { // clockwise
				triangles.Add (idxs [i0]);
				triangles.Add (idxs [i1]);
				triangles.Add (idxs [i2]);
			} else { // counter clockwise
				triangles.Add (idxs [i0]);
				triangles.Add (idxs [i2]);
				triangles.Add (idxs [i1]);
			}

			if( i%2 == 0 ){
				i0 = i2;
				--i2;
			}
			else{
				i0 = i1;
				++i1;
			}
		}
	}

	protected void DrawMesh( int rows, int cols, int[,] idxs )
	{
		var maxrows = rows - 1;
		var maxcols = cols - 1;

		if (pen.topology == Pen.Topology.CloseRows) {
			maxrows++;
		}
		else if (pen.topology == Pen.Topology.CloseColumns) {
			maxcols++;
		}
		else if (pen.topology == Pen.Topology.CloseBoth ){
			maxrows++;
			maxcols++;
		}

		for (var r = 0; r < maxrows; r++) {
			for (var c = 0; c < maxcols; c++) {

				var idx_ld = idxs [r, c];
				var idx_lu = idxs [(r + 1)%rows, c];
				var idx_rd = idxs [r, (c + 1)%cols];
				var idx_ru = idxs [(r + 1)%rows, (c + 1)%cols];

				// quad
				if (idx_ld >= 0 && idx_lu >= 0 && idx_rd  >= 0 && idx_ru  >= 0 ) {

					DrawQuad (idx_lu, idx_ld, idx_ru, idx_rd);
				}

				// lower left
				else if (idx_ld >= 0 && idx_lu >= 0 && idx_rd >= 0 ) {

					DrawTriangle (idx_ld, idx_lu, idx_rd);
				}

				// lower right
				else if (idx_ld >= 0 && idx_ru >= 0 && idx_rd >= 0 ) {

					DrawTriangle (idx_ld, idx_ru, idx_rd);
				}

				// upper left
				else if (idx_ld >= 0 && idx_lu >= 0 && idx_ru>= 0 ) {

					DrawTriangle (idx_ld, idx_lu, idx_ru);
				}

				// upper right
				else if (idx_lu >= 0 && idx_ru >= 0 && idx_rd >= 0 ) {

					DrawTriangle (idx_lu, idx_ru, idx_rd);
				}
			}
		}
	}

	protected List<Vector3> GetSection( int stack )
	{
		var section = new List<Vector3> ();
		var phi = Mathf.PI / (pen.stacks+1) * stack; // Latitude
		for (var s = 0; s < pen.slices; ++s) {
			var theta = 2f * Mathf.PI / pen.slices * s; // Longitude
			theta += Mathf.PI / 2; // section[0] becomes forward
			var x = pen.radius * Mathf.Sin (phi) * Mathf.Cos (theta);
			var y = pen.radius * Mathf.Cos (phi);
			var z = pen.radius * Mathf.Sin (phi) * Mathf.Sin (theta);
			section.Add (new Vector3 (x, y, z));
		}
		return section;
	}
}
