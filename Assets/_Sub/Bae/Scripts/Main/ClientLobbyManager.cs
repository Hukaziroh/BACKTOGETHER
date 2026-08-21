using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // 씬 초기화용 네임스페이스 추가
using UnityEngine.UI;

public partial class ClientLobbyManager : MonoBehaviour
{
    public bool IsConnecting { get; private set; }
    [Header("UI 패널 연결")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject clientSelectionPanel;
    [SerializeField] private GameObject clientPublicPanel;
    [SerializeField] private GameObject clientPrivatePanel;
    [SerializeField] private GameObject logo;

    [Header("선택 패널 버튼 연결 (Client Selection)")]
    [SerializeField] private Button mainClientButton;
    [SerializeField] private Button selectPublicModeButton;
    [SerializeField] private Button selectPrivateModeButton;
    [SerializeField] private Button quickJoinSelectionButton;

    [Header("퍼블릭 방 리스트 - 상단 컨트롤")]
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Button researchButton;
    [SerializeField] private GameObject chapterFilterSelectObject;
    [SerializeField] private TextMeshProUGUI filterChapterText;
    [SerializeField] private Button prevFilterChapterButton;
    [SerializeField] private Button nextFilterChapterButton;

    [Header("프라이빗 로비 매니저 연동")]
    [SerializeField] private PrivateLobbyManager privateLobbyManager; // ★ PrivateLobbyManager 연결 필드 추가

    [Header("비공개 방 관련")]
    [SerializeField] private TMP_InputField privateRoomInputField; // 비공개 방 코드/비밀번호 입력용 인풋필드

    [Header("퍼블릭 방 리스트 - 스크롤 및 로비 아이템")]
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private GameObject publicRoomNoRoomText; // 퍼블릭 방 리스트 전용 텍스트

    [Header("퀵 조인 관련")]
    [SerializeField] private GameObject quickJoinNoRoomText; // 퀵 조인 전용 텍스트 (3초 뒤 자동 꺼짐)

    [Header("퍼블릭 방 리스트 - 하단 페이지네이션")]
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private TextMeshProUGUI pageText;

    [Header("로딩 UI 연결 (비워두어도 자동 탐색됩니다)")]
    [SerializeField] private GameObject loadingPanel;

    [Header("일반 에러 팝업 UI 연결 (기본 Fallback용)")]
    [SerializeField] private GameObject errorPopupPanel;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private Button errorCloseButton;

    [Header("연결 타임아웃 팝업 UI (10초 초과)")]
    [SerializeField] private GameObject timeoutPopupPanel;
    [SerializeField] private TextMeshProUGUI timeoutMessageText;
    [SerializeField] private Button timeoutCloseButton;

    [Header("꽉 찬 방 알림 텍스트 (인스펙터에서 할당)")]
    [SerializeField] private TextMeshProUGUI fullRoomMessageText;
    private Coroutine fullRoomMessageCoroutine;

    private EOSLobby eosLobby;
    private bool isSubscribed = false;

    private bool isLocalSearchRequest = false;

    private List<LobbyDetails> allFetchedLobbies = new List<LobbyDetails>();
    private List<LobbyDetails> filteredLobbies = new List<LobbyDetails>();

    private int currentPage = 0;
    private const int itemsPerPage = 8;
    private int selectedFilterChapter = 0; // 0: All, 1~N: Chapter 1~N
    private bool isQuickJoining = false;

    private Coroutine connectionTimeoutCoroutine;
    private Coroutine hideQuickJoinNoRoomCoroutine;
    private GameObject lastSelectedBeforeSearch; // ★ 리로드 직전 포커스 저장용 변수 추가
}
