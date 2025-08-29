using UnityEngine;

public class TestTarget : MonoBehaviour
{
    public float health = 100f;

    
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("LongRangePhaser"))
        {
            health -= 10f * Time.deltaTime; 
            Debug.Log($"Target Health: {health}"); 
        }
    }
}