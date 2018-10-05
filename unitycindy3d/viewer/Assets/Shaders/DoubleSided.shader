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

Shader "GeometricObject/DoubleSided"
{
	// from Standard.shader in builtin_shaders-2018.*.zip
	Properties
	{
		_FColor ("Front Color", Color) = (1,1,1,1)
		_BColor ("Back Color",  Color) = (1,1,1,1)

		_Metallic ( "Metallic", Range(0.0, 1.0)) = 0.0
        _Glossiness ( "Smoothness", Range(0.0, 1.0)) = 0.5

		[ToggleOff] _SpecularHighlights( "Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections( "Reflections", Float) = 0.0
	}

    SubShader {

        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // renders the depth buffer
        Pass {
          Cull Off
          ZWrite On
          ColorMask 0
        }

        // renders the back surface
        Cull Front
        CGPROGRAM
		#pragma surface surf_back Standard alpha fullforwardshadows
		#include "Common.cginc"
        ENDCG

        // renders the front surface
   		Cull Back
        CGPROGRAM
		#pragma surface surf_front Standard alpha fullforwardshadows
		#include "Common.cginc"
        ENDCG
	}

	FallBack "Diffuse"
}