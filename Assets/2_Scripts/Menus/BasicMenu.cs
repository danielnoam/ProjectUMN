using UnityEngine;
using UnityEngine.UI;

public class BasicMenu : MonoBehaviour
{
    [SerializeField] private Button nextTestButton;
    [SerializeField] private Button quitButton;
    
    private void Start()
    { 
        if (!TestManager.Instance) return;
        
        nextTestButton.onClick.AddListener(TestManager.Instance.LoadNextTest);
        quitButton.onClick.AddListener(TestManager.Instance.QuitApplication);
    }
}
