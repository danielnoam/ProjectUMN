using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine.InputSystem;
using VInspector;

public class MenuPage : MonoBehaviour
{
    
    [Header("Selectable Settings")] 
    [SerializeField] private List<Selectable> selectables = new List<Selectable>();
    
    [Foldout("Audio")]
    [SerializeField] private SOAudioEvent sfxSelectableSelected;
    [SerializeField] private SOAudioEvent sfxButtonClick;
    [EndFoldout]
    
    [Foldout("Page Animations")]
    [Header("Selected")]
    [SerializeField] private Vector3 moveInFromDirection = new Vector3(0, -700f, 0);
    [SerializeField] private float moveInDuration = 0.3f;
    [SerializeField] private float moveInDelay = 0.2f;
    [SerializeField] private Ease moveInEase = Ease.OutSine;
    [SerializeField] private List<GameObject> moveInObjects = new List<GameObject>();
    
    [Header("DeSelected")]
    [SerializeField] private Vector3 moveOutToDirection = new Vector3(0, 700f, 0);
    [SerializeField] private float moveOutDuration = 0.3f;
    [SerializeField] private float moveOutDelay = 0.2f;
    [SerializeField] private Ease moveOutEase = Ease.OutSine;
    [SerializeField] private List<GameObject> moveOutObjects = new List<GameObject>();
    [EndFoldout]
    
    [Foldout("Selectables Animations")]
    [Header("Scale")]
    [SerializeField] private bool scaleOnSelect = false;
    [ShowIf("scaleOnSelect")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float scaleDuration = 0.15f;
    [SerializeField] private Ease scaleEase = Ease.InOutBounce;
    [SerializeField] private List<Selectable> scaleExclusions = new List<Selectable>();
    [EndIf]
    [Header("Rotate")]
    [SerializeField] private bool rotateOnSelect = false;
    [ShowIf("rotateOnSelect")]
    [SerializeField] private Vector3 rotationStrength = new Vector3(0, 0, 15);
    [SerializeField] private float rotationDuration = 0.15f;
    [SerializeField] private Ease rotationEase = Ease.InOutBounce;
    [SerializeField] private List<Selectable> rotateExclusions = new List<Selectable>();
    [EndIf]
    [Header("Shake")]
    [SerializeField] private bool shakeOnSelect = false;
    [ShowIf("shakeOnSelect")]
    [SerializeField] private bool shakeOnDeselect = false;
    [SerializeField] private Vector3 shakeAxis = new Vector3(3, 3, 0);
    [SerializeField] private float shakeFrequency = 10f;
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private Ease shakeEase = Ease.Default;
    [SerializeField] private List<Selectable> shakeExclusions = new List<Selectable>();
    [EndIf]
    [EndFoldout]
    
    [Space(10)]
    [CustomAttribute.ReadOnly] public Selectable currentSelectable;
    [CustomAttribute.ReadOnly] public Selectable previousSelectable;
    [CustomAttribute.ReadOnly] public bool canSelect;
    
    private MenuController _menuController;
    private bool _pageIsActive = true;
    public bool PageIsActive => _pageIsActive;
    private readonly Dictionary<GameObject, Vector3> _moveInObjectsOriginalPositions = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<Selectable, Vector3> _selectableOriginalScales = new Dictionary<Selectable, Vector3>();
    private readonly Dictionary<Selectable, Vector3> _selectableOriginalRotations = new Dictionary<Selectable, Vector3>();
    private readonly Dictionary<Selectable, Vector3> _selectableOriginalPositions = new Dictionary<Selectable, Vector3>();
    private readonly Dictionary<Selectable, bool> _selectableOriginalState = new Dictionary<Selectable, bool>();
    private readonly List<LayoutGroup> _layoutGroups = new List<LayoutGroup>();
    
    private void Awake()
    {
        _menuController = GetComponentInParent<MenuController>();

        if (!_menuController)
        {
            Debug.LogError("No menu controller found!");
            return;
        }
        
        // Setup selectables
        if (selectables.Count > 0)
        {
            foreach (Selectable selectable in selectables)
            {
                SetupSelectable(selectable);
            }
        }

        // Save positions for animation
        if (moveInObjects.Count > 0)
        {
            foreach (GameObject obj in moveInObjects)
            {
                _moveInObjectsOriginalPositions[obj] = obj.transform.localPosition;
            }
        }
        
        // Save layout groups for animation
        AddLayoutGroupsRecursively(transform);
        OnPageDeselected(false);
    }
    
    

    public void OnPageSelected(bool playAnimation)
    {

        PlayMoveInAnimation(playAnimation);
    }
    
    public void OnPageDeselected(bool playAnimation)
    {
        PlayMoveOutAnimation(playAnimation);
        
    }

    public void OnNavigate(InputAction.CallbackContext context) // Input event
    {
        if (!canSelect) return;

        if (EventSystem.current.currentSelectedGameObject) return;
        SelectFirstAvailableSelectable();
    }
    
    

#region Selectables Events // ---------------------------------------------------------------------

    private void OnSelect(BaseEventData eventData)
    {
        if (!canSelect) return;

        if (!eventData.selectedObject.activeSelf) return;

        currentSelectable = eventData.selectedObject.GetComponent<Selectable>();
        if (currentSelectable == null) return;

        if (scaleOnSelect && !scaleExclusions.Contains(currentSelectable))
        {
            PlayScaleAnimation(eventData.selectedObject.transform, true);
        }

        if (rotateOnSelect && !rotateExclusions.Contains(currentSelectable))
        {
            PlayRotateAnimation(eventData.selectedObject.transform, true);
        }

        if (shakeOnSelect && !shakeExclusions.Contains(currentSelectable))
        {
            PlayShakeAnimation(eventData.selectedObject.transform);
        }

        sfxSelectableSelected?.Play(_menuController.AudioSource);
    }

    private void OnDeselect(BaseEventData eventData)
    {
        if (!canSelect) return;

        if (!eventData.selectedObject.activeSelf || currentSelectable == null) return;

        if (scaleOnSelect && !scaleExclusions.Contains(currentSelectable))
        {
            PlayScaleAnimation(eventData.selectedObject.transform, false);
        }

        if (rotateOnSelect && !rotateExclusions.Contains(currentSelectable))
        {
            PlayRotateAnimation(eventData.selectedObject.transform, false);
        }

        if (shakeOnSelect && shakeOnDeselect && !shakeExclusions.Contains(currentSelectable))
        {
            PlayShakeAnimation(eventData.selectedObject.transform);
        }

        previousSelectable = currentSelectable;
        currentSelectable = null;
    }

    private void OnPointerEnter(BaseEventData eventData)
    {
        if (!canSelect) return;

        if (eventData is PointerEventData pointerEventData)
        {
            pointerEventData.selectedObject = pointerEventData.pointerEnter;
        }
    }

    private void OnPointerExit(BaseEventData eventData)
    {
        if (!canSelect) return;

        if (eventData is PointerEventData pointerEventData)
        {
            pointerEventData.selectedObject = null;
        }
    }
    

#endregion Selectables Events // ---------------------------------------------------------------------

#region Page Management // ---------------------------------------------------------------------
    
private void SetupSelectable(Selectable selectable)
    {
        // Store all original information
        _selectableOriginalScales[selectable] = selectable.transform.localScale;
        _selectableOriginalRotations[selectable] = selectable.transform.localRotation.eulerAngles;
        _selectableOriginalPositions[selectable] = selectable.transform.localPosition;
        _selectableOriginalState[selectable] = selectable.interactable;
        
        // Add events
        var eventTrigger = selectable.GetComponent<EventTrigger>() ?? selectable.gameObject.AddComponent<EventTrigger>();
        AddEventTriggerEntry(eventTrigger, EventTriggerType.Select, OnSelect);
        AddEventTriggerEntry(eventTrigger, EventTriggerType.Deselect, OnDeselect);
        AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerEnter, OnPointerEnter);
        AddEventTriggerEntry(eventTrigger, EventTriggerType.PointerExit, OnPointerExit);
        
        // Add click sfx for button selectables
        if (selectable is Button button)
        {
            button.onClick.AddListener(() => sfxButtonClick?.Play(_menuController.AudioSource));
        }
    }
    
    private void AddEventTriggerEntry(EventTrigger eventTrigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = type;
        entry.callback = new EventTrigger.TriggerEvent();
        entry.callback.AddListener(callback);
        eventTrigger.triggers.Add(entry);
    }

    [Button] private void ResetAllSelectables() 
    {
        if (selectables.Count == 0) return;
        
        foreach (Selectable selectable in selectables)
        {
            if (!selectable.gameObject.activeSelf) continue; // Reset disabled selectables as well
            
            selectable.transform.localScale = _selectableOriginalScales[selectable];
            selectable.transform.localRotation = Quaternion.Euler(_selectableOriginalRotations[selectable]);
            selectable.transform.localPosition = _selectableOriginalPositions[selectable];
            selectable.interactable = _selectableOriginalState[selectable];
        }
        
        // Just resetting the position bugs the selectables that are in a layout group
        // so find the layout group if it exists and restart it to fix the positions
        ResetLayoutGroups();
    }
    
    private void DisableAllSelectables()
    {
        foreach (Selectable selectable in selectables)
        {
            selectable.interactable = false;
        }
    }
    
    private void EnableAllSelectables()
    {
        foreach (Selectable selectable in selectables)
        {
            selectable.interactable = _selectableOriginalState[selectable];
        }
    }

    private IEnumerator SelectFirstAvailableSelectableCar(float delay = 0)
    {
        yield return new WaitForSeconds(0.03f + delay); // small delay so the selectables will have time to initialize
        SelectFirstAvailableSelectable();
    }
    
    private void SelectFirstAvailableSelectable()
    {
        if (selectables.Count == 0) return;
        
        // Try to select previous selectable if it's valid
        if (previousSelectable && previousSelectable.isActiveAndEnabled)
        {
            EventSystem.current.SetSelectedGameObject(previousSelectable.gameObject);
            return;
        }
    
        // Look for first active selectable starting from index 0
        for (var i = 0; i < selectables.Count; i++)
        {
            if (!selectables[i].gameObject.activeSelf || !selectables[i].interactable) continue; // Skip disabled or non interactable
            EventSystem.current.SetSelectedGameObject(selectables[i].gameObject);
            return;
        }
    }
    
    private void ResetLayoutGroups()
    {
        if (_layoutGroups.Count == 0) return;
        
        foreach (LayoutGroup group in _layoutGroups)
        {
            group.enabled = false;
            group.enabled = true;
        }
    }

    private void DisableAllLayoutGroups()
    {
        if (_layoutGroups.Count == 0) return;
        
        foreach (LayoutGroup group in _layoutGroups)
        {
            group.enabled = false;
        }
    }
    
    private void AddLayoutGroupsRecursively(Transform parent)
    {
        // Check if the current object has a LayoutGroup component
        if (parent.TryGetComponent(out LayoutGroup group) && !_layoutGroups.Contains(group))
        {
            _layoutGroups.Add(group);
        }

        // Recursively check all children
        foreach (Transform child in parent)
        {
            AddLayoutGroupsRecursively(child);
        }
    }

#endregion Page Management // ---------------------------------------------------------------------

#region Animations // ---------------------------------------------------------------------
    
    private void PlayScaleAnimation(Transform target, bool scaleUp)
    {
        Vector3 endScale = scaleUp ? _selectableOriginalScales[currentSelectable] * scaleMultiplier : _selectableOriginalScales[currentSelectable];
        Tween.Scale(target, endScale, scaleDuration, scaleEase);
    }

    private void PlayRotateAnimation(Transform target, bool rotateToTarget)
    {
        Vector3 endRotation = rotateToTarget ? rotationStrength : _selectableOriginalRotations[currentSelectable];
        Tween.LocalRotation(target, endRotation, rotationDuration, rotationEase);
    }

    private void PlayShakeAnimation(Transform target)
    {
        Tween.ShakeLocalPosition(target, shakeAxis, shakeDuration, shakeFrequency, easeBetweenShakes: shakeEase);
    }
    
    [Button] private void PlayMoveInAnimation(bool playAnimation = true)
    { 
        currentSelectable = null;
        previousSelectable = null;
        EventSystem.current.SetSelectedGameObject(null);
        canSelect = false;
        EnableAllSelectables();
        
        if (playAnimation)
        {
            // Animate the movement
            for (int i = 0; i < moveInObjects.Count; i++)
            {
                GameObject currentObject = moveInObjects[i];
                currentObject.transform.localPosition = moveInFromDirection;
                Tween.LocalPosition(currentObject.transform, startValue: moveInFromDirection, endValue: _moveInObjectsOriginalPositions[currentObject], moveInDuration, ease: moveInEase, startDelay: i * moveInDelay, useUnscaledTime: true);
            }
            
            float totalAnimationTime = moveInDuration + (moveInDelay * (moveInObjects.Count - 1));
            StartCoroutine(SetCanSelect(true, totalAnimationTime));
        }
        else
        {
            // Set position instantly
            for (int i = 0; i < moveInObjects.Count; i++)
            {
                GameObject currentObject = moveInObjects[i];
                currentObject.transform.localPosition = _moveInObjectsOriginalPositions[currentObject];
            }
            
            canSelect = true;
            ResetAllSelectables();
        }
    }
    
    private IEnumerator SetCanSelect(bool value, float delay)
    {
        yield return new WaitForSeconds(delay);
        canSelect = value;
    }
    
    [Button] private void PlayMoveOutAnimation(bool playAnimation)
    {
        ResetAllSelectables();
        DisableAllSelectables();
        EventSystem.current.SetSelectedGameObject(null);
        previousSelectable = null;
        currentSelectable = null;
        canSelect = false;
        
        if (playAnimation)
        {
            // Animate the movement
            for (int i = 0; i < moveOutObjects.Count; i++)
            {
                GameObject currentObject = moveOutObjects[i];
                Tween.LocalPosition(currentObject.transform, startValue: _moveInObjectsOriginalPositions[currentObject], endValue: moveOutToDirection, moveOutDuration, ease: moveOutEase, startDelay: i * moveOutDelay, useUnscaledTime: true);
            }
        }
        else
        {
            // Set position instantly
            for (int i = 0; i < moveOutObjects.Count; i++)
            {
                GameObject currentObject = moveOutObjects[i];
                currentObject.transform.localPosition = moveOutToDirection;
            }
        }
    }
    
#endregion Animations // ---------------------------------------------------------------------



#if UNITY_EDITOR
    
    
[Button] private void AddAllSelectablesInPage()
{
    // Find all the selectables in the page
    // Start the recursive search from this transform
    AddSelectablesRecursively(transform);
        
    // Clean up any null references
    selectables.RemoveAll(item => item == null);
}

private void AddSelectablesRecursively(Transform parent)
{
    // Check if the current object has a Selectable component
    if (parent.TryGetComponent(out Selectable selectable) && !selectables.Contains(selectable))
    {
        selectables.Add(selectable);
    }

    // Recursively check all children
    foreach (Transform child in parent)
    {
        AddSelectablesRecursively(child);
    }
}

[Button] private void FindAndDisableAllLayoutGroups()
{
    // Find all layout groups in the page and disable them
    LayoutGroup[] groups = GetComponentsInChildren<LayoutGroup>(true);
    foreach (LayoutGroup group in groups)
    {
        group.enabled = false;
    }
}

[Button] private void FindAndEnableAllLayoutGroups()
{
    // Find all layout groups in the page and enable them
    LayoutGroup[] groups = GetComponentsInChildren<LayoutGroup>(true);
    foreach (LayoutGroup group in groups)
    {
        group.enabled = true;
    }
}
#endif
}