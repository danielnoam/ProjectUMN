using System;
using System.Collections;
using PrimeTween;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using VInspector;

[SelectionBase]
[DefaultExecutionOrder(-5)]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(TestEnvironmentAnimator))]
[RequireComponent(typeof(TestEffectsHandler))]
public class TestManager : MonoBehaviour
{
    public static TestManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private PlayerStateMachine playerPrefab;
    [SerializeField] private RobotCompanion robotPrefab;
    [SerializeField] private SOTest defaultTest;
    [SerializeField] private SOAudioManager audioManager;

    [Header("Intro Sequence")] 
    [SerializeField, Min(0)] private float introSequenceDuration = 10f;
    [SerializeField] private SOTest introTest;
        
    [Header("Credits Sequence")] 
    [SerializeField, Min(0)] private float creditsSequenceDuration = 40f;
    [SerializeField] private SOTest creditsTest;
    [SerializeField] private SOAudioEvent robotSfx;
    [Space(10)]
    
    [SerializeField] private SOTest[] tests;
    
    
    [Foldout("Debug")]
    [SerializeField] private bool debugMode;
    [SerializeField] private TextMeshProUGUI debugTextRight;
    [SerializeField] private TextMeshProUGUI debugTextLeft;
    [SerializeField, ReadOnly] private PlayerStateMachine currentPlayer;
    [SerializeField, ReadOnly] private RobotCompanion currentRobot;
    [SerializeField, ReadOnly] private SOTest currentTest;
    [SerializeField, ReadOnly] private GameObject currentEnvironment;
    [SerializeField, ReadOnly] private ISpawnPoint currentSpawnPoint;
    [SerializeField, ReadOnly] private SOAudioEvent currentTheme;
    [SerializeField, ReadOnly] private TestLightSettings currentLightSettings;
    [EndFoldout]
    
    [Foldout("Events")]
    public UnityEvent onIntroSequenceStart = new UnityEvent();
    public UnityEvent onIntroSequenceEnd = new UnityEvent();
    public UnityEvent onCreditsSequenceStart = new UnityEvent();
    public UnityEvent onCreditsSequenceEnd = new UnityEvent();
    public UnityEvent<SOTest> onTestStartLoading = new UnityEvent<SOTest>();
    public UnityEvent<SOTest> onTestLoaded = new UnityEvent<SOTest>();
    public UnityEvent<SOTest> onTestStartUnloading = new UnityEvent<SOTest>();
    public UnityEvent<SOTest> onTestUnloaded = new UnityEvent<SOTest>();
    [EndFoldout]
    
    
    public bool DebugMode => debugMode;
    public TestLightSettings DefaultLightSettings => defaultTest.GetLightSettings();
    public GameObject DefaultEnvironment => defaultTest.GetPrefab();
    public PlayerStateMachine Player => currentPlayer;
    public RobotCompanion Robot => currentRobot;
    public SOTest CurrentTest => currentTest;
    public SOTest IntroTest => introTest;
    public SOTest CreditsTest => creditsTest;
    public SOTest DefaultTest => defaultTest;
    
    public SOAudioEvent CurrentTheme => currentTheme;
    public ISpawnPoint CurrentSpawnPoint {
        get
        {
            if (currentSpawnPoint != null) return currentSpawnPoint;
            if (currentTest) currentTest.GetPlayerStartPoint();
            return null;
        }
    }

    public ISpawnPoint CurrentStartPoint => currentTest.GetPlayerStartPoint();
    public TextMeshProUGUI DebugTextLeft => debugTextLeft;
    public TextMeshProUGUI DebugTextRight => debugTextRight;
    public float IntroSequenceDuration => introSequenceDuration;
    public float IntroSequenceState => _introSequenceTime / introSequenceDuration;
    public bool IsIntroSequenceActive => _introSequenceTime > 0;
    public float CreditsSequenceDuration => creditsSequenceDuration;
    public float CreditsSequenceState => _creditsSequenceTime / creditsSequenceDuration;
    public bool IsCreditsSequenceActive => _creditsSequenceTime > 0;
    public float PlayerVersion => _gameCompleted + _playerDeaths;
    
    private Coroutine _activeLoadCoroutine;
    private Coroutine _activeUnloadCoroutine;
    private Coroutine _activeSequenceCoroutine;
    private AudioSource _audioSource;
    private TestEnvironmentAnimator _testEnvironmentAnimator;
    private TestEffectsHandler _testEffectsHandler;
    private float _introSequenceTime;
    private float _creditsSequenceTime;
    private float _playerDeaths;
    private int _gameCompleted;
    
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
        

        PrimeTweenConfig.SetTweensCapacity(800);
        _testEnvironmentAnimator = GetComponent<TestEnvironmentAnimator>();
        _testEffectsHandler = GetComponent<TestEffectsHandler>();
        _audioSource = GetComponent<AudioSource>();

        

        if (SaveManager.HasKey("playerDeaths"))
        {
            _playerDeaths = SaveManager.LoadFloat("playerDeaths", 0.0001f);
        }
        
        if (SaveManager.HasKey("gameCompleted"))
        {
            _gameCompleted = SaveManager.LoadInt("gameCompleted", 0);
        }
    }
    
    private void Start()
    {
        audioManager.LoadAllVolumes();
        
        currentPlayer = PlayerStateMachine.Instance;
        SubscribeToPlayerEvents();
        currentRobot = FindFirstObjectByType<RobotCompanion>();
        

        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            StartIntroSequence();
        }
        else if (SceneManager.GetActiveScene().buildIndex == 1 && !debugMode)
        {
            StartClearTestSequence();
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
            StartClearTestSequence();
        }
        
        if (Input.GetKeyDown(KeyCode.Keypad9))
        {
            ToggleDebugMode();
        }
        
        if (IsCreditsSequenceActive && Input.GetKeyDown(KeyCode.Escape))
        {
            StopCoroutine(CreditsSequenceCoroutine());
            _creditsSequenceTime = 0;
            onCreditsSequenceEnd?.Invoke();
            StartIntroSequence();
        }
    }


    private void OnEnable()
    {
        SubscribeToPlayerEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromPlayerEvents();
    }

    #region Test control ----------------------------------------------------------------------------


    public void StartClearTestSequence()
    {
        StartCoroutine(ClearTestSequence());
    }
    
    private IEnumerator ClearTestSequence()
    {
        if (currentTest)
        {
            yield return UnLoadTest();
        }
        yield return LoadTest(defaultTest);
    }
    
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
    
    
    public void QuitApplication()
    {
        _testEffectsHandler.FadeScreen(true, 0.5f);
        StartCoroutine(QuitAppEnumerator());
    }
    
    private IEnumerator QuitAppEnumerator() 
    {
        yield return new WaitForSeconds(0.5f);
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
    
    public void SetSpawnPosition(ISpawnPoint spawnPoint)
    {
        currentSpawnPoint?.SetSpawnPointNotReached();
        currentSpawnPoint = spawnPoint;
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
    
    
    #region Intro Sequence ----------------------------------------------------------------------------

    
    [Button]
    public void StartIntroSequence()
    {
        StartCoroutine(IntroSequenceCoroutine());
    }
    
    private IEnumerator IntroSequenceCoroutine()
    {
        _testEffectsHandler.FadeScreen(true, 0f);
        
        if (currentTest) 
        {
            yield return UnLoadTest();
        }
        
        yield return LoadTest(introTest);
        
        _introSequenceTime = introSequenceDuration;
        onIntroSequenceStart?.Invoke();
        
        while (_introSequenceTime > 0 )
        {
            _introSequenceTime -= Time.deltaTime;
            yield return null; 
        }
        
        onIntroSequenceEnd?.Invoke();
    }

    #endregion Intro Sequence ----------------------------------------------------------------------------
    
    
    #region Credits Sequence ----------------------------------------------------------------------------
    
    [Button]
    public void StartCreditsSequence()
    {
        StartCoroutine(CreditsSequenceCoroutine());
    }
    
    private IEnumerator CreditsSequenceCoroutine()
    {
        bool fadeOutTimeReached = false;
        float fadeOutTime = creditsSequenceDuration * 0.1f;
        
        if (currentTest) 
        {
            _testEffectsHandler.FadeScreen(true, 2f);
            yield return UnLoadTest();
        }
        
        StartCoroutine(LoadTest(creditsTest));
        _creditsSequenceTime = creditsSequenceDuration;
        onCreditsSequenceStart?.Invoke();
        _testEffectsHandler.FadeScreen(false, 1f);
        
        
        
        while (_creditsSequenceTime > 0)
        {
            // Check if we've reached 80% of the sequence
            if (_creditsSequenceTime <= fadeOutTime && !fadeOutTimeReached)
            {
                fadeOutTimeReached = true;
                // fade out the screen with the left time
                _testEffectsHandler.FadeScreen(true, fadeOutTime * 1.5f);
                robotSfx?.PlayAtPoint();
            }
            
            
            _creditsSequenceTime -= Time.deltaTime;
            yield return null; 
        }
        
        onCreditsSequenceEnd?.Invoke();
        StartIntroSequence();
    }



    #endregion Credits Sequence ----------------------------------------------------------------------------

    
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
                _activeLoadCoroutine = StartCoroutine(LoadTest(tests[testIndex], false));
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
                _activeLoadCoroutine = StartCoroutine(LoadTest(tests[testIndex], false));
                yield return _activeLoadCoroutine;
            }
        }
        
        // Clear sequence reference when done
        _activeSequenceCoroutine = null;
    }
    

    private IEnumerator LoadTest(SOTest testIndex, bool isStandaloneCall = true)
    {
        if (testIndex == null)
        {
            if (isStandaloneCall) _activeLoadCoroutine = null;
            yield break;
        }
        
        Debug.Log("Loading... " + testIndex.Name);
        
        currentTest = testIndex;
        currentTheme = currentTest.GetTheme();
        ApplyLightSettings(currentTest.GetLightSettings());
        currentEnvironment = Instantiate(currentTest.GetPrefab());
        currentEnvironment.name = currentTest.Name + " Environment";

        if (currentTest.NeedsPlayer)
        {
            if (!currentPlayer)
            {
                ISpawnPoint spawnPoint = currentTest.GetPlayerStartPoint();
                currentPlayer = Instantiate(playerPrefab, spawnPoint.GetSpawnPosition(), spawnPoint.GetSpawnRotation());
                SubscribeToPlayerEvents();
            }
        }
        else
        {
            UnsubscribeFromPlayerEvents();
            if (currentPlayer) Destroy(currentPlayer.gameObject);
            currentPlayer = null;
        }
        
        if (currentTest.NeedsRobot)
        {
            if (currentTest.HasRobot()) // The new test has a robot in it
            {
                if (currentRobot) Destroy(currentRobot.gameObject);
                currentRobot = null;
                currentRobot = FindFirstObjectByType<RobotCompanion>();
            
            } else if (!currentRobot && robotPrefab) // The new test has no robot and there is no robot in the scene
            {
                currentRobot = Instantiate(robotPrefab, currentTest.GetRobotSpawnPoint(), quaternion.identity);
                currentRobot.TurnOn();
            }
        }
        else
        {
            if (currentRobot) Destroy(currentRobot.gameObject);
            currentRobot = null;
        }

        onTestStartLoading?.Invoke(currentTest);
        
        // Play scale-up animation 
        if (_testEnvironmentAnimator && _testEnvironmentAnimator.PlayOnTestLoading)
        {
            
            // Give a small delay before playing the animation
            _testEnvironmentAnimator.RefreshForNewEnvironment();
            _testEnvironmentAnimator.SetAllObjectsToZeroScale(true);
            yield return new WaitForSeconds(0.5f);
            _testEnvironmentAnimator.PlayLoadSequence(currentTest.GetTimeToLoad());
            
            
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
        Debug.Log("Loaded " + currentTest.Name);
        
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
        Debug.Log("Unloading... " + test.Name);
        onTestStartUnloading?.Invoke(test);
        if (currentTheme) StartCoroutine(currentTheme?.FadeOutRoutine(_audioSource, test.GetTimeToUnload()));
        
        // Play scale-down animation if enabled and MeshScaleSequence exists
        if (_testEnvironmentAnimator && _testEnvironmentAnimator.PlayOnTestUnloading)
        {
            // Give a small delay before playing the animation
            _testEnvironmentAnimator.RefreshForNewEnvironment();
            yield return new WaitForSeconds(0.5f);
            _testEnvironmentAnimator.PlayUnLoadSequence(test.GetTimeToUnload());
            
            // Wait for animation to complete
            yield return new WaitForSeconds(test.GetTimeToUnload());
        }
        else
        {
            // If not using animations, still wait the unload time
            yield return new WaitForSeconds(test.GetTimeToUnload());
        }
        
        
        Destroy(currentEnvironment);
        if (currentRobot) Destroy(currentRobot.gameObject);
        currentTest = null;
        currentEnvironment = null;
        currentSpawnPoint = null;
        currentTheme = null;
        ApplyLightSettings(defaultTest.GetLightSettings());
        _activeUnloadCoroutine = null;
        onTestUnloaded?.Invoke(test);
        Debug.Log("Unloaded " + test.Name);
    }
    
    #endregion Private methods ----------------------------------------------------------------------------


    #region Version ----------------------------------------------------------------------------

    private void OnPlayerDeath()
    {
        _playerDeaths += 0.0001f;
        SaveManager.SaveFloat("playerDeaths", _playerDeaths);
    }
    
    public void OnGameCompleted()
    {
        _gameCompleted += 1;
        SaveManager.SaveInt("gameCompleted", _gameCompleted);
    }
    
    private void SubscribeToPlayerEvents()
    {
        currentPlayer?.onPlayerDeath.AddListener(OnPlayerDeath);
    }

    private void UnsubscribeFromPlayerEvents()
    {
        currentPlayer?.onPlayerDeath.RemoveListener(OnPlayerDeath);
    }

    #endregion Version ----------------------------------------------------------------------------
}