using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerFlashlight : NetworkBehaviour
{
    private PlayerController controller;

    [Header("손전등 설정")]
    public GameObject[] flashlightObjs;

    [SyncVar(hook = nameof(OnFlashlightToggled))]
    public bool isFlashlightOn = true;

    public bool canUseFlashlight = true;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        if (SceneManager.GetActiveScene().name != "chapter5")
        {
            if (flashlightObjs != null)
            {
                foreach (GameObject obj in flashlightObjs)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (controller.input.ActionPressedThisFrame)
        {
            if (SceneManager.GetActiveScene().name == "chapter5" && canUseFlashlight)
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

    [Command]
    public void CmdSetFlashlight(bool state)
    {
        isFlashlightOn = state;
    }

    private void OnFlashlightToggled(bool oldState, bool newState)
    {
        if (flashlightObjs != null)
        {
            foreach (GameObject obj in flashlightObjs)
            {
                if (obj != null) obj.SetActive(newState);
            }
        }
    }

    public void SetFlashlightPermission(bool isAllowed)
    {
        if (!isLocalPlayer) return;

        canUseFlashlight = isAllowed;

        if (!canUseFlashlight && isFlashlightOn)
        {
            CmdSetFlashlight(false);
        }
    }

    public void TurnOnFlashlight()
    {
        if (!isLocalPlayer) return;

        if (canUseFlashlight && !isFlashlightOn)
        {
            CmdSetFlashlight(true);
        }
    }
}