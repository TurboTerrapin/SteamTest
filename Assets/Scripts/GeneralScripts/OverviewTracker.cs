/*
    OverviewTracker.cs
    - Handles star date
    - Handles distance to DSF
    - Handles updating the screen in back of bridge
    Contributor(s): Jake Schott
    Last Updated: 6/21/2026
*/

using TMPro;
using UnityEngine;

public class OverviewTracker : MonoBehaviour
{
    private static float STARTING_STAR_DATE = 5199.509f;
    private static float ENDING_STAR_DATE = 5201.999f;
    private static float DISTANCE_TO_DSF = 0.099f;

    public GameObject overview_display;

    private static string trailingZerosHelper(float number, int digits_before_decimal, int desired_length)
    {
        string ans_string = number.ToString();
        if (ans_string.Length < desired_length)
        {
            if (ans_string.Length == digits_before_decimal)
            {
                ans_string = ans_string + ".";
            }
            while (ans_string.Length < desired_length)
            {
                ans_string = ans_string + "0";
            }
        }
        return ans_string;
    }

    public static string getStarDate(float percentage_to_DSF)
    {
        float star_date = Mathf.Lerp(STARTING_STAR_DATE, ENDING_STAR_DATE, percentage_to_DSF);
        star_date = Mathf.Round(star_date * 1000.0f) / 1000.0f;
        return trailingZerosHelper(star_date, 3, 8);
    }

    public static string getDistanceToDSF(float percentage_to_DSF)
    {
        float distance_to_DSF = Mathf.Lerp(DISTANCE_TO_DSF, 0.0f, percentage_to_DSF);
        distance_to_DSF = Mathf.Round(distance_to_DSF * 1000.0f) / 1000.0f;
        return trailingZerosHelper(distance_to_DSF, 1, 5);
    }

    public void updateOverviewDisplay(float percentage_to_DSF)
    {
        overview_display.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>().fillAmount = Mathf.Max(0.01f, percentage_to_DSF);
        overview_display.transform.GetChild(4).GetComponent<TMP_Text>().SetText("DISTANCE REMAINING: " + getDistanceToDSF(percentage_to_DSF) + " ly");
        overview_display.transform.GetChild(5).GetComponent<TMP_Text>().SetText("STARDATE: " + getStarDate(percentage_to_DSF));
        if (percentage_to_DSF == 1.0f)
        {
            overview_display.transform.GetChild(1).GetChild(1).GetComponent<UnityEngine.UI.RawImage>().color = new Color(0.0f, 0.84f, 1.0f, 1.0f);
        }
    }
}