using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class ZoneKeyGuideTrigger : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("존 안에 계속 머물러야 하는 시간 (2초)")]
    public float requiredHoldTime = 2.0f;

    [Header("비주얼 설정 (선택)")]
    [Tooltip("진행 상황을 보여줄 프로그레스 바 이미지 (Fill 타입 권장)")]
    public Image progressBarImage;

    [Tooltip("존에 진입했을 때 켜질 바닥 이펙트나 가이드 오브젝트")]
    public GameObject zoneActiveVisual;

    private bool isMyPlayerInside = false;
    private float currentHoldTimer = 0f;
    private bool panelTriggered = false;

    private void Start()
    {
        if (progressBarImage != null) progressBarImage.fillAmount = 0f;
        if (zoneActiveVisual != null) zoneActiveVisual.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (panelTriggered) return;

        if (other.CompareTag("Player"))
        {
            NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
            if (netId != null && netId.isLocalPlayer)
            {
                isMyPlayerInside = true;
                if (zoneActiveVisual != null) zoneActiveVisual.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (panelTriggered) return;

        if (other.CompareTag("Player"))
        {
            NetworkIdentity netId = other.GetComponent<NetworkIdentity>();
            if (netId != null && netId.isLocalPlayer)
            {
                ResetZoneState();
            }
        }
    }

    private void Update()
    {
        if (!isMyPlayerInside || panelTriggered) return;

        if (OptionsManager.instance != null &&
            OptionsManager.instance.IsAnyKeyGuideActive())
        {
            return;
        }

        currentHoldTimer += Time.deltaTime;

        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = currentHoldTimer / requiredHoldTime;
        }

        if (currentHoldTimer >= requiredHoldTime)
        {
            OpenKeyGuideOnly();
        }
    }

    private void OpenKeyGuideOnly()
    {
        panelTriggered = true;

        if (OptionsManager.instance != null &&
            OptionsManager.instance.allKeyGuidePanels != null &&
            OptionsManager.instance.allKeyGuidePanels.Length > 0 &&
            OptionsManager.instance.allKeyGuidePanels[0] != null)
        {
            // 존 트리거로 열었음을 활성화
            OptionsManager.instance.isOpenedFromZone = true;

            if (OptionsManager.instance.volumePanel != null) OptionsManager.instance.volumePanel.SetActive(false);
            if (OptionsManager.instance.optionsPanel != null) OptionsManager.instance.optionsPanel.SetActive(false);

            // 기본 키 가이드 패널(배열 첫 번째, 예: KeyboardPanel) 활성화
            OptionsManager.instance.allKeyGuidePanels[0].SetActive(true);

            if (GlobalSceneInputManager.Instance != null)
            {
                GlobalSceneInputManager.Instance.SetFocusScope(OptionsManager.instance.allKeyGuidePanels[0]);
            }
        }

        ResetZoneState();

        StartCoroutine(MonitorKeyGuideCloseRoutine());
    }

    private IEnumerator MonitorKeyGuideCloseRoutine()
    {
        while (OptionsManager.instance != null &&
               OptionsManager.instance.IsAnyKeyGuideActive())
        {
            yield return null;
        }

        if (OptionsManager.instance != null)
        {
            OptionsManager.instance.isOpenedFromZone = false;
        }

        if (GlobalSceneInputManager.Instance != null)
        {
            GlobalSceneInputManager.Instance.ClearFocusScope();
        }

        panelTriggered = false;
    }

    private void ResetZoneState()
    {
        isMyPlayerInside = false;
        currentHoldTimer = 0f;
        if (progressBarImage != null) progressBarImage.fillAmount = 0f;
        if (zoneActiveVisual != null) zoneActiveVisual.SetActive(false);
    }
}