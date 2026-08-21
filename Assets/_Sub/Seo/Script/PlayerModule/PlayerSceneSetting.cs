using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSceneSetting : MonoBehaviour
{
    [Header("적용할 씬")]
    [SerializeField] private string[] targetSceneNames;

    [Header("다른 플레이어 몸통 알파")]
    [Range(0f, 1f)]
    [SerializeField] private float otherPlayerBodyAlpha = 0.5f;

    [Header("다른 플레이어 눈 알파")]
    [Range(0f, 1f)]
    [SerializeField] private float otherPlayerFaceAlpha = 0.5f;

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

        bool isMine = identity.isLocalPlayer;

        float bodyAlpha = isMine ? 1f : otherPlayerBodyAlpha;
        float faceAlpha = isMine ? 1f : otherPlayerFaceAlpha;

        // 몸통
        if (playerSpriteRenderer != null)
        {
            Color color = playerSpriteRenderer.color;
            color.a = bodyAlpha;
            playerSpriteRenderer.color = color;
        }

        // 눈
        if (faceSpriteRenderer != null)
        {
            Color color = faceSpriteRenderer.color;
            color.a = faceAlpha;
            faceSpriteRenderer.color = color;
        }
    }

    private bool IsTargetScene()
    {
        if (targetSceneNames == null || targetSceneNames.Length == 0)
            return false;

        string currentSceneName = SceneManager.GetActiveScene().name;

        foreach (string sceneName in targetSceneNames)
        {
            if (currentSceneName == sceneName)
                return true;
        }

        return false;
    }
}