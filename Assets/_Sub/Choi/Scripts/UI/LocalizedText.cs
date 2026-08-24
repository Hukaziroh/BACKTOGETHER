using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    private TextMeshProUGUI tmpText;
    private Material runtimeFontMaterial;

    [Header("언어별 텍스트 데이터 (직접 입력)")]
    [TextArea] public string koreanText;
    [TextArea] public string englishText;
    [TextArea] public string japaneseText;
    [TextArea] public string chineseText;
    [TextArea] public string russianText;
    [TextArea] public string italianText;
    [TextArea] public string frenchText;
    [TextArea] public string germanText;
    [TextArea] public string spanishText;
    [TextArea] public string portugueseText;

    [Header("가독성 보정 설정 (영어 메인폰트 전용)")]
    public float englishDilate = 1f;
    public float englishOutlineThickness = 1;

    [Header("기본 가독성 설정 (다국어/폴백폰트용)")]
    public float defaultDilate = 1f;
    public float defaultOutlineThickness = 1;

    private const string FaceDilateId = "_FaceDilate";
    private const string OutlineWidthId = "_OutlineWidth";
    private const string OutlineSoftnessId = "_OutlineSoftness";
    private const string UnderlayColorId = "_UnderlayColor";
    private const string UnderlayOffsetXId = "_UnderlayOffsetX";
    private const string UnderlayOffsetYId = "_UnderlayOffsetY";
    private const string UnderlayDilateId = "_UnderlayDilate";
    private const string UnderlaySoftnessId = "_UnderlaySoftness";
    private const string GlowPowerId = "_GlowPower";

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        CreateRuntimeMaterial();
    }

    void OnEnable()
    {
        LocalizationController.RegisterText(this);
        UpdateTextAndStyle();
    }

    void OnDisable()
    {
        LocalizationController.UnregisterText(this);
    }

    void OnDestroy()
    {
        if (runtimeFontMaterial != null)
        {
            Destroy(runtimeFontMaterial);
        }
    }

    public void UpdateTextAndStyle()
    {
        if (tmpText == null || LocalizationController.Instance == null) return;

        Language lang = LocalizationController.Instance.currentLanguage;

        switch (lang)
        {
            case Language.Korean:
                tmpText.text = koreanText;
                break;
            case Language.English:
                tmpText.text = string.IsNullOrEmpty(englishText) ? koreanText : englishText;
                break;
            case Language.Japanese:
                tmpText.text = string.IsNullOrEmpty(japaneseText) ? koreanText : japaneseText;
                break;
            case Language.Chinese:
                tmpText.text = string.IsNullOrEmpty(chineseText) ? koreanText : chineseText;
                break;
            case Language.Russian:
                tmpText.text = string.IsNullOrEmpty(russianText) ? koreanText : russianText;
                break;
            case Language.Italian:
                tmpText.text = string.IsNullOrEmpty(italianText) ? koreanText : italianText;
                break;
            case Language.French:
                tmpText.text = string.IsNullOrEmpty(frenchText) ? koreanText : frenchText;
                break;
            case Language.German:
                tmpText.text = string.IsNullOrEmpty(germanText) ? koreanText : germanText;
                break;
            case Language.Spanish:
                tmpText.text = string.IsNullOrEmpty(spanishText) ? koreanText : spanishText;
                break;
            case Language.Portuguese:
                tmpText.text = string.IsNullOrEmpty(portugueseText) ? koreanText : portugueseText;
                break;
        }

        Material mat = GetRuntimeMaterial();
        if (mat != null)
        {
            if (lang == Language.English)
            {
                ApplyReadableTextStyle(mat, englishOutlineThickness);
            }
            else
            {
                ApplyReadableTextStyle(mat, defaultOutlineThickness);
            }

            tmpText.fontMaterial = mat;
            tmpText.SetMaterialDirty();
            tmpText.SetVerticesDirty();
        }
    }

    private Material GetRuntimeMaterial()
    {
        if (runtimeFontMaterial == null)
        {
            CreateRuntimeMaterial();
        }

        return runtimeFontMaterial;
    }

    private void CreateRuntimeMaterial()
    {
        if (tmpText == null) return;

        Material sourceMaterial = tmpText.fontSharedMaterial;
        if (sourceMaterial == null) return;

        runtimeFontMaterial = new Material(sourceMaterial)
        {
            name = sourceMaterial.name + " (LocalizedText Instance)"
        };

        tmpText.fontMaterial = runtimeFontMaterial;
    }

    private void ApplyReadableTextStyle(Material mat, float outlinePixels)
    {
        DisableTmpBoxArtifacts(mat);
        ApplyTmpOutline(mat, outlinePixels);
    }

    private void DisableTmpBoxArtifacts(Material mat)
    {
        if (mat.HasProperty(FaceDilateId))
        {
            mat.SetFloat(FaceDilateId, 0f);
        }

        if (mat.HasProperty(OutlineWidthId))
        {
            mat.SetFloat(OutlineWidthId, 0f);
        }

        SetMaterialFloatIfExists(mat, OutlineSoftnessId, 0f);

        if (mat.HasProperty(UnderlayColorId))
        {
            mat.SetColor(UnderlayColorId, Color.clear);
        }

        SetMaterialFloatIfExists(mat, UnderlayOffsetXId, 0f);
        SetMaterialFloatIfExists(mat, UnderlayOffsetYId, 0f);
        SetMaterialFloatIfExists(mat, UnderlayDilateId, 0f);
        SetMaterialFloatIfExists(mat, UnderlaySoftnessId, 0f);
        SetMaterialFloatIfExists(mat, GlowPowerId, 0f);

        mat.DisableKeyword("UNDERLAY_ON");
        mat.DisableKeyword("UNDERLAY_INNER");
        mat.DisableKeyword("GLOW_ON");
    }

    private void ApplyTmpOutline(Material mat, float outlinePixels)
    {
        // Inspector의 1은 1px 외곽선 의미로 유지하고 TMP의 0~1 셰이더 값으로 변환한다.
        float safeOutline = Mathf.Clamp(outlinePixels, 0f, 1f) * 0.1f;

        if (mat.HasProperty(OutlineWidthId))
        {
            mat.SetFloat(OutlineWidthId, safeOutline);
        }
    }

    private void SetMaterialFloatIfExists(Material mat, string propertyId, float value)
    {
        if (mat.HasProperty(propertyId))
        {
            mat.SetFloat(propertyId, value);
        }
    }
}
