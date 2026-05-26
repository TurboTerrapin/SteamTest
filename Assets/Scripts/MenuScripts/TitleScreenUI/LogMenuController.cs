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
        public int TimesBeaten;
        public bool HasBeaten;
    }

    // AnomalyData = DEFINITION (identified name, tier, and sprite)
    // Not saved to JSON
    [System.Serializable]
    public class AnomalyDatabase 
    {
        public string AnomalyID;
        public string Tier;
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

    public List<PlayerLogData> PlayerLogDataList = new List<PlayerLogData>(); // Runtime player progress

    public int TotalAnomalies = 16;

    public GameObject LogsMenu;
    public GameObject MainMenu;

    public GameObject Page1;
    public GameObject Page2;

    public GameObject NextPageButton;
    public GameObject PreviousPageButton;
    public TMP_Text PageNumberText;

    public LogSlot[] LogSlots; // UI slot for each anomaly

    public Sprite QuestionMark; // Unidentified Anomaly Sprite

    private string filePath; // file path where JSON is stored

    void Start()
    {
        // Build save file path
        filePath = Path.Combine(Application.persistentDataPath, "logData.json");

        LoadLogData();

        //ResetLogs(); // For testing purposes 
        //HasBeaten(0); // For testing purpose (0 is ANOMALY 1A)
        //HasBeaten(1); // For testing purpose (1 is ANOMALY 1B)

        Page1.SetActive(true);
        NextPageButton.SetActive(true);

        Page2.SetActive(false);
        PreviousPageButton.SetActive(false);        
    }

    // Creates default unknown anomaly slots if no save file exists
    public void CreateUnidentifiedAnomalyLogs() 
    {
        PlayerLogDataList = new List<PlayerLogData>(); // Reset list

        // Loop through each anomaly and set player progress to 0
        for (int i = 0; i < TotalAnomalies; i++) 
        {
            PlayerLogDataList.Add(new PlayerLogData
            {
                TimesBeaten = 0,
                HasBeaten = false
            });
        }
    }
    
    // Clears all log data (for testing purposes)
    public void ResetLogs()
    {
        PlayerLogDataList.Clear();
        CreateUnidentifiedAnomalyLogs();
        UpdateLogUI() ;
        if (File.Exists(filePath)) 
        {
            File.Delete(filePath);
        }

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
                string CurrentTier = "Tier ?";
                Color CurrentTierColor = Color.white;
                string CurrentAnomalyID = "UNIDENTIFIED ANOMALY";

                // if the player has beaten any anomaly before, set all anomaly data to identified info
                if (PlayerLogDataList[i].HasBeaten)
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
                    PlayerLogDataList[i].TimesBeaten,
                    PlayerLogDataList[i].HasBeaten,
                    CurrentSprite,
                    QuestionMark,
                    CurrentTierColor
                );

            }
        }
    }

    // Marks an anomaly as beaten and updates/saves the data
    public void HasBeaten(int index) 
    {
        PlayerLogDataList[index].HasBeaten = true;
        PlayerLogDataList[index].TimesBeaten++;

        SaveLogData();
        UpdateLogUI();
    }

    // Saves log data into a JSON file
    public void SaveLogData()
    {
        // Create wrapper object with current log data so it can be written
        LogSaveData data = new LogSaveData 
        {
            logs = PlayerLogDataList 
        };

        string json = JsonUtility.ToJson(data); // Convert to JSON string
        File.WriteAllText(filePath, json); // Write file
    }

    // Loads log data from disk OR calls CreateUnknownAnomalyLogs()
    public void LoadLogData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath); // Read file
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
