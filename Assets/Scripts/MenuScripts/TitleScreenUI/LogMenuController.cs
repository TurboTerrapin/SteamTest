using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class LogMenuController : MonoBehaviour
{
    // PlayerLogData = STATE 
    // Saved to JSON
    [System.Serializable]
    public class PlayerLogData 
    {
        public bool HasEncountered;
        public int TimesBeaten;
    }

    // AnomalyData = DEFINITION (identified name, tier, and sprite)
    // Not saved to JSON
    [System.Serializable]
    public class AnomalyDatabase 
    {
        public string AnomalyID;
        public int Tier;
        public Color TierColor;
        public Sprite Sprite;
    }

    // Wrapper class for JSON serialization
    [System.Serializable]
    public class LogSaveData
    {
        public List<PlayerLogData> logs = new List<PlayerLogData>();
    }

    public AnomalyDatabase[] IdentifiedAnomalyInfo; // All anomaly definitions

    public static List<PlayerLogData> PlayerLogDataList = new List<PlayerLogData>(); // Runtime player progress

    public static int TotalAnomalies = 16;

    public GameObject LogsMenu;
    public GameObject MainMenu;

    public GameObject Page1;
    public GameObject Page2;

    public GameObject NextPageButton;
    public GameObject PreviousPageButton;
    public TMP_Text PageNumberText;

    public LogSlot[] LogSlots; // UI slot for each anomaly

    public Sprite QuestionMark; // Unidentified Anomaly Sprite

    private static string FilePath;

    private void Start()
    {
        // If unloaded, load data
        if (PlayerLogDataList.Count == 0)
        {
            LoadLogData();
        }
        //ResetLogs(); // For testing purposes 
        //OnScenarioBeaten(0); // For testing purpose (0 is ANOMALY 1A)
        //OnScenarioBeaten(1); // For testing purpose (1 is ANOMALY 1B)
        UpdateLogUI();

        Page1.SetActive(true);
        NextPageButton.SetActive(true);

        Page2.SetActive(false);
        PreviousPageButton.SetActive(false);        
    }

    // Creates default unknown anomaly slots if no save file exists
    public static void CreateUnidentifiedAnomalyLogs() 
    {
        PlayerLogDataList = new List<PlayerLogData>(); // Reset list

        // Loop through each anomaly and set player progress to 0
        for (int i = 0; i < TotalAnomalies; i++) 
        {
            PlayerLogDataList.Add(new PlayerLogData
            {
                HasEncountered = false,
                TimesBeaten = 0
            });
        }
    }
    
    // Clears all log data (for testing purposes)
    public static void ResetLogs()
    {
        PlayerLogDataList.Clear();
        if (FilePath != "" && File.Exists(FilePath)) 
        {
            File.Delete(FilePath);
        }
        LoadLogData();
        SaveLogData();
    }

    // Updates all log slots UI
    public void UpdateLogUI()
    {
        // Loop through the 16 anomaly slots
        for (int i = 0; i < LogSlots.Length; i++) 
        {
            // If there is saved player data in this slot (unidentified/identified)
            if (i < PlayerLogDataList.Count)
            {
                Sprite CurrentSprite = QuestionMark;
                int CurrentTier = 0;
                Color CurrentTierColor = Color.white;
                string CurrentAnomalyID = "UNIDENTIFIED ANOMALY";

                // If the player has encountered the anomaly before, set all anomaly data to identified info
                if (PlayerLogDataList[i].HasEncountered)
                {
                    CurrentSprite = IdentifiedAnomalyInfo[i].Sprite;
                    CurrentTier = IdentifiedAnomalyInfo[i].Tier;
                    CurrentTierColor = IdentifiedAnomalyInfo[i].TierColor;
                    CurrentAnomalyID = IdentifiedAnomalyInfo[i].AnomalyID;
                }

                // Send everything to UpdateLogSlot() in LogSlot.cs
                LogSlots[i].UpdateLogSlot(
                    CurrentAnomalyID,
                    CurrentTier,
                    PlayerLogDataList[i].HasEncountered,
                    PlayerLogDataList[i].TimesBeaten,
                    CurrentSprite,
                    QuestionMark,
                    CurrentTierColor
                );

            }
        }
    }

    // Marks anomaly as encountered and updates/saves the data
    public static void OnScenarioEncountered(int index)
    {
        LoadLogData();
        PlayerLogDataList[index].HasEncountered = true;
        SaveLogData();
    }

    // Increments anomaly beaten count and updates/saves the data
    public static void OnScenarioBeaten(int index) 
    {
        LoadLogData();
        PlayerLogDataList[index].HasEncountered = true;
        PlayerLogDataList[index].TimesBeaten++;
        SaveLogData();
    }

    // Saves log data into a JSON file
    public static void SaveLogData()
    {
        // Create wrapper object with current log data so it can be written
        LogSaveData data = new LogSaveData 
        {
            logs = PlayerLogDataList 
        };

        string json = JsonUtility.ToJson(data); // Convert to JSON string
        File.WriteAllText(FilePath, json); // Write file
    }

    // Loads log data from disk OR calls CreateUnknownAnomalyLogs()
    public static void LoadLogData()
    {
        // If loaded already, return
        if (PlayerLogDataList.Count > 0)
        {
            return;
        }

        FilePath = Path.Combine(Application.persistentDataPath, "logData.json"); // file path where JSON is stored
        if (File.Exists(FilePath))
        {
            string json = File.ReadAllText(FilePath); // Read file
            LogSaveData data = JsonUtility.FromJson<LogSaveData>(json); // Parse JSON

            PlayerLogDataList = data.logs;
        }
        else
        {
            CreateUnidentifiedAnomalyLogs();
        }
    }
 
    public void HandleNextPageButtonClick()
    {
        Page1.SetActive(false);
        NextPageButton.SetActive(false);

        Page2.SetActive(true);
        PreviousPageButton.SetActive(true);
        PageNumberText.text = "2";
    }

    public void HandlePreviousPageButtonClick()
    {
        Page2.SetActive(false);
        PreviousPageButton.SetActive(false);

        Page1.SetActive(true);
        NextPageButton.SetActive(true);
        PageNumberText.text = "1";
    }

    public void HandleXButtonClick()
    {
        LogsMenu.SetActive(false);
        MainMenu.SetActive(true);
    }
}