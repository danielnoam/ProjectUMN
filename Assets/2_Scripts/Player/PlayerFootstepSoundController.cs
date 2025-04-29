using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerFootstepSoundController : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private float raycastDistance = 0.04f;
    [SerializeField] private LayerMask groundLayer;
    


    [Header("Sound Settings")]
    [SerializeField] private SOAudioEvent footstepLightSfx;
    [SerializeField] private SOAudioEvent footstepHeavySfx;
    [SerializeField] private float stepFrequency = 0.2f;

    
    private PlayerStateMachine _player;
    private bool _leftFootOnGround = false;
    private bool _rightFootOnGround = false;
    private float _lastStepTime = 0f;
    private bool CanPlaySound => _player && _player.IsGrounded;
    private bool IsRunning => _player && _player.ActiveHorizontalVelocity > 5f;

    private void Awake()
    {
        _player = GetComponent<PlayerStateMachine>();
    }

    private void Update()
    {
        CheckFootsteps();
    }
    
    private void CheckFootsteps()
    {
        bool wasLeftFootOnGround = _leftFootOnGround;
        bool wasRightFootOnGround = _rightFootOnGround;
        
        // Cast rays from both feet
        _leftFootOnGround = Physics.Raycast(leftFoot.position, Vector3.down, raycastDistance, groundLayer);
        _rightFootOnGround = Physics.Raycast(rightFoot.position, Vector3.down, raycastDistance, groundLayer);
        
        
        // Check if either foot has just touched the ground
        if (Time.time - _lastStepTime >= stepFrequency && CanPlaySound)
        {
            if (!wasLeftFootOnGround && _leftFootOnGround)
            {
                PlayFootstepSound(leftFoot.position);
                _lastStepTime = Time.time;
            }
            else if (!wasRightFootOnGround && _rightFootOnGround)
            {
                PlayFootstepSound(rightFoot.position);
                _lastStepTime = Time.time;
            }
        }
    }
    
    private void PlayFootstepSound(Vector3 position)
    {
        if (IsRunning)
            footstepHeavySfx?.PlayAtPoint(position);
        else
            footstepLightSfx?.PlayAtPoint(position);
    }

    private void OnDrawGizmosSelected()
    {
        if (!leftFoot|| !rightFoot) return;
        
        Debug.DrawRay(leftFoot.position, Vector3.down * raycastDistance, _leftFootOnGround ? Color.green : Color.red);
        Debug.DrawRay(rightFoot.position, Vector3.down * raycastDistance, _rightFootOnGround ? Color.green : Color.red);
    }
}
