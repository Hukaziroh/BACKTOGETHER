using Epic.OnlineServices;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;

using System;
using System.Runtime.InteropServices;

using UnityEngine;

/// <summary>
/// Manages the Epic Online Services SDK
///
/// 중요:
/// EOS SDK는 게임 실행 중 한 번만 초기화하고,
/// 메인 씬 <-> 게임 씬 이동 시에도 유지해야 합니다.
///
/// EOS SDK 종료는 반드시 게임 프로세스가 종료될 때만 수행합니다.
/// </summary>
namespace EpicTransport
{
    [DefaultExecutionOrder(-32000)]
    public class EOSSDKComponent : MonoBehaviour
    {
        // =========================================================
        // Inspector
        // =========================================================

        [SerializeField]
        private EosApiKey apiKeys;

        [Header("User Login")]
        public bool authInterfaceLogin = false;

        public Epic.OnlineServices.Auth.LoginCredentialType authInterfaceCredentialType =
            Epic.OnlineServices.Auth.LoginCredentialType.AccountPortal;

        public uint devAuthToolPort = 7878;

        public string devAuthToolCredentialName = "";

        public Epic.OnlineServices.ExternalCredentialType connectInterfaceCredentialType =
            Epic.OnlineServices.ExternalCredentialType.DeviceidAccessToken;

        public string deviceModel = "PC Windows 64bit";

        [SerializeField]
        private string displayName = "User";

        public static string DisplayName
        {
            get
            {
                if (Instance == null)
                    return "";

                return Instance.displayName;
            }
            set
            {
                if (Instance != null)
                    Instance.displayName = value;
            }
        }


        [Header("Misc")]

        public LogLevel epicLoggerLevel = LogLevel.Error;

        [SerializeField]
        private bool collectPlayerMetrics = true;

        public static bool CollectPlayerMetrics
        {
            get
            {
                if (Instance == null)
                    return false;

                return Instance.collectPlayerMetrics;
            }
        }

        public bool checkForEpicLauncherAndRestart = false;

        public bool delayedInitialization = false;

        public float platformTickIntervalInSeconds = 0.0f;

        private float platformTickTimer = 0f;

        public uint tickBudgetInMilliseconds = 0;


        // =========================================================
        // 내부 변수
        // =========================================================

        private ulong authExpirationHandle;

        private string authInterfaceLoginCredentialId = null;

        private string authInterfaceCredentialToken = null;

        private string connectInterfaceCredentialToken = null;


        // EOS Platform
        private PlatformInterface EOS;


        // =========================================================
        // Static Instance
        // =========================================================

        public static EOSSDKComponent instance;

        public static EOSSDKComponent Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.LogError(
                        "[EOS SDK] Instance가 NULL입니다. " +
                        "EOSSDKComponent가 씬에 존재하지 않습니다."
                    );

                    return null;
                }

                return instance;
            }
        }


        // =========================================================
        // EOS Interface 접근
        // =========================================================

        public static Epic.OnlineServices.Achievements.AchievementsInterface GetAchievementsInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetAchievementsInterface();
        }

        public static Epic.OnlineServices.Auth.AuthInterface GetAuthInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetAuthInterface();
        }

        public static Epic.OnlineServices.Connect.ConnectInterface GetConnectInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetConnectInterface();
        }

        public static Epic.OnlineServices.Ecom.EcomInterface GetEcomInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetEcomInterface();
        }

        public static Epic.OnlineServices.Friends.FriendsInterface GetFriendsInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetFriendsInterface();
        }

        public static Epic.OnlineServices.Leaderboards.LeaderboardsInterface GetLeaderboardsInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetLeaderboardsInterface();
        }

        public static Epic.OnlineServices.Lobby.LobbyInterface GetLobbyInterface()
        {
            if (!IsPlatformValid())
            {
                Debug.LogError(
                    "[EOS SDK] GetLobbyInterface 실패! " +
                    "EOS Platform이 초기화되지 않았습니다."
                );

                return null;
            }

            return Instance.EOS.GetLobbyInterface();
        }

        public static Epic.OnlineServices.Metrics.MetricsInterface GetMetricsInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetMetricsInterface();
        }

        public static Epic.OnlineServices.Mods.ModsInterface GetModsInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetModsInterface();
        }

        public static Epic.OnlineServices.P2P.P2PInterface GetP2PInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetP2PInterface();
        }

        public static Epic.OnlineServices.PlayerDataStorage.PlayerDataStorageInterface GetPlayerDataStorageInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetPlayerDataStorageInterface();
        }

        public static Epic.OnlineServices.Presence.PresenceInterface GetPresenceInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetPresenceInterface();
        }

        public static Epic.OnlineServices.Sessions.SessionsInterface GetSessionsInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetSessionsInterface();
        }

        public static Epic.OnlineServices.TitleStorage.TitleStorageInterface GetTitleStorageInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetTitleStorageInterface();
        }

        public static Epic.OnlineServices.UI.UIInterface GetUIInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetUIInterface();
        }

        public static Epic.OnlineServices.UserInfo.UserInfoInterface GetUserInfoInterface()
        {
            if (!IsPlatformValid())
                return null;

            return Instance.EOS.GetUserInfoInterface();
        }


        // =========================================================
        // Platform 상태 확인
        // =========================================================

        public static bool IsPlatformValid()
        {
            return instance != null &&
                   instance.EOS != null;
        }


        public static bool IsEOSReady()
        {
            return instance != null &&
                   instance.EOS != null &&
                   instance.initialized &&
                   !instance.isConnecting;
        }


        // =========================================================
        // User ID
        // =========================================================

        protected EpicAccountId localUserAccountId;

        public static EpicAccountId LocalUserAccountId
        {
            get
            {
                if (Instance == null)
                    return null;

                return Instance.localUserAccountId;
            }
        }


        protected string localUserAccountIdString;

        public static string LocalUserAccountIdString
        {
            get
            {
                if (Instance == null)
                    return "";

                return Instance.localUserAccountIdString;
            }
        }


        protected ProductUserId localUserProductId;

        public static ProductUserId LocalUserProductId
        {
            get
            {
                if (Instance == null)
                    return null;

                return Instance.localUserProductId;
            }
        }


        protected string localUserProductIdString;

        public static string LocalUserProductIdString
        {
            get
            {
                if (Instance == null)
                    return "";

                return Instance.localUserProductIdString;
            }
        }


        // =========================================================
        // 상태
        // =========================================================

        protected bool initialized;

        public static bool Initialized
        {
            get
            {
                if (Instance == null)
                    return false;

                return Instance.initialized;
            }
        }


        protected bool isConnecting;

        public static bool IsConnecting
        {
            get
            {
                if (Instance == null)
                    return false;

                return Instance.isConnecting;
            }
        }


        // =========================================================
        // Credential
        // =========================================================

        public static void SetAuthInterfaceLoginCredentialId(string credentialId)
        {
            if (Instance != null)
                Instance.authInterfaceLoginCredentialId = credentialId;
        }


        public static void SetAuthInterfaceCredentialToken(string credentialToken)
        {
            if (Instance != null)
                Instance.authInterfaceCredentialToken = credentialToken;
        }


        public static void SetConnectInterfaceCredentialToken(string credentialToken)
        {
            if (Instance != null)
                Instance.connectInterfaceCredentialToken = credentialToken;
        }


        // =========================================================
        // EOS Tick
        // =========================================================

        public static void Tick()
        {
            // EOSSDKComponent 자체가 없음
            if (instance == null)
                return;

            // EOS Platform이 종료됨
            if (instance.EOS == null)
                return;

            try
            {
                instance.platformTickTimer -= Time.deltaTime;

                instance.EOS.Tick();
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "[EOS SDK] Tick 예외 발생\n" +
                    e
                );
            }
        }


        // =========================================================
        // Editor DLL
        // =========================================================

#if UNITY_EDITOR_WIN

        [DllImport("Kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("Kernel32.dll")]
        private static extern int FreeLibrary(IntPtr hLibModule);

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetProcAddress(
            IntPtr hModule,
            string lpProcName
        );

        private IntPtr libraryPointer;

#endif


#if UNITY_EDITOR_LINUX

        [DllImport("libdl.so", EntryPoint = "dlopen")]
        private static extern IntPtr LoadLibrary(
            String lpFileName,
            int flags = 2
        );

        [DllImport("libdl.so", EntryPoint = "dlclose")]
        private static extern int FreeLibrary(
            IntPtr hLibModule
        );

        [DllImport("libdl.so")]
        private static extern IntPtr dlsym(
            IntPtr handle,
            String symbol
        );

        [DllImport("libdl.so")]
        private static extern IntPtr dlerror();

        private static IntPtr GetProcAddress(
            IntPtr hModule,
            string lpProcName
        )
        {
            dlerror();

            var res = dlsym(
                hModule,
                lpProcName
            );

            var errPtr = dlerror();

            if (errPtr != IntPtr.Zero)
            {
                throw new Exception(
                    "dlsym: " +
                    Marshal.PtrToStringAnsi(errPtr)
                );
            }

            return res;
        }

        private IntPtr libraryPointer;

#endif


        // =========================================================
        // Awake
        // =========================================================

        private void Awake()
        {
            Debug.Log(
    $"[EOS SDK] Awake InstanceExists={instance != null} " +
    $"Object={gameObject.name} Scene={gameObject.scene.name}"
);
            Debug.LogError(
                $"[EOS SDK] Awake | " +
                $"Object={gameObject.name} | " +
                $"InstanceExists={(instance != null)}"
            );


            // 중복 EOSSDKComponent
            if (instance != null &&
                instance != this)
            {
                Debug.LogError(
                    "[EOS SDK] 중복 EOSSDKComponent 발견 → " +
                    "새로운 컴포넌트 파괴"
                );

                Destroy(gameObject);

                return;
            }


            // Singleton 등록
            instance = this;


            // 씬 이동에도 유지
            DontDestroyOnLoad(gameObject);


            Debug.LogError(
                "[EOS SDK] Singleton 등록 완료 | " +
                $"Object={gameObject.name}"
            );


#if UNITY_EDITOR

            var libraryPath =
                "Assets/Mirror/Runtime/Transport/EpicOnlineTransport/EOSSDK/" +
                Config.LibraryName;


            libraryPointer =
                LoadLibrary(libraryPath);


            if (libraryPointer == IntPtr.Zero)
            {
                throw new Exception(
                    "Failed to load library " +
                    libraryPath
                );
            }


            Bindings.Hook(
                libraryPointer,
                GetProcAddress
            );

#endif


            if (!delayedInitialization)
            {
                Initialize();
            }
        }


        // =========================================================
        // Initialize
        // =========================================================

        protected void InitializeImplementation()
        {
            if (EOS != null)
            {
                Debug.Log(
                    "[EOS SDK] 이미 EOS Platform이 존재합니다. " +
                    "중복 Initialize 방지"
                );

                return;
            }


            isConnecting = true;


            var initializeOptions =
                new InitializeOptions()
                {
                    ProductName =
                        apiKeys.epicProductName,

                    ProductVersion =
                        apiKeys.epicProductVersion
                };


            var initializeResult =
                PlatformInterface.Initialize(
                    initializeOptions
                );


            var isAlreadyConfiguredInEditor =
                Application.isEditor &&
                initializeResult ==
                Result.AlreadyConfigured;


            if (initializeResult != Result.Success &&
     initializeResult != Result.AlreadyConfigured)
            {
                isConnecting = false;

                throw new Exception(
                    "Failed to initialize platform: " +
                    initializeResult
                );
            }
            if (initializeResult == Result.AlreadyConfigured)
            {
                Debug.Log("[EOS SDK] Platform은 이미 Initialize 되어 있습니다.");
            }

            LoggingInterface.SetLogLevel(
                LogCategory.AllCategories,
                epicLoggerLevel
            );


            LoggingInterface.SetCallback(
                message =>
                    Logger.EpicDebugLog(message)
            );


            var options =
                new Options()
                {
                    ProductId =
                        apiKeys.epicProductId,

                    SandboxId =
                        apiKeys.epicSandboxId,

                    DeploymentId =
                        apiKeys.epicDeploymentId,

                    ClientCredentials =
                        new ClientCredentials()
                        {
                            ClientId =
                                apiKeys.epicClientId,

                            ClientSecret =
                                apiKeys.epicClientSecret
                        },

                    TickBudgetInMilliseconds =
                        tickBudgetInMilliseconds
                };


            EOS =
                PlatformInterface.Create(
                    options
                );


            if (EOS == null)
            {
                isConnecting = false;

                throw new Exception(
                    "Failed to create platform"
                );
            }


            Debug.LogError(
                "[EOS SDK] Platform 생성 성공"
            );


            if (checkForEpicLauncherAndRestart)
            {
                Result result =
                    EOS.CheckForLauncherAndRestart();


                if (result != Result.NoChange)
                {
                    if (result ==
                        Result.UnexpectedError)
                    {
                        Debug.LogError(
                            "Unexpected Error while checking " +
                            "if app was started through epic launcher"
                        );
                    }

                    Application.Quit();

                    return;
                }
            }


            if (authInterfaceLogin)
            {
                if (authInterfaceCredentialType ==
                    Epic.OnlineServices.Auth.LoginCredentialType.Developer)
                {
                    authInterfaceLoginCredentialId =
                        "localhost:" +
                        devAuthToolPort;

                    authInterfaceCredentialToken =
                        devAuthToolCredentialName;
                }


                var loginOptions =
                    new Epic.OnlineServices.Auth.LoginOptions()
                    {
                        Credentials =
                            new Epic.OnlineServices.Auth.Credentials()
                            {
                                Type =
                                    authInterfaceCredentialType,

                                Id =
                                    authInterfaceLoginCredentialId,

                                Token =
                                    authInterfaceCredentialToken
                            },

                        ScopeFlags =
                            Epic.OnlineServices.Auth.AuthScopeFlags.BasicProfile |
                            Epic.OnlineServices.Auth.AuthScopeFlags.FriendsList |
                            Epic.OnlineServices.Auth.AuthScopeFlags.Presence
                    };


                EOS.GetAuthInterface().Login(
                    loginOptions,
                    null,
                    OnAuthInterfaceLogin
                );
            }
            else
            {
                if (connectInterfaceCredentialType ==
                    Epic.OnlineServices.ExternalCredentialType.DeviceidAccessToken)
                {
                    var createDeviceIdOptions =
                        new Epic.OnlineServices.Connect.CreateDeviceIdOptions();

                    createDeviceIdOptions.DeviceModel =
                        deviceModel;


                    EOS.GetConnectInterface().CreateDeviceId(
                        createDeviceIdOptions,
                        null,
                        OnCreateDeviceId
                    );
                }
                else
                {
                    ConnectInterfaceLogin();
                }
            }
        }


        public static void Initialize()
        {
            if (Instance == null)
                return;


            if (Instance.initialized)
            {
                Debug.Log(
                    "[EOS SDK] 이미 초기화 완료"
                );

                return;
            }


            if (Instance.isConnecting)
            {
                Debug.Log(
                    "[EOS SDK] 현재 초기화 중"
                );

                return;
            }


            if (Instance.EOS != null)
            {
                Debug.Log(
                    "[EOS SDK] EOS Platform은 이미 존재함"
                );

                return;
            }


            Instance.InitializeImplementation();
        }


        // =========================================================
        // Auth Login
        // =========================================================

        private void OnAuthInterfaceLogin(
            Epic.OnlineServices.Auth.LoginCallbackInfo loginCallbackInfo
        )
        {
            if (loginCallbackInfo.ResultCode ==
                Result.Success)
            {
                Debug.Log(
                    "Auth Interface Login succeeded"
                );


                string accountIdString;


                Result result =
                    loginCallbackInfo.LocalUserId.ToString(
                        out accountIdString
                    );


                if (result == Result.Success)
                {
                    Debug.Log(
                        "EOS User ID:" +
                        accountIdString
                    );


                    localUserAccountIdString =
                        accountIdString;


                    localUserAccountId =
                        loginCallbackInfo.LocalUserId;
                }


                ConnectInterfaceLogin();
            }
            else if (
                Epic.OnlineServices.Common.IsOperationComplete(
                    loginCallbackInfo.ResultCode
                ))
            {
                Debug.Log(
                    "Login returned " +
                    loginCallbackInfo.ResultCode
                );
            }
        }


        // =========================================================
        // Device ID
        // =========================================================

        private void OnCreateDeviceId(
            Epic.OnlineServices.Connect.CreateDeviceIdCallbackInfo callbackInfo
        )
        {
            if (callbackInfo.ResultCode ==
                    Result.Success ||
                callbackInfo.ResultCode ==
                    Result.DuplicateNotAllowed)
            {
                ConnectInterfaceLogin();
            }
            else if (
                Epic.OnlineServices.Common.IsOperationComplete(
                    callbackInfo.ResultCode
                ))
            {
                Debug.Log(
                    "Device ID creation returned " +
                    callbackInfo.ResultCode
                );
            }
        }


        // =========================================================
        // Connect Login
        // =========================================================

        private void ConnectInterfaceLogin()
        {
            if (EOS == null)
            {
                Debug.LogError(
                    "[EOS SDK] ConnectInterfaceLogin 실패 | " +
                    "EOS Platform이 NULL"
                );

                return;
            }


            var loginOptions =
                new Epic.OnlineServices.Connect.LoginOptions();


            if (connectInterfaceCredentialType ==
                Epic.OnlineServices.ExternalCredentialType.Epic)
            {
                Epic.OnlineServices.Auth.Token token;


                Result result =
                    EOS.GetAuthInterface()
                        .CopyUserAuthToken(
                            new Epic.OnlineServices.Auth.CopyUserAuthTokenOptions(),
                            localUserAccountId,
                            out token
                        );


                if (result == Result.Success)
                {
                    connectInterfaceCredentialToken =
                        token.AccessToken;
                }
                else
                {
                    Debug.LogError(
                        "Failed to retrieve User Auth Token"
                    );
                }
            }
            else if (
                connectInterfaceCredentialType ==
                Epic.OnlineServices.ExternalCredentialType.DeviceidAccessToken)
            {
                loginOptions.UserLoginInfo =
                    new Epic.OnlineServices.Connect.UserLoginInfo();

                loginOptions.UserLoginInfo.DisplayName =
                    displayName;
            }


            loginOptions.Credentials =
                new Epic.OnlineServices.Connect.Credentials();


            loginOptions.Credentials.Type =
                connectInterfaceCredentialType;


            loginOptions.Credentials.Token =
                connectInterfaceCredentialToken;


            EOS.GetConnectInterface().Login(
                loginOptions,
                null,
                OnConnectInterfaceLogin
            );
        }


        // =========================================================
        // Connect Login Callback
        // =========================================================

        private void OnConnectInterfaceLogin(
            Epic.OnlineServices.Connect.LoginCallbackInfo loginCallbackInfo
        )
        {
            if (loginCallbackInfo.ResultCode ==
                Result.Success)
            {
                Debug.Log(
                    "Connect Interface Login succeeded"
                );


                string productIdString;


                Result result =
                    loginCallbackInfo.LocalUserId.ToString(
                        out productIdString
                    );


                if (result == Result.Success)
                {
                    Debug.Log(
                        "EOS User Product ID:" +
                        productIdString
                    );


                    localUserProductIdString =
                        productIdString;


                    localUserProductId =
                        loginCallbackInfo.LocalUserId;
                }


                initialized = true;

                isConnecting = false;


                if (EOS != null)
                {
                    var authExpirationOptions =
                        new Epic.OnlineServices.Connect.AddNotifyAuthExpirationOptions();


                    authExpirationHandle =
                        EOS.GetConnectInterface()
                           .AddNotifyAuthExpiration(
                               authExpirationOptions,
                               null,
                               OnAuthExpiration
                           );
                }


                Debug.LogError(
                    "[EOS SDK] EOS 초기화 및 로그인 완료"
                );
            }
            else if (
                Epic.OnlineServices.Common.IsOperationComplete(
                    loginCallbackInfo.ResultCode
                ))
            {
                Debug.Log(
                    "Login returned " +
                    loginCallbackInfo.ResultCode +
                    "\nRetrying..."
                );


                if (EOS == null)
                    return;


                EOS.GetConnectInterface()
                    .CreateUser(
                        new Epic.OnlineServices.Connect.CreateUserOptions()
                        {
                            ContinuanceToken =
                                loginCallbackInfo.ContinuanceToken
                        },
                        null,
                        cb =>
                        {
                            if (cb.ResultCode !=
                                Result.Success)
                            {
                                Debug.Log(
                                    cb.ResultCode
                                );

                                return;
                            }


                            localUserProductId =
                                cb.LocalUserId;


                            ConnectInterfaceLogin();
                        }
                    );
            }
        }


        // =========================================================
        // Auth Expiration
        // =========================================================

        private void OnAuthExpiration(
            Epic.OnlineServices.Connect.AuthExpirationCallbackInfo callbackInfo
        )
        {
            Debug.Log(
                "AuthExpiration callback"
            );


            if (EOS == null)
                return;


            EOS.GetConnectInterface()
                .RemoveNotifyAuthExpiration(
                    authExpirationHandle
                );


            ConnectInterfaceLogin();
        }

        private void OnDestroy()
        {
            // 중복 NetworkManager 쪽 컴포넌트가 파괴될 때 현재 EOS 핸들을 건드리면 안 된다.
            if (!ReferenceEquals(instance, this))
            {
                Debug.Log("[EOS SDK] OnDestroy (중복 인스턴스, EOS 핸드 유지)");
                return;
            }

            // Mirror는 online -> offline 씬 전환 시 기존 NetworkManager를 교체한다.
            // 전역 SDK는 종료하지 않되 이 인스턴스의 Platform 핸들은 반드시 Release해야
            // 과거 P2P 큐/콜백이 더 이상 Tick되지 않는 핸들에 남지 않는다.
            if (EOS != null)
            {
                if (authExpirationHandle != 0)
                {
                    EOS.GetConnectInterface().RemoveNotifyAuthExpiration(authExpirationHandle);
                    authExpirationHandle = 0;
                }

                EOS.Release();
                EOS = null;
            }

            initialized = false;
            isConnecting = false;
            localUserProductId = null;
            localUserProductIdString = string.Empty;
            instance = null;

            Debug.Log("[EOS SDK] OnDestroy | Platform 핸들 정리 완료");
        }
    
        // =========================================================
        // LateUpdate
        // =========================================================

        private void LateUpdate()
        {
            if (EOS == null)
                return;


            platformTickTimer +=
                Time.deltaTime;


            if (platformTickTimer >=
                platformTickIntervalInSeconds)
            {
                platformTickTimer = 0f;

                try
                {
                    EOS.Tick();
                }
                catch (Exception e)
                {
                    Debug.LogError(
                        "[EOS SDK] LateUpdate Tick 예외\n" +
                        e
                    );
                }
            }
        }


        // =========================================================
        // OnApplicationQuit
        // =========================================================

        public void SafeReleaseEOS()
        {
            if (EOS != null)
            {
                EOS.Release();
                EOS = null;
                PlatformInterface.Shutdown();

                initialized = false;
                isConnecting = false;

                Debug.LogError("[EOS SDK] EOS Release 및 Shutdown 사전 완료 (SafeReleaseEOS)");
            }
        }

        // =========================================================
        // OnApplicationQuit
        // =========================================================
        private void OnApplicationQuit()
        {
            Debug.LogError(
                $"[EOS SDK] OnApplicationQuit | " +
                $"EOS={(EOS != null ? "NOT NULL" : "NULL")}"
            );

            SafeReleaseEOS();

#if UNITY_EDITOR
            if (libraryPointer != IntPtr.Zero)
            {
                Bindings.Unhook();

                while (FreeLibrary(libraryPointer) != 0)
                {
                }

                libraryPointer = IntPtr.Zero;
            }
#endif
        }
    }
}
