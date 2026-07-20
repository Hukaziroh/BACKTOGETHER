using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class HoldButtonForLand : NetworkBehaviour
{
    [Header("설정")]
    [Tooltip("버튼을 계속 밟고 있어야 하는 시간 (5초)")]
    public float requiredHoldTime = 5.0f;

    [Header("연결 설정")]
    [Tooltip("이 버튼을 누르면 사라질 특정 땅 오브젝트의 LandNetworkSync")]
    public LandNetworkSync targetLand;

    [Header("버튼 비주얼 설정")]
    public GameObject unpressedVisual; // 안 밟았을 때 이미지
    public GameObject pressedVisual;   // 밟고 있을 때 이미지

    [Header("선택: 진행바 이미지 (Fill 타입 권장)")]
    public Image progressBarImage;

    private HashSet<GameObject> playersOnButton = new HashSet<GameObject>();

    [SyncVar(hook = nameof(OnButtonStateChanged))]
    private bool isPressed = false;

    private float serverHoldTimer = 0f;
    private float clientHoldTimer = 0f;
    private bool actionTriggered = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        UpdateVisual(isPressed);
        if (progressBarImage != null) progressBarImage.fillAmount = 0f;
    }

    [ServerCallback]
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (actionTriggered) return;
        if (other.CompareTag("Player"))
        {
            playersOnButton.Add(other.gameObject);
            EvaluateButtonState();
        }
    }

    [ServerCallback]
    private void OnTriggerExit2D(Collider2D other)
    {
        if (actionTriggered) return;
        if (other.CompareTag("Player"))
        {
            playersOnButton.Remove(other.gameObject);
            EvaluateButtonState();
        }
    }

    [Server]
    private void EvaluateButtonState()
    {
        playersOnButton.RemoveWhere(go => go == null || !go.activeInHierarchy);
        bool currentlyPressed = (playersOnButton.Count > 0);

        if (isPressed != currentlyPressed)
        {
            isPressed = currentlyPressed;
            if (!isPressed)
            {
                serverHoldTimer = 0f;
            }
        }
    }

    private void Update()
    {
        // 1. 클라이언트 측: 밟고 있는 동안 게이지바 부드럽게 채우기
        if (isPressed && !actionTriggered)
        {
            clientHoldTimer += Time.deltaTime;
            if (progressBarImage != null)
            {
                progressBarImage.fillAmount = clientHoldTimer / requiredHoldTime;
            }
        }
        else if (!isPressed && !actionTriggered)
        {
            clientHoldTimer = 0f;
            if (progressBarImage != null)
            {
                progressBarImage.fillAmount = 0f;
            }
        }

        // 2. 서버 측: 5초 유지 시간 계산 및 실행
        if (!NetworkServer.active || actionTriggered) return;

        if (isPressed)
        {
            playersOnButton.RemoveWhere(go => go == null || !go.activeInHierarchy);
            if (playersOnButton.Count == 0)
            {
                isPressed = false;
                serverHoldTimer = 0f;
                return;
            }

            serverHoldTimer += Time.deltaTime;

            // 5초를 다 채웠을 때
            if (serverHoldTimer >= requiredHoldTime)
            {
                actionTriggered = true;

                if (targetLand != null)
                {
                    targetLand.CmdDisableLand();
                }
                else
                {
                    Debug.LogWarning("[버튼 시스템] 경고: 버튼에 연결된 Target Land가 없습니다!", this);
                }

                RpcDisableButton();
            }
        }
    }

    private void OnButtonStateChanged(bool oldState, bool newState)
    {
        UpdateVisual(newState);
        if (!newState)
        {
            clientHoldTimer = 0f;
        }
    }

    private void UpdateVisual(bool pressed)
    {
        if (unpressedVisual != null) unpressedVisual.SetActive(!pressed);
        if (pressedVisual != null) pressedVisual.SetActive(pressed);
    }

    [ClientRpc]
    private void RpcDisableButton()
    {
        gameObject.SetActive(false);
    }
}