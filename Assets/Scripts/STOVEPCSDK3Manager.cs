using System;
using System.Text;
using System.Collections;
using UnityEngine;

// PC SDK 3.0 기본(Base) 모듈만 사용
using static Stove.PCSDK.Base;

public class STOVEPCSDK3Manager : MonoBehaviour
{
    [Header("STOVE SDK 연동 키")]
    public string gameId = "YOUR_GAME_ID";             // 스튜디오에서 발급받은 게임 ID
    public string applicationKey = "YOUR_APPLICATION_KEY"; // 스튜디오에서 발급받은 App Key

    // 초기화 여부를 저장하기 위한 변수
    private bool _isInitialized;

    // 코루틴 실행 주기를 저장하기 위한 변수 (1.0f초 마다 서버 응답 체크)
    private float _runCallbackInterval = 1.0f;

    // RunCallbackLoop 코루틴을 저장하기 위한 변수
    private Coroutine _runCallbackCoroutine;

    // 오브젝트를 Singleton 형태로 사용하기 위한 정적 변수
    private static STOVEPCSDK3Manager _instance;
    private static object _lockObject = new object();

    public static STOVEPCSDK3Manager Instance
    {
        get
        {
            lock (_lockObject)
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<STOVEPCSDK3Manager>();

                    if (_instance == null)
                    {
                        _instance = new GameObject().AddComponent<STOVEPCSDK3Manager>();
                        _instance.name = "STOVEPCSDK3Manager";
                    }
                }
            }
            return _instance;
        }
    }

    #region Unity Lifecycle

    // DontDestroyOnLoad 처리를 진행 (씬이 변경되어도 파괴되지 않음)
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // OnDestroy 에서 UnInitialize 호출 (게임 종료 시 안전하게 해제)
    private void OnDestroy()
    {
        if (_isInitialized)
        {
            UnInitialize();
        }
    }

    #endregion        

    #region Coroutine

    // RunCallback을 처리하기 위한 코루틴 (서버 비동기 응답 대기)
    private IEnumerator RunCallbackCoroutine()
    {
        var wfs = new WaitForSeconds(_runCallbackInterval);

        while (true)
        {
            Base_RunCallback();
            yield return wfs;
        }
    }

    public void StartRunCallbackLoop()
    {
        if (_runCallbackCoroutine == null)
        {
            Debug.Log("Start RunCallbackLoop");
            _runCallbackCoroutine = StartCoroutine(RunCallbackCoroutine());
        }
    }

    public void StopRunCallbackLoop()
    {
        if (_runCallbackCoroutine != null)
        {
            Debug.Log("Stop RunCallbackLoop");
            StopCoroutine(_runCallbackCoroutine);
            _runCallbackCoroutine = null;
        }
    }

    #endregion

    #region STOVE SDK 초기화 및 해제

    // 모듈 통합 초기화를 위한 Initialize 메소드
    public void Initialize()
    {
        // 1. 콜백 루프 시작
        StartRunCallbackLoop();

        var initParam = new StovePCInitializeParamEx2
        {
            environment = "LIVE", // 개발/테스트 완료 후 실제 출시 때는 반드시 "LIVE"
            gameId = this.gameId,
            applicationKey = this.applicationKey,
            waitTimeMillisec = 60000,
            launchLauncher = true
        };

        // 2. 런처 확인
        Base_RestartAppIfNecessaryAsyncEx2(initParam, (CallbackResult callbackResult, bool restartAppIfNecessary) =>
        {
            if (restartAppIfNecessary)
            {
                Debug.LogWarning("스토브 런처를 통한 재실행이 필요합니다. 앱을 종료합니다.");
                Application.Quit();
                return;
            }

            // 3. 캐시된 파라미터로 Base SDK 진짜 초기화
            if (callbackResult.result.IsSuccessful())
            {
                Base_InitializeEx((CallbackResult cbResult) =>
                {
                    PrintCallbackResult(cbResult);

                    if (cbResult.result.IsSuccessful())
                    {
                        Debug.Log("🎉 STOVE Base SDK 초기화 성공!");
                        _isInitialized = true;
                    }
                    else
                    {
                        Debug.LogError("Base SDK 초기화 실패");
                    }
                });
            }
        });
    }

    // 모듈 통합 정리
    public void UnInitialize()
    {
        this.StopRunCallbackLoop();

        Result baseResult = Base_UnInitialize();
        PrintResult(baseResult);

        _isInitialized = false;
    }

    #endregion

    #region Debug Methods

    public void PrintResult(Result r)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# Result");
        sb.AppendLine($" - Result.sdkName : {r.sdkName}");
        sb.AppendLine($" - Result.methodCode : {r.methodCode}");
        sb.AppendLine($" - Result.resultCode : {r.resultCode}");
        sb.AppendLine($" - Result.exceptionMessage : {r.exceptionMessage}");
        Debug.Log(sb.ToString());
    }

    public void PrintCallbackResult(CallbackResult cr)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# CallbackResult");
        sb.AppendLine($" - Result.sdkName : {cr.result.sdkName}");
        sb.AppendLine($" - Result.methodCode : {cr.result.methodCode}");
        sb.AppendLine($" - Result.resultCode : {cr.result.resultCode}");
        sb.AppendLine($" - Result.exceptionMessage : {cr.result.exceptionMessage}");
        sb.AppendLine($" - message : {cr.message}");
        sb.AppendLine($" - externalError : {cr.externalError}");
        Debug.Log(sb.ToString());
    }

    #endregion
}