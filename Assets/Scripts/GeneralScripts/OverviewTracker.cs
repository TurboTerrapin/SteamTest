/*
    OverviewTracker.cs
    - Handles star date
    - Handles distance to DSF
    - Handles updating the screen in back of bridge
    Contributor(s): Jake Schott
    Last Updated: 6/19/2026
*/


using UnityEngine;

public class OverviewTracker : MonoBehaviour
{
    private static float STARTING_STAR_DATE = 5199.509f;
    private static float ENDING_STAR_DATE = 5201.999f;
    private static float DISTANCE_TO_DSF = 0.099f;

    public GameObject overview_display;

    public static string getStarDate(float percentage_to_DSF)
    {
        float star_date = Mathf.Lerp(STARTING_STAR_DATE, ENDING_STAR_DATE, percentage_to_DSF);
        star_date = Mathf.Round(star_date * 1000.0f) / 1000.0f;
        return star_date.ToString();
    }

    public static string getDistanceToDSF(float percentage_to_DSF)
    {
        float distance_to_DSF = Mathf.Lerp(DISTANCE_TO_DSF, 0.0f, percentage_to_DSF);
        distance_to_DSF = Mathf.Round(distance_to_DSF * 1000.0f) / 1000.0f;
        return distance_to_DSF.ToString();
    }
}
