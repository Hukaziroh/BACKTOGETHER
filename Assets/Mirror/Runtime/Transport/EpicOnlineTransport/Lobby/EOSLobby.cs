using EpicTransport;
using Epic.OnlineServices.Lobby;
using UnityEngine;
using Epic.OnlineServices;
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

    //batch update attributes events (여러 속성을 한 번의 UpdateLobby 요청으로 반영할 때 사용)
    public delegate void UpdateAttributesBatchSuccess();
    /// <summary>When invoked, all attributes passed to <see cref="UpdateLobbyAttributes"/> were applied in a single UpdateLobby request.</summary>
    public event UpdateAttributesBatchSuccess LobbyAttributesUpdateSucceeded;

    public delegate void UpdateAttributesBatchFailure(string errorMessage);
    /// <summary>When invoked, the batch attribute update in <see cref="UpdateLobbyAttributes"/> failed.</summary>
    public event UpdateAttributesBatchFailure LobbyAttributesUpdateFailed;

    //lobby update events
    private ulong lobbyMemberStatusNotifyId = 0;
    private ulong lobbyAttributeUpdateNotifyId = 0;

    public delegate void LobbyMemberStatusUpdate(LobbyMemberStatusReceivedCallbackInfo callback);
    /// <summary>When invoked, a message is sent to all subscribers with an update on member status.</summary>
    public event LobbyMemberStatusUpdate LobbyMemberStatusUpdated;

    public delegate void LobbyAttributeUpdate(LobbyUpdateReceivedCallbackInfo callback);
    /// <summary>When invoked, a message is sent to all subscribers with information on the lobby that was updated.</summary>
    public event LobbyAttributeUpdate LobbyAttributeUpdated;

    public virtual void Start()
    {
        if (!EOSSDKComponent.IsEOSReady())
        {
            Debug.LogWarning(
                "[EOSLobby] Start 시점에 EOS가 아직 준비되지 않았습니다."
            );

            return;
        }

        var lobbyInterface = EOSSDKComponent.GetLobbyInterface();

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

                    if (callback.CurrentStatus == LobbyMemberStatus.Closed)
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

        Debug.Log(
            "[EOSLobby] EOS Lobby Notify 등록 완료"
        );
    }

    /// <summary>
    /// Creates a lobby based on given parameters using Epic Online Services.
    /// <para>You can get the data that was added to the lobby by subscribing to the <see cref="CreateLobbySucceeded"/> event which gives you a list of <see cref="Attribute"/>.</para>
    /// <para>This process may throw errors. You can get errors by subscribing to the <see cref="CreateLobbyFailed"/> event.</para>
    /// </summary>
    /// <param name="maxConnections">The maximum amount of connections the lobby allows.</param>
    /// <param name="permissionLevel">The restriction on the lobby to prevent unwanted people from joining.</param>
    /// <param name="presenceEnabled">Use Epic's overlay to display information to others.</param>
    /// <param name="lobbyData">Optional data that you can to the lobby. By default, there is an empty attribute for searching and an attribute which holds the host's network address.</param>
    public virtual void CreateLobby(uint maxConnections, LobbyPermissionLevel permissionLevel, bool presenceEnabled, AttributeData[] lobbyData = null)
    {

        EOSSDKComponent.GetLobbyInterface().CreateLobby(new CreateLobbyOptions
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
            EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

            //add attributes
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = defaultData, Visibility = LobbyAttributeVisibility.Public });
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = hostAddressData, Visibility = LobbyAttributeVisibility.Public });

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
            EOSSDKComponent.GetLobbyInterface().UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo updateCallback) => {

                //if there was an error while updating the lobby, invoke an error event and return
                if (updateCallback.ResultCode != Result.Success)
                {
                    CreateLobbyFailed?.Invoke("There was an error while updating the lobby. Error: " + updateCallback.ResultCode);
                    return;
                }

                LobbyDetails details;
                EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out details);

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
    /// <para>You can get the found lobbies by subscribing to the <see cref="FindLobbiesSucceeded"/> event which gives you a list of <see cref="LobbyDetails"/>.</para>
    /// <para>This process may throw errors. You can get errors by subscribing to the <see cref="FindLobbiesFailed"/> event.</para>
    /// </summary>
    /// <param name="maxResults">The maximum amount of results to return.</param>
    /// <param name="lobbySearchSetParameterOptions">The parameters to search by. If left empty, then the search will use the default attribute attached to all the lobbies.</param>
    public virtual void FindLobbies(uint maxResults = 100, LobbySearchSetParameterOptions[] lobbySearchSetParameterOptions = null)
    {
        //create search handle and list of lobby details
        LobbySearch search = new LobbySearch();

        //set the search handle
        EOSSDKComponent.GetLobbyInterface().CreateLobbySearch(new CreateLobbySearchOptions { MaxResults = maxResults }, out search);

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
            // (LobbyDetails는 EOS 네이티브 메모리를 들고 있으므로, 검색을 반복할 때마다
            //  덮어쓰기 전에 반드시 Release 해줘야 누적 누수가 발생하지 않습니다.)
            ReleaseFoundLobbies();

            //for each lobby found, add data to details
            for (int i = 0; i < search.GetSearchResultCount(new LobbySearchGetSearchResultCountOptions { }); i++)
            {
                LobbyDetails lobbyInformation;
                search.CopySearchResultByIndex(new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = (uint)i }, out lobbyInformation);
                foundLobbies.Add(lobbyInformation);
            }

            // 검색 결과는 이미 각 LobbyDetails로 복사해왔으므로 LobbySearch 핸들 자체는 더 필요 없습니다.
            search.Release();

            //invoke event
            FindLobbiesSucceeded?.Invoke(foundLobbies);
        });
    }

    /// <summary>
    /// foundLobbies에 들어있는 이전 검색 결과의 LobbyDetails 핸들을 전부 Release하고 리스트를 비웁니다.
    /// EOSLobby가 이 핸들들의 유일한 소유자이므로(리스트를 그대로 넘겨 쓰는 쪽에서는 복사해서 쓰는 걸 권장),
    /// 여기서만 Release 하도록 통일해 중복 해제(double free)를 피합니다.
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
    /// <para>You can get the lobby's data by subscribing to the <see cref="JoinLobbySucceeded"/> event which gives you a list of <see cref="Attribute"/>.</para>
    /// <para>This process may throw errors. You can get errors by subscribing to the <see cref="JoinLobbyFailed"/> event.</para>
    /// </summary>
    /// <param name="lobbyToJoin"><see cref="LobbyDetails"/> of the lobby to join that is retrieved from the <see cref="FindLobbiesSucceeded"/> event.</param>
    /// <param name="attributeKeys">The keys to use to retrieve the data attached to the lobby. If you leave this empty, the host address attribute will still be read.</param>
    /// <param name="presenceEnabled">Use Epic's overlay to display information to others.</param>
    public virtual void JoinLobby(LobbyDetails lobbyToJoin, string[] attributeKeys = null, bool presenceEnabled = false)
    {
        //join lobby
        EOSSDKComponent.GetLobbyInterface().JoinLobby(new JoinLobbyOptions { LobbyDetailsHandle = lobbyToJoin, LocalUserId = EOSSDKComponent.LocalUserProductId, PresenceEnabled = presenceEnabled }, null, (JoinLobbyCallbackInfo callback) => {
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
            EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandle(new CopyLobbyDetailsHandleOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out details);

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
        LobbySearch search = new LobbySearch();
        EOSSDKComponent.GetLobbyInterface().CreateLobbySearch(new CreateLobbySearchOptions { MaxResults = 1 }, out search);
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
    /// ClientRoomItemUI, ClientLobbyManager(Quick Join) 등 여러 곳에서 중복 구현되던 로직을 하나로 모았습니다.
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
    /// Leave the lobby that the user is connected to. If the creator of the lobby leaves, the lobby will be destroyed, and any client connected to the lobby will leave. If a member leaves, there will be no further action.
    /// <para>If the player was able to destroy or leave the lobby, the <see cref="LeaveLobbySucceeded"/> event will be invoked.</para>
    /// <para>This process may throw errors. You can errors by subscribing to the <see cref="LeaveLobbyFailed"/> event.</para>
    /// </summary>
    public void LeaveLobby()
    {
        Debug.Log(
            $"[EOSLobby] ① LeaveLobby() 호출 | " +
            $"ConnectedToLobby = {ConnectedToLobby} | " +
            $"LobbyID = {currentLobbyId}"
        );

        if (!EOSSDKComponent.IsEOSReady())
        {
            Debug.LogWarning(
                "[EOSLobby] ② EOS SDK가 준비되지 않았습니다."
            );

            return;
        }

        if (IsLeavingLobby)
        {
            Debug.LogWarning(
                "[EOSLobby] 이미 Lobby 퇴장 요청 중입니다."
            );

            return;
        }

        if (!ConnectedToLobby || string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogWarning(
                "[EOSLobby] 현재 Lobby에 연결되어 있지 않습니다."
            );

            ConnectedToLobby = false;
            ConnectedLobbyDetails = null;
            currentLobbyId = string.Empty;
            isLobbyOwner = false;

            return;
        }

        IsLeavingLobby = true;

        string leavingLobbyId = currentLobbyId;

        LeaveLobbyOptions options = new LeaveLobbyOptions()
        {
            LobbyId = leavingLobbyId,
            LocalUserId = EOSSDKComponent.LocalUserProductId
        };

        Debug.Log(
            $"[EOSLobby] ② EOS LeaveLobby API 호출 | " +
            $"LobbyID = {leavingLobbyId}"
        );

        EOSSDKComponent.GetLobbyInterface().LeaveLobby(
            options,
            null,
            OnLeaveLobbyCompleted
        );
    }
    private void OnLeaveLobbyCompleted(
    LeaveLobbyCallbackInfo data
)
    {
        Debug.Log(
            $"[EOSLobby] ③ LeaveLobby 콜백 도착 | " +
            $"ResultCode = {data.ResultCode}"
        );

        IsLeavingLobby = false;

        if (data.ResultCode == Result.Success)
        {
            ConnectedToLobby = false;
            ConnectedLobbyDetails = null;
            currentLobbyId = string.Empty;
            isLobbyOwner = false;

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
        }
    }
    /// <summary>
    /// Remove an attribute attached to the lobby.
    /// </summary>
    /// <param name="key">The key of the attribute that will be removed.</param>
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
    /// <param name="attribute">The new data to apply.</param>
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

    /// <summary>
    /// Update a boolean attribute.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <param name="newValue">The new boolean value.</param>
    public void UpdateLobbyAttribute(string key, bool newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// Update an integer attribute.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <param name="newValue">The new integer value.</param>
    public void UpdateLobbyAttribute(string key, int newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// Update a double attribute.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <param name="newValue">The new double value.</param>
    public void UpdateLobbyAttribute(string key, double newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// Update a string attribute.
    /// </summary>
    /// <param name="key">The key of the attribute.</param>
    /// <param name="newValue">The new string value.</param>
    public void UpdateLobbyAttribute(string key, string newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    /// <summary>
    /// 여러 속성을 하나의 LobbyModification / UpdateLobby 요청으로 한 번에 반영합니다.
    /// <para>
    /// UpdateLobbyAttribute를 여러 번 연달아 호출하면 각 호출이 서로 기다리지 않는
    /// 별개의 비동기 요청이 되어 순서를 보장할 수 없고, 그중 하나만 실패해도
    /// 나머지 속성만 반영된 채로 남을 수 있습니다.
    /// 방 생성처럼 여러 속성을 "한 세트"로 등록해야 하는 경우에는 이 메서드를 사용하세요.
    /// </para>
    /// <para>결과는 <see cref="LobbyAttributesUpdateSucceeded"/> / <see cref="LobbyAttributesUpdateFailed"/> 이벤트로 전달됩니다.</para>
    /// </summary>
    /// <param name="attributes">한 번에 반영할 속성 목록.</param>
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
    /// <returns>current lobby id</returns>
    public string GetCurrentLobbyId()
    {
        return currentLobbyId;
    }
}