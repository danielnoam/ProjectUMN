using System;
using UnityEngine;
using VInspector;
using PrimeTween;

public class ClosingWallsTest : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float animationTime = 10f;
    [SerializeField] private Ease animationEase = Ease.Linear;
    
    [Header("Shake Effect")]
    [SerializeField] private float shakeDuration = 2f;
    [SerializeField] private Vector3 shakeStrength = new Vector3(0.2f, 0, 0);
    [SerializeField] private int shakeFrequency = 10;
    
    [Foldout("Left Wall")]
    [SerializeField] private Transform leftWall;
    [SerializeField] private Vector3 closedPosL;
    [SerializeField, ReadOnly] private Vector3 openedPosL;
    [EndFoldout]
    
    [Foldout("Right Wall")]
    [SerializeField] private Transform rightWall;
    [SerializeField] private Vector3 closedPosR;
    [SerializeField, ReadOnly] private Vector3 openedPosR;
    [EndFoldout]
    
    [Header("Debug")]
    [SerializeField, ReadOnly] private bool isClosed;
    
    private Sequence _animationSequence;

    private void Awake()
    {
        // Store the initial positions as the opened positions
        if (leftWall) openedPosL = leftWall.localPosition;
        
        if (rightWall) openedPosR = rightWall.localPosition;
        
        // Apply the initial state
        ApplyStateImmediate(isClosed);
    }

    [Button]
    public void Open()
    {
        OpenWalls();
    }
    
    [Button]
    public void Close()
    {
        CloseWalls();
    }
    
    public void SetState(bool closed, bool instant = false)
    {
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
        isClosed = closed;
        
        if (closed) 
        {
            if (leftWall) leftWall.localPosition = closedPosL;
            
            if (rightWall) rightWall.localPosition = closedPosR;
        }
        else 
        {
            if (leftWall) leftWall.localPosition = openedPosL;
            
            if (rightWall) rightWall.localPosition = openedPosR;
        }
    }
    
    public Sequence OpenWalls()
    {
        if (!isClosed) 
        {
            return Sequence.Create();
        }
        
        isClosed = false;
        
        if (_animationSequence.isAlive) 
        {
            _animationSequence.Stop();
        }
        
        var tweenSettings = new TweenSettings(animationTime, animationEase);
        _animationSequence = Sequence.Create();
        
        // Add movement animations
        if (leftWall)
        {
            _animationSequence = _animationSequence.Chain(
                Tween.LocalPosition(leftWall, openedPosL, tweenSettings)
            );
        }
        
        if (rightWall)
        {
            _animationSequence = _animationSequence.Group(
                Tween.LocalPosition(rightWall, openedPosR, tweenSettings)
            );
        }
        
        return _animationSequence;
    }
    
    public Sequence CloseWalls()
    {
        if (isClosed) 
        {
            return Sequence.Create();
        }
        
        isClosed = true;
        
        if (_animationSequence.isAlive) 
        {
            _animationSequence.Stop();
        }
        
        var tweenSettings = new TweenSettings(animationTime, animationEase);
        _animationSequence = Sequence.Create();
        
        // Add shake effect for each wall
        if (leftWall)
        {
            _animationSequence = _animationSequence.Chain(
                Tween.ShakeLocalPosition(leftWall, strength: shakeStrength, duration: shakeDuration, frequency: shakeFrequency)
            );
        }
        
        if (rightWall)
        {
            _animationSequence = _animationSequence.Group(
                Tween.ShakeLocalPosition(rightWall, strength: shakeStrength, duration: shakeDuration, frequency: shakeFrequency)
            );
        }
        
        // Add movement animations after shake
        if (leftWall)
        {
            _animationSequence = _animationSequence.Chain(
                Tween.LocalPosition(leftWall, closedPosL, tweenSettings)
            );
        }
        
        if (rightWall)
        {
            _animationSequence = _animationSequence.Group(
                Tween.LocalPosition(rightWall, closedPosR, tweenSettings)
            );
        }
        
        return _animationSequence;
    }
    
}