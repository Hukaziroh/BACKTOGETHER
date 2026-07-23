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

    // 💡 [리팩토링]: 매 프레임 문자열 생성을 막는 씬 캐싱 변수
    private bool isChapter5Scene = false;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // 💡 시작 시점에 한 번만 씬 검사 수행
        isChapter5Scene = (SceneManager.GetActiveScene().name == "chapter5");

        if (!isChapter5Scene)
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

    // 💡 [리팩토링]: PlayerController에서 순서대로 호출
    public void CustomUpdate()
    {
        if (controller != null && controller.input != null && controller.input.ActionPressedThisFrame)
        {
            if (isChapter5Scene && canUseFlashlight)
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