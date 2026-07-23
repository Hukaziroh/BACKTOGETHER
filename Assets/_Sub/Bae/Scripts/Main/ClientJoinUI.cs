#nullable enable
using System.Collections;
using UnityEngine;
using Mirror;
using EpicTransport;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;

public class ClientJoinUI : MonoBehaviour
{
    public SixDigitCodeInputUI? sixDigitUI;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    private LobbySearch? currentSearchHandle;
    private string foundHostAddress = "";
    private bool searchFinished = false;

    private void Start()
    {
        // 시작 시 로딩 패널 자동 확보 및 비활성화
        GameObject? panel = GetLoadingPanel();
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>
    /// 로딩 패널이 연결되지 않았거나 씬 전환으로 참조가 끊겼을 때 자동으로 찾아주는 함수
    /// </summary>
    private GameObject? GetLoadingPanel()
    {
        if (loadingPanel == null)
        {
            // 씬 내에 숨겨져(Inactive) 있는 WalkingLoadingPanel 컴포넌트까지 전부 탐색
            WalkingLoadingPanel? panelScript = FindObjectOfType<WalkingLoadingPanel>(true);
            if (panelScript != null)
            {
                loadingPanel = panelScript.gameObject;
            }
        }
        return loadingPanel;
    }

    public void OnJoinByCodeButtonClicked()
    {
        if (sixDigitUI == null)
        {
            Debug.LogError("SixDigitCodeInputUI가 연결되지 않았습니다!");
            return;
        }
        string roomCode = sixDigitUI.GetCode();

        if (string.IsNullOrEmpty(roomCode) || roomCode.Length != 6)
        {
            Debug.LogWarning("올바른 6자리 방 코드를 입력해주세요!");
            return;
        }

        StartCoroutine(SearchAndJoinRoutine(roomCode));
    }

    private IEnumerator SearchAndJoinRoutine(string shortCode)
    {
        // =========================================================
        // 0. 로딩 패널 켜기 및 초기화 (0.1)
        // =========================================================
        GameObject? currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            currentPanel.SetActive(true);
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.1f);
        }

        Debug.Log("에픽 서버 로그인 상태 확인 중...");
        while (string.IsNullOrEmpty(EOSSDKComponent.LocalUserProductIdString))
        {
            yield return null;
        }

        currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.3f);
        }

        Debug.Log($"[{shortCode}] 방을 에픽 서버에서 검색합니다...");
        searchFinished = false;
        foundHostAddress = "";

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();

        CreateLobbySearchOptions searchOptions = new CreateLobbySearchOptions();
        searchOptions.MaxResults = 1;

        lobbyInterface.CreateLobbySearch(searchOptions, out currentSearchHandle);

        AttributeData attrData = new AttributeData();
        attrData.Key = "SHORTCODE";
        attrData.Value = new AttributeDataValue() { AsUtf8 = shortCode };

        LobbySearchSetParameterOptions paramOptions = new LobbySearchSetParameterOptions();
        paramOptions.Parameter = attrData;
        paramOptions.ComparisonOp = ComparisonOp.Equal;

        if (currentSearchHandle != null)
        {
            currentSearchHandle.SetParameter(paramOptions);

            LobbySearchFindOptions findOptions = new LobbySearchFindOptions();
            findOptions.LocalUserId = EOSSDKComponent.LocalUserProductId;

            currentSearchHandle.Find(findOptions, null, OnLobbySearchCompleted);
        }

        currentPanel = GetLoadingPanel();
        if (currentPanel != null)
        {
            var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
            if (panelScript != null) panelScript.SetProgress(0.6f);
        }

        float searchTimer = 0f;
        while (!searchFinished)
        {
            searchTimer += Time.unscaledDeltaTime;
            float currentProg = Mathf.Lerp(0.6f, 0.9f, Mathf.Clamp01(searchTimer / 3f));

            currentPanel = GetLoadingPanel();
            if (currentPanel != null)
            {
                var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
                if (panelScript != null) panelScript.SetProgress(currentProg);
            }
            yield return null;
        }

        // =========================================================
        // 결과 처리 및 씬 전환 (로딩 패널은 새 씬 로드 후 알아서 꺼짐)
        // =========================================================
        if (!string.IsNullOrEmpty(foundHostAddress))
        {
            currentPanel = GetLoadingPanel();
            if (currentPanel != null)
            {
                var panelScript = currentPanel.GetComponent<WalkingLoadingPanel>();
                if (panelScript != null) panelScript.SetProgress(1.0f);
            }
            yield return new WaitForSecondsRealtime(0.2f);

            Debug.Log($"방 검색 성공! 접속합니다. (ID: {foundHostAddress})");
            NetworkManager.singleton.networkAddress = foundHostAddress;
            NetworkManager.singleton.StartClient(); // 씬 전환 실행
        }
        else
        {
            Debug.LogError("방을 찾을 수 없거나 코드(대/소문자)를 다시 확인해주세요.");

            // 실패 시에만 즉시 패널 끄기
            currentPanel = GetLoadingPanel();
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }
        }
    }

    private void OnLobbySearchCompleted(LobbySearchFindCallbackInfo data)
    {
        if (data.ResultCode == Result.Success && currentSearchHandle != null)
        {
            LobbySearchGetSearchResultCountOptions countOptions = new LobbySearchGetSearchResultCountOptions();
            uint count = currentSearchHandle.GetSearchResultCount(countOptions);

            if (count > 0)
            {
                LobbySearchCopySearchResultByIndexOptions copyOptions = new LobbySearchCopySearchResultByIndexOptions();
                copyOptions.LobbyIndex = 0;

                currentSearchHandle.CopySearchResultByIndex(copyOptions, out LobbyDetails lobbyDetails);

                LobbyDetailsCopyInfoOptions infoOptions = new LobbyDetailsCopyInfoOptions();
                lobbyDetails.CopyInfo(infoOptions, out var lobbyInfo);

                foundHostAddress = lobbyInfo?.LobbyOwnerUserId.ToString() ?? "";
            }
        }

        searchFinished = true;
    }
}