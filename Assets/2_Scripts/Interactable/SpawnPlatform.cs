using System;
using UnityEngine;
using UnityEngine.Events;

[SelectionBase]
public class SpawnPlatform : MonoBehaviour, ISpawnPoint
{
    [Header("Settings")]
    [SerializeField] private CommandToSend commandToSend = CommandToSend.Nothing;
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private UnityEvent onReachedEvent = new UnityEvent();
    [SerializeField] private UnityEvent onUsedEvent = new UnityEvent();
    public bool HasReached { get; set; }
    private PlayerStateMachine _player;

    private void OnTriggerEnter(Collider other)
    {
        if (!TestManager.Instance || HasReached) return;
        
        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            _player = player;
            SetSpawnPointReached();
            _player?.onPlayerSpawned.AddListener(OnPlayerSpawned);
        }
    }

    private void OnPlayerSpawned(ISpawnPoint spawnPoint)
    {
        if (HasReached && this == (SpawnPlatform)spawnPoint)
        {
            SpawnPointUsed();
        }
    }

    public void SetSpawnPointReached()
    {
        HasReached = true;
        onReachedEvent?.Invoke();
        TestManager.Instance?.SetSpawnPosition(this);
    }

    public void SetSpawnPointNotReached()
    {
        HasReached = false;
        _player?.onPlayerSpawned.RemoveListener(OnPlayerSpawned);
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
