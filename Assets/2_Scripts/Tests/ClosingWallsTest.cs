using System;
using UnityEngine;
using VInspector;
using PrimeTween;

public class ClosingWallsTest : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float animationTime = 10f;
    [SerializeField] private float animationStartDelay = 0f;
    [SerializeField] private Ease animationEase = Ease.Linear;
    
    [Header("Shake Effect")]
    [SerializeField] private float shakeDuration = 2f;
    [SerializeField] private Vector3 shakeStrength = new Vector3(0.2f, 0, 0);
    [SerializeField] private int shakeFrequency = 10;
    [SerializeField] private bool shakeWhileMoving = true;
    
    [Foldout("Left Wall")]
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform leftWallShake;
    [SerializeField] private Vector3 closedPosL;
    [SerializeField, ReadOnly] private Vector3 openedPosL;
    [EndFoldout]
    
    [Foldout("Right Wall")]
    [SerializeField] private Transform rightWall;
    [SerializeField] private Transform rightWallShake;
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
    public void ToggleState()
    {
        SetState(!isClosed);
    }
    
    [Button]
    public void Open()
    {
        SetState(false);
    }
    
    [Button]
    public void Close()
    {
        SetState(true);
    }
    
    public void SetState(bool closed, bool instant = false)
    {
        if (!Application.isPlaying || instant)
        {
            ApplyStateImmediate(closed);
            return;
        }
        
        PlayWallAnimation(closed);
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
    
    
    public Sequence PlayWallAnimation(bool closed)
    {
        if (isClosed == closed) 
        {
            return Sequence.Create();
        }
        
        isClosed = closed;
        
        if (_animationSequence.isAlive) 
        {
            _animationSequence.Stop();
        }
        
        var tweenSettings = new TweenSettings(animationTime, animationEase, startDelay: animationStartDelay);
        _animationSequence = Sequence.Create();
        
        // When closing, add shake effect before movement
        if (closed)
        {
            // Add shake effect for each wall
            if (leftWall)
            {
                _animationSequence = _animationSequence.Chain(
                    Tween.ShakeLocalPosition(leftWallShake, strength: shakeStrength, duration: shakeDuration, frequency: shakeFrequency)
                );
            }
            
            if (rightWall)
            {
                _animationSequence = _animationSequence.Group(
                    Tween.ShakeLocalPosition(rightWallShake, strength: shakeStrength, duration: shakeDuration, frequency: shakeFrequency)
                );
            }
        }
        
        // Add movement animations with additional shake effect during movement
        if (leftWall)
        {
            if (shakeWhileMoving)
            {
                // Group the movement and shake together
                _animationSequence = _animationSequence.Chain(
                    Tween.LocalPosition(leftWall, closed ? closedPosL : openedPosL, tweenSettings)
                    .Group(Tween.ShakeLocalPosition(leftWallShake, strength: shakeStrength * 0.5f, duration: tweenSettings.duration * 0.9f, frequency: shakeFrequency * 1f))
                );
            }
            else
            {
                // Just movement without shake
                _animationSequence = _animationSequence.Chain(
                    Tween.LocalPosition(leftWall, closed ? closedPosL : openedPosL, tweenSettings)
                );
            }
        }
        
        if (rightWall)
        {
            if (shakeWhileMoving)
            {
                // Group the movement and shake together
                _animationSequence = _animationSequence.Group(
                    Tween.LocalPosition(rightWall, closed ? closedPosR : openedPosR, tweenSettings)
                    .Group(Tween.ShakeLocalPosition(rightWallShake, strength: shakeStrength * 0.5f, duration: tweenSettings.duration * 0.9f, frequency: shakeFrequency * 1f))
                );
            }
            else
            {
                // Just movement without shake
                _animationSequence = _animationSequence.Group(
                    Tween.LocalPosition(rightWall, closed ? closedPosR : openedPosR, tweenSettings)
                );
            }
        }
        
        return _animationSequence;
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        
        // Store the initial positions if we're in the editor
        if (leftWall) openedPosL = leftWall.localPosition;
        if (rightWall) openedPosR = rightWall.localPosition;
        ApplyStateImmediate(isClosed);
    }
}