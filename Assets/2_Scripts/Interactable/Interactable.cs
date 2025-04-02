using System.Collections;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VInspector;

public enum CommandToSend
{
    Nothing,
    Follow,
    Sit,
    Idle,
}

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
    [SerializeField] private Image interactPromptBackground;
    [SerializeField] private TextMeshProUGUI interactPromptText;
    [SerializeField] private SOAudioEvent interactSfx;
    [SerializeField] private Outline outlineObject;
    [SerializeField] private Color playerOutlineColor = Color.cyan;
    [SerializeField] private Color robotOutlineColor = Color.magenta;
    [SerializeField] private float outlineWidth = 4f;
    
    [Header("Prompt Animation")]
    [SerializeField] private float promptFadeDuration = 0.3f;
    [SerializeField] private Ease promptFadeEase = Ease.OutSine;
    [SerializeField] private float outlineFadeDuration = 0.3f;
    [SerializeField] private Ease outlineFadeEase = Ease.OutSine;
    
    [Header("Events")]
    public UnityEvent onInteractStartEvents;
    public UnityEvent onInteractEndEvents;

    public bool OnlyPlayerCanInteract => allowedInteractors == InteractorType.Player;
    public bool OnlyRobotCanInteract => allowedInteractors == InteractorType.Robot;
    public bool BothCanInteract => allowedInteractors == InteractorType.Both;
    public bool MarkedForInteraction => _markedForInteraction;
    public CommandToSend Command => commandToSend;
    private bool _interacted = false;
    private bool _isInteracting = false;
    private bool _markedForInteraction = false;
    private AudioSource _audioSource;
    private Sequence _promptSequence;
    private Sequence _outlineSequence;
    private float _defaultBackgroundAlpha;
    private float _defaultTextAlpha;
    

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        if (interactPromptBackground)
        {
            _defaultBackgroundAlpha = interactPromptBackground.color.a;
            SetAlpha(interactPromptBackground, 0f);
        }
        
        if (interactPromptText)
        {
            _defaultTextAlpha = interactPromptText.color.a;
            SetAlpha(interactPromptText, 0f);
        }
        
        if (outlineObject)
        {
            outlineObject.OutlineColor = Color.clear;
            outlineObject.OutlineWidth = outlineWidth;
        }
        

        _promptSequence = Sequence.Create();
        _outlineSequence = Sequence.Create();
    } 

    public void MarkForPlayerInteraction(Iinteractor interactor)
    {
        if (_markedForInteraction && !OnlyRobotCanInteract)
        {
            ChangeOutlineColor(playerOutlineColor);
            _markedForInteraction = true;
            interactPromptText.text = $"E";
            FadePrompt(true);
        }
        // Otherwise use the normal check
        else if (CanMarkForInteraction(interactor))
        {
            ChangeOutlineColor(playerOutlineColor);
            _markedForInteraction = true;
            interactPromptText.text = $"E"; // <sprite name=E>
            FadePrompt(true); 
        }
    }
    
    public void MarkForRobotInteraction(Iinteractor interactor)
    {
        if (CanMarkForInteraction(interactor))
        {
            ChangeOutlineColor(robotOutlineColor);
            _markedForInteraction = true;
            interactPromptText.text = $"R";
            FadePrompt(true); 
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
            FadePrompt(false);
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
            interactSfx?.Play(_audioSource);
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
    
   
    private void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
    
    
    
    
    #region Effect --------------------------------------------------------------------------------------------------------

    private void FadePrompt(bool fadeIn)
    {
        _promptSequence.Stop();
        _promptSequence = Sequence.Create();
        
     
        if (interactPromptBackground)
        {
            float targetAlpha = fadeIn ? _defaultBackgroundAlpha : 0f;
            
            _promptSequence.Group(
                Tween.Alpha(
                    interactPromptBackground,
                    startValue: interactPromptBackground.color.a,
                    endValue: targetAlpha,
                    duration: promptFadeDuration,
                    ease: promptFadeEase
                )
            );
        }
        
   
        if (interactPromptText)
        {
            float targetAlpha = fadeIn ? _defaultTextAlpha : 0f;
            
            _promptSequence.Group(
                Tween.Alpha(
                    interactPromptText,
                    startValue: interactPromptText.color.a,
                    endValue: targetAlpha,
                    duration: promptFadeDuration,
                    ease: promptFadeEase
                )
            );
        }
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

    #endregion Effect --------------------------------------------------------------------------------------------------------



    #region Editor --------------------------------------------------------------------------------------------------------

#if UNITY_EDITOR
    
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
        
        if (!interactPromptBackground)
        {
            Gizmos.DrawLine(transform.position, interactPromptBackground.transform.position);
            Gizmos.DrawWireSphere(interactPromptBackground.transform.position, 0.1f);
            UnityEditor.Handles.Label(interactPromptBackground.transform.position + new Vector3(0f, 0.1f, 0f), "Interact Prompt", style);
        }
    }
#endif

    #endregion Editor --------------------------------------------------------------------------------------------------------
}