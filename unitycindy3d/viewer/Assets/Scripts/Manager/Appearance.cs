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

public class Appearance{

	public Color32 frontColor{ get; set; }
	public Color32 backColor{ get; set; }
	public float shininess{ get; set; }
	public float radius{ get; set; }
	public float alpha{ get; set; }

	public Appearance( Color32 _frontColor, Color32 _backColor, float _shininess, float _radius, float _alpha ){
		frontColor = _frontColor;
		backColor = _backColor;
		shininess = _shininess;
		radius = _radius;
		alpha = _alpha;
	}
}
