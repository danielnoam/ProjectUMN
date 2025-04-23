using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button backButton;
    
    [Header("References")]
    [SerializeField] private PlayerStateMachine player;
    

    private void Start()
    {
        if (TestManager.Instance)
        {
            backButton.onClick.AddListener(() =>
            {
                player.InMenuState.SelectPage(player.InMenuState.PreviousPage);
            });
        }
    }
}
