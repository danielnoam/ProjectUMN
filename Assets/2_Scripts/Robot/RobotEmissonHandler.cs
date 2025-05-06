using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Serialization;

public class RobotEmissonHandler : MonoBehaviour
{

    [Header("Light Settings")]
    [SerializeField] private float lightSmoothSpeed = 5f;
    
    
    [Header("Emission Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Ease animationEase = Ease.OutCubic;
    [SerializeField] private float bodyEmissionIntensity = 1.0f;
    
    [Header("References")] 
    [SerializeField] private RobotCompanion robot;
    [SerializeField] private Renderer bodyRend;
    [SerializeField] private Renderer eyeRend;
    [SerializeField] private Light eyeLight;
    [SerializeField] private Light areaLight;

    private Sequence _eyeEmissionSequence;
    private Sequence _bodyEmissionSequence;
    private Material _eyeMaterial;
    private Material _bodyMaterial;
    private Color _defaultEyeEmissionColor;
    private Color _defaultBodyEmissionColor;
    private float _eyeEmissionIntensity;
    private static readonly int EyeEmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int BodyEmissionColor = Shader.PropertyToID("_Emission_Color");
    private bool _lastAmbientState = false;
    
    private Color _defaultEyeLightColor;
    private float _fullEyeLightIntensity;
    private float _defaultInnerSpotAngle;
    private float _defaultOuterSpotAngle;
    
    private void Awake()
    {
        if (!robot) return;
        
        if (eyeLight)
        {
            _defaultEyeLightColor = eyeLight.color;
            _fullEyeLightIntensity = eyeLight.intensity;
            _defaultInnerSpotAngle = eyeLight.innerSpotAngle;
            _defaultOuterSpotAngle = eyeLight.spotAngle;
        }
        
        bool isDark = RenderSettings.ambientIntensity == 0;
        
        UpdateAreaLight(isDark);
        
        
        if (isDark != _lastAmbientState)
        {
            _lastAmbientState = isDark;
            UpdateBodyEmissionState(isDark);
        }
        
        InitializeEyeMaterial();
        InitializeBodyMaterial();
    }

    private void OnEnable()
    {
        robot.onRobotTurnedOn.AddListener(OnTurnedOn);
        robot.onRobotTurnedOff.AddListener(OnTurnedOff);
        TestManager.Instance?.onTestLoaded.AddListener(OnTestLoaded);
    }

    private void OnDisable()
    {
        robot.onRobotTurnedOn.RemoveListener(OnTurnedOn);
        robot.onRobotTurnedOff.RemoveListener(OnTurnedOff);
        TestManager.Instance?.onTestLoaded.AddListener(OnTestLoaded);
    }


    private void Update()
    {
        UpdateEyeLight();
    }

    private void OnTurnedOn()
    {
        UpdateEyeEmissionState();
    }
    
    private void OnTurnedOff()
    {
        UpdateEyeEmissionState();
    }
    private void OnTestLoaded(SOTest test)
    {
        bool isDark = RenderSettings.ambientIntensity == 0;
        
        UpdateAreaLight(isDark);
        
        
        if (isDark != _lastAmbientState)
        {
            _lastAmbientState = isDark;
            UpdateBodyEmissionState(isDark);
        }
    }



    #region Light ------------------------------------------------------------------------

    private void UpdateEyeLight()
    {
        
        bool isPlayerAiming = robot.CurrentState == RobotState.FollowingPlayer  && robot.PlayerIsAiming;
        
        if (!robot.IsOn())
        {
            eyeLight.intensity = 0;
            eyeLight.color = Color.black;
        }
        else if (robot.CurrentButtery <= robot.LowBattery)
        {
            eyeLight.color = Color.Lerp(eyeLight.color, Color.red, lightSmoothSpeed * Time.deltaTime);
        }
        else
        {

            if (isPlayerAiming)
            {
                // Lerp to aiming values
                eyeLight.intensity = Mathf.Lerp(eyeLight.intensity, _fullEyeLightIntensity * 3, lightSmoothSpeed * Time.deltaTime);
                eyeLight.range = Mathf.Lerp(eyeLight.range, 90f, lightSmoothSpeed * Time.deltaTime);
                eyeLight.innerSpotAngle = Mathf.Lerp(eyeLight.innerSpotAngle, _defaultInnerSpotAngle * 2, lightSmoothSpeed * Time.deltaTime);
                eyeLight.spotAngle = Mathf.Lerp(eyeLight.spotAngle, _defaultOuterSpotAngle * 2, lightSmoothSpeed * Time.deltaTime);
            }
            else
            {
                // Lerp to normal values
                eyeLight.intensity = Mathf.Lerp(eyeLight.intensity, _fullEyeLightIntensity, lightSmoothSpeed * Time.deltaTime);
                eyeLight.range = Mathf.Lerp(eyeLight.range, 5f, lightSmoothSpeed * Time.deltaTime);
                eyeLight.innerSpotAngle = Mathf.Lerp(eyeLight.innerSpotAngle, _defaultInnerSpotAngle, lightSmoothSpeed * Time.deltaTime);
                eyeLight.spotAngle = Mathf.Lerp(eyeLight.spotAngle, _defaultOuterSpotAngle, lightSmoothSpeed * Time.deltaTime);
            }
            eyeLight.color = Color.Lerp(eyeLight.color, _defaultEyeLightColor, lightSmoothSpeed * Time.deltaTime);
        }
    }

    private void UpdateAreaLight(bool isDark)
    {
        // Update area light based on ambient intensity
        if (isDark && !areaLight.gameObject.activeSelf)
        {
            areaLight.gameObject.SetActive(true);
        }
        else if (!isDark && areaLight.gameObject.activeSelf)
        {
            areaLight.gameObject.SetActive(false);
        }
    }
    
    #endregion Light ------------------------------------------------------------------------


    #region Emission --------------------------------------------------------------------

    private void UpdateEyeEmissionState()
    {
        if (_eyeEmissionSequence.isAlive)
        {
            _eyeEmissionSequence.Stop();
        }

        Color startColor = _eyeMaterial.GetColor(EyeEmissionColor);
        Color targetColor = robot.IsOn() ? _defaultEyeEmissionColor * _eyeEmissionIntensity : Color.black;
        float duration = robot.IsOn() ? animationDuration : animationDuration /2;

        _eyeEmissionSequence = Sequence.Create();
    
        _eyeEmissionSequence.Group(Tween.MaterialProperty(_eyeMaterial, EyeEmissionColor, startValue: startColor, endValue: targetColor, ease: animationEase, duration: duration));
    }
    
    private void UpdateBodyEmissionState(bool turnOn)
    {
        if (_bodyEmissionSequence.isAlive)
        {
            _bodyEmissionSequence.Stop();
        }

        if (!robot.IsOn())
        {
            // If robot is off, always turn body emission off
            turnOn = false;
        }

        Color startColor = _bodyMaterial.GetColor(BodyEmissionColor);
        Color targetColor = turnOn ? _defaultBodyEmissionColor * bodyEmissionIntensity : Color.black;
        float duration = animationDuration * 3;

        _bodyEmissionSequence = Sequence.Create();
        _bodyEmissionSequence.Group(Tween.MaterialProperty(_bodyMaterial, BodyEmissionColor, startValue: startColor, endValue: targetColor, ease: animationEase, duration: duration));
    }
    
    private void InitializeEyeMaterial()
    {
        // Initialize eye material
        _eyeMaterial = new Material(eyeRend.material);
        eyeRend.material = _eyeMaterial;
        
        if (!_eyeMaterial.HasProperty(EyeEmissionColor))
        {
            Debug.LogWarning($"Eye material on {gameObject.name} does not have required emission property {EyeEmissionColor}");
        }
        else
        {
            // Enable emission on the eye material
            _eyeMaterial.EnableKeyword("_Emission");
            
            // Get the existing emission color from the material
            _defaultEyeEmissionColor = _eyeMaterial.GetColor(EyeEmissionColor);
            
            // Calculate the emission intensity from the brightest component
            _eyeEmissionIntensity = Mathf.Max(_defaultEyeEmissionColor.r, _defaultEyeEmissionColor.g, _defaultEyeEmissionColor.b);
            
            // Normalize the color if it has intensity
            if (_eyeEmissionIntensity > 0)
            {
                _defaultEyeEmissionColor /= _eyeEmissionIntensity;
            }
            else
            {
                // Default to white if there's no emission
                _defaultEyeEmissionColor = Color.white;
                _eyeEmissionIntensity = 1.0f;
            }
        }
    }

    private void InitializeBodyMaterial()
    {
        // Initialize body material
        _bodyMaterial = new Material(bodyRend.material);
        bodyRend.material = _bodyMaterial;
        
        if (!_bodyMaterial.HasProperty(BodyEmissionColor))
        {
            Debug.LogWarning($"Body material on {gameObject.name} does not have required emission property {BodyEmissionColor}");
        }
        else
        {
            // Enable emission on the body material
            _bodyMaterial.EnableKeyword("_Emission");
            
            // Get the existing emission color from the material
            _defaultBodyEmissionColor = _bodyMaterial.GetColor(BodyEmissionColor);
            
            // Normalize the color if needed (preserve the original color)
            float bodyEmissionIntensityTemp = Mathf.Max(_defaultBodyEmissionColor.r, _defaultBodyEmissionColor.g, _defaultBodyEmissionColor.b);
            if (bodyEmissionIntensityTemp > 0)
            {
                _defaultBodyEmissionColor /= bodyEmissionIntensityTemp;
            }
            else
            {
                // Default to white if there's no emission
                _defaultBodyEmissionColor = Color.white;
            }
            
            // Start with emission turned off for body
            _bodyMaterial.SetColor(BodyEmissionColor, Color.black);
        }
        
        // Initialize the eye material with the appropriate state
        UpdateEyeEmissionState();
        
        // Check initial ambient intensity
        _lastAmbientState = RenderSettings.ambientIntensity == 0;
        if (_lastAmbientState && robot.IsOn())
        {
            UpdateBodyEmissionState(true);
        }
    }

    #endregion

}