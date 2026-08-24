using UnityEngine;
using Mirror;
using System.Collections;

public class LandNetworkSync : NetworkBehaviour
{
    private Renderer landRenderer;
    private Collider2D landCollider;

    private void Awake()
    {
        // SpriteRenderer, TilemapRenderer 등 종류와 관계없이 렌더러 컴포넌트를 자동으로 가져옵니다.
        landRenderer = GetComponent<Renderer>();
        landCollider = GetComponent<Collider2D>();
    }

    [Command(requiresAuthority = false)]
    public void CmdDisableLand()
    {
        ServerDisableLand();
    }

    [Server]
    public void ServerDisableLand()
    {
        StartCoroutine(LandRespawnRoutine());
    }

    private IEnumerator LandRespawnRoutine()
    {
        // 1. 땅 숨기기 및 충돌 해제
        RpcSetLandActive(false);

        // 2. 3초 대기
        yield return new WaitForSeconds(3.0f);

        // 3. 땅 다시 보이기 및 충돌 복구
        RpcSetLandActive(true);
    }

    [ClientRpc]
    private void RpcSetLandActive(bool isActive)
    {
        if (landRenderer != null) landRenderer.enabled = isActive;
        if (landCollider != null) landCollider.enabled = isActive;
    }
}