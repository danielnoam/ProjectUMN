using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;
using PrimeTween;
using UnityEngine.Events;

public class TestClosingWalls : MonoBehaviour
{
    [Foldout("Closing Animation")]
    [Header("Part 1: Initial Shake Effect")]
    [SerializeField] private float initialShakeStartDelay = 0.5f;
    [SerializeField] private float initialShakeEndDelay = 0f;
    [SerializeField] private float initialShakeTime = 1f;
    [SerializeField] private int initialShakeFrequency = 10;
    [SerializeField] private Vector3 initialShakeStrength = new Vector3(0.2f, 0, 0);
    [SerializeField] private UnityEvent initialShakeEvent = new UnityEvent();
    
    [Header("Part 2: Move to Semi-Closed")]
    [SerializeField] private float moveToSemiClosedStartDelay = 0.5f;
    [SerializeField] private float moveToSemiClosedEndDelay = 1f;
    [SerializeField] private float moveToSemiClosedTime = 5f;
    [SerializeField] private float wallStartDelayIncrement = 0.2f; // Delay increment between each wall
    [SerializeField] private Ease moveToSemiClosedEase = Ease.Linear;
    [SerializeField] private UnityEvent moveToSemiClosedEvent = new UnityEvent();
    
    [Header("Part 3: Move to Closed")]
    [SerializeField] private float moveToClosedStartDelay = 0.5f;
    [SerializeField] private float moveToClosedEndDelay = 0f;
    [SerializeField] private float moveToClosedTime = 1f;
    [SerializeField] private Ease moveToClosedEase = Ease.Linear;
    [SerializeField] private UnityEvent moveToClosedEvent = new UnityEvent();
    [SerializeField] private UnityEvent moveToClosedFinishedEvent = new UnityEvent();
    [EndFoldout]
    
    [Foldout("Opening Animation")]
    [SerializeField] private float openingStartDelay = 0f;
    [SerializeField] private float openingEndDelay = 0f;
    [SerializeField] private float openingTime = 2f;
    [SerializeField] private Ease openingEase = Ease.Linear;
    [SerializeField] private UnityEvent openingEvent = new UnityEvent();
    [EndFoldout]
    
    [Foldout("Left Walls")]
    [SerializeField] private List<Transform> leftWalls = new List<Transform>();
    [SerializeField] private float semiClosedPosL;
    [SerializeField] private float closedPosL; 
    [EndFoldout]
    
    [Foldout("Right Walls")]
    [SerializeField] private List<Transform> rightWalls = new List<Transform>();
    [SerializeField] private float semiClosedPosR; 
    [SerializeField] private float closedPosR; 
    [EndFoldout]
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool isSemiClosed;
    [SerializeField, ReadOnly] private bool isClosed;
    

    private List<Vector3> _openedPosL = new List<Vector3>();
    private List<Vector3> _openedPosR = new List<Vector3>();
    private Sequence _animationSequence;

    private void Awake()
    {
        // Check if we have any walls to animate
        if (leftWalls.Count == 0 && rightWalls.Count == 0) return;
        
        // Store initial positions for all walls
        foreach (var wall in leftWalls)
        {
            if (wall != null)
                _openedPosL.Add(wall.localPosition);
        }
        
        foreach (var wall in rightWalls)
        {
            if (wall != null)
                _openedPosR.Add(wall.localPosition);
        }
        
        // Apply the initial state
        ApplyStateImmediate(isClosed);
    }

    private void Start()
    {
        PlayerStateMachine.Instance?.onPlayerDeath.AddListener(Open);
        TestManager.Instance?.Robot?.onRobotDeath.AddListener(Open);
        TestManager.Instance?.Robot?.onRobotRespawn.AddListener(Open);
    }
    
    private void OnDestroy()
    {
        TestManager.Instance?.Robot?.onRobotDeath.RemoveListener(Open);
        TestManager.Instance?.Robot?.onRobotRespawn.RemoveListener(Open);
        PlayerStateMachine.Instance?.onPlayerDeath.RemoveListener(Open);
        _animationSequence.Stop();
    }

    [Button]
    public void Open()
    {
        SetState(false, false);
    }
    
    [Button]
    public void Close()
    {
        SetState(true, false);
    }
    
    private void SetState(bool closed, bool instant = false)
    {
        // Check if we have any walls to animate
        if (leftWalls.Count == 0 && rightWalls.Count == 0) return;
            
        if (!Application.isPlaying || instant)
        {
            ApplyStateImmediate(closed);
            return;
        }
        
        if (closed)
        {
            CloseWalls();
        }
        else
        {
            OpenWalls();
        }
    }
    
    private void ApplyStateImmediate(bool closed)
    {
        if (leftWalls.Count == 0 && rightWalls.Count == 0) return;
            
        isClosed = closed;
        isSemiClosed = closed;
        
        if (closed) 
        {
            // Move all left walls to closed position
            for (int i = 0; i < leftWalls.Count; i++)
            {
                if (leftWalls[i] != null && i < _openedPosL.Count)
                {
                    Vector3 closedPos = new Vector3(_openedPosL[i].x + closedPosL, _openedPosL[i].y, _openedPosL[i].z);
                    leftWalls[i].localPosition = closedPos;
                }
            }
            
            // Move all right walls to closed position
            for (int i = 0; i < rightWalls.Count; i++)
            {
                if (rightWalls[i] != null && i < _openedPosR.Count)
                {
                    Vector3 closedPos = new Vector3(_openedPosR[i].x + closedPosR, _openedPosR[i].y, _openedPosR[i].z);
                    rightWalls[i].localPosition = closedPos;
                }
            }
        }
        else 
        {
            // Move all left walls to open position
            for (int i = 0; i < leftWalls.Count; i++)
            {
                if (leftWalls[i] != null && i < _openedPosL.Count)
                {
                    leftWalls[i].localPosition = _openedPosL[i];
                }
            }
            
            // Move all right walls to open position
            for (int i = 0; i < rightWalls.Count; i++)
            {
                if (rightWalls[i] != null && i < _openedPosR.Count)
                {
                    rightWalls[i].localPosition = _openedPosR[i];
                }
            }
        }
    }
    
    private void OpenWalls()
    {
        if (leftWalls.Count == 0 && rightWalls.Count == 0) return;
            
        if (!isClosed) return;
        
        if (_animationSequence.isAlive) 
        {
            _animationSequence.Stop();
        }
        
        var tweenSettings = new TweenSettings(openingTime, openingEase, startDelay: openingStartDelay, endDelay: openingEndDelay);
        _animationSequence = Sequence.Create();

        _animationSequence = _animationSequence
            .ChainCallback(() => { Debug.Log("Opening"); })
            .ChainCallback(() => { openingEvent?.Invoke(); });

        // Animate all left walls
        for (int i = 0; i < leftWalls.Count; i++)
        {
            if (leftWalls[i] != null && i < _openedPosL.Count)
            {
                if (i == 0)
                {
                    _animationSequence = _animationSequence.Chain(Tween.LocalPosition(leftWalls[i], _openedPosL[i], tweenSettings));
                }
                else
                {
                    _animationSequence = _animationSequence.Group(Tween.LocalPosition(leftWalls[i], _openedPosL[i], tweenSettings));
                }
            }
        }

        // Animate all right walls
        for (int i = 0; i < rightWalls.Count; i++)
        {
            if (rightWalls[i] != null && i < _openedPosR.Count)
            {
                _animationSequence = _animationSequence.Group(Tween.LocalPosition(rightWalls[i], _openedPosR[i], tweenSettings));
            }
        }

        _animationSequence = _animationSequence
            .ChainCallback(() => isSemiClosed = false)
            .ChainCallback(() => isClosed = false);
    }
    
    private void CloseWalls()
    {
        if (leftWalls.Count == 0 && rightWalls.Count == 0) return;
        
        if (isClosed) return;

        if (_animationSequence.isAlive) 
        {
            _animationSequence.Stop();
        }
        
        _animationSequence = Sequence.Create();
        
        var shakeSettings = new ShakeSettings(frequency: initialShakeFrequency, duration:initialShakeTime, strength: initialShakeStrength, startDelay: initialShakeStartDelay, endDelay: initialShakeEndDelay);

        _animationSequence = _animationSequence
            // Part 1: Initial shake
            .ChainCallback(() => { Debug.Log("Initial Shake"); })
            .ChainCallback(() => { initialShakeEvent?.Invoke(); });
        
        // Shake all left walls
        for (int i = 0; i < leftWalls.Count; i++)
        {
            if (leftWalls[i] != null && i < _openedPosL.Count)
            {
                if (i == 0)
                {
                    _animationSequence = _animationSequence.Chain(Tween.ShakeLocalPosition(leftWalls[i], shakeSettings));
                }
                else
                {
                    _animationSequence = _animationSequence.Group(Tween.ShakeLocalPosition(leftWalls[i], shakeSettings));
                }
            }
        }
        
        // Shake all right walls
        for (int i = 0; i < rightWalls.Count; i++)
        {
            if (rightWalls[i] != null && i < _openedPosR.Count)
            {
                _animationSequence = _animationSequence.Group(Tween.ShakeLocalPosition(rightWalls[i], shakeSettings));
            }
        }

        // Part 2: Move to semi-closed position with delays
        _animationSequence = _animationSequence
            .ChainCallback(() => { Debug.Log("Moving to semi-closed"); })
            .ChainCallback(() => { moveToSemiClosedEvent?.Invoke(); });

        // Move all left walls to semi-closed with incrementing delays
        for (int i = 0; i < leftWalls.Count; i++)
        {
            if (leftWalls[i] != null && i < _openedPosL.Count)
            {
                // Calculate staggered start delay for each wall
                float wallStartDelay = moveToSemiClosedStartDelay + (i * wallStartDelayIncrement);
                var semiClosedSettings = new TweenSettings(moveToSemiClosedTime, moveToSemiClosedEase, startDelay: wallStartDelay, endDelay: moveToSemiClosedEndDelay);
                
                Vector3 semiClosedPos = new Vector3(_openedPosL[i].x + semiClosedPosL, _openedPosL[i].y, _openedPosL[i].z);
                
                if (i == 0)
                {
                    _animationSequence = _animationSequence.Chain(Tween.LocalPosition(leftWalls[i], startValue: _openedPosL[i], endValue: semiClosedPos, semiClosedSettings));
                }
                else
                {
                    _animationSequence = _animationSequence.Group(Tween.LocalPosition(leftWalls[i], startValue: _openedPosL[i], endValue: semiClosedPos, semiClosedSettings));
                }
            }
        }
        
        // Move all right walls to semi-closed with incrementing delays
        for (int i = 0; i < rightWalls.Count; i++)
        {
            if (rightWalls[i] != null && i < _openedPosR.Count)
            {
                // Calculate staggered start delay for each wall
                float wallStartDelay = moveToSemiClosedStartDelay + (i * wallStartDelayIncrement);
                var semiClosedSettings = new TweenSettings(moveToSemiClosedTime, moveToSemiClosedEase, startDelay: wallStartDelay, endDelay: moveToSemiClosedEndDelay);
                
                Vector3 semiClosedPos = new Vector3(_openedPosR[i].x + semiClosedPosR, _openedPosR[i].y, _openedPosR[i].z);
                
                _animationSequence = _animationSequence.Group(Tween.LocalPosition(rightWalls[i], startValue: _openedPosR[i], endValue: semiClosedPos, semiClosedSettings));
            }
        }

        _animationSequence = _animationSequence.ChainCallback(() => isSemiClosed = true);

        // Part 3: Move to fully closed position from semi-closed position (all together)
        _animationSequence = _animationSequence
            .ChainCallback(() => { Debug.Log("Moving to closed"); })
            .ChainCallback(() => { moveToClosedEvent?.Invoke(); });
        
        var closedSettings = new TweenSettings(moveToClosedTime, moveToClosedEase, startDelay: moveToClosedStartDelay, endDelay: moveToClosedEndDelay);
        
        // Move all left walls from semi-closed to closed simultaneously
        for (int i = 0; i < leftWalls.Count; i++)
        {
            if (leftWalls[i] != null && i < _openedPosL.Count)
            {
                Vector3 semiClosedPos = new Vector3(_openedPosL[i].x + semiClosedPosL, _openedPosL[i].y, _openedPosL[i].z);
                Vector3 closedPos = new Vector3(_openedPosL[i].x + closedPosL, _openedPosL[i].y, _openedPosL[i].z);
                
                if (i == 0)
                {
                    _animationSequence = _animationSequence.Chain(Tween.LocalPosition(leftWalls[i], startValue: semiClosedPos, endValue: closedPos, closedSettings));
                }
                else
                {
                    _animationSequence = _animationSequence.Group(Tween.LocalPosition(leftWalls[i], startValue: semiClosedPos, endValue: closedPos, closedSettings));
                }
            }
        }
        
        // Move all right walls from semi-closed to closed simultaneously
        for (int i = 0; i < rightWalls.Count; i++)
        {
            if (rightWalls[i] != null && i < _openedPosR.Count)
            {
                Vector3 semiClosedPos = new Vector3(_openedPosR[i].x + semiClosedPosR, _openedPosR[i].y, _openedPosR[i].z);
                Vector3 closedPos = new Vector3(_openedPosR[i].x + closedPosR, _openedPosR[i].y, _openedPosR[i].z);
                
                _animationSequence = _animationSequence.Group(Tween.LocalPosition(rightWalls[i], startValue: semiClosedPos, endValue: closedPos, closedSettings));
            }
        }
        
        _animationSequence = _animationSequence
            .ChainCallback(() => isClosed = true)
            .ChainCallback(() => { moveToClosedFinishedEvent?.Invoke(); });
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Check if we have any walls to visualize
        if (leftWalls.Count == 0 && rightWalls.Count == 0)
            return;
        
        // Define gizmo settings directly here instead of in inspector
        float sphereRadius = 0.2f;
        float labelOffset = 0.3f;
        
        // Define colors here instead of in inspector
        Color leftWallColor = Color.red;
        Color rightWallColor = Color.blue;
        
        // Visualize left walls
        for (int i = 0; i < leftWalls.Count; i++)
        {
            if (leftWalls[i] == null) continue;
            
            // Save the current transform values to calculate positions correctly in editor mode
            Vector3 tempOpenPosL = Application.isPlaying && i < _openedPosL.Count ? _openedPosL[i] : leftWalls[i].localPosition;
            
            // Get current position (open position)
            Vector3 leftOpenPos = leftWalls[i].position;
            
            // Calculate semi-closed position in world space using the OFFSET logic
            Vector3 leftSemiLocalPos = new Vector3(tempOpenPosL.x + semiClosedPosL, tempOpenPosL.y, tempOpenPosL.z);
            Vector3 leftSemiPos = leftWalls[i].parent ? 
                leftWalls[i].parent.TransformPoint(leftSemiLocalPos) : 
                new Vector3(tempOpenPosL.x + semiClosedPosL, leftWalls[i].position.y, leftWalls[i].position.z);
                
            // Calculate closed position in world space using the OFFSET logic
            Vector3 leftClosedLocalPos = new Vector3(tempOpenPosL.x + closedPosL, tempOpenPosL.y, tempOpenPosL.z);
            Vector3 leftClosedPos = leftWalls[i].parent ? 
                leftWalls[i].parent.TransformPoint(leftClosedLocalPos) : 
                new Vector3(tempOpenPosL.x + closedPosL, leftWalls[i].position.y, leftWalls[i].position.z);
            
            // Draw spheres at positions
            Gizmos.color = leftWallColor;
            Gizmos.DrawSphere(leftOpenPos, sphereRadius * 0.5f); // Smaller sphere for current position
            
            Gizmos.color = new Color(leftWallColor.r, leftWallColor.g, leftWallColor.b, 0.5f);
            Gizmos.DrawSphere(leftSemiPos, sphereRadius);
            
            Gizmos.color = leftWallColor;
            Gizmos.DrawSphere(leftClosedPos, sphereRadius);
            
            // Draw connecting lines
            Gizmos.DrawLine(leftOpenPos, leftSemiPos);
            Gizmos.DrawLine(leftSemiPos, leftClosedPos);
            
            // Add position labels if it's the first wall (to avoid cluttering)
            if (i == 0)
            {
                UnityEditor.Handles.color = leftWallColor;
                UnityEditor.Handles.Label(leftOpenPos + Vector3.up * labelOffset, "Open");
                
                UnityEditor.Handles.color = new Color(leftWallColor.r, leftWallColor.g, leftWallColor.b, 0.5f);
                UnityEditor.Handles.Label(leftSemiPos + Vector3.up * labelOffset, "Semi");
                
                UnityEditor.Handles.color = leftWallColor;
                UnityEditor.Handles.Label(leftClosedPos + Vector3.up * labelOffset, "Closed");
            }
        }
        
        // Visualize right walls
        for (int i = 0; i < rightWalls.Count; i++)
        {
            if (rightWalls[i] == null) continue;
            
            // Save the current transform values to calculate positions correctly in editor mode
            Vector3 tempOpenPosR = Application.isPlaying && i < _openedPosR.Count ? _openedPosR[i] : rightWalls[i].localPosition;
            
            // Get current position (open position)
            Vector3 rightOpenPos = rightWalls[i].position;
            
            // Calculate semi-closed position in world space using the OFFSET logic
            Vector3 rightSemiLocalPos = new Vector3(tempOpenPosR.x + semiClosedPosR, tempOpenPosR.y, tempOpenPosR.z);
            Vector3 rightSemiPos = rightWalls[i].parent ? 
                rightWalls[i].parent.TransformPoint(rightSemiLocalPos) : 
                new Vector3(tempOpenPosR.x + semiClosedPosR, rightWalls[i].position.y, rightWalls[i].position.z);
                
            // Calculate closed position in world space using the OFFSET logic
            Vector3 rightClosedLocalPos = new Vector3(tempOpenPosR.x + closedPosR, tempOpenPosR.y, tempOpenPosR.z);
            Vector3 rightClosedPos = rightWalls[i].parent ? 
                rightWalls[i].parent.TransformPoint(rightClosedLocalPos) : 
                new Vector3(tempOpenPosR.x + closedPosR, rightWalls[i].position.y, rightWalls[i].position.z);
            
            // Draw spheres at positions
            Gizmos.color = rightWallColor;
            Gizmos.DrawSphere(rightOpenPos, sphereRadius * 0.5f); // Smaller sphere for current position
            
            Gizmos.color = new Color(rightWallColor.r, rightWallColor.g, rightWallColor.b, 0.5f);
            Gizmos.DrawSphere(rightSemiPos, sphereRadius);
            
            Gizmos.color = rightWallColor;
            Gizmos.DrawSphere(rightClosedPos, sphereRadius);
            
            // Draw connecting lines
            Gizmos.DrawLine(rightOpenPos, rightSemiPos);
            Gizmos.DrawLine(rightSemiPos, rightClosedPos);
            
            // Add position labels if it's the first wall (to avoid cluttering)
            if (i == 0)
            {
                UnityEditor.Handles.color = rightWallColor;
                UnityEditor.Handles.Label(rightOpenPos + Vector3.up * labelOffset, "Open");
                
                UnityEditor.Handles.color = new Color(rightWallColor.r, rightWallColor.g, rightWallColor.b, 0.5f);
                UnityEditor.Handles.Label(rightSemiPos + Vector3.up * labelOffset, "Semi");
                
                UnityEditor.Handles.color = rightWallColor;
                UnityEditor.Handles.Label(rightClosedPos + Vector3.up * labelOffset, "Closed");
            }
        }
    }
#endif
}