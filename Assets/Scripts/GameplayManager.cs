using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenu;

    [Header("Optional References")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private FirstPersonPlayerController playerController;
    [SerializeField] private PlayerBallHandler ballHandler;

    private float timeScaleBeforePause = 1f;
    private float fixedDeltaTimeBeforePause;

    public static bool IsPaused { get; private set; }

    private void Awake()
    {
        fixedDeltaTimeBeforePause = Time.fixedDeltaTime;
        SetPaused(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void SetSensitivity(float sensitivity)
    {
        cameraController.SetMouseSensitivity(sensitivity);
    }

    public void Pause()
    {
        SetPaused(true);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    private void SetPaused(bool paused)
    {
        if (paused == IsPaused && pauseMenu != null &&
            pauseMenu.activeSelf == paused)
        {
            return;
        }

        IsPaused = paused;

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(paused);
        }

        if (paused)
        {
            timeScaleBeforePause = Mathf.Max(Time.timeScale, 0.01f);
            fixedDeltaTimeBeforePause = Time.fixedDeltaTime;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = timeScaleBeforePause;
            Time.fixedDeltaTime = fixedDeltaTimeBeforePause;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDestroy()
    {
        if (IsPaused)
        {
            IsPaused = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = fixedDeltaTimeBeforePause;
        }
    }
}
