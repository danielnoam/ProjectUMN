using System;
using System.Collections.Generic;
using Shapes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VInspector;



public class OptionsMenu : MonoBehaviour
{
    [Header("Panels")] 
    [SerializeField] private GameObject[] panels;
    [SerializeField] private Color defaultPanelIconsColor = Color.white;
    [SerializeField] private Color selectedPanelIconsColor = Color.cyan;
    [SerializeField,ReadOnly] private int _currentPanelIndex;
    
    [Header("Selectables")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextPanelButton;
    [SerializeField] private Button previousPanelButton;
    [SerializeField] private Button resetVolumesButton;
    [SerializeField] private Button resetControlsButton;
    [SerializeField] private VolumeSlider[] volumeSliders;
    [SerializeField] private SensitivitySlider[] sensitivitySliders;
    [SerializeField] private InputToggle[] inputToggles;
    
    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private MenuPage optionsPage;
    [SerializeField] private GameObject panelIconsHolder;
    [SerializeField] private GameObject panelIconPrefab;
    [SerializeField] private SOAudioManager audioManager;
    [SerializeField] private SOInputReader inputManager;
    private readonly Dictionary<GameObject, ShapeGroup> _panelShapes = new Dictionary<GameObject, ShapeGroup>();
    private readonly Dictionary<GameObject, CanvasGroup> _panelCanvasGroups = new Dictionary<GameObject, CanvasGroup>();
    private readonly Dictionary<GameObject, ShapeGroup> _panelIcons = new Dictionary<GameObject, ShapeGroup>();
    
    private void Awake()
    {
        _currentPanelIndex = -1;
        
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
            
            var icon = panelIconsHolder.transform.Find(panel.name);
            if (icon)
            {
                icon.TryGetComponent(out ShapeGroup iconShapeGroup);
                if (iconShapeGroup) _panelIcons.Add(panel, iconShapeGroup);
                iconShapeGroup.Color = defaultPanelIconsColor;
            }
        }
    }
    

    private void Start()
    {
        if (TestManager.Instance)
        {
            backButton.onClick.AddListener(() =>
            {
                player?.InMenuState.SelectPage(player.InMenuState.PreviousPage);
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
        
        resetVolumesButton.onClick.AddListener(() =>
        {
            audioManager?.ResetAllVolumes();
            
            foreach (var slider in volumeSliders)
            {
                slider.ResetSliderValue(audioManager);
            }
        });
        
        resetControlsButton.onClick.AddListener(() =>
        {
            inputManager?.ResetInputSettings();
            
            // Update sensitivity sliders when controls are reset
            foreach (var slider in sensitivitySliders)
            {
                slider.UpdateSliderValue(inputManager);
            }
            
            // Update toggle values when controls are reset
            foreach (var toggle in inputToggles)
            {
                toggle.UpdateToggleValue(inputManager);
            }
        });

        // Initialize volume sliders
        foreach (var slider in volumeSliders)
        {
            slider.SetUpSlider(audioManager);
        }
        
        // Initialize sensitivity sliders
        foreach (var slider in sensitivitySliders)
        {
            slider.SetupSlider(inputManager);
        }
        
        // Initialize input toggles
        foreach (var toggle in inputToggles)
        {
            toggle.SetupToggle(inputManager);
        }
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

    
    

    #region Panels --------------------------------------------------------------------------------

        private void OnPageSelected()
    {
        // Reset panel states first to ensure a clean state
        foreach (var panel in panels)
        {
            SetPanelState(Array.IndexOf(panels, panel), 0);
        }
        // Then select the first panel
        SelectPanel(0);
    }
    
    private void OnPageDeselected()
    {
        foreach (var panel in panels)
        {
            SetPanelState(Array.IndexOf(panels, panel), 0);
        }
        _currentPanelIndex = -1;
    }


    private void SelectPanel(int index)
    {
        if (index < 0 || index >= panels.Length)
        {
            return;
        }
        
        // Only update the previous panel if it's different from the current one
        if (_currentPanelIndex >= 0 && _currentPanelIndex < panels.Length && _currentPanelIndex != index)
        {
            SetPanelState(_currentPanelIndex, 0);
        }
        
        _currentPanelIndex = index;
        SetPanelState(_currentPanelIndex, 1);
    }

    private void SetPanelState(int index, float alpha)
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
        
        if (_panelIcons.TryGetValue(panels[index], out var icon))
        {
            icon.Color = alpha > 0 ? selectedPanelIconsColor : defaultPanelIconsColor;
            icon.transform.localScale = Vector3.one * (alpha > 0 ? 1.2f : 1f);
        }
    }



    [Button]
    private void CreatePanelIcons()
    {
        // create a list of for current child icons and delete them
        var currentIcons = new List<GameObject>();
        foreach (Transform child in panelIconsHolder.transform)
        {
            currentIcons.Add(child.gameObject);
        }
        foreach (var icon in currentIcons)
        {
            DestroyImmediate(icon);
        }
        
        
        foreach (var panel in panels)
        {
            var icon = Instantiate(panelIconPrefab, panelIconsHolder.transform);
            icon.name = panel.name;
        }
    }

    [Button]
    private void ToggleLayoutGroup()
    {
        var layoutGroup = panelIconsHolder.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup)
        {
            layoutGroup.enabled = !layoutGroup.enabled;
        }   
    }

    #endregion Panels --------------------------------------------------------------------------------

}



#region Selectable Classes ----------------------------------------------------------------------------

[Serializable]
public class VolumeSlider
{
    [SerializeField] private VolumeType volumeType;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI sliderValue;
    
    
    public void SetUpSlider(SOAudioManager audioManager)
    {
        if (!slider) return;
    
        slider.value = audioManager.LoadVolume(volumeType);
        SetSliderText(slider.value);
        
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(value => SetVolume(audioManager, value));
        slider.onValueChanged.AddListener(SetSliderText);
        
    }
    
    public void SetSliderText(float value)
    {
        if (!sliderValue) return;
        
        sliderValue.text = Mathf.Round(value * 100) + "%";
    }
    
    public void SetVolume(SOAudioManager audioManager,float volume)
    {
        if (!audioManager) return;
        
        audioManager.SetVolume(volumeType, volume);
        audioManager.SaveVolume(volumeType, volume);
        
    }
    
    public void ResetSliderValue(SOAudioManager audioManager)
    {
        if (!slider) return;
        float defaultVolume = volumeType switch
        {
            VolumeType.MasterVolume => audioManager.DefaultMasterVolume,
            VolumeType.MusicVolume => audioManager.DefaultMusicVolume,
            VolumeType.SoundFXVolume => audioManager.DefaultSoundFXVolume,
            _ => 0f
        };
        SetSliderText(defaultVolume);
        slider.value = defaultVolume;
    }
}

[Serializable]
public class SensitivitySlider
{
    public enum SensitivityType
    {
        FreeCameraSensitivity,
        AimCameraSensitivity
    }
    
    [SerializeField] private ControlType controlType;
    [SerializeField] private SensitivityType sensitivityType;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI sliderValueText;

    public void SetupSlider(SOInputReader inputReader)
    {
        if (slider == null || inputReader == null) return;
        
        // Set the min and max value from the input reader's settings
        SetSliderMinMaxValues();
        
        // Set initial slider value based on the specified control type
        UpdateSliderValue(inputReader);

        // Add listeners
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(value => OnSliderValueChanged(inputReader, value));
    }
    
    private void SetSliderMinMaxValues()
    {
        // Use range values for the slider based on sensitivity type
        switch (sensitivityType)
        {
            case SensitivityType.FreeCameraSensitivity:
                slider.minValue = 0.1f; // Based on the Range attribute in InputSettings
                slider.maxValue = 10f;  // Based on the Range attribute in InputSettings
                break;
                
            case SensitivityType.AimCameraSensitivity:
                slider.minValue = 0.1f; // Based on the Range attribute in InputSettings
                slider.maxValue = 10f;  // Based on the Range attribute in InputSettings
                break;
        }
    }

    public void UpdateSliderValue(SOInputReader inputReader)
    {
        if (slider == null || inputReader == null) return;
        
        float value = 1f;
        
        // Get value from the appropriate settings based on control type
        InputSettings settings = (controlType == ControlType.KeyboardMouse) 
            ? inputReader.MouseKeyboardSettings 
            : inputReader.GamepadSettings;
        
        switch (sensitivityType)
        {
            case SensitivityType.FreeCameraSensitivity:
                value = settings.freeCameraSensitivity;
                break;
                
            case SensitivityType.AimCameraSensitivity:
                value = settings.aimCameraSensitivity;
                break;
        }
        
        slider.value = value;
        UpdateSliderText(value);
    }

    private void OnSliderValueChanged(SOInputReader inputReader, float value)
    {
        if (inputReader == null) return;
        
        // Update the setting in the input reader using our specific control type
        switch (sensitivityType)
        {
            case SensitivityType.FreeCameraSensitivity:
                inputReader.SetFreeCameraSensitivity(controlType, value);
                break;
                
            case SensitivityType.AimCameraSensitivity:
                inputReader.SetAimCameraSensitivity(controlType, value);
                break;
        }

        // Update the display text
        UpdateSliderText(value);
    }

    private void UpdateSliderText(float value)
    {
        if (sliderValueText == null) return;
        sliderValueText.text = $"{value:F1}";
    }
}

[Serializable]
public class InputToggle
{
    public enum ToggleSettingType
    {
        ToggleSprint,
        ToggleCrouch,
        ToggleAimInput
    }
    
    [SerializeField] private ControlType controlType;
    [SerializeField] private ToggleSettingType toggleType;
    [SerializeField] private Toggle toggle;

    public void SetupToggle(SOInputReader inputReader)
    {
        if (toggle == null || inputReader == null) return;

        // Set initial toggle value based on the specified control type
        UpdateToggleValue(inputReader);

        // Add listeners
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(value => OnToggleValueChanged(inputReader, value));
    }

    public void UpdateToggleValue(SOInputReader inputReader)
    {
        if (toggle == null || inputReader == null) return;
        
        // Get the appropriate settings based on control type
        InputSettings settings = (controlType == ControlType.KeyboardMouse) 
            ? inputReader.MouseKeyboardSettings 
            : inputReader.GamepadSettings;
            
        bool isOn = false;
        
        switch (toggleType)
        {
            case ToggleSettingType.ToggleSprint:
                isOn = settings.toggleSprint;
                break;
                
            case ToggleSettingType.ToggleCrouch:
                isOn = settings.toggleCrouch;
                break;
            
            case ToggleSettingType.ToggleAimInput:
                isOn = settings.toggleAimInput;
                break;
        }
        
        toggle.isOn = isOn;
    }

    private void OnToggleValueChanged(SOInputReader inputReader, bool isOn)
    {
        if (inputReader == null) return;
        
        // Update the setting in the input reader using our specific control type
        switch (toggleType)
        {
            case ToggleSettingType.ToggleSprint:
                inputReader.SetToggleSprint(controlType, isOn);
                break;
                
            case ToggleSettingType.ToggleCrouch:
                inputReader.SetToggleCrouch(controlType, isOn);
                break;
                
            case ToggleSettingType.ToggleAimInput:
                inputReader.SetToggleAimInput(controlType, isOn);
                break;
        }
    }
}

#endregion Selectable Classes ----------------------------------------------------------------------------