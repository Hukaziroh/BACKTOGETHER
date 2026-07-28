using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    private TextMeshProUGUI tmpText;

    [Header("언어별 텍스트 데이터 (직접 입력)")]
    [TextArea] public string koreanText;
    [TextArea] public string englishText;
    [TextArea] public string japaneseText;
    [TextArea] public string chineseText;
    [TextArea] public string russianText;

    [Header("가독성 보정 설정 (영어 메인폰트 전용)")]
    public float englishDilate = 0f;          // 뭉개지지 않도록 기본값 0 기준 조절
    public float englishOutlineThickness = 0.4f; // 깔끔한 테두리 두께

    [Header("기본 가독성 설정 (다국어/폴백폰트용)")]
    public float defaultDilate = 0f;
    public float defaultOutlineThickness = 0.1f;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
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
        }

        Material mat = tmpText.fontMaterial;
        if (mat != null)
        {
            if (lang == Language.English)
            {
                mat.SetFloat("_FaceDilate", englishDilate);
                mat.SetFloat("_OutlineWidth", englishOutlineThickness);
            }
            else
            {
                mat.SetFloat("_FaceDilate", defaultDilate);
                mat.SetFloat("_OutlineWidth", defaultOutlineThickness);
            }

            // 변경된 머티리얼 인스턴스를 다시 적용하고 갱신을 강제함
            tmpText.fontMaterial = mat;
            tmpText.SetVerticesDirty();
        }
    }
}