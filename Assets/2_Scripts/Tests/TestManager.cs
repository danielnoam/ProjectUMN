using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class TestManager : MonoBehaviour
{
    public static TestManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private SOTest[] tests;
    
    [Header("Current test")]
    [SerializeField, ReadOnly] private GameObject playerPrefab;
    [SerializeField, ReadOnly] private GameObject robotPrefab;
    [SerializeField, ReadOnly] private SOTest currentTest;
    [SerializeField, ReadOnly] private GameObject currentEnvironment;
    [SerializeField, ReadOnly] private PlayerStateMachine currentPlayer;
    [SerializeField, ReadOnly] private RobotCompanion currentRobot;
    [SerializeField, ReadOnly] private Transform currentCheckpoint;
    [SerializeField, ReadOnly] private SOAudioEvent currentTheme;

    [Header("Events")]
    public UnityEvent<SOTest> onTestLoaded = new UnityEvent<SOTest>();
    public UnityEvent<SOTest> onTestUnloaded = new UnityEvent<SOTest>();
    
    
    
    public bool DebugMode => debugMode;
    public PlayerStateMachine Player => currentPlayer;
    public RobotCompanion Robot => currentRobot;
    public SOTest CurrentTest => currentTest;
    public SOAudioEvent CurrentTheme => currentTheme;
    
    
    private Coroutine _activeLoadCoroutine;
    private Coroutine _activeUnloadCoroutine;
    private Coroutine _activeSequenceCoroutine;
    private  AudioSource _audioSource;
    
    
    
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
        
        _audioSource = GetComponent<AudioSource>();
    }
    
    private void Start()
    {
        currentPlayer = FindFirstObjectByType<PlayerStateMachine>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggleDebugMode();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartTest(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            StartTest(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartTest(2);
        }
    }
    

    #region Public methods ----------------------------------------------------------------------------

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
    public void QuitApplication()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

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

    public void ToggleDebugMode()
    {
        debugMode = !debugMode;
    }
    
    public void SetCheckpointPosition(Transform checkpoint)
    {
        currentCheckpoint = checkpoint;
    }

    public Vector3 GetCheckPoint()
    {
        if (!currentCheckpoint) return Vector3.zero;
        return GetSpawnPoint();
    }

    public Vector3 GetSpawnPoint()
    {
        if (!currentTest) return Vector3.zero;
        return currentTest.GetPlayerSpawnPoint();
    }
    
    
    

    
    #endregion Public methods ----------------------------------------------------------------------------
    
    
    
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
        // Early exit if invalid test index
        if (tests.Length <= 0 || testIndex >= tests.Length)
        {
            if (isStandaloneCall) _activeLoadCoroutine = null;
            yield break;
        }
        
        Debug.Log("Loading... " + tests[testIndex].GetName());
        yield return new WaitForSeconds(tests[testIndex].GetTimeToLoad());
        Debug.Log("Loaded " + tests[testIndex].GetName());
        currentTest = tests[testIndex];
        currentEnvironment = Instantiate(currentTest.GetPrefab());
        currentTheme = currentTest.GetTheme();
        currentTheme?.Play(_audioSource);

        if (currentTest.HasRobot()) // The new test has a robot in it
        {
            if (currentRobot) Destroy(currentRobot.gameObject);
            currentRobot = FindFirstObjectByType<RobotCompanion>();
            
        } else if (!currentRobot && robotPrefab) // The new test has no robot and there is no robot in the scene
        {
            GameObject newRobot = Instantiate(robotPrefab);
            currentRobot = newRobot.GetComponent<RobotCompanion>();
            currentRobot.TurnOn();
        }
        
        onTestLoaded.Invoke(currentTest);
        _activeLoadCoroutine = null;
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
        yield return new WaitForSeconds(test.GetTimeToUnload());
        Debug.Log("Unloaded " + test.GetName());
        Destroy(currentEnvironment);
        currentTest = null;
        currentEnvironment = null;
        currentCheckpoint = null;
        onTestUnloaded.Invoke(test);
        
        // Clear unload coroutine reference
        _activeUnloadCoroutine = null;
    }
    

    #endregion Private methods ----------------------------------------------------------------------------
}