using UnityEngine;

public class SceneButton : MonoBehaviour
{

    public void SwapScene()
    {
        SceneSwapper.Instance.ChangeScenarioEasy();
    }
}
