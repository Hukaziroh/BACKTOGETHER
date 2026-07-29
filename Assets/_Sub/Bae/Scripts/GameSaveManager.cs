using UnityEngine;
using System.IO;

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [System.Serializable]
    public class SaveData
    {
        public int maxClearedChapter = 0; 
    }

    public SaveData currentData = new SaveData();
    private string saveFilePath;

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
            return;
        }
        saveFilePath = Path.Combine(Application.persistentDataPath, "PicoSaveData.json");
        LoadGame(); 
    }
    public void ClearChapter(int chapterNumber)
    {
        if (chapterNumber > currentData.maxClearedChapter)
        {
            currentData.maxClearedChapter = chapterNumber;
            SaveGame();
        }
    }
    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[Save] 로컬 세이브 완료! 최고 클리어 챕터: {currentData.maxClearedChapter} / 경로: {saveFilePath}");
    }
    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[Load] 세이브 로드 완료. 최고 클리어 챕터: {currentData.maxClearedChapter}");
        }
        else
        {
            Debug.Log("[Load] 세이브 파일이 없어 새로 생성합니다.");
            SaveGame();
        }
    }
}