using System;
using System.Text;
using System.Collections;
using UnityEngine;

// 🚨 에러의 원인이었던 GameSupport를 지우고, 오직 Base 모듈만 사용합니다!
using static Stove.PCSDK.Base;

public class StovePCSDK3Manager : Singleton<StovePCSDK3Manager>
{
    [Header("[StovePCInitializeParam]")]
    [SerializeField] private string environment = "LIVE";
    [SerializeField] private string gameId;
    [SerializeField] private string applicationKey;

    [Header("[Running Setting]")]
    [SerializeField] private float runCallbackInterval = 1.0f; // 1초 단위 코루틴 루프
    private bool isCallbackRunning = false;
    private Coroutine callbackCoroutine; // 코루틴 제어용 변수

    [field: Space(10)]
    [field: SerializeField] public bool isInitialized { get; private set; } = false;

    // Base SDK 초기화 여부 콜백
    private OnInitializeFinished onInitializeFinished;

    private readonly StringBuilder sb = new StringBuilder(60);

    protected override void Awake()
    {
        base.Awake();

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Initialize();
    }

    private void OnApplicationQuit()
    {
        UnInitialize();
    }

    /// <summary>
    /// SDK 초기화
    /// </summary>
    private void Initialize()
    {
        StovePCInitializeParam initParam = new StovePCInitializeParam
        {
            environment = environment,
            gameId = gameId,
            applicationKey = applicationKey
        };

        // 콜백 루프 시작
        StartRunCallbackLoop();

        // PC 클라이언트로부터 게임이 실행이 되었는지 검증
        Base_RestartAppIfNecessaryAsync(initParam, 10_000, (callbackResult, restartAppIfNecessary) =>
        {
            PrintCallbackResult(callbackResult);
            if (restartAppIfNecessary)
            {
                Debug.LogWarning("스토브 런처를 통해 게임을 실행해야 합니다. 앱을 종료합니다.");
                Application.Quit();
            }
            else
            {
                BaseSDKInitializeFinished();
                Base_Initialize(initParam, onInitializeFinished);
            }
        });
    }

    protected virtual void BaseSDKInitializeFinished()
    {
        onInitializeFinished = (callbackResult) =>
        {
            PrintCallbackResult(callbackResult);

            if (callbackResult.result.IsSuccessful())
            {
                isInitialized = true;
                Debug.Log("🎉 STOVE Base SDK 초기화 성공!");
            }
            else
            {
                Debug.LogError("STOVE Base SDK 초기화 실패");
            }
        };
    }

    /// <summary>
    /// SDK 해제
    /// </summary>
    private void UnInitialize()
    {
        StopRunCallbackLoop();

        Result result = Base_UnInitialize();
        PrintResult(result);

        isInitialized = false;
    }

    private void StartRunCallbackLoop()
    {
        if (!isCallbackRunning)
        {
            isCallbackRunning = true;
            callbackCoroutine = StartCoroutine(RunCallbackCoroutine());
        }
    }

    private void StopRunCallbackLoop()
    {
        isCallbackRunning = false;
        if (callbackCoroutine != null)
        {
            StopCoroutine(callbackCoroutine);
            callbackCoroutine = null;
        }
    }

    private IEnumerator RunCallbackCoroutine()
    {
        WaitForSeconds wfs = new WaitForSeconds(runCallbackInterval);
        while (isCallbackRunning)
        {
            Base_RunCallback();
            yield return wfs;
        }
    }

    /// <summary>
    /// 결과 출력 유틸 (에러 나던 message, externalError 삭제 완료)
    /// </summary>
    private void PrintResult(Result result)
    {
        sb.Clear();
        sb.AppendLine("# Result");
        sb.AppendLine($" - Result.IsSuccessful : {result.IsSuccessful()}");
        sb.AppendLine($" - Result.sdkName : {result.sdkName}");
        sb.AppendLine($" - Result.methodCode : {result.methodCode}");
        sb.AppendLine($" - Result.resultCode : {result.resultCode}");
        sb.AppendLine($" - Result.exceptionMessage : {result.exceptionMessage}");
        Debug.Log(sb.ToString());
    }

    public void PrintCallbackResult(CallbackResult callbackResult)
    {
        sb.Clear();
        sb.AppendLine("# CallbackResult");
        sb.AppendLine($" - CallbackResult.IsSuccessful : {callbackResult.result.IsSuccessful()}");
        sb.AppendLine($" - CallbackResult.sdkName : {callbackResult.result.sdkName}");
        sb.AppendLine($" - CallbackResult.methodCode : {callbackResult.result.methodCode}");
        sb.AppendLine($" - CallbackResult.resultCode : {callbackResult.result.resultCode}");
        sb.AppendLine($" - CallbackResult.exceptionMessage : {callbackResult.result.exceptionMessage}");
        Debug.Log(sb.ToString());
    }

    // ==========================================
    // 아래는 게임 내에서 자유롭게 호출할 수 있는 
    // 스토브 유저 정보 관련 함수들입니다. (정상 작동함)
    // ==========================================

    public string GetAccessToken()
    {
        string token = default;
        uint strlen = 1024;
        Result result = Base_GetAccessToken(ref token, strlen);
        PrintResult(result);
        return result.IsSuccessful() ? token : null;
    }

    public (bool, StovePCUser) GetUser()
    {
        StovePCUser user = default;
        Result result = Base_GetUser(ref user);
        PrintResult(result);
        return result.IsSuccessful() ? (true, user) : (false, user);
    }

    public (bool, string) GetVersion()
    {
        string version = default;
        uint strlen = 256;
        Result result = Base_GetVersion(ref version, strlen);
        PrintResult(result);
        return result.IsSuccessful() ? (true, version) : (false, version);
    }
}