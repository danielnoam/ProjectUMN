using System;
using UnityEngine;
using VInspector;
using PrimeTween;
using UnityEngine.Events;

public class ClosingWallsTest : MonoBehaviour
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
    
    [Foldout("Left Wall")]
    [SerializeField] private Transform leftWall;
    [SerializeField] private float semiClosedPosL;
    [SerializeField] private float closedPosL; 
    [EndFoldout]
    
    [Foldout("Right Wall")]
    [SerializeField] private Transform rightWall;
    [SerializeField] private float semiClosedPosR; 
    [SerializeField] private float closedPosR; 
    [EndFoldout]
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool isSemiClosed;
    [SerializeField, ReadOnly] private bool isClosed;
    

    private Vector3 _openedPosL;
    private Vector3 _openedPosR;
    private Sequence _animationSequence;
    private Vector3 ClosedPosLeftV => new Vector3(_openedPosL.x + closedPosL, _openedPosL.y, _openedPosL.z);
    private Vector3 SemiClosedPosLeftV => new Vector3(_openedPosL.x  + semiClosedPosL, _openedPosL.y, _openedPosL.z);
    private Vector3 ClosedPosRightV => new Vector3(_openedPosR.x + closedPosR, _openedPosR.y, _openedPosR.z);
    private Vector3 SemiClosedPosRightV => new Vector3(_openedPosR.x + semiClosedPosR, _openedPosR.y, _openedPosR.z);

    private void Awake()
    {
        // Check if we have any walls to animate
        if (!leftWall || !rightWall)  return;
        
        _openedPosL = leftWall.localPosition;
        _openedPosR = rightWall.localPosition;
        
        // Apply the initial state
        ApplyStateImmediate(isClosed);
    }

    private void Start()
    {
        PlayerStateMachine.Instance?.onPlayerDeath.AddListener(Open);
        TestManager.Instance?.Robot.onRobotDeath.AddListener(Open);
        TestManager.Instance?.Robot.onRobotRespawn.AddListener(Open);
    }
    
    private void OnDestroy()
    {
        TestManager.Instance?.Robot.onRobotDeath.RemoveListener(Open);
        TestManager.Instance?.Robot.onRobotRespawn.RemoveListener(Open);
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
        if (!leftWall || !rightWall)  return;
            
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
        if (!leftWall || !rightWall)  return;
            
        isClosed = closed;
        isSemiClosed = closed;
        
        if (closed) 
        {
            leftWall.localPosition = ClosedPosLeftV;
            rightWall.localPosition = ClosedPosRightV;
        }
        else 
        {
            leftWall.localPosition = _openedPosL;
            rightWall.localPosition = _openedPosR;
        }
    }
    
    private void OpenWalls()
    {
        if (!leftWall || !rightWall)  return;
            
        if (!isClosed) return;
        
        if (_animationSequence.isAlive) 
        {
            _animationSequence.Stop();
        }
        
        
        var tweenSettings = new TweenSettings(openingTime, openingEase, startDelay: openingStartDelay, endDelay: openingEndDelay);
        _animationSequence = Sequence.Create();

        _animationSequence = _animationSequence
            .ChainCallback(() => { Debug.Log("Opening"); })
            .ChainCallback(() => { openingEvent?.Invoke(); })
            .Chain(Tween.LocalPosition(leftWall, _openedPosL, tweenSettings))
            .Group(Tween.LocalPosition(rightWall, _openedPosR, tweenSettings))
            .ChainCallback(() => isSemiClosed = false)
            .ChainCallback(() => isClosed = false);


    }
    
    private void CloseWalls()
    {
        if (!leftWall || !rightWall)  return;
        
        if (isClosed)  return;

        if (_animationSequence.isAlive) 
        {
            _animationSequence.Stop();
        }
        
        _animationSequence = Sequence.Create();
        
        var semiClosedSettings = new TweenSettings(moveToSemiClosedTime, moveToSemiClosedEase, startDelay: moveToSemiClosedStartDelay, endDelay: moveToSemiClosedEndDelay);
        var closedSettings = new TweenSettings(moveToClosedTime, moveToClosedEase, startDelay: moveToClosedStartDelay, endDelay: moveToClosedEndDelay);
        var shakeSettings = new ShakeSettings(frequency: initialShakeFrequency, duration:initialShakeTime, strength: initialShakeStrength, startDelay: initialShakeStartDelay, endDelay: initialShakeEndDelay);

        _animationSequence = _animationSequence

            // Part 1: Initial shake
            .ChainCallback(() => { Debug.Log("Initial Shake"); })
            .ChainCallback(() => { initialShakeEvent?.Invoke(); })
            .Chain(Tween.ShakeLocalPosition(leftWall, shakeSettings))
            .Group(Tween.ShakeLocalPosition(rightWall, shakeSettings))

            // Part 2: Move to semi-closed position
            .ChainCallback(() => { Debug.Log("Moving to semi-closed"); })
            .ChainCallback(() => { moveToSemiClosedEvent?.Invoke(); })
            .Chain(Tween.LocalPosition(leftWall, startValue: _openedPosL, endValue: SemiClosedPosLeftV, semiClosedSettings))
            .Group(Tween.LocalPosition(rightWall, startValue: _openedPosR, endValue: SemiClosedPosRightV, semiClosedSettings))
            .ChainCallback(() => isSemiClosed = true)


            // Part 3: Move to fully closed position from semi-closed position
            .ChainCallback(() => { Debug.Log("Moving to closed"); })
            .ChainCallback(() => { moveToClosedEvent?.Invoke(); })
            .Chain(Tween.LocalPosition(leftWall, startValue: SemiClosedPosLeftV, endValue: ClosedPosLeftV, closedSettings))
            .Group(Tween.LocalPosition(rightWall, startValue: SemiClosedPosRightV, endValue: ClosedPosRightV, closedSettings))
            .ChainCallback(() => isClosed = true)
            .ChainCallback(() => { moveToClosedFinishedEvent?.Invoke(); });
    }
        

    
    
    
    private void OnDrawGizmosSelected()
    {
        // Check if we have any walls to visualize
        if (leftWall == null && rightWall == null)
            return;
        
        // Define gizmo settings directly here instead of in inspector
        float sphereRadius = 0.2f;
        float labelOffset = 0.3f;
        
        // Define colors here instead of in inspector
        Color leftWallColor = Color.red;
        Color rightWallColor = Color.blue;
        
        // Save the current transform values to calculate positions correctly in editor mode
        Vector3 tempOpenPosL = Application.isPlaying ? _openedPosL: leftWall.localPosition;
        Vector3 tempOpenPosR = Application.isPlaying ? _openedPosR: rightWall.localPosition;
        
        // --- Left Wall Visualization ---
        // Get current position (open position)
        Vector3 leftOpenPos = leftWall.position;
        
        // Calculate semi-closed position in world space using the OFFSET logic
        Vector3 leftSemiLocalPos = new Vector3(tempOpenPosL.x + semiClosedPosL, tempOpenPosL.y, tempOpenPosL.z);
        Vector3 leftSemiPos = leftWall.parent ? 
            leftWall.parent.TransformPoint(leftSemiLocalPos) : 
            new Vector3(tempOpenPosL.x + semiClosedPosL, leftWall.position.y, leftWall.position.z);
            
        // Calculate closed position in world space using the OFFSET logic
        Vector3 leftClosedLocalPos = new Vector3(tempOpenPosL.x + closedPosL, tempOpenPosL.y, tempOpenPosL.z);
        Vector3 leftClosedPos = leftWall.parent ? 
            leftWall.parent.TransformPoint(leftClosedLocalPos) : 
            new Vector3(tempOpenPosL.x + closedPosL, leftWall.position.y, leftWall.position.z);
        
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
        
        // Add position labels
        UnityEditor.Handles.color = leftWallColor;
        UnityEditor.Handles.Label(leftOpenPos + Vector3.up * labelOffset, "Open");
        
        UnityEditor.Handles.color = new Color(leftWallColor.r, leftWallColor.g, leftWallColor.b, 0.5f);
        UnityEditor.Handles.Label(leftSemiPos + Vector3.up * labelOffset, "Semi");
        
        UnityEditor.Handles.color = leftWallColor;
        UnityEditor.Handles.Label(leftClosedPos + Vector3.up * labelOffset, "Closed");
        
        // --- Right Wall Visualization ---
        // Get current position (open position)
        Vector3 rightOpenPos = rightWall.position;
        
        // Calculate semi-closed position in world space using the OFFSET logic
        Vector3 rightSemiLocalPos = new Vector3(tempOpenPosR.x + semiClosedPosR, tempOpenPosR.y, tempOpenPosR.z);
        Vector3 rightSemiPos = rightWall.parent ? 
            rightWall.parent.TransformPoint(rightSemiLocalPos) : 
            new Vector3(tempOpenPosR.x + semiClosedPosR, rightWall.position.y, rightWall.position.z);
            
        // Calculate closed position in world space using the OFFSET logic
        Vector3 rightClosedLocalPos = new Vector3(tempOpenPosR.x + closedPosR, tempOpenPosR.y, tempOpenPosR.z);
        Vector3 rightClosedPos = rightWall.parent ? 
            rightWall.parent.TransformPoint(rightClosedLocalPos) : 
            new Vector3(tempOpenPosR.x + closedPosR, rightWall.position.y, rightWall.position.z);
        
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
        
        // Add position labels
        UnityEditor.Handles.color = rightWallColor;
        UnityEditor.Handles.Label(rightOpenPos + Vector3.up * labelOffset, "Open");
        
        UnityEditor.Handles.color = new Color(rightWallColor.r, rightWallColor.g, rightWallColor.b, 0.5f);
        UnityEditor.Handles.Label(rightSemiPos + Vector3.up * labelOffset, "Semi");
        
        UnityEditor.Handles.color = rightWallColor;
        UnityEditor.Handles.Label(rightClosedPos + Vector3.up * labelOffset, "Closed");
    }
        
    
}