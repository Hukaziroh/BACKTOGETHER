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
    public event CreateLobbySuccess CreateLobbySucceeded;

    public delegate void CreateLobbyFailure(string errorMessage);
    public event CreateLobbyFailure CreateLobbyFailed;

    //leave lobby events
    public delegate void LeaveLobbySuccess();
    public event LeaveLobbySuccess LeaveLobbySucceeded;

    public delegate void LeaveLobbyFailure(string errorMessage);
    public event LeaveLobbyFailure LeaveLobbyFailed;

    //destroy lobby events (★ 추가됨)
    public delegate void DestroyLobbySuccess();
    public event DestroyLobbySuccess DestroyLobbySucceeded;

    public delegate void DestroyLobbyFailure(string errorMessage);
    public event DestroyLobbyFailure DestroyLobbyFailed;

    //find lobbies events
    public delegate void FindLobbiesSuccess(List<LobbyDetails> foundLobbies);
    public event FindLobbiesSuccess FindLobbiesSucceeded;

    public delegate void FindLobbiesFailure(string errorMessage);
    public event FindLobbiesFailure FindLobbiesFailed;

    //lobby attribute update events
    public delegate void LobbyAttributeUpdateSuccess();
    public event LobbyAttributeUpdateSuccess LobbyAttributeUpdateSucceeded;

    public delegate void LobbyAttributeUpdateFailure(string errorMessage);
    public event LobbyAttributeUpdateFailure LobbyAttributeUpdateFailed;

    public delegate void LobbyAttributesUpdateSuccess();
    public event LobbyAttributesUpdateSuccess LobbyAttributesUpdateSucceeded;

    public delegate void LobbyAttributesUpdateFailure(string errorMessage);
    public event LobbyAttributesUpdateFailure LobbyAttributesUpdateFailed;

    public static bool IsLobbyJoinable(LobbyDetails lobby, out uint currentMembers, out uint maxMembers)
    {
        currentMembers = lobby.GetMemberCount(new LobbyDetailsGetMemberCountOptions());
        maxMembers = 4;

        LobbyDetailsInfo lobbyInfo;
        if (lobby.CopyInfo(new LobbyDetailsCopyInfoOptions(), out lobbyInfo) == Result.Success)
        {
            maxMembers = lobbyInfo.MaxMembers;
        }

        return currentMembers < maxMembers;
    }

    public void CreateLobby(uint maxAlllowedPlayers, LobbyPermissionLevel permissionLevel, bool presenceEnabled, AttributeData[] initialAttributes = null)
    {
        if (ConnectedToLobby)
        {
            CreateLobbyFailed?.Invoke("이미 로비에 연결되어 있어 새 로비를 생성할 수 없습니다.");
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();

        CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
        {
            Alignment = 1,
            AllowInvites = true,
            BucketId = DefaultAttributeKey,
            MaxLobbyMembers = maxAlllowedPlayers,
            PermissionLevel = permissionLevel,
            PresenceEnabled = presenceEnabled
        };

        lobbyInterface.CreateLobby(createLobbyOptions, null, (CreateLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success)
            {
                CreateLobbyFailed?.Invoke("Error creating lobby: " + callback.ResultCode);
                return;
            }

            LobbyModification modHandle = new LobbyModification();
            lobbyInterface.UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = callback.LobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

            AttributeData hostAddressData = new AttributeData { Key = hostAddressKey, Value = EOSSDKComponent.LocalUserProductIdString };
            modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = hostAddressData, Visibility = LobbyAttributeVisibility.Public });

            if (initialAttributes != null)
            {
                foreach (AttributeData data in initialAttributes)
                {
                    modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = data, Visibility = LobbyAttributeVisibility.Public });
                }
            }

            lobbyInterface.UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo updateCallback) => {
                if (updateCallback.ResultCode != Result.Success)
                {
                    CreateLobbyFailed?.Invoke("Error updating lobby attributes: " + updateCallback.ResultCode);
                    return;
                }

                currentLobbyId = callback.LobbyId;
                ConnectedToLobby = true;
                isLobbyOwner = true;

                EOSSDKComponent.GetLobbyInterface().CopyLobbyDetailsHandleByLobbyId(new CopyLobbyDetailsHandleByLobbyIdOptions { LobbyId = currentLobbyId }, out LobbyDetails details);
                ConnectedLobbyDetails = details;

                List<Attribute> attributes = new List<Attribute>();
                uint attributeCount = details.GetAttributeCount(new LobbyDetailsGetAttributeCountOptions());

                for (uint i = 0; i < attributeCount; i++)
                {
                    details.CopyAttributeByIndex(new LobbyDetailsCopyAttributeByIndexOptions { AttrIndex = i }, out Attribute attribute);
                    attributes.Add(attribute);
                }

                CreateLobbySucceeded?.Invoke(attributes);
            });
        });
    }

    public void LeaveLobby()
    {
        if (string.IsNullOrEmpty(currentLobbyId))
        {
            return;
        }

        IsLeavingLobby = true;

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        LeaveLobbyOptions leaveLobbyOptions = new LeaveLobbyOptions { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId };

        lobbyInterface.LeaveLobby(leaveLobbyOptions, null, (LeaveLobbyCallbackInfo callback) => {
            IsLeavingLobby = false;

            if (callback.ResultCode != Result.Success)
            {
                LeaveLobbyFailed?.Invoke("Error leaving lobby: " + callback.ResultCode);
                return;
            }

            currentLobbyId = string.Empty;
            ConnectedToLobby = false;
            ConnectedLobbyDetails = null;
            isLobbyOwner = false;
            LeaveLobbySucceeded?.Invoke();
        });
    }

    /// <summary>
    /// ★ 추가됨: 방장이 에픽 서버에서 로비를 완전히 파괴/삭제합니다.
    /// </summary>
    public void DestroyLobby()
    {
        if (string.IsNullOrEmpty(currentLobbyId))
        {
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();
        DestroyLobbyOptions destroyLobbyOptions = new DestroyLobbyOptions
        {
            LobbyId = currentLobbyId,
            LocalUserId = EOSSDKComponent.LocalUserProductId
        };

        lobbyInterface.DestroyLobby(destroyLobbyOptions, null, (DestroyLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success)
            {
                Debug.LogWarning("[EOSLobby] DestroyLobby failed: " + callback.ResultCode);
                DestroyLobbyFailed?.Invoke("Error destroying lobby: " + callback.ResultCode);
                LeaveLobby(); // 파괴 실패 시 퇴장이라도 수행
                return;
            }

            Debug.Log("[EOSLobby] 에픽 서버에서 로비가 성공적으로 파괴되었습니다.");
            currentLobbyId = string.Empty;
            ConnectedToLobby = false;
            ConnectedLobbyDetails = null;
            isLobbyOwner = false;

            DestroyLobbySucceeded?.Invoke();
        });
    }

    public void JoinLobby(LobbyDetails lobbyDetails)
    {
        if (ConnectedToLobby)
        {
            return;
        }

        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();

        JoinLobbyOptions joinLobbyOptions = new JoinLobbyOptions
        {
            LobbyDetailsHandle = lobbyDetails,
            LocalUserId = EOSSDKComponent.LocalUserProductId,
            PresenceEnabled = true
        };

        lobbyInterface.JoinLobby(joinLobbyOptions, null, (JoinLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success)
            {
                return;
            }

            currentLobbyId = callback.LobbyId;
            ConnectedToLobby = true;
            ConnectedLobbyDetails = lobbyDetails;
            isLobbyOwner = false;
        });
    }

    public void FindLobbies(uint maxResults = 10, LobbySearchSetParameterOptions[] searchSetParameterOptions = null)
    {
        LobbyInterface lobbyInterface = EOSSDKComponent.GetLobbyInterface();

        lobbyInterface.CreateLobbySearch(new CreateLobbySearchOptions { MaxResults = maxResults }, out LobbySearch lobbySearch);

        if (searchSetParameterOptions != null)
        {
            foreach (LobbySearchSetParameterOptions searchSetParameterOption in searchSetParameterOptions)
            {
                lobbySearch.SetParameter(searchSetParameterOption);
            }
        }

        lobbySearch.Find(new LobbySearchFindOptions { LocalUserId = EOSSDKComponent.LocalUserProductId }, null, (LobbySearchFindCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success)
            {
                FindLobbiesFailed?.Invoke("Error finding lobbies: " + callback.ResultCode);
                return;
            }

            foundLobbies.Clear();
            uint lobbyCount = lobbySearch.GetSearchResultCount(new LobbySearchGetSearchResultCountOptions());

            for (uint i = 0; i < lobbyCount; i++)
            {
                lobbySearch.CopySearchResultByIndex(new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i }, out LobbyDetails lobbyDetails);
                foundLobbies.Add(lobbyDetails);
            }

            FindLobbiesSucceeded?.Invoke(foundLobbies);
        });
    }

    public void UpdateLobbyAttribute(string key, string newValue)
    {
        AttributeData data = new AttributeData { Key = key, Value = newValue };
        UpdateAttribute(data);
    }

    private void UpdateAttribute(AttributeData data)
    {
        if (string.IsNullOrEmpty(currentLobbyId)) return;

        LobbyModification modHandle = new LobbyModification();
        EOSSDKComponent.GetLobbyInterface().UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = currentLobbyId, LocalUserId = EOSSDKComponent.LocalUserProductId }, out modHandle);

        modHandle.AddAttribute(new LobbyModificationAddAttributeOptions { Attribute = data, Visibility = LobbyAttributeVisibility.Public });

        EOSSDKComponent.GetLobbyInterface().UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modHandle }, null, (UpdateLobbyCallbackInfo callback) => {
            if (callback.ResultCode != Result.Success)
            {
                LobbyAttributeUpdateFailed?.Invoke("Error updating attribute: " + callback.ResultCode);
                return;
            }

            LobbyAttributeUpdateSucceeded?.Invoke();
        });
    }

    public string GetCurrentLobbyId()
    {
        return currentLobbyId;
    }
}