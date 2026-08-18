using System.Collections;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NoCheckpointButtonController : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private Button noCheckpointButton;
    [SerializeField] private GameObject noCheckpointCheckImage; // 체크 표시용 이미지 오브젝트

    [Header("씬 설정")]
    [SerializeField] private string defaultLobbySceneName = "Lobby";
    [SerializeField] private string noCheckpointLobbySceneName = "NLobby";

    private bool isChecked = false;
    private EOSLobby cachedLobby;

    private void Start()
    {
        isChecked = false;
        UpdateVisuals();
        StartCoroutine(InitEOSLobbyRoutine());
    }

    private void Update()
    {
        // 1. 챕터 6 클리어 여부에 따른 버튼 활성화/비활성화
        if (GameSaveManager.Instance != null && noCheckpointButton != null)
        {
            int maxCleared = GameSaveManager.Instance.currentData.maxClearedChapter;
            bool canUse = (maxCleared >= 5);
            noCheckpointButton.gameObject.SetActive(canUse);

            if (!canUse && isChecked)
            {
                isChecked = false;
                UpdateVisuals();
                ApplySceneChange();
            }
        }

        // 2. 키보드 또는 패드로 해당 버튼이 선택(포커스)된 상태에서의 입력 감지
        if (noCheckpointButton != null && noCheckpointButton.gameObject.activeInHierarchy)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == noCheckpointButton.gameObject)
            {
                bool actionPressed = false;

                // 키보드 입력 (엔터, 스페이스)
                if (Keyboard.current != null)
                {
                    actionPressed |= Keyboard.current.enterKey.wasPressedThisFrame ||
                                     Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                     Keyboard.current.spaceKey.wasPressedThisFrame;
                }

                // 게임패드 입력 (패드 A버튼 / 결정 버튼)
                if (Gamepad.current != null)
                {
                    actionPressed |= Gamepad.current.buttonSouth.wasPressedThisFrame ||
                                     Gamepad.current.buttonEast.wasPressedThisFrame;
                }

                if (actionPressed)
                {
                    ToggleNoCheckpoint();
                }
            }
        }
    }

    private IEnumerator InitEOSLobbyRoutine()
    {
        while (NetworkManager.singleton == null || NetworkManager.singleton.GetComponent<EOSLobby>() == null)
        {
            yield return null;
        }

        cachedLobby = NetworkManager.singleton.GetComponent<EOSLobby>();
        cachedLobby.CreateLobbySucceeded += OnCreateLobbySucceeded;
    }

    private void OnDestroy()
    {
        if (cachedLobby != null)
        {
            cachedLobby.CreateLobbySucceeded -= OnCreateLobbySucceeded;
        }
    }

    // 버튼 클릭, 키보드 엔터, 게임패드 결정 버튼 입력 시 공통으로 실행
    public void ToggleNoCheckpoint()
    {
        isChecked = !isChecked;
        UpdateVisuals();
        ApplySceneChange();
    }

    private void UpdateVisuals()
    {
        if (noCheckpointCheckImage != null)
        {
            noCheckpointCheckImage.SetActive(isChecked);
        }
    }

    private void ApplySceneChange()
    {
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.onlineScene = isChecked ? noCheckpointLobbySceneName : defaultLobbySceneName;
        }
    }

    private void OnCreateLobbySucceeded(List<Epic.OnlineServices.Lobby.Attribute> attributes)
    {
        if (cachedLobby != null)
        {
            AttributeData[] attrData = new AttributeData[]
            {
                new AttributeData { Key = "NO_CHECKPOINT", Value = isChecked ? "1" : "0" }
            };
            cachedLobby.UpdateLobbyAttributes(attrData);
        }
    }
}