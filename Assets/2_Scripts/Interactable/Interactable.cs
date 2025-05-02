using System;
using System.Collections;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VInspector;



[RequireComponent(typeof(AudioSource))]
public class Interactable : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private InteractorType allowedInteractors = InteractorType.Both;
    [SerializeField] private bool allowMultipleInteractions = false;
    [SerializeField, Range(0f, 4f)] private float interactionTime = 0.3f;
    [SerializeField] private Transform interactPosition;
    
    [DisableIf("OnlyPlayerCanInteract")]
    [Header("Robot")]
    [SerializeField] private Transform robotInteractPosition;
    [SerializeField] private CommandToSend commandToSend = CommandToSend.Follow;
    [EndIf]
    
    [Header("Feedback")]
    [SerializeField] private InteractPrompt interactPrompt;
    [SerializeField] private Outline outlineObject;
    [SerializeField] private Color playerOutlineColor = Color.cyan;
    [SerializeField] private Color robotOutlineColor = Color.magenta;
    [SerializeField] private float outlineWidth = 4f;
    [SerializeField] private float outlineFadeDuration = 0.3f;
    [SerializeField] private Ease outlineFadeEase = Ease.OutSine;
    
    [Header("Events")]
    public UnityEvent onInteractStartEvents;
    public UnityEvent onInteractEndEvents;

    public bool OnlyPlayerCanInteract => allowedInteractors == InteractorType.Player;
    public bool OnlyRobotCanInteract => allowedInteractors == InteractorType.Robot;
    public bool BothCanInteract => allowedInteractors == InteractorType.Both;
    public bool MarkedForInteraction => _markedForInteraction;
    public Transform RobotInteractPosition => robotInteractPosition;
    public CommandToSend Command => commandToSend;
    private bool _interacted = false;
    private bool _isInteracting = false;
    private bool _markedForInteraction = false;
    private AudioSource _audioSource;
    private Sequence _outlineSequence;
    private float _defaultBackgroundAlpha;
    private float _defaultTextAlpha;
    

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        if (outlineObject)
        {
            outlineObject.OutlineColor = Color.clear;
            outlineObject.OutlineWidth = outlineWidth;
        }
    }
    

    public void MarkForPlayerInteraction(Iinteractor interactor)
    {
        if (_markedForInteraction && !OnlyRobotCanInteract)
        {
            ChangeOutlineColor(playerOutlineColor);
            _markedForInteraction = true;
            interactPrompt.UpdateInteractPrompt(true);
            interactPrompt.FadePrompt(true);
        }
        // Otherwise use the normal check
        else if (CanMarkForInteraction(interactor))
        {
            ChangeOutlineColor(playerOutlineColor);
            _markedForInteraction = true;
            interactPrompt.UpdateInteractPrompt(true);
            interactPrompt.FadePrompt(true); 
        }
    }
    
    public void MarkForRobotInteraction(Iinteractor interactor)
    {
        if (CanMarkForInteraction(interactor))
        {
            ChangeOutlineColor(robotOutlineColor);
            _markedForInteraction = true;
            interactPrompt.UpdateInteractPrompt(false);
            interactPrompt.FadePrompt(true); 
        }
    }
    
    public void UnmarkForInteraction()
    {
        if (_markedForInteraction)
        {
            // Fade out outline color
            Color transparentOutlineColor = outlineObject.OutlineColor;
            transparentOutlineColor.a = 0f;
            ChangeOutlineColor(transparentOutlineColor);
            interactPrompt.FadePrompt(false);
            _markedForInteraction = false;
        }
    }

    public void Interact(Iinteractor interactor)
    {
        if (CanInteract(interactor))
        {
            UnmarkForInteraction();
            _interacted = true;
            _isInteracting = true;
            onInteractStartEvents?.Invoke();
            interactor?.OnInteractionStart(this);
        
        
            if (interactionTime > 0f)
            {
                StartCoroutine(DelayedEndInteraction(interactor, interactionTime));
            }
            else
            {
                EndInteraction(interactor); 
            }
        }
    }
    
    public void EndInteraction(Iinteractor interactor)
    {
        _isInteracting = false;
        onInteractEndEvents?.Invoke();
        interactor?.OnInteractionEnd(this);
    }
    
    public void CancelInteraction()
    {
        if (!_isInteracting) return;
        
        _isInteracting = false;
        _interacted = false;
        _markedForInteraction = false;
    }
    
    public Transform GetInteractPosition(Iinteractor interactor)
    {
        if (interactor.InteractorType == InteractorType.Robot && robotInteractPosition)
        {
            return robotInteractPosition;
        }
        
        if (interactor.InteractorType == InteractorType.Player && interactPosition)
        {
            return interactPosition;
        }
        
        return interactPosition ? interactPosition : transform;
    }

    
    private IEnumerator DelayedEndInteraction(Iinteractor interactor, float delay)
    {
        yield return new WaitForSeconds(delay);
        EndInteraction(interactor);
    }
    
    private bool CanInteract(Iinteractor interactor)
    {
        bool interactorAllowed;
        if (allowedInteractors == InteractorType.Both)
        {
            interactorAllowed = true;
        }
        else
        {
            interactorAllowed = (interactor.InteractorType == InteractorType.Player && OnlyPlayerCanInteract) || (interactor.InteractorType == InteractorType.Robot && OnlyRobotCanInteract);
        }
            
        return !_isInteracting && (!_interacted || allowMultipleInteractions) && interactorAllowed;
    }

    private bool CanMarkForInteraction(Iinteractor interactor)
    {
        bool interactorAllowed;
        if (allowedInteractors == InteractorType.Both)
        {
            interactorAllowed = true;
        }
        else
        {
            interactorAllowed = (interactor.InteractorType == InteractorType.Player && OnlyPlayerCanInteract) || (interactor.InteractorType == InteractorType.Robot && OnlyRobotCanInteract);
        }
        
        return !_isInteracting && (!_interacted || allowMultipleInteractions) && !_markedForInteraction && interactorAllowed;
    }



    public void ResetInteracted()
    {
        _interacted = false;
        UnmarkForInteraction();
        _isInteracting = false;
    }

    
    
    
    private void ChangeOutlineColor(Color targetColor)
    {
        if (!outlineObject) return;
        
        _outlineSequence.Stop();
        _outlineSequence = Sequence.Create();
        
        _outlineSequence.Group(
            Tween.Custom(
                outlineObject.OutlineColor,
                targetColor,
                outlineFadeDuration,
                onValueChange: newColor => outlineObject.OutlineColor = newColor,
                ease: outlineFadeEase
            )
        );
    }
    
    

#if UNITY_EDITOR
    
    #region Editor --------------------------------------------------------------------------------------------------------
    private void OnValidate()
    {
        if (outlineObject)
        {
            outlineObject.OutlineWidth = outlineWidth;
            outlineObject.OutlineMode = Outline.Mode.OutlineVisible;
        }
        
        if (GetComponent<AudioSource>() == null)
        {
            gameObject.AddComponent<AudioSource>();
        }

        if (OnlyPlayerCanInteract)
        {
            commandToSend = CommandToSend.Nothing;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.green;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        if (interactPosition && !OnlyRobotCanInteract)
        {
            Gizmos.DrawLine(transform.position, interactPosition.position);
            Gizmos.DrawWireSphere(interactPosition.position, 0.1f);
            UnityEditor.Handles.Label(interactPosition.position + new Vector3(0f, 0.1f, 0f), "Interact Position", style);
        }
        
        if (robotInteractPosition && !OnlyPlayerCanInteract)
        {
            Gizmos.DrawLine(transform.position, robotInteractPosition.position);
            Gizmos.DrawWireSphere(robotInteractPosition.position, 0.1f);
            UnityEditor.Handles.Label(robotInteractPosition.position + new Vector3(0f, 0.1f, 0f), "Robot Interact Position", style);
        }
        
        if (interactPrompt)
        {
            Gizmos.DrawLine(transform.position, interactPrompt.transform.position);
            Gizmos.DrawWireSphere(interactPrompt.transform.position, 0.1f);
            UnityEditor.Handles.Label(interactPrompt.transform.position + new Vector3(0f, 0.1f, 0f), "Interact Prompt", style);
        }
    }
    
    #endregion Editor --------------------------------------------------------------------------------------------------------
#endif

  
}