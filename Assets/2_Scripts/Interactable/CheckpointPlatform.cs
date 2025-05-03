using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VInspector;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class CheckpointPlatform : MonoBehaviour, ISpawnPoint
{
    [Header("Settings")]
    [SerializeField] private CommandToSend commandToSend = CommandToSend.Nothing;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private UnityEvent onReachedEvent = new UnityEvent();
    [SerializeField] private UnityEvent onUsedEvent = new UnityEvent();
    public bool HasReached { get; set; }
    private PlayerStateMachine _player;

    private void Awake()
    {
        TurnOff();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!TestManager.Instance|| HasReached) return;
        
        
        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            _player = player;
            SetSpawnPointReached();
            _player?.onPlayerSpawnedFromCheckpoint.AddListener(OnPlayerSpawned);
        }
    }

    private void OnPlayerSpawned(ISpawnPoint spawnPoint)
    {
        if (HasReached && this == (CheckpointPlatform)spawnPoint)
        {
            SpawnPointUsed();
        }
    }


    public void SetSpawnPointReached()
    {
        TurnOn();
        TestManager.Instance?.SetSpawnPosition(this);
    }

    public void SetSpawnPointNotReached()
    {
        TurnOff();
    }

    public void SpawnPointUsed()
    {
        SendRobotCommand();
        onUsedEvent?.Invoke();
    }

    public Vector3 GetSpawnPosition()
    {
        return spawnPosition.position;
    }

    public Quaternion GetSpawnRotation()
    {
        return spawnPosition.rotation;
    }

    private void TurnOn()
    {
        HasReached = true;
        
        onReachedEvent?.Invoke();
        
        if (audioSource) audioSource.Play();
        
        if (particleSystems.Length > 0)
        {
            
            foreach (var particle in particleSystems)
            {
                particle.Play();
            }
        }
    }

    private void TurnOff()
    {
        HasReached = false;
        _player?.onPlayerSpawnedFromCheckpoint.RemoveListener(OnPlayerSpawned);
        
        
        if (audioSource) audioSource.Stop();
        
        if (particleSystems.Length > 0)
        {
            
            foreach (var particle in particleSystems)
            {
                particle.Stop();
            }
        }
    }
    
    private void SendRobotCommand()
    {
        if (TestManager.Instance && TestManager.Instance.Robot)
        {
            switch (commandToSend)
            {
                case CommandToSend.Idle:
                    TestManager.Instance.Robot.CommandIdle();
                    break;
                case CommandToSend.Follow:
                    TestManager.Instance.Robot.CommandFollowPlayer();
                    break;
                case CommandToSend.Sit:
                    TestManager.Instance.Robot.CommandSitDown();
                    break;
                case CommandToSend.Nothing:
                    break;
                case CommandToSend.Respawn:
                    TestManager.Instance.Robot.Respawn();
                    break;
                default:
                    Debug.Log($"Command {commandToSend} not recognized. No action taken.");
                    break;
            }
        }
    }

}
