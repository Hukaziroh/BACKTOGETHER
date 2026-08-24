using EpicTransport;
using Epic.OnlineServices.Lobby;
using UnityEngine;
using Epic.OnlineServices;
using System.Collections;
using System.Collections.Generic;
#pragma warning disable 0067, 0414

public class EOSLobby : MonoBehaviour
{
    public bool IsLeavingLobby { get; private set; }

    ///<returns>True if the user is connected to a lobby.</returns>
    [HideInInspector] public bool ConnectedToLobby { get; private set; }
    ///<returns>The details of the lobby that the user is currently connected to.</returns>
    public LobbyDetails ConnectedLobbyDetails { get; private set; }

    ///<value>The available keys assigned by the user.</value>
    [SerializeField] public string[] AttributeKeys = new string[] { "lobby_name" };

    private const string DefaultAttributeKey = "default";
    public const string hostAddressKey = "host_address";

    private string currentLobbyId = string.Empty;
    private bool isLobbyOwner = false;
    private List<LobbyDetails> foundLobbies = new List<LobbyDetails>();
    private List<Attribute> lobbyData = new List<Attribute>();

    //create lobby events
    public delegate void CreateLobbySuccess(List<Attribute> attributes);
    /// <summary>When invoked, a message is sent to all subscribers with a list of <see cref="Attribute"/> that were assigned to the lobby. </summary>
    public event CreateLobbySuccess CreateLobbySucceeded;

    public delegate void CreateLobbyFailure(string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with an error message. </summary>
    public event CreateLobbyFailure CreateLobbyFailed;

    //join lobby events
    public delegate void JoinLobbySuccess(List<Attribute> attributes);
    /// <summary>When invoked, a message is sent to all subscribers with a list of <see cref="Attribute"/> that were found when joining the lobby. </summary>
    public event JoinLobbySuccess JoinLobbySucceeded;

    public delegate void JoinLobbyFailure(string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with an error message. </summary>
    public event JoinLobbyFailure JoinLobbyFailed;

    //find lobby events
    public delegate void FindLobbiesSuccess(List<LobbyDetails> foundLobbies);
    /// <summary>When invoked, a message is sent to all subscribers with a list of <see cref="LobbyDetails"/> that contains the found lobbies.</summary>
    public event FindLobbiesSuccess FindLobbiesSucceeded;

    public delegate void FindLobbiesFailure(string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with an error message. </summary>
    public event FindLobbiesFailure FindLobbiesFailed;

    //leave lobby events
    public delegate void LeaveLobbySuccess();
    /// <summary>When invoked, an empty message is sent to all subscribers.</summary>
    public event LeaveLobbySuccess LeaveLobbySucceeded;

    public delegate void LeaveLobbyFailure(string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with an error message. </summary>
    public event LeaveLobbyFailure LeaveLobbyFailed;

    //update attribute events
    public delegate void UpdateAttributeSuccess(string key);
    /// <summary>When invoked, a message is sent to all subscribers with the key of the attribute that was updated.</summary>
    public event UpdateAttributeSuccess AttributeUpdateSucceeded;

    public delegate void UpdateAttributeFailure(string key, string errorMessage);
    /// <summary>When invoked, a message is sent to all subscribers with the key of the attribute that wasn't updated and an error message. </summary>
    public event UpdateAttributeFailure AttributeUpdateFailed;

    //batch update attributes events
    public delegate void UpdateAttributesBatchSuccess();
    /// <summary>When invoked, all attributes passed to <see cref="UpdateLobbyAttributes"/> were applied in a single UpdateLobby request.</summary>
    public event UpdateAttributesBatchSuccess LobbyAttributesUpdateSucceeded;

    public delegate void UpdateAttributesBatchFailure(string errorMessage);
    /// <summary>When invoked, the batch attribute update in <see cref="UpdateLobbyAttributes"/> failed.</summary>
    public event UpdateAttributesBatchFailure LobbyAttributesUpdateFailed;

    //lobby update events
    private ulong lobbyMemberStatusNotifyId = 0;
    private ulong lobbyAttributeUpdateNotifyId = 0;
    private bool notificationsRegistered;

    public delegate void LobbyMemberStatusUpdate(LobbyMemberStatusReceivedCallbackInfo callback);
    /// <summary>When invoked, a message is sent to all subscribers with an update on member status.</summary>
    public event LobbyMemberStatusUpdate LobbyMemberStatusUpdated;

    public delegate void LobbyAttributeUpdate(LobbyUpdateReceivedCallbackInfo callback);
    /// <summary>When invoked, a message is sent to all subscribers with information on the lobby that was updated.</summary>
    public event LobbyAttributeUpdate LobbyAttributeUpdated;

    public virtual void Start()
    {
        StartCoroutine(RegisterLobbyNotificationsWhenReady());
    }

    private IEnumerator RegisterLobbyNotificationsWhenReady()
    {
        if (!EOSSDKComponent.IsEOSReady())
        {
            Debug.LogWarning("[EOSLobby] EOS 준비를 기다린 뒤 Lobby Notify를 등록합니다.");
        }

        while (!EOSSDKComponent.IsEOSReady())
        {
            yield return null;
        }

        RegisterLobbyNotifications();
    }

    private void RegisterLobbyNotifications()
    {
        if (notificationsRegistered) return;

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();

        if (lobbyInterface == null)
        {
            Debug.LogError(
                "[EOSLobby] LobbyInterface를 가져오지 못했습니다."
            );

            return;
        }

        lobbyMemberStatusNotifyId =
            lobbyInterface.AddNotifyLobbyMemberStatusReceived(
                new AddNotifyLobbyMemberStatusReceivedOptions(),
                null,
                (LobbyMemberStatusReceivedCallbackInfo callback) =>
                {
                    LobbyMemberStatusUpdated?.Invoke(callback);

                    if (callback.CurrentStatus == LobbyMemberStatus.Closed &&
                        callback.LobbyId == currentLobbyId)
                    {
                        LeaveLobby();
                    }
                }
            );

        lobbyAttributeUpdateNotifyId =
            lobbyInterface.AddNotifyLobbyUpdateReceived(
                new AddNotifyLobbyUpdateReceivedOptions(),
                null,
                (LobbyUpdateReceivedCallbackInfo callback) =>
                {
                    LobbyAttributeUpdated?.Invoke(callback);
                }
            );

        notificationsRegistered = lobbyMemberStatusNotifyId != 0 ||
                                  lobbyAttributeUpdateNotifyId != 0;

        Debug.Log(
            "[EOSLobby] EOS Lobby Notify 등록 완료"
        );
    }

    /// <summary>
    /// Creates a lobby based on given parameters using Epic Online Services.
    /// </summary>
    public virtual void CreateLobby(uint maxConnections, LobbyPermissionLevel permissionLevel, bool presenceEnabled, AttributeData[] lobbyData = null)
    {
        if (IsLeavingLobby)
        {
            CreateLobbyFailed?.Invoke("이전 Lobby 정리가 끝나는 중입니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        if (!EOSSDKComponent.IsEOSReady())
        {
            CreateLobbyFailed?.Invoke("EOS 네트워크가 아직 준비되지 않았습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        if (lobbyInterface == null)
        {
            CreateLobbyFailed?.Invoke("EOS Lobby 인터페이스를 찾을 수 없습니다.");
            return;
        }

        lobbyInterface.CreateLobby(new CreateLobbyOptions
        {
            //lobby options
            LocalUserId = EOSSDKComponent.LocalUserProductId,
            MaxLobbyMembers = maxConnections,
            PermissionLevel = permissionLevel,
            PresenceEnabled = presenceEnabled,
            BucketId = DefaultAttributeKey,
        }, null, (CreateLobbyCallbackInfo callback) => {
            List<Attribute> lobbyReturnData = new List<Attribute>();

            //if the result of CreateLobby is not successful, invoke an error event and return
            if (callback.ResultCode != Result.Success)
            {
                CreateLobbyFailed?.Invoke("There was an error while creating a lobby. Error: " + callback.ResultCode);
                return;
            }

            //create mod handle and lobby data
            LobbyModification modHandle = new LobbyModification();
            AttributeData defaultData = new AttributeData { Key = DefaultAttributeKey, Value = DefaultAttributeKey };
            AttributeData hostAddressData = new AttributeData { Key = hostAddressKey, Value = EOSSDKComponent.LocalUserProductIdString };

            //set the mod handle
            lobbyInterface.UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

            //add attributes
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = defaultData, Visibility = LobbyAttributeVisibility.Public });
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = hostAddressData, Visibility = LobbyAttributeVisibility.Public });

            // ⭐ [추가됨] 방 생성 시간을 유닉스 타임스탬프(숫자)로 저장하여 속성에 부여합니다.
            long currentTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AttributeData createdAtData = new AttributeData { Key = "created_at", Value = currentTimestamp };
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = createdAtData, Visibility = LobbyAttributeVisibility.Public });
            lobbyReturnData.Add(new Attribute { Data = createdAtData, Visibility = LobbyAttributeVisibility.Public });

            //add user attributes
            if (lobbyData != null)
            {
                foreach (AttributeData data in lobbyData)
                {
                    modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = data, Visibility = LobbyAttributeVisibility.Public });
                    lobbyReturnData.Add(new Attribute { Data = data, Visibility = LobbyAttributeVisibility.Public });
                }
            }

            //update the lobby
            lobbyInterface.UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo updateCallback) => {

                //if there was an error while updating the lobby, invoke an error event and return
                if (updateCallback.ResultCode != Result.Success)
                {
                    CreateLobbyFailed?.Invoke("There was an error while updating the lobby. Error: " + updateCallback.ResultCode);
                    return;
                }

                LobbyDetails details;
                lobbyInterface.CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out details);

                // 이전에 들고 있던 ConnectedLobbyDetails 핸들이 있다면 해제 후 교체 (메모리 누수 방지)
                ConnectedLobbyDetails?.Release();
                ConnectedLobbyDetails = details;
                isLobbyOwner = true;
                ConnectedToLobby = true;
                currentLobbyId = callback.LobbyId;

                //invoke event
                CreateLobbySucceeded?.Invoke(lobbyReturnData);
            });
        });
    }

    /// <summary>
    /// Finds lobbies based on given parameters using Epic Online Services.
    /// </summary>
    public virtual void FindLobbies(uint maxResults = 100, LobbySearchSetParameterOptions[] lobbySearchSetParameterOptions = null)
    {
        if (!EOSSDKComponent.IsEOSReady())
        {
            FindLobbiesFailed?.Invoke("EOS 네트워크가 아직 준비되지 않았습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        if (lobbyInterface == null)
        {
            FindLobbiesFailed?.Invoke("EOS Lobby 인터페이스를 찾을 수 없습니다.");
            return;
        }

        //create search handle and list of lobby details
        LobbySearch search = new LobbySearch();

        //set the search handle
        lobbyInterface.CreateLobbySearch(new CreateLobbySearchOptions { MaxResults = maxResults }, out search);

        //set search parameters
        if (lobbySearchSetParameterOptions != null)
        {
            foreach (LobbySearchSetParameterOptions searchOption in lobbySearchSetParameterOptions)
            {
                search.SetParameter(searchOption);
            }
        }
        else
        {
            search.SetParameter(new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = new AttributeData { Key = DefaultAttributeKey, Value = DefaultAttributeKey }
            });
        }

        //find lobbies
        search.Find(new LobbySearchFindOptions { LocalUserId = EOSSDKComponent.LocalUserProductId }, null, (LobbySearchFindCallbackInfo callback) => {
            //if the search was unsuccessful, invoke an error event and return
            if (callback.ResultCode != Result.Success)
            {
                FindLobbiesFailed?.Invoke("There was an error while finding lobbies. Error: " + callback.ResultCode);
                return;
            }

            // 이전 검색 결과로 받은 LobbyDetails 핸들을 해제한 뒤 비웁니다.
            ReleaseFoundLobbies();

            //for each lobby found, add data to details
            for (int i = 0; i < search.GetSearchResultCount(new LobbySearchGetSearchResultCountOptions { }); i++)
            {
                LobbyDetails lobbyInformation;
                search.CopySearchResultByIndex(new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = (uint)i }, out lobbyInformation);

                // ⭐ [추가됨] EOS 백엔드 삭제 지연으로 남은 인원수 0명 유령방 필터링
                uint memberCount = lobbyInformation.GetMemberCount(new LobbyDetailsGetMemberCountOptions());
                if (memberCount == 0)
                {
                    // 인원이 0명이면 에픽 백엔드에서 삭제 진행 중인 방이므로
                    // 네이티브 메모리를 해제하고 목록에 추가하지 않고 건너뜁니다.
                    lobbyInformation.Release();
                    continue;
                }

                foundLobbies.Add(lobbyInformation);
            }

            // ⭐ [추가됨] 방 리스트를 최신순으로 정렬합니다. (새로고침 시 순서 섞임 방지)
            foundLobbies.Sort((lobbyA, lobbyB) =>
            {
                Attribute attrA = new Attribute();
                Attribute attrB = new Attribute();

                long timeA = 0;
                long timeB = 0;

                if (lobbyA.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "created_at" }, out attrA) == Result.Success)
                {
                    if (attrA.Data != null)
                        timeA = attrA.Data.Value.AsInt64 ?? 0;
                }

                if (lobbyB.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "created_at" }, out attrB) == Result.Success)
                {
                    if (attrB.Data != null)
                        timeB = attrB.Data.Value.AsInt64 ?? 0;
                }

                return timeB.CompareTo(timeA); // 내림차순 정렬 (나중에 만들어진 방이 위로)
            });

            // 검색 결과는 이미 각 LobbyDetails로 복사해왔으므로 LobbySearch 핸들 자체는 더 필요 없습니다.
            search.Release();

            //invoke event
            FindLobbiesSucceeded?.Invoke(foundLobbies);
        });
    }

    /// <summary>
    /// foundLobbies에 들어있는 이전 검색 결과의 LobbyDetails 핸들을 전부 Release하고 리스트를 비웁니다.
    /// </summary>
    private void ReleaseFoundLobbies()
    {
        foreach (LobbyDetails oldDetails in foundLobbies)
        {
            oldDetails?.Release();
        }
        foundLobbies.Clear();
    }

    /// <summary>
    /// Join the given lobby and get the data attached.
    /// </summary>
    public virtual void JoinLobby(LobbyDetails lobbyToJoin, string[] attributeKeys = null, bool presenceEnabled = false)
    {
        if (IsLeavingLobby)
        {
            JoinLobbyFailed?.Invoke("이전 Lobby 정리가 끝나는 중입니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        if (!EOSSDKComponent.IsEOSReady())
        {
            JoinLobbyFailed?.Invoke("EOS 네트워크가 아직 준비되지 않았습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        if (lobbyInterface == null)
        {
            JoinLobbyFailed?.Invoke("EOS Lobby 인터페이스를 찾을 수 없습니다.");
            return;
        }

        if (lobbyToJoin == null)
        {
            JoinLobbyFailed?.Invoke("입장할 로비 정보가 없습니다.");
            return;
        }

        //join lobby
        lobbyInterface.JoinLobby(new JoinLobbyOptions { LobbyDetailsHandle = lobbyToJoin, LocalUserId = EOSSDKComponent.LocalUserProductId, PresenceEnabled = presenceEnabled }, null, (JoinLobbyCallbackInfo callback) => {
            //if the result was not a success, invoke an error event and return
            if (callback.ResultCode != Result.Success)
            {
                JoinLobbyFailed?.Invoke("There was an error while joining a lobby. Error: " + callback.ResultCode);
                return;
            }

            lobbyData.Clear();

            Attribute hostAddress = new Attribute();
            lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = hostAddressKey }, out hostAddress);
            lobbyData.Add(hostAddress);

            if (attributeKeys != null)
            {
                foreach (string key in attributeKeys)
                {
                    Attribute attribute = new Attribute();
                    lobbyToJoin.CopyAttributeByKey(new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key }, out attribute);
                    lobbyData.Add(attribute);
                }
            }

            LobbyDetails details;
            lobbyInterface.CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out details);

            // 이전에 들고 있던 ConnectedLobbyDetails 핸들이 있다면 해제 후 교체 (메모리 누수 방지)
            ConnectedLobbyDetails?.Release();
            ConnectedLobbyDetails = details;
            isLobbyOwner = false;
            ConnectedToLobby = true;
            currentLobbyId = callback.LobbyId;

            //invoke event
            JoinLobbySucceeded?.Invoke(lobbyData);
        });
    }

    public virtual void JoinLobbyByID(string lobbyID)
    {
        if (!EOSSDKComponent.IsEOSReady())
        {
            FindLobbiesFailed?.Invoke("EOS 네트워크가 아직 준비되지 않았습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        if (lobbyInterface == null)
        {
            FindLobbiesFailed?.Invoke("EOS Lobby 인터페이스를 찾을 수 없습니다.");
            return;
        }

        LobbySearch search = new LobbySearch();
        lobbyInterface.CreateLobbySearch(new CreateLobbySearchOptions { MaxResults = 1 }, out search);
        search.SetLobbyId(new LobbySearchSetLobbyIdOptions { LobbyId = lobbyID });

        search.Find(new LobbySearchFindOptions { LocalUserId = EOSSDKComponent.LocalUserProductId }, null, (LobbySearchFindCallbackInfo callback) => {
            //if the search was unsuccessful, invoke an error event and return
            if (callback.ResultCode != Result.Success)
            {
                FindLobbiesFailed?.Invoke("There was an error while finding lobbies. Error: " + callback.ResultCode);
                return;
            }

            ReleaseFoundLobbies();

            //for each lobby found, add data to details
            for (int i = 0; i < search.GetSearchResultCount(new LobbySearchGetSearchResultCountOptions { }); i++)
            {
                LobbyDetails lobbyInformation;
                search.CopySearchResultByIndex(new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = (uint)i }, out lobbyInformation);
                foundLobbies.Add(lobbyInformation);
            }

            search.Release();

            if (foundLobbies.Count > 0)
            {
                JoinLobby(foundLobbies[0]);
            }
        });
    }

    /// <summary>
    /// 로비가 참가 가능한 상태인지(정원이 다 차지 않았는지) 확인하고, 현재/최대 인원도 함께 돌려줍니다.
    /// </summary>
    public static bool IsLobbyJoinable(LobbyDetails lobby, out uint currentMembers, out uint maxMembers)
    {
        currentMembers = lobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions());
        maxMembers = 4; // 기본값

        LobbyDetailsInfo lobbyInfo;
        if (lobby.CopyInfo(new LobbyDetailsCopyInfoOptions(), out lobbyInfo) == Result.Success)
        {
            maxMembers = lobbyInfo.MaxMembers;
        }

        return currentMembers < maxMembers;
    }

    /// <summary>
    /// Leave the lobby that the user is connected to.
    /// </summary>
    public void LeaveLobby()
    {
        if (IsLeavingLobby)
        {
            Debug.LogWarning("[EOSLobby] 이미 Lobby 퇴장 요청 중입니다.");
            return;
        }

        if (!ConnectedToLobby || string.IsNullOrEmpty(currentLobbyId))
        {
            ClearConnectedLobbyState();
            return;
        }

        if (!EOSSDKComponent.IsEOSReady())
        {
            LeaveLobbyFailed?.Invoke("EOS 네트워크가 준비되지 않아 Lobby를 나갈 수 없습니다.");
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.IsPlatformValid()
            ? EOSSDKComponent.GetLobbyInterface()
            : null;
        if (lobbyInterface == null)
        {
            LeaveLobbyFailed?.Invoke("EOS Lobby 인터페이스를 찾을 수 없습니다.");
            return;
        }

        string lobbyId = currentLobbyId;
        IsLeavingLobby = true;
        BeginLeaveLobby(lobbyInterface, lobbyId);
    }

    private void BeginLeaveLobby(LobbyInterface lobbyInterface, string lobbyId)
    {
        LeaveLobbyOptions options = new LeaveLobbyOptions()
        {
            LobbyId = lobbyId,
            LocalUserId = EOSSDKComponent.LocalUserProductId
        };

        lobbyInterface.LeaveLobby(options, null, OnLeaveLobbyCompleted);
    }


    private void OnLeaveLobbyCompleted(LeaveLobbyCallbackInfo data)
    {
        Debug.Log(
            $"[EOSLobby] ③ LeaveLobby 콜백 도착 | " +
            $"ResultCode = {data.ResultCode}"
        );

        if (data.LobbyId != currentLobbyId)
        {
            Debug.LogWarning(
                $"[EOSLobby] 과거 Lobby 퇴장 콜백 무시 | " +
                $"CallbackLobby={data.LobbyId} | CurrentLobby={currentLobbyId}"
            );
            return;
        }

        IsLeavingLobby = false;

        if (data.ResultCode == Result.Success || data.ResultCode == Result.NotFound)
        {
            ClearConnectedLobbyState();

            Debug.Log(
                "[EOSLobby] ④ Lobby 정상 퇴장 완료"
            );

            LeaveLobbySucceeded?.Invoke();
        }
        else
        {
            Debug.LogError(
                $"[EOSLobby] ④ Lobby 퇴장 실패 | " +
                $"ResultCode = {data.ResultCode}"
            );

            LeaveLobbyFailed?.Invoke("Lobby 퇴장 실패: " + data.ResultCode);
        }
    }

    /// <summary>
    /// Remove an attribute attached to the lobby.
    /// </summary>
    public virtual void RemoveAttribute(string key)
    {
        LobbyModification modHandle = new LobbyModification();

        EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

        modHandle.RemoveAttribute(new LobbyModificationRemoveAttributeOptions { Key = key });

        EOSSDKComponent.GetLobbyInterface().UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success)
            {
                AttributeUpdateFailed?.Invoke(key, $"There was an error while removing attribute \"{key}\". Error: " + callback.ResultCode);
                return;
            }

            AttributeUpdateSucceeded?.Invoke(key);
        });
    }

    /// <summary>
    /// Update an attribute that is attached to the lobby.
    /// </summary>
    private void UpdateAttribute(AttributeData attribute)
    {
        LobbyModification modHandle = new LobbyModification();

        EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

        modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = attribute, Visibility = LobbyAttributeVisibility.Public });

        EOSSDKComponent.GetLobbyInterface().UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success)
            {
                AttributeUpdateFailed?.Invoke(attribute.Key, $"There was an error while updating attribute \"{attribute.Key}\". Error: " + callback.ResultCode);
                return;
            }

            AttributeUpdateSucceeded?.Invoke(attribute.Key);
        });
    }

    public void UpdateLobbyAttribute(string key, bool newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    public void UpdateLobbyAttribute(string key, int newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    public void UpdateLobbyAttribute(string key, double newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    public void UpdateLobbyAttribute(string key, string newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// 여러 속성을 하나의 LobbyModification / UpdateLobby 요청으로 한 번에 반영합니다.
    /// </summary>
    public void UpdateLobbyAttributes(AttributeData[] attributes)
    {
        if (attributes == null || attributes.Length == 0)
        {
            return;
        }

        if (string.IsNullOrEmpty(currentLobbyId))
        {
            LobbyAttributesUpdateFailed?.Invoke("현재 연결된 로비가 없어 속성을 업데이트할 수 없습니다.");
            return;
        }

        LobbyModification modHandle = new LobbyModification();
        EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

        foreach (AttributeData attribute in attributes)
        {
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = attribute, Visibility = LobbyAttributeVisibility.Public });
        }

        EOSSDKComponent.GetLobbyInterface().UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success)
            {
                LobbyAttributesUpdateFailed?.Invoke("로비 속성 일괄 업데이트 중 오류가 발생했습니다. Error: " + callback.ResultCode);
                return;
            }

            LobbyAttributesUpdateSucceeded?.Invoke();
        });
    }

    /// <summary>
    /// Returns the current lobby id
    /// </summary>
    public string GetCurrentLobbyId()
    {
        return currentLobbyId;
    }

    /// <summary>
    /// 방장이 에픽 서버에서 로비를 완전히 파괴/삭제합니다.
    /// </summary>
    public void DestroyLobby()
    {
        if (IsLeavingLobby)
        {
            Debug.LogWarning("[EOSLobby] 이미 Lobby 퇴장/파괴 요청 중입니다.");
            return;
        }

        if (!ConnectedToLobby || string.IsNullOrEmpty(currentLobbyId))
        {
            ClearConnectedLobbyState();
            return;
        }

        if (!EOSSDKComponent.IsEOSReady())
        {
            Debug.LogWarning("[EOSLobby] EOS 네트워크가 준비되지 않아 Lobby를 파괴할 수 없습니다.");
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        if (lobbyInterface == null)
        {
            Debug.LogWarning("[EOSLobby] EOS Lobby 인터페이스를 찾을 수 없습니다.");
            return;
        }

        string lobbyId = currentLobbyId;
        IsLeavingLobby = true;

        DestroyLobbyOptions destroyLobbyOptions = new DestroyLobbyOptions
        {
            LobbyId = lobbyId,
            LocalUserId = EOSSDKComponent.LocalUserProductId
        };

        lobbyInterface.DestroyLobby(destroyLobbyOptions, null, (DestroyLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success && callback.ResultCode != Result.NotFound)
            {
                Debug.LogWarning("[EOSLobby] DestroyLobby failed: " + callback.ResultCode);

                // busy 상태를 유지한 채 일반 퇴장까지 완료해야 호출자가 실제 종료를 기다릴 수 있다.
                BeginLeaveLobby(lobbyInterface, lobbyId);
                return;
            }

            Debug.Log("[EOSLobby] 에픽 서버에서 로비가 성공적으로 파괴되었습니다.");
            ClearConnectedLobbyState();
            IsLeavingLobby = false;
        });
    }

    private void ClearConnectedLobbyState()
    {
        ConnectedLobbyDetails?.Release();
        ConnectedLobbyDetails = null;
        currentLobbyId = string.Empty;
        ConnectedToLobby = false;
        isLobbyOwner = false;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();

        LobbyInterface lobbyInterface = EOSSDKComponent.IsPlatformValid()
            ? EOSSDKComponent.GetLobbyInterface()
            : null;
        if (lobbyInterface != null)
        {
            if (lobbyMemberStatusNotifyId != 0)
            {
                lobbyInterface.RemoveNotifyLobbyMemberStatusReceived(lobbyMemberStatusNotifyId);
                lobbyMemberStatusNotifyId = 0;
            }

            if (lobbyAttributeUpdateNotifyId != 0)
            {
                lobbyInterface.RemoveNotifyLobbyUpdateReceived(lobbyAttributeUpdateNotifyId);
                lobbyAttributeUpdateNotifyId = 0;
            }
        }

        notificationsRegistered = false;
        ReleaseFoundLobbies();
        ClearConnectedLobbyState();
        IsLeavingLobby = false;
    }

    ////private void OnApplicationQuit()
    ////{
    ////    if (!EOSSDKComponent.IsEOSReady()) return;

    ////    if (ConnectedToLobby && !string.IsNullOrEmpty(currentLobbyId))
    ////    {
    ////        Debug.Log("[EOSLobby] 강제 종료 감지! 에픽 서버에 로비 파괴/퇴장 신호를 긴급 송신합니다.");

    ////        if (isLobbyOwner)
    ////        {
    ////            DestroyLobby();
    ////        }
    ////        else
    ////        {
    ////            LeaveLobby();
    ////        }
    ////    }
    ////}
}
