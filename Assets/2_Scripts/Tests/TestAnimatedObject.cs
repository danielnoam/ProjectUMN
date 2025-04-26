using UnityEngine;

public class TestAnimatedObject : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private bool affectedByTestAnimations = true;
    public bool AffectedByTestAnimations => affectedByTestAnimations;
}
