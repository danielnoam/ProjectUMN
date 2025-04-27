// Copyright (c) 2024 Liquid Glass Studios. All rights reserved.

using UnityEngine;

#if UNITY_EDITOR

namespace Manul.Toolbar.Examples
{
	public class ExampleComponent : MonoBehaviour
	{
		void MethodFromComponent()
		{
			Debug.Log("This is a method invoked using the 'Component Method' action type.");
		}

		public void InvokeMethodByEvent(int parameter)
		{
			Debug.Log("This is a method invoked using an event by the 'Invoke Event' action type. The parameter is: " + parameter);
		}

	}
}

#endif
