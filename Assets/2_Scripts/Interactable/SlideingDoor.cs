using System;
using UnityEngine;
using VInspector;
using PrimeTween;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[SelectionBase]
public class SlideingDoor : MonoBehaviour
{

    [Header("Settings")] 
    [SerializeField] private bool usePowerSystem;
    [SerializeField ,ShowIf("usePowerSystem")] private bool powerTurnOn = true; 
    [SerializeField,ShowIf("usePowerSystem")] private bool stayOpenWhenPowered;
    [SerializeField ,Min(1),ShowIf("usePowerSystem")] private float powerSourcesNeeded = 1;
    [EndIf]
    
    [Header("Animation")]
    [SerializeField] private float animationTime = 0.4f;
    [SerializeField] private float animationStartDelay = 0.4f;
    [SerializeField] private Ease animationEase = Ease.OutBack;
    
    [Foldout("Left Door")]
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Vector3 openedPosL, closedPosL;
    [EndFoldout]
    
    [Foldout("Right Door")]
    [SerializeField] private Transform rightAnchor;
    [SerializeField] private Vector3 openedPosR, closedPosR;
    [EndFoldout]
    
    [Space(10)]
    [SerializeField ,ReadOnly] private bool isClosed;
    [SerializeField, ReadOnly] private float powerSources;
    private Sequence _animationSequence;
    private readonly HashSet<Object> _powerSources = new HashSet<Object>();

    private void Awake()
    {
        SetState(isClosed, true);
    }

    private void Update()
    {
        if (!usePowerSystem) return;
        
        
        
        if (powerTurnOn)
        {

            if (_powerSources.Count >= powerSourcesNeeded && isClosed)
            {
                SetState(false);
            }
            else if (_powerSources.Count < powerSourcesNeeded && !isClosed && !stayOpenWhenPowered)
            {
                SetState(true);
            }
            
                
        }
        else
        {
            if (_powerSources.Count >= powerSourcesNeeded && !isClosed && !stayOpenWhenPowered)
            {
                SetState(true);
            }
            else if (_powerSources.Count < powerSourcesNeeded && isClosed)
            {
                SetState(false);
            }
            
        }
        
        powerSources = _powerSources.Count;
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
    
    
    private Sequence SetState(bool _isClosed, bool instant = false) {
        
        if (usePowerSystem && _powerSources.Count > 0) Sequence.Create();
        
        
        
        if (!Application.isPlaying || instant)
        {
            isClosed = _isClosed;
            
            if (_isClosed) {
                leftAnchor.localPosition = closedPosL;
                rightAnchor.localPosition = closedPosR;
            }
            else {
                leftAnchor.localPosition = openedPosL;
                rightAnchor.localPosition = openedPosR;
            }
            
            return  Sequence.Create();
        }
        
        
        if (isClosed == _isClosed) {
            return Sequence.Create();
        }
        isClosed = _isClosed;
        if (_animationSequence.isAlive) {
            _animationSequence.Stop();
        }
        
        var tweenSettings = new TweenSettings(animationTime, animationEase, startDelay: animationStartDelay);
        _animationSequence =
            Tween.LocalPosition(leftAnchor, _isClosed ? closedPosL : openedPosL, tweenSettings)
                .Group(Tween.LocalPosition(rightAnchor, _isClosed ? closedPosR : openedPosR, tweenSettings));
        
        
        return _animationSequence;
    }

    
    public void AddPowerSource(Object source)
    {
        _powerSources.Add(source);
    }
    
    public void RemovePowerSource(Object source)
    {
        _powerSources.Remove(source);
    }

    
    
    #region Editor

    
    private void OnValidate()
    {
        if (!Application.isPlaying)
            SetState(isClosed, true);
    }

    #endregion

}
