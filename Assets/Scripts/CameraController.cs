using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Mouse Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;
    
    [Header("References")]
    [SerializeField] private Transform playerBody;

    private Rigidbody playerRigidbody;
    private float xRotation;
    private float yRotation;

    private void Start()
    {
        if (playerBody == null)
        {
            playerBody = transform.parent;
        }

        playerRigidbody = playerBody.GetComponent<Rigidbody>();
        xRotation = NormalizeAngle(transform.localEulerAngles.x);
        yRotation = playerBody.eulerAngles.y;

#if !UNITY_WEBGL
        LockCursor();
#else
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#endif
    }

    private void Update()
    {
        if (GameplayManager.IsPaused)
        {
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }

            return;
        }

        HandleMouseLook();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        Quaternion bodyRotation = Quaternion.Euler(0f, yRotation, 0f);

        if (playerRigidbody != null)
        {
            playerRigidbody.rotation = bodyRotation;
        }
        else
        {
            playerBody.rotation = bodyRotation;
        }

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
}
