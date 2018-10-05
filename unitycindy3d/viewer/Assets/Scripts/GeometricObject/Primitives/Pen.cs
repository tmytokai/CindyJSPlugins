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

public class Pen
{
	public enum ShaderType
	{
		SingleSided,
		DoubleSided,
		VertexColors
	};

	public enum Topology
	{
		Open,
		Close,
		CloseRows,
		CloseColumns,
		CloseBoth
	};

	public Color32 frontColor{ get; set; }
	public Color32 backColor{ get; set; }
	// public float shininess{ get; set; }
	// public float alpha{ get; set; }
	// public LineType lineType{ get; set; }
	// public NormalType normalYype{ get; set; }
	public Topology topology{ get; set; }
	public float radius{ get; set; }
	public int stacks{ get; set; }   // ‎lines of latitude
	public int slices{ get; set; }   // ‎lines of longitude
	public ShaderType shaderType{ get; set; }

	public Pen()
	{
		Init ();
	}

	public Pen( Pen p )
	{
		Init ();

		frontColor = p.frontColor;
		backColor = p.backColor;
		topology = p.topology;
		radius = p.radius;
		stacks = p.stacks;
		slices = p.slices;
		shaderType = p.shaderType;
	}

	public void Init()
	{
		frontColor = Color.white;
		backColor = Color.black;
		topology = Topology.Open;
		radius = 0f;
		stacks = 0;
		slices = 0;
		shaderType = ShaderType.SingleSided;
	}

	public static bool operator == ( Pen a,  Pen b )
	{
		if ( (Color)a.frontColor == (Color)b.frontColor
			&& (Color)a.backColor == (Color)b.backColor
			&& a.topology == b.topology
			&& a.radius == b.radius
			&& a.stacks == b.stacks
			&& a.slices == b.slices
		    && a.shaderType == b.shaderType 
		) {
			return true;
		}

		return false;
	}

	public static bool operator != ( Pen a,  Pen b )
	{
		return !( a == b );
	}

	public override bool Equals( object a )
	{
		if (!(a is Pen)) {
			return false;
		}

		return ( this == (Pen)a );
	}

	public override int GetHashCode()
	{
		var hash = frontColor.GetHashCode ()
		           ^ backColor.GetHashCode ()
		           ^ topology.GetHashCode ()
		           ^ radius.GetHashCode ()
		           ^ stacks
		           ^ slices
		           ^ shaderType.GetHashCode ();
		
		return hash;
	}
}
