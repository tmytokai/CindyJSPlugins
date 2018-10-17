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

using System;
using System.Threading;

public class Manager : MonoBehaviour {

	public static SynchronizationContext context{ get; private set; } = null;

	void Start () {}

	void Update () {}

	public static void DebugLog ( object message )
	{
		if (context != null) {

			if (context != SynchronizationContext.Current) {
				var id = System.Threading.Thread.CurrentThread.ManagedThreadId;
				context.Post ( (state) => Debug.Log (state), "[" + id + "] " + message);
			} else {
				Debug.Log ("[UI] " + message);
			}				
		}
		else{
			Debug.Log ( message );
		}
	}

	public static void DebugLogWarning(string message)
	{
		if (context != null) {

			if (context != SynchronizationContext.Current) {
				var id = System.Threading.Thread.CurrentThread.ManagedThreadId;
				context.Post ( (state) => Debug.LogWarning (state), "[" + id + "] " + message);
			} else {
				Debug.LogWarning ("[UI] " + message);
			}				
		}
		else{
			Debug.LogWarning ( message );
		}
	}

	public static void DebugLogError(string message)
	{
		if (context != null) {

			if (context != SynchronizationContext.Current) {
				var id = System.Threading.Thread.CurrentThread.ManagedThreadId;
				context.Post ( (state) => Debug.LogError (state), "[" + id + "] " + message);
			} else {
				Debug.LogError ("[UI] " + message);
			}				
		}
		else{
			Debug.LogError ( message );
		}
	}
}
