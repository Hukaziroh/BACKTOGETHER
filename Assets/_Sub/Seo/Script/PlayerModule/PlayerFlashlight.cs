using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerFlashlight : NetworkBehaviour
{
    private PlayerController controller;

    [Header("손전등 설정")]
    public GameObject flashlightObj;

    [SyncVar(hook = nameof(OnFlashlightToggled))]
    public bool isFlashlightOn = true;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        if (SceneManager.GetActiveScene().name != "chapter5")
        {
            if (flashlightObj != null) flashlightObj.SetActive(false);
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (controller.input.ActionPressedThisFrame)
        {
            if (SceneManager.GetActiveScene().name == "chapter5")
            {
                CmdToggleFlashlight();
            }
        }
    }

    [Command]
    public void CmdToggleFlashlight()
    {
        isFlashlightOn = !isFlashlightOn;
    }

    private void OnFlashlightToggled(bool oldState, bool newState)
    {
        if (flashlightObj != null)
        {
            flashlightObj.SetActive(newState);
        }
    }
}