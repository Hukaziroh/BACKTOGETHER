using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSceneSetting : MonoBehaviour
{
    [Header("적용할 씬")]
    [SerializeField] private string targetSceneName = "Ex";

    [Header("다른 플레이어 알파")]
    [Range(0f, 1f)]
    [SerializeField] private float otherPlayerAlpha = 0.5f;

    private SpriteRenderer playerSpriteRenderer;
    private SpriteRenderer faceSpriteRenderer;
    private CoopPlayerIdentity identity;

    private int playerLayer;

    private static bool collisionIgnored = false;

    private void Awake()
    {
        playerSpriteRenderer = GetComponent<SpriteRenderer>();

        identity = GetComponent<CoopPlayerIdentity>();

        if (identity != null)
        {
            faceSpriteRenderer = identity.faceSpriteRenderer;
        }

        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void Start()
    {
        if (!IsTargetScene())
            return;

        ApplyCollisionSetting();

        ApplyAlpha();
    }

    private void LateUpdate()
    {
        if (!IsTargetScene())
            return;

        ApplyAlpha();
    }

    private void ApplyCollisionSetting()
    {
        if (collisionIgnored)
            return;

        if (playerLayer < 0)
            return;

        Physics2D.IgnoreLayerCollision(
            playerLayer,
            playerLayer,
            true
        );

        collisionIgnored = true;
    }

    private void ApplyAlpha()
    {
        if (identity == null)
            return;

        // 내 플레이어인지 확인
        bool isMine = identity.isLocalPlayer;

        float alpha = isMine ? 1f : otherPlayerAlpha;

        // 몸통
        if (playerSpriteRenderer != null)
        {
            Color color = playerSpriteRenderer.color;
            color.a = alpha;
            playerSpriteRenderer.color = color;
        }

        // 눈
        if (faceSpriteRenderer != null)
        {
            Color color = faceSpriteRenderer.color;
            color.a = alpha;
            faceSpriteRenderer.color = color;
        }
    }

    private bool IsTargetScene()
    {
        return SceneManager.GetActiveScene().name == targetSceneName;
    }

}