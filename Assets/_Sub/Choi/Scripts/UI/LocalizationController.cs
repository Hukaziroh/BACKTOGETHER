using UnityEngine;
using System.Collections.Generic;

public enum Language
{
    English = 0,
    Korean = 1,
    Japanese = 2,
    Chinese = 3,
    Russian = 4,
    Italian = 5,
    French = 6,
    German = 7,
    Spanish = 8,
    Portuguese = 9
}

public class LocalizationController : MonoBehaviour
{
    public static LocalizationController Instance;

    [Header("현재 설정된 언어")]
    public Language currentLanguage = Language.English;

    private static List<LocalizedText> allLocalizedTexts = new List<LocalizedText>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeLanguage(int langIndex)
    {
        ChangeLanguage((Language)langIndex);
    }

    public void ChangeLanguage(Language newLang)
    {
        currentLanguage = newLang;
        RefreshAllTexts();
    }

    public static void RegisterText(LocalizedText textComp)
    {
        if (!allLocalizedTexts.Contains(textComp))
            allLocalizedTexts.Add(textComp);
    }

    public static void UnregisterText(LocalizedText textComp)
    {
        if (allLocalizedTexts.Contains(textComp))
            allLocalizedTexts.Remove(textComp);
    }

    private void RefreshAllTexts()
    {
        foreach (var textComp in allLocalizedTexts)
        {
            if (textComp != null)
                textComp.UpdateTextAndStyle();
        }
    }
}