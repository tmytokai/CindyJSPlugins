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

public class PointPrimitive : SpherePrimitive {

	public override void Begin()
	{
		base.Begin ();

		name = "PointPrimitive";

		pen.radius = 0.02f;
		pen.stacks = 3;
		pen.slices = 6;
	}

}
