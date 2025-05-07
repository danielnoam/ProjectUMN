using System;
using System.Collections.Generic;
using Shapes;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VInspector;

public class OptionsMenu : MonoBehaviour
{
    [Header("Panels")] 
    [SerializeField] private GameObject[] panels;
    
    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextPanelButton;
    [SerializeField] private Button previousPanelButton;
    
    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private MenuPage optionsPage;
    [SerializeField,ReadOnly] private int _currentPanelIndex;
    private readonly Dictionary<GameObject, ShapeGroup> _panelShapes = new Dictionary<GameObject, ShapeGroup>();
    private readonly Dictionary<GameObject, CanvasGroup> _panelCanvasGroups = new Dictionary<GameObject, CanvasGroup>();

    private void Awake()
    {
        foreach (var panel in panels)
        {
            var shapeGroup = panel.GetComponent<ShapeGroup>();
            if (shapeGroup)
            {
                _panelShapes.Add(panel, shapeGroup);
            }
            
            var canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup)
            {
                _panelCanvasGroups.Add(panel, canvasGroup);
            }
        }
        
        OnPageSelected();
    }
    

    private void Start()
    {
        if (TestManager.Instance)
        {
            backButton.onClick.AddListener(() =>
            {
                player.InMenuState.SelectPage(player.InMenuState.PreviousPage);
            });
        }
        
        nextPanelButton.onClick.AddListener(() =>
        {
            SelectPanel((_currentPanelIndex + 1) % panels.Length);
        });
        
        previousPanelButton.onClick.AddListener(() =>
        {
            SelectPanel((_currentPanelIndex - 1 + panels.Length) % panels.Length);
        });
    }

    private void OnEnable()
    {
        optionsPage?.onPageSelected.AddListener(OnPageSelected);
        optionsPage?.onPageDeselected.AddListener(OnPageDeselected);
    }
    
    private void OnDisable()
    {
        optionsPage?.onPageSelected.RemoveListener(OnPageSelected);
        optionsPage?.onPageDeselected.RemoveListener(OnPageDeselected);
    }


    private void OnPageSelected()
    {
        SelectPanel(0);
    }
    
    private void OnPageDeselected()
    {
        SelectPanel(0);
        foreach (var panel in panels)
        {
            SetPanelAlpha(Array.IndexOf(panels, panel), 0);
        }
        _currentPanelIndex = -1;
    }


    private void SelectPanel(int index)
    {
        if (index < 0 || index >= panels.Length || _currentPanelIndex == index)
        {
            return;
        }
        
 
        
        SetPanelAlpha(_currentPanelIndex, 0);
        _currentPanelIndex = index;
        SetPanelAlpha(_currentPanelIndex, 1);
    }

    private void SetPanelAlpha(int index, float alpha)
    {
        if (index < 0 || index >= panels.Length)
        {
            return;
        }

        if (_panelShapes.TryGetValue(panels[index], out var shapeGroup))
        {
            Color shapeColor = shapeGroup.Color;
            shapeColor.a = alpha;
            shapeGroup.Color = shapeColor;
        }

        if (_panelCanvasGroups.TryGetValue(panels[index], out var canvasGroup))
        {
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = alpha > 0;
            canvasGroup.blocksRaycasts = alpha > 0;
        }
        
    }
}
