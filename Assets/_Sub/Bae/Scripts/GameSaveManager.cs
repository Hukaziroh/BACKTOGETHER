using UnityEngine;
using System.IO;

public class GameSaveManager : MonoBehaviour
{
    // 어디서든 쉽게 접근하기 위한 싱글톤
    public static GameSaveManager Instance { get; private set; }

    [System.Serializable]
    public class SaveData
    {
        public int maxClearedChapter = 0; // 클리어한 가장 높은 챕터 번호
    }

    public SaveData currentData = new SaveData();
    private string saveFilePath;

    private void Awake()
    {
        // 싱글톤 세팅 및 씬 전환 시 파괴 방지
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

        // 스토브/스팀 런처가 자동 백업하기 좋은 폴더에 저장 경로 설정
        saveFilePath = Path.Combine(Application.persistentDataPath, "PicoSaveData.json");
        LoadGame(); // 게임 켜질 때 자동 로드
    }

    // 챕터를 클리어 했을 때 부르는 함수
    public void ClearChapter(int chapterNumber)
    {
        // 방금 깬 챕터가 내 최고 기록보다 높을 때만 갱신
        if (chapterNumber > currentData.maxClearedChapter)
        {
            currentData.maxClearedChapter = chapterNumber;
            SaveGame();
        }
    }

    // 실제 파일 저장 (JSON)
    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"[Save] 로컬 세이브 완료! 최고 클리어 챕터: {currentData.maxClearedChapter} / 경로: {saveFilePath}");
    }

    // 파일 불러오기
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
            SaveGame(); // 최초 파일 생성
        }
    }
}