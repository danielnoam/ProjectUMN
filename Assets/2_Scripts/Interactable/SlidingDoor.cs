using System;
using UnityEngine;
using VInspector;
using PrimeTween;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class SlidingDoor : MonoBehaviour
{

    [Header("Settings")] 
    [SerializeField] private bool usePowerSystem;
    [SerializeField ,ShowIf("usePowerSystem")] private bool powerTurnOn = true; 
    [SerializeField,ShowIf("usePowerSystem")] private bool stayOpenWhenPowered;
    [SerializeField ,Min(1),ShowIf("usePowerSystem")] private float powerSourcesNeeded = 1;
    [EndIf]
    
    [Header("Feedback")]
    [SerializeField] private SOAudioEvent sfxDoorMoving;
    [SerializeField] private SOAudioEvent sfxDoorFinishedMoving;
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
    
    [Header("Debug")]
    [SerializeField ,ReadOnly] private bool isClosed;
    [SerializeField, ReadOnly] private float powerSources;
    
    
    
    private Sequence _animationSequence;
    private readonly HashSet<Object> _powerSources = new HashSet<Object>();
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        ApplyStateImmediate(isClosed);
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

    #region Door State Control ------------------------------------------------------------------------------------------------

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
    
    private void SetState(bool closed, bool instant = false) 
    {
        if (!Application.isPlaying || instant)
        {
            ApplyStateImmediate(closed);
            return;
        }
        
        PlayDoorAnimation(closed);
    }
    
    private void ApplyStateImmediate(bool closed)
    {
        isClosed = closed;
        
        if (closed) 
        {
            leftAnchor.localPosition = closedPosL;
            rightAnchor.localPosition = closedPosR;
        }
        else 
        {
            leftAnchor.localPosition = openedPosL;
            rightAnchor.localPosition = openedPosR;
        }
    }
    
    
    private Sequence PlayDoorAnimation(bool closed)
    {
        if (usePowerSystem && _powerSources.Count > 0) Sequence.Create();
        
        if (isClosed == closed) 
        {
            return Sequence.Create();
        }
        
        isClosed = closed;
        
        if (_animationSequence.isAlive) 
        {
            _animationSequence.Stop();
        }
        
        var tweenSettings = new TweenSettings(animationTime, animationEase);

        _animationSequence = Sequence.Create()
            .ChainDelay(animationStartDelay)
            .ChainCallback(() => { sfxDoorMoving?.Play(_audioSource); })
            .Group(Tween.LocalPosition(rightAnchor, closed ? closedPosR : openedPosR, tweenSettings))
            .Group(Tween.LocalPosition(leftAnchor, closed ? closedPosL : openedPosL, tweenSettings))
            .ChainCallback(() => { if (closed) sfxDoorFinishedMoving?.Play(_audioSource); });
        
        return _animationSequence;
    }
    

    #endregion Door State Control ------------------------------------------------------------------------------------------------
    
    


    
    
    #region Power System ------------------------------------------------------------------------------------------------
    
    public void AddPowerSource(Object source)
    {
        _powerSources.Add(source);
    }
    
    public void RemovePowerSource(Object source)
    {
        _powerSources.Remove(source);
    }
    
    
     
    #endregion Power System ------------------------------------------------------------------------------------------------
    
    

    #region Editor ------------------------------------------------------------------------------------------------
    
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyStateImmediate(isClosed);
    }

    #endregion Editor ------------------------------------------------------------------------------------------------
}