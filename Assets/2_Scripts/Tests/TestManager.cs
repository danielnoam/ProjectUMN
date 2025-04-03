using System;
using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class TestManager : MonoBehaviour
{
    public static TestManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private PlayerStateMachine playerPrefab;
    [SerializeField] private RobotCompanion robotPrefab;
    [SerializeField] private SOAudioEvent introTheme;
    [SerializeField] private SOTest[] tests;
    [Foldout("Events")]
    public UnityEvent onIntroSequenceStart = new UnityEvent();
    public UnityEvent<SOTest> onTestStartLoading = new UnityEvent<SOTest>();
    public UnityEvent<SOTest> onTestLoaded = new UnityEvent<SOTest>();
    public UnityEvent<SOTest> onTestStartUnloading = new UnityEvent<SOTest>();
    public UnityEvent<SOTest> onTestUnloaded = new UnityEvent<SOTest>();
    [EndFoldout]
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private TextMeshProUGUI debugTextRight;
    [SerializeField] private TextMeshProUGUI debugTextLeft;
    [SerializeField, ReadOnly] private PlayerStateMachine currentPlayer;
    [SerializeField, ReadOnly] private RobotCompanion currentRobot;
    [SerializeField, ReadOnly] private SOTest currentTest;
    [SerializeField, ReadOnly] private GameObject currentEnvironment;
    [SerializeField, ReadOnly] private Transform currentCheckpoint;
    [SerializeField, ReadOnly] private SOAudioEvent currentTheme;
    [SerializeField, ReadOnly] private TestLightSettings currentLightSettings;


    
    public bool DebugMode => debugMode;
    public TestLightSettings DefaultLightSettings => _defaultLightSettings;
    public PlayerStateMachine Player => currentPlayer;
    public RobotCompanion Robot => currentRobot;
    public SOTest CurrentTest => currentTest;
    public SOAudioEvent CurrentTheme => currentTheme;
    public Vector3 CurrentCheckpoint => currentCheckpoint ? currentCheckpoint.position : currentTest.GetPlayerSpawnPoint();
    public Vector3 CurrentSpawnPoint => currentTest ? currentTest.GetPlayerSpawnPoint() : Vector3.zero;
    public TextMeshProUGUI DebugTextLeft => debugTextLeft;
    public TextMeshProUGUI DebugTextRight => debugTextRight;
    
    private Coroutine _activeLoadCoroutine;
    private Coroutine _activeUnloadCoroutine;
    private Coroutine _activeSequenceCoroutine;
    private AudioSource _audioSource;
    private CameraManager _cameraManager;
    private TestLightSettings _defaultLightSettings;
    private Material _defaultTestFloorMaterial;
    private TestAnimator _testAnimator;
    private Renderer _testFloorRenderer;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        
        _testAnimator = GetComponent<TestAnimator>();
        _audioSource = GetComponent<AudioSource>();
        _defaultLightSettings = tests[0].GetLightSettings();
        _defaultTestFloorMaterial = tests[0].GetFloorMaterial();
    }
    
    private void Start()
    {
        currentPlayer = FindFirstObjectByType<PlayerStateMachine>();
        currentRobot = FindFirstObjectByType<RobotCompanion>();
        _cameraManager = FindFirstObjectByType<CameraManager>();
        _testFloorRenderer = GameObject.Find("Floor").GetComponent<Renderer>();
        
        

        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            StartIntroSequence();   
        }
    }
    
    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            StartTest(0);
        }
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            StartTest(1);
        }
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            StartTest(2);
        }
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            StartTest(3);
        }
        if (Input.GetKeyDown(KeyCode.KeypadPeriod))
        {
            RemoveCurrentTest();
        }
        
        
        if (Input.GetKeyDown(KeyCode.Keypad9))
        {
            ToggleDebugMode();
        }
        if (Input.GetKeyDown(KeyCode.Keypad8))
        {
            ApplyLightSettings(tests[2].GetLightSettings());
        }
        if (Input.GetKeyDown(KeyCode.Keypad7))
        {
            ApplyLightSettings(_defaultLightSettings);
        }
    }
    

    #region Test control ----------------------------------------------------------------------------

    [Button]
    public void StartTest(int testIndex)
    {
        // Check if a sequence is already running
        if (_activeSequenceCoroutine != null)
        {
            Debug.Log("A test sequence is currently running. Please wait...");
            return;
        }
        
        // Start and track the sequence coroutine
        _activeSequenceCoroutine = StartCoroutine(StartTestLoadingSequence(testIndex));
    }
    
    [Button]
    public void RemoveCurrentTest()
    {
        if (!currentTest) return;
        
        // Check if an unload operation is already running
        if (_activeUnloadCoroutine != null)
        {
            Debug.Log("A test is currently unloading. Please wait...");
            return;
        }
        
        // Start and track the unload coroutine
        _activeUnloadCoroutine = StartCoroutine(UnLoadTest());
    }
    
    [Button]
    public void LoadNextTest()
    {
        // Check if a sequence is already running
        if (_activeSequenceCoroutine != null)
        {
            Debug.Log("A test sequence is currently running. Please wait...");
            return;
        }
        
        // Get the current test index
        int currentTestIndex = Array.IndexOf(tests, currentTest);
        if (currentTestIndex == -1)
        {
            Debug.Log("Current test not found in the tests array");
            _activeSequenceCoroutine = StartCoroutine(StartTestLoadingSequence(0));
            return;
        }
        
        // Load the next test
        if (currentTestIndex < tests.Length - 1)
        {
            _activeSequenceCoroutine = StartCoroutine(StartTestLoadingSequence(currentTestIndex + 1));
        }
        else
        {
            Debug.Log("No more tests to load");
        }
    }
    
    [Button]
    public void RestartCurrentTest()
    {
        if (!currentTest) return;
        
        // get test index from currentTest
        int testIndex = Array.IndexOf(tests, currentTest);
        if (testIndex == -1)
        {
            Debug.Log("Current test not found in the tests array");
            return;
        }
        StartTest(testIndex);
    }
    

    [Button]
    public void StartIntroSequence()
    {
        if (!_cameraManager || !currentPlayer) return;
        
        currentPlayer.SwitchState(currentPlayer.GroundedState);
        currentPlayer.transform.position = Vector3.zero + new Vector3(0, 0.9f, 0);
        currentTheme = introTheme;
        currentTheme?.Play(_audioSource);
        _cameraManager.StartIntroSequenceCamera();
    }
    
    public void QuitApplication()
    {
        Application.Quit();
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void ToggleDebugMode()
    {
        debugMode = !debugMode;

        if (debugMode == false)
        {
            debugTextLeft.text = "";
            debugTextRight.text = "";
        }
    }
    
    public void SetCheckpointPosition(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
    }
    
    
    private void ApplyLightSettings(TestLightSettings lightSettings)
    {
        RenderSettings.ambientIntensity = lightSettings.ambientIntensity;
        RenderSettings.defaultReflectionMode = lightSettings.reflectionMode;
        
        RenderSettings.fog = lightSettings.useFog;
        RenderSettings.fogMode = lightSettings.fogMode;
        RenderSettings.fogColor = lightSettings.fogColor;
        switch (lightSettings.fogMode)
        {
            case FogMode.Exponential or FogMode.ExponentialSquared:
                RenderSettings.fogDensity = lightSettings.fogDensity;
                break;
            case FogMode.Linear:
                RenderSettings.fogStartDistance = lightSettings.fogStart;
                RenderSettings.fogEndDistance = lightSettings.fogEnd;
                break;
        }
    }
    
    
    #endregion Test control ----------------------------------------------------------------------------
    

    
    #region Private methods ----------------------------------------------------------------------------
    
    private IEnumerator StartTestLoadingSequence(int testIndex)
    {
        // Early exit if invalid test index
        if (tests.Length <= 0 || testIndex >= tests.Length)
        {
            _activeSequenceCoroutine = null;
            yield break;
        }
        
        if (currentTest)
        {
            int unloadTime = currentTest.GetTimeToUnload();
            
            // If an unload is already running, wait for it to finish
            if (_activeUnloadCoroutine != null)
            {
                yield return _activeUnloadCoroutine;
            }
            else
            {
                // Start a new unload operation
                _activeUnloadCoroutine = StartCoroutine(UnLoadTest(false));
                yield return _activeUnloadCoroutine;
            }
            
            
            yield return new WaitForSeconds(unloadTime);
            
            // If a load is already running, wait for it to finish
            if (_activeLoadCoroutine != null)
            {
                yield return _activeLoadCoroutine;
            }
            else
            {
                // Start a new load operation
                _activeLoadCoroutine = StartCoroutine(LoadTest(testIndex, false));
                yield return _activeLoadCoroutine;
            }
        }
        else
        {
            // If a load is already running, wait for it to finish
            if (_activeLoadCoroutine != null)
            {
                yield return _activeLoadCoroutine;
            }
            else
            {
                // Start a new load operation
                _activeLoadCoroutine = StartCoroutine(LoadTest(testIndex, false));
                yield return _activeLoadCoroutine;
            }
        }
        
        // Clear sequence reference when done
        _activeSequenceCoroutine = null;
    }
    
    private IEnumerator LoadTest(int testIndex, bool isStandaloneCall = true)
    {
        if (tests.Length <= 0 || testIndex >= tests.Length)
        {
            if (isStandaloneCall) _activeLoadCoroutine = null;
            yield break;
        }
        
        Debug.Log("Loading... " + tests[testIndex].GetName());
        
        currentTest = tests[testIndex];
        currentTheme = currentTest.GetTheme();
        ApplyLightSettings(currentTest.GetLightSettings());
        if (_testFloorRenderer) _testFloorRenderer.material = currentTest.GetFloorMaterial();
        currentEnvironment = Instantiate(currentTest.GetPrefab(), new Vector3(0,-0.03f,0), quaternion.identity); // a bit of offset for the intersection effect
        currentEnvironment.name = currentTest.GetName() + " Environment";
        if (currentTest.HasRobot()) // The new test has a robot in it
        {
            if (currentRobot) Destroy(currentRobot.gameObject);
            currentRobot = null;
            currentRobot = FindFirstObjectByType<RobotCompanion>();
            
        } else if (!currentRobot && robotPrefab) // The new test has no robot and there is no robot in the scene
        {
            currentRobot = Instantiate(robotPrefab);
            currentRobot.TurnOn();
        }
        onTestStartLoading?.Invoke(tests[testIndex]);
        
        // Play scale-up animation 
        if (_testAnimator && _testAnimator.PlayOnTestLoading)
        {
            
            // Give a small delay before playing the animation
            _testAnimator.RefreshForNewEnvironment();
            _testAnimator.SetAllObjectsToZeroScale();
            yield return new WaitForSeconds(0.5f);
            _testAnimator.PlayScaleSequence(currentTest.GetTimeToLoad());
            
            
            // Wait for animation to complete
            yield return new WaitForSeconds(currentTest.GetTimeToLoad());
        }
        else
        {
            yield return new WaitForSeconds(currentTest.GetTimeToLoad());
        }

        
        currentTheme?.Play(_audioSource);
        _activeLoadCoroutine = null;
        onTestLoaded?.Invoke(currentTest);
        Debug.Log("Loaded " + currentTest.GetName());
    }
    
    
    private IEnumerator UnLoadTest(bool isStandaloneCall = true)
    {
        // Early exit if no test is currently loaded
        if (!currentTest)
        {
            if (isStandaloneCall) _activeUnloadCoroutine = null;
            yield break;
        }
        
        
        SOTest test = currentTest;
        Debug.Log("Unloading... " + test.GetName());
        onTestStartUnloading?.Invoke(test);
        StartCoroutine(currentTheme?.FadeOutRoutine(_audioSource, test.GetTimeToUnload()));
        
        // Play scale-down animation if enabled and MeshScaleSequence exists
        if (_testAnimator && _testAnimator.PlayOnTestUnloading)
        {
            // Give a small delay before playing the animation
            _testAnimator.RefreshForNewEnvironment();
            yield return new WaitForSeconds(0.5f);
            _testAnimator.PlayReverseSequence(test.GetTimeToUnload());
            
            // Wait for animation to complete
            yield return new WaitForSeconds(test.GetTimeToUnload());
        }
        else
        {
            // If not using animations, still wait the unload time
            yield return new WaitForSeconds(test.GetTimeToUnload());
        }
        
        
        Destroy(currentEnvironment);
        if (!currentRobot) currentRobot = null;
        currentTest = null;
        currentEnvironment = null;
        currentCheckpoint = null;
        currentTheme = null;
        ApplyLightSettings(_defaultLightSettings);
        if (_testFloorRenderer) _testFloorRenderer.material = _defaultTestFloorMaterial;
        _activeUnloadCoroutine = null;
        onTestUnloaded?.Invoke(test);
        Debug.Log("Unloaded " + test.GetName());
    }
    
    #endregion Private methods ----------------------------------------------------------------------------
}