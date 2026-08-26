using UnityEngine;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(Image))]
public class BossProximityVignette : MonoBehaviour
{
    [Header("타겟 (비워두면 태그로 자동 탐색)")]
    public Transform boss;
    public string bossTag = "Boss";
    public Transform localPlayer;
    public string playerTag = "Player";

    [Header("거리 기준")]
    [Tooltip("이 거리보다 멀면 효과 없음")]
    public float safeDistance = 12f;
    [Tooltip("이 거리 이하면 효과 최대")]
    public float dangerDistance = 3f;

    [Header("색상 / 세기")]
    public Color edgeColor = new Color(0.55f, 0f, 0f, 1f);
    public Color coreCrackColor = Color.black;
    public float maxAlpha = 0.85f;
    public float pulseSpeed = 4f;
    public float pulseAmount = 0.15f;
    [Tooltip("클수록 중간 거리에서부터 확 진해짐 (1=선형, 2~3 추천)")]
    public float curveSharpness = 2.5f;
    [Tooltip("최대 위험일 때 검정이 얼마나 섞일지 (0=계속 빨강, 1=완전 검정)")]
    [Range(0f, 1f)]
    public float maxBlackMix = 0.2f;

    private Image vignetteImage;
    private float findTimer = 0f;

    void Start()
    {
        vignetteImage = GetComponent<Image>();
        vignetteImage.sprite = GenerateVignetteSprite();
        vignetteImage.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, 0f);
        vignetteImage.raycastTarget = false;
    }

    void Update()
    {
        if (boss == null || localPlayer == null)
        {
            findTimer += Time.deltaTime;
            if (findTimer < 0.5f) return;
            findTimer = 0f;

            if (boss == null)
            {
                GameObject b = GameObject.FindGameObjectWithTag(bossTag);
                if (b != null) boss = b.transform;
            }
            if (localPlayer == null)
            {
                foreach (GameObject p in GameObject.FindGameObjectsWithTag(playerTag))
                {
                    NetworkIdentity identity = p.GetComponent<NetworkIdentity>();
                    if (identity != null && identity.isLocalPlayer)
                    {
                        localPlayer = p.transform;
                        break;
                    }
                }
            }
        }

        if (boss == null || localPlayer == null) return;

        float dist = Vector2.Distance(localPlayer.position, boss.position);
        float rawT = Mathf.Clamp01(Mathf.InverseLerp(safeDistance, dangerDistance, dist));

        // 선형이면 거의 붙어야 강해지는 느낌이라, 곡선을 줘서 중간 거리에서도 눈에 띄게 만듦
        float t = 1f - Mathf.Pow(1f - rawT, curveSharpness);

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount * t;
        float alpha = maxAlpha * t * pulse;

        Color mixed = Color.Lerp(edgeColor, coreCrackColor, t * maxBlackMix);
        vignetteImage.color = new Color(mixed.r, mixed.g, mixed.b, alpha);
    }

    private Sprite GenerateVignetteSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float alpha = Mathf.Clamp01(Mathf.Pow(dist, 2.2f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
