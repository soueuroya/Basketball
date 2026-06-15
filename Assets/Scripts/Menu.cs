using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] private Button quitButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Slider sensitivitySlider;

    [SerializeField] private GameplayManager gameplayManager;

    private void Awake()
    {
        quitButton.onClick.AddListener(Exit);
        resumeButton.onClick.AddListener(gameplayManager.TogglePause);
        sensitivitySlider.onValueChanged.AddListener(gameplayManager.SetSensitivity);
    }


    public void Exit()
    {
        Application.Quit();
    }    
}
