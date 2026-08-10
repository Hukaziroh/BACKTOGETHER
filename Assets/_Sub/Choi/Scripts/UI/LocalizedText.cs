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

            tmpText.fontMaterial = mat;
            tmpText.SetVerticesDirty();
        }
    }
}