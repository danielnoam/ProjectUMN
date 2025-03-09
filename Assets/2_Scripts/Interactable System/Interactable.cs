using System.Collections;
using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(AudioSource))]
public class Interactable : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private InteractorType allowedInteractors = InteractorType.Both;
    [SerializeField] private bool allowMultipleInteractions = false;
    [SerializeField, Range(0f, 4f)] private float interactionTime = 0f;
    [SerializeField] private Transform interactPosition;
    
    [Header("Feedback")]
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private SOAudioEvent interactSfx;
    [SerializeField] private Outline outlineObject;
    [SerializeField] private Color playerOutlineColor = Color.cyan;
    [SerializeField] private Color robotOutlineColor = Color.magenta;
    [SerializeField] private float outlineWidth = 4f;

    
    [Header("Events")]
    public UnityEvent onInteractStartEvents;
    public UnityEvent onInteractEndEvents;
    

    public bool OnlyPlayerCanInteract => allowedInteractors == InteractorType.Player;
    public bool OnlyRobotCanInteract => allowedInteractors == InteractorType.Robot;
    public bool BothCanInteract => allowedInteractors == InteractorType.Both;
    public bool MarkedForInteraction => _markedForInteraction;

    private bool _interacted = false;
    private bool _isInteracting = false;
    private bool _markedForInteraction = false;
    private AudioSource _audioSource;
    
    


    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        interactPrompt?.SetActive(false);
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
            outlineObject.OutlineColor = playerOutlineColor;
            _markedForInteraction = true;
            interactPrompt?.SetActive(true);
        }
        // Otherwise use the normal check
        else if (CanMarkForInteraction(interactor))
        {
            outlineObject.OutlineColor = playerOutlineColor;
            _markedForInteraction = true;
            interactPrompt?.SetActive(true);
        }
    }
    
    public void MarkForRobotInteraction(Iinteractor interactor)
    {
        if (CanMarkForInteraction(interactor))
        {
            outlineObject.OutlineColor = robotOutlineColor;
            _markedForInteraction = true;
            interactPrompt?.SetActive(true);
        }
    }
    
    public void UnmarkForInteraction()
    {
        if (_markedForInteraction)
        {
            outlineObject.OutlineColor = Color.clear;
            _markedForInteraction = false;
            interactPrompt?.SetActive(false);
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
    
    public Transform GetInteractPosition()
    {
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
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.green;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        if (interactPosition)
        {
            Gizmos.DrawLine(transform.position, interactPosition.position);
            Gizmos.DrawWireSphere(interactPosition.position, 0.1f);
            UnityEditor.Handles.Label(interactPosition.position + new Vector3(0f, 0.1f, 0f), "Interact Position", style);
        }

        if (!interactPrompt)
        {
            Gizmos.DrawLine(transform.position, interactPrompt.transform.position);
            Gizmos.DrawWireSphere(interactPrompt.transform.position, 0.1f);
            UnityEditor.Handles.Label(interactPrompt.transform.position + new Vector3(0f, 0.1f, 0f), "Interact Prompt", style);
        }
    }
#endif
    
}
