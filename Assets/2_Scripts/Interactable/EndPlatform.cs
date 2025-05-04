using UnityEngine;
using VInspector;

[SelectionBase]
public class EndPlatform : MonoBehaviour
{
    
    
    [Header("Settings")]
    [SerializeField] private bool isActive;
    [SerializeField] private bool startCreditsSequence;
    
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Light pointLight;
    [SerializeField] private Transform endPoint;
    [SerializeField] private ParticleSystem[] particleSystems;
    
    private TestManager _testManager;
    private float _lightIntensity;
    public Transform EndPoint => endPoint;
    public bool IsActive => isActive;
    

    private void Awake()
    {
        if (pointLight)
        {
            _lightIntensity = pointLight.intensity;
        }
        SetActiveState(isActive);
    }

    private void Start()
    {
        _testManager = TestManager.Instance;
    }
    
    private void Update()
    {
        if (pointLight)
        {
            pointLight.intensity = Mathf.Lerp(pointLight.intensity, isActive ? _lightIntensity : 0, Time.deltaTime * 2);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        
        if (!_testManager)
        {
            Debug.LogError("TestManager is null");
            return;
        }
        
        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            if (startCreditsSequence)
            {
                _testManager.StartCreditsSequence(false);
                _testManager.OnGameCompleted();
            }
            else
            {
                _testManager.LoadNextTest();
            }
            
        }
    }

    public void SetActiveState(bool state)
    {
        isActive = state;
        
        if (state)
        {
            audioSource?.Play();
            
            
            foreach (var particle in particleSystems)
            {
                particle?.Play();
            }
        }
        else
        {
            audioSource?.Stop();
            
            foreach (var particle in particleSystems)
            {
                particle?.Stop();
            }
        }
    }

    [Button] private void SetActive() { SetActiveState(true); }
    [Button] private void SetInactive() { SetActiveState(false); }

    [Button]
    public void ToggleState()
    {
        if (isActive)
        {
            SetInactive();
        }
        else
        {
            SetActive();
        }
    }
}
