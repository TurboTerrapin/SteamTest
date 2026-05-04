using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LogSlot : MonoBehaviour
{
    public TMP_Text AnomalyIDText;
    public TMP_Text TierText;
    public TMP_Text TimesBeatenText;
    public Image Sprite;
    public GameObject DarkOverlay;

    public void UpdateLogSlot(string AnomalyID, string Tier, int TimesBeaten, bool HasBeaten, Sprite AnomalySprite, Sprite QuestionMark, Color TierColor)
    {
        if (HasBeaten == true)
        {
            AnomalyIDText.text = AnomalyID;
            TierText.text = Tier;
            TierText.color = TierColor;
            TimesBeatenText.color = TierColor;
            Sprite.sprite = AnomalySprite;
            DarkOverlay.SetActive(false);
        }
        else
        {
            AnomalyIDText.text = "UNIDENTIFIED ANOMALY";
            TierText.text = "TIER ?";
            TierText.color = Color.white;
            TimesBeatenText.color = Color.white;
            Sprite.sprite = QuestionMark;
            DarkOverlay.SetActive(true);
        }

        TimesBeatenText.text = TimesBeaten.ToString();
    }
}
