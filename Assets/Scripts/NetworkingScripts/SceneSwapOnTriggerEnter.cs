using UnityEngine;

public class SceneSwapOnTriggerEnter : MonoBehaviour
{

    private enum Difficulty
    {
        Random,
        Easy,
        Medium,
        Hard,
        Specific
    }

    [SerializeField]
    private Difficulty difficulty = Difficulty.Easy;

    [SerializeField]
    private int specificSceneNum = 0;
    [SerializeField]
    private string specificSceneName = "Placeholder";

    void OnTriggerEnter(Collider other)
    {
        if (difficulty == Difficulty.Random)
        {
            SceneSwapper.Instance.ChangeSceneRandom();
        }
        else if (difficulty == Difficulty.Easy)
        {
            SceneSwapper.Instance.ChangeScenarioEasy();
        }
        else if (difficulty == Difficulty.Medium)
        {
            SceneSwapper.Instance.ChangeScenarioMedium();
        }
        else if (difficulty == Difficulty.Hard)
        {
            SceneSwapper.Instance.ChangeScenarioHard();
        }
        else if (difficulty == Difficulty.Specific)
        {
            SceneSwapper.Instance.ChangeScene(specificSceneName, specificSceneNum);
        }
    }
}
