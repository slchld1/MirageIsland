using UnityEditor.Build;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{

    public GameObject menuCanvas;

    public InputActionReference toggleUIAction;


    private void OnEnable()
    {
        toggleUIAction.action.Enable();
        toggleUIAction.action.performed += ToggleUI;
    }
    private void OnDisable()
    {
        toggleUIAction.action.performed -= ToggleUI;
        toggleUIAction.action.Disable();

    }
    private void Awake()
    {
        menuCanvas.SetActive(false);
    }

    private void ToggleUI(InputAction.CallbackContext context)
    {
        if(!menuCanvas.activeSelf && PauseController.IsGamePaused)
        {
            return;
        }

        menuCanvas.SetActive(!menuCanvas.activeSelf);
        PauseController.SetPause(menuCanvas.activeSelf);
    }
}
