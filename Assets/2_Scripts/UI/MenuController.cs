using System;
using UnityEngine;
using System.Collections.Generic;
using CustomAttribute;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private List<MenuPage> menuPages = new List<MenuPage>();

    [Header("References")]
    [SerializeField] private SOInputReader inputReader;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private MenuPage startPage;
    [SerializeField] private MenuPage pausePage;
    [SerializeField] private MenuPage debugPage;
    [SerializeField] private MenuPage optionsPage;
    
    [Space(10)]
    [ReadOnly] public MenuPage currentPage;
    public AudioSource AudioSource => audioSource;
    

    private void Start()
    {
        if (player != null && player.InMenuState != null)
        {
            player.InMenuState.SetupPages(this,startPage, pausePage, debugPage, optionsPage);
        }
    }

    private void OnEnable()
    {
        if (inputReader)
        {
            inputReader.NavigateEvent += OnNavigate;
        }
    }

    private void OnDisable()
    {
        if (inputReader)
        {
            inputReader.NavigateEvent -= OnNavigate;
        }
    }
    
    private void OnNavigate(InputAction.CallbackContext context) // Input event
    {
        if (currentPage) 
        {
            currentPage.OnNavigate(context);
        }
    }
    
    
#region Page Management //-------------------------------------------------------------

    public void SelectPage(MenuPage page, bool playAnimation)
    {
        if (!page) return;

        // Only deselect the currently active page if there is one
        if (currentPage && currentPage != page)
        {
            currentPage.OnPageDeselected(playAnimation);
        }

        currentPage = page;
        page.OnPageSelected(playAnimation);
    }
    

    public void DeselectAllPages(bool playAnimation)
    {
        // Only deselect pages that are currently active
        foreach (MenuPage page in menuPages)
        {
            if (page && page.PageIsActive)
            {
                page.OnPageDeselected(playAnimation);
            }
        }
        currentPage = null;
    }

#endregion Page Management //-------------------------------------------------------------



#if UNITY_EDITOR
private void OnValidate()
{
    // Find all pages that are direct children of the controller
    foreach (Transform child in transform)
    {
        if (!child.TryGetComponent(out MenuPage page)) continue;
        if (!menuPages.Contains(page))
        {
            menuPages.Add(page);
        }
    }
    menuPages.RemoveAll(menu => menu == null);
}
#endif
}