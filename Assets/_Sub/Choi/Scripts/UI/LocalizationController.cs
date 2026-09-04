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

    private const string LanguagePrefKey = "Language";

    [Header("현재 설정된 언어")]
    public Language currentLanguage = Language.English;

    private static List<LocalizedText> allLocalizedTexts = new List<LocalizedText>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadSavedLanguage();
            DontDestroyOnLoad(gameObject);
            // Apply the saved language to texts that enabled before this controller.
            RefreshAllTexts();
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
        if (!System.Enum.IsDefined(typeof(Language), newLang)) return;

        currentLanguage = newLang;
        PlayerPrefs.SetInt(LanguagePrefKey, (int)currentLanguage);
        PlayerPrefs.Save();
        RefreshAllTexts();
    }

    private void LoadSavedLanguage()
    {
        int savedLanguage = PlayerPrefs.GetInt(LanguagePrefKey, (int)currentLanguage);
        if (System.Enum.IsDefined(typeof(Language), savedLanguage))
        {
            currentLanguage = (Language)savedLanguage;
        }
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
