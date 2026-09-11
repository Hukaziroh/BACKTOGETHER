using UnityEngine;
using Mirror;

public class CoopWallManager : NetworkBehaviour
{
    [Header("연결할 버튼들")]
    [Tooltip("여기에 4개의 CoopButton 오브젝트를 드래그해서 넣으세요.")]
    public CoopButton[] requiredButtons;

    [Header("사라질 벽 오브젝트")]
    public GameObject wallVisuals;
    public Collider2D wallCollider;

    [SyncVar(hook = nameof(OnWallOpenChanged))]
    public bool isOpen = false;
    public override void OnStartServer()
    {
        base.OnStartServer();
        CoopButtonUtility.SubscribeButtons(requiredButtons, CheckAllButtons);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isOpen)
        {
            if (wallVisuals != null) wallVisuals.SetActive(false);
            if (wallCollider != null) wallCollider.enabled = false;
        }
    }
    [Server]
    private void CheckAllButtons(CoopButton changedButton)
    {
        if (isOpen) return;
        if (CoopButtonUtility.AreAllPressed(requiredButtons))
        {
            isOpen = true;
        }
    }

    void OnWallOpenChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            if (wallVisuals != null) wallVisuals.SetActive(false);
            if (wallCollider != null) wallCollider.enabled = false;

            Debug.Log("모든 버튼이 눌려 벽이 영구적으로 사라졌습니다!");
        }
    }
}