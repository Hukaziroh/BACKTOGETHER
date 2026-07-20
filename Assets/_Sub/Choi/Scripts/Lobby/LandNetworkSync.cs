using UnityEngine;
using Mirror;

public class LandNetworkSync : NetworkBehaviour
{
    // 누구나 서버로 비활성화를 요청할 수 있음 ([Command(requiresAuthority = false)])
    [Command(requiresAuthority = false)]
    public void CmdDisableLand()
    {
        RpcDisableLand();
    }

    [ClientRpc]
    private void RpcDisableLand()
    {
        gameObject.SetActive(false);
        Debug.Log("[땅 시스템] 땅이 비활성화되었습니다: " + gameObject.name);
    }
}