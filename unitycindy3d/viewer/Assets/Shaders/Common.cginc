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

#pragma target 3.0   // default: 2.5

#pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
#pragma shader_feature _ _GLOSSYREFLECTIONS_OFF

fixed4 _FColor;
fixed4 _BColor;

half _Glossiness;
half _Metallic;

struct Input {
	float4 vertColor;
};

void surf_front (Input IN, inout SurfaceOutputStandard o) {
    fixed4 c = _FColor;
    o.Albedo = c.rgb;
    o.Alpha = c.a;
	o.Metallic = _Metallic;
	o.Smoothness = _Glossiness;
}

void surf_back (Input IN, inout SurfaceOutputStandard o) {
    fixed4 c = _BColor;
    o.Albedo = c.rgb;
    o.Alpha = c.a;
	o.Metallic = _Metallic;
	o.Smoothness = _Glossiness;
}

void surf_vert (Input IN, inout SurfaceOutputStandard o) {
	o.Albedo = IN.vertColor.rgb;
	o.Alpha = IN.vertColor.a;
	o.Metallic = _Metallic;
	o.Smoothness = _Glossiness;
}

void vert_vert(inout appdata_full v, out Input o){
	UNITY_INITIALIZE_OUTPUT(Input, o);
	o.vertColor = v.color;
}
