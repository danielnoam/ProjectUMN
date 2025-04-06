using System;
using UnityEngine;

[SelectionBase]
[RequireComponent(typeof(AudioSource))]
public class CheckpointPlatform : MonoBehaviour
{
    [SerializeField] private Transform spawnPosition;
    [SerializeField] private ParticleSystem[] particleSystems;
    private bool _hasReached;
    private TestManager _testManager;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        TurnOff();
    }

    private void Start()
    {
        _testManager = TestManager.Instance;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_hasReached) return;
        
        if (!_testManager)
        {
            Debug.LogError("TestManager is null");
            return;
        }
        
        
        if (other.TryGetComponent(out PlayerStateMachine player))
        {
            TurnOn();
            _testManager.SetCheckpointPosition(spawnPosition);
        }
    }
    
    
    private void TurnOn()
    {
        _hasReached = true;
        if (_audioSource) _audioSource.Play();
        
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
        _hasReached = false;
        
        if (_audioSource) _audioSource.Stop();
        
        if (particleSystems.Length > 0)
        {
            
            foreach (var particle in particleSystems)
            {
                particle.Stop();
            }
        }
    }
}
