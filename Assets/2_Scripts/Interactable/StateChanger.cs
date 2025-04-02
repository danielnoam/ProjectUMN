using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VInspector;

[Serializable]
public class State
{
    public string name;
    public bool skipStateOnCycle;
    public UnityEvent stateEnterEvent;
    public UnityEvent stateUpdateEvent;
    public UnityEvent stateExitEvent;
}


public class StateChanger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool cycleStates = true;
    [SerializeField] private State[] states;
    
    
    [SerializeField, ReadOnly] private string currentStateName;
    [SerializeField, ReadOnly] private string previousStateName;
    private State _currentState;
    private State _previousState;
    
    
    private void Start()
    {
        if (states.Length == 0) return;

        ChangeState(states[0].name);
    }

    private void Update()
    {
        _currentState?.stateUpdateEvent?.Invoke();
        
        currentStateName = _currentState?.name;
        previousStateName = _previousState?.name;
    }
    
    public void ChangeState(string stateName)
    {
        foreach (var state in states)
        {
            if (state.name == stateName)
            {
                if (_currentState != null)
                {
                    _previousState = _currentState;
                    _currentState.stateExitEvent?.Invoke();
                }
                _currentState = state;
                _currentState.stateEnterEvent?.Invoke();
                return;
            }
        }
    }

    [Button]
    public void SetNextState()
    {
        if (cycleStates)
        {
            // Safety check for empty states array
            if (states.Length == 0 || _currentState == null)
                return;
                
            // Get starting index to avoid infinite loop
            int startIndex = Array.IndexOf(states, _currentState);
            int index = startIndex;
            
            // Track which states we've already tried to avoid infinite loops
            bool[] visited = new bool[states.Length];
            
            do
            {
                // Move to the next state
                index = (index + 1) % states.Length;
                
                // If we've found a state we can use, change to it
                if (!states[index].skipStateOnCycle)
                {
                    ChangeState(states[index].name);
                    return;
                }
                
                // Mark this state as visited
                visited[index] = true;
                
                // If we've visited all states and they're all marked as skip, 
                // just use the next state regardless of its skip status
                if (index == startIndex || AllStatesVisited(visited))
                {
                    ChangeState(states[(startIndex + 1) % states.Length].name);
                    return;
                }
            } while (true); // We'll always return from inside the loop
        }
        else
        {
            if (_currentState != null)
            {
                ChangeState(_currentState.name);
            }
        }
    }
    
    [Button]
    public void SetPreviousState()
    {
        if (cycleStates)
        {
            // Safety check for empty states array
            if (states.Length == 0 || _currentState == null)
                return;
                
            // Get starting index to avoid infinite loop
            int startIndex = Array.IndexOf(states, _currentState);
            int index = startIndex;
            
            // Track which states we've already tried to avoid infinite loops
            bool[] visited = new bool[states.Length];
            
            do
            {
                // Move to the previous state
                index = (index - 1 + states.Length) % states.Length;
                
                // If we've found a state we can use, change to it
                if (!states[index].skipStateOnCycle)
                {
                    ChangeState(states[index].name);
                    return;
                }
                
                // Mark this state as visited
                visited[index] = true;
                
                // If we've visited all states and they're all marked as skip, 
                // just use the previous state regardless of its skip status
                if (index == startIndex || AllStatesVisited(visited))
                {
                    ChangeState(states[(startIndex - 1 + states.Length) % states.Length].name);
                    return;
                }
            } while (true); // We'll always return from inside the loop
        }
        else
        {
            if (_currentState != null)
            {
                ChangeState(_currentState.name);
            }
        }
    }
    
    // Helper method to check if all states have been visited
    private bool AllStatesVisited(bool[] visited)
    {
        foreach (bool visit in visited)
        {
            if (!visit)
                return false;
        }
        return true;
    }
}