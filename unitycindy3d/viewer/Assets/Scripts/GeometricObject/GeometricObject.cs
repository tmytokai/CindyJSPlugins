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

public class GeometricObject : MonoBehaviour 
{
	public Material material_singleSided;
	public Material material_doubleSided;
	public Material material_vertexColors;

	public bool rendering{ get; set; }
	public bool initialized{ get; private set; }

	private List<PrimitiveBase> primitives;
	private PointPrimitive ptp;
	private LinePrimitive lp;
	private PolygonPrimitive pp;
	private SpherePrimitive sp;
	private MeshPrimitive mp;

	public void SetPointFrontColor (Color32 frontColor){ ptp.SetFrontColor (frontColor); }
	public void SetPointRadius (float radius){ ptp.SetRadius(radius); }
	public void SetPointStacks (int stacks){ ptp.SetStacks(stacks); }
	public void SetPointSlices (int slices){ ptp.SetSlices(slices); }
	public void AddPoint ( Vector3 point ){ ptp.Add (point); }

	public void SetLineFrontColor (Color32 frontColor){ lp.SetFrontColor (frontColor); }
	public void SetLineTopology( Pen.Topology topology ){ lp.SetTopology(topology);}
	public void SetLineRadius (float radius){ lp.SetRadius(radius); }
	public void SetLineSlices (int slices){ lp.SetSlices(slices); }
	public void AddLine ( List<Vector3> points, List<Color32> frontColors ){ lp.Add (points, frontColors); }

	public void SetPolygonFrontColor (Color32 frontColor){pp.SetFrontColor (frontColor); }
	public void SetPolygonBackColor (Color32 backColor){pp.SetBackColor (backColor); }
	public void AddPolygon ( List<Vector3> points ){ pp.Add (points);}

	public void SetSphereFrontColor (Color32 frontColor){ sp.SetFrontColor (frontColor); }
	public void SetSphereRadius (float radius){ sp.SetRadius(radius); }
	public void SetSphereStacks (int stacks){ sp.SetStacks(stacks); }
	public void SetSphereSlices (int slices){ sp.SetSlices(slices); }
	public void AddSphere ( Vector3 center ){ sp.Add (center); }

	public void SetMeshFrontColor (Color32 frontColor){ mp.SetFrontColor (frontColor); }
	public void SetMeshBackColor (Color32 backColor){ mp.SetBackColor (backColor); }
	public void SetMeshTopology( Pen.Topology topology ){ mp.SetTopology(topology);}
	public void AddMesh ( int rows, int cols, List<Vector3> points, int[,] idxs ){ mp.Add (rows, cols, points, idxs);}

	public GeometricObject()
	{
		rendering = false;
		initialized = false;

		primitives = new List<PrimitiveBase> ();

		ptp = new PointPrimitive ();
		primitives.Add (ptp);

		lp = new LinePrimitive ();
		primitives.Add (lp);

		pp = new PolygonPrimitive ();
		primitives.Add (pp);

		sp = new SpherePrimitive ();
		primitives.Add (sp);

		mp = new MeshPrimitive ();
		primitives.Add (mp);
	}

	public void Begin()
	{
		foreach (var prim in primitives) {
			prim.Begin ();
		}

		initialized = true;
	}

	public void End()
	{
		foreach (var prim in primitives) {
			prim.End ();
		}

		var idx_base = 0;
		var vertices = new List<Vector3> ();
		var triangles = new List<int> ();
		var submeshes_count = 0;
		var colors = new List<Color32> ();
		foreach (var prim in primitives) {
			vertices.AddRange (prim.vertices);
			triangles.AddRange ( prim.triangles.Select (e => e + idx_base).ToList () );
			submeshes_count += prim.submeshes.Count;
			colors.AddRange (prim.vcolors);
			idx_base += prim.vertices.Count;
		}

		/*
		Manager.DebugLog ("----------");
		Manager.DebugLog ("End:" + name);
		Manager.DebugLog ("total size of vertices = " + vertices.Count);
		Manager.DebugLog ("total size of triangles = " + triangles.Count);
		Manager.DebugLog ("total size of colors = " + colors.Count);
		Manager.DebugLog ("total size of submeshes = " + submeshes_count);

		foreach (var prim in primitives) {
			Manager.DebugLog ("----------");
			Manager.DebugLog (prim.name);
			Manager.DebugLog ("size of vertices = " + prim.vertices.Count);
			Manager.DebugLog ("size of triangles = " + prim.triangles.Count);
			Manager.DebugLog ("size of colorss = " + prim.vcolors.Count);
			Manager.DebugLog ("sizeof submeshes = " + prim.submeshes.Count);
		}
		*/

		var mesh = new Mesh();
		mesh.name = name;
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.colors32 = colors.ToArray ();

		GetComponent<MeshFilter> ().sharedMesh = mesh;

		var i = 0;
		idx_base = 0;
		var materials = new Material[submeshes_count];
		mesh.subMeshCount = submeshes_count;
		foreach (var prim in primitives) {			
			foreach (var subm in prim.submeshes) {
 
				if (subm.pen.shaderType == Pen.ShaderType.SingleSided ){
					materials [i] = material_singleSided;
				} else if (subm.pen.shaderType == Pen.ShaderType.DoubleSided ){
					materials [i] = material_doubleSided;
				} else if (subm.pen.shaderType == Pen.ShaderType.VertexColors) {
					materials [i] = material_vertexColors;
				}

				mesh.SetTriangles ( subm.triangles.Select (e => e + idx_base).ToList (), i );
				++i;
			}

			idx_base += prim.vertices.Count;
		}

		GetComponent<Renderer> ().materials = materials;

		i = 0;
		foreach (var prim in primitives) {			
			foreach (var subm in prim.submeshes) {

				GetComponent<Renderer> ().materials [i].SetColor ("_FColor", subm.pen.frontColor);
				GetComponent<Renderer> ().materials [i].SetColor ("_BColor", subm.pen.backColor);
				++i;
			}
		}

		//mesh.RecalculateNormals();
		NormalSolver.RecalculateNormals(mesh,60f); // see http://schemingdeveloper.com/2017/03/26/better-method-recalculate-normals-unity-part-2/
		mesh.RecalculateBounds();

		rendering = false;
		initialized = false;
	}
}
