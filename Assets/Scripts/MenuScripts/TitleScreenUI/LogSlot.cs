using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LogSlot : MonoBehaviour
{
    public Image Border;
    public CanvasGroup CanvasGroup;
    public TMP_Text AnomalyIDText;
    public TMP_Text TierText;
    public TMP_Text TimesBeatenText;
    public Image Sprite;
    public GameObject DarkOverlay;

    public void UpdateLogSlot(string AnomalyID, int Tier, bool HasEncountered, int TimesBeaten,  Sprite AnomalySprite, Sprite QuestionMark, Color TierColor)
    {
        if (HasEncountered == true)
        {
            AnomalyIDText.text = "ANOMALY ID#" + AnomalyID;
            TierText.text = "TIER " + Tier.ToString();
            TierText.color = TierColor;
            TimesBeatenText.color = TierColor;
            Sprite.sprite = AnomalySprite;
            DarkOverlay.SetActive(false);
            if (TimesBeaten > 0)
            {
                CanvasGroup.alpha = 1.0f;
            }
            else
            {
                CanvasGroup.alpha = 0.1f;
                TimesBeatenText.color = Color.white;
            }
        }
        else
        {
            CanvasGroup.alpha = 1.0f;
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
