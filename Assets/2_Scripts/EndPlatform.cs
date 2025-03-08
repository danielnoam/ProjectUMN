using System;
using UnityEngine;

public class EndPlatform : MonoBehaviour
{
    
    private TestManager _testManager;

    private void Start()
    {
        _testManager = TestManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_testManager)
        {
            Debug.LogError("TestManager is null");
            return;
        }
        
        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            _testManager.LoadNextTest();
        }
    }
}
