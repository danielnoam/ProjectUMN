// Copyright (c) 2024 Liquid Glass Studios. All rights reserved.

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace Manul.Toolbar.Examples
{
	public class ExampleScriptableObject : ScriptableObject
	{ 
		public void TestNumberFloat()
		{
			Debug.Log("Value of Float Number was changed. New value: " + EditorPrefs.GetFloat("float number example pref"));
		}

		public void TestNumberInt()
		{
			Debug.Log("Value of Int Number was changed. New value: " + EditorPrefs.GetInt("int number example pref"));
		}
	}
}

#endif