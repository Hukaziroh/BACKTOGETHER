using UnityEngine;
using System.IO;
using static Stove.PCSDK.Base; 

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

        saveFilePath = GetStoveCloudSavePath();

        LoadGame();
    }

    /// <summary>
    /// 스토브 SDK를 통해 클라우드 경로를 받아오고, 실패 시 기본 경로로 대체합니다.
    /// </summary>
    private string GetStoveCloudSavePath()
    {
        string cloudPath = string.Empty;
        uint length = 512;

        Result result = Base_GetCloudSavingPath(ref cloudPath, length);

        if (result.IsSuccessful() && !string.IsNullOrEmpty(cloudPath))
        {
            Debug.Log("[Stove] 스토브 클라우드 세이브 경로 획득 성공: " + cloudPath);
            return Path.Combine(cloudPath, "PicoSaveData.json");
        }
        else
        {
            Debug.LogWarning("[Stove] 클라우드 경로 획득 실패 (에디터 테스트 중이거나 런처 미연동). 기본 LocalLow 경로를 사용합니다.");
            return Path.Combine(Application.persistentDataPath, "PicoSaveData.json");
        }
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
        string dir = Path.GetDirectoryName(saveFilePath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[Save] 세이브 저장 완료! 경로: {saveFilePath}");
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            currentData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[Load] 세이브 로드 성공! 최고 챕터: {currentData.maxClearedChapter}");
        }
        else
        {
            Debug.Log("[Load] 세이브 파일이 없어 새로 생성합니다.");
            SaveGame();
        }
    }
}