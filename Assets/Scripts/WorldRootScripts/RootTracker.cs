using System.Collections.Generic;
using UnityEngine;

public class RootTracker : MonoBehaviour
{
    private List<Rigidbody> childRigidbodies = new List<Rigidbody>();
    private Dictionary<Rigidbody, Vector3> childRelativeVelocities = new Dictionary<Rigidbody, Vector3>();

    private Vector3 previousPosition;

    void Start()
    {
        previousPosition = transform.position;

        Rigidbody[] allChildren = GetComponentsInChildren<Rigidbody>(includeInactive: true);
        foreach (Rigidbody rb in allChildren)
        {
            if (rb.gameObject != gameObject && rb.isKinematic == false)
            {
                childRigidbodies.Add(rb);
                childRelativeVelocities[rb] = Vector3.zero;
            }
        }

        Debug.Log($"[WorldRootVelocityTracker] Collected {childRigidbodies.Count} dynamic rigidbody children.");
    }

    void FixedUpdate()
    {
        Vector3 rootDelta = transform.position - previousPosition;
        Vector3 rootVelocity = rootDelta / Time.fixedDeltaTime;

        foreach (Rigidbody rb in childRigidbodies)
        {
            Vector3 childVelocity = rb.linearVelocity;
            Vector3 relativeVelocity = childVelocity - rootVelocity;

            childRelativeVelocities[rb] = relativeVelocity;

        }

        previousPosition = transform.position;
    }


    public Vector3 GetRelativeVelocity(Rigidbody child)
    {
        if (childRelativeVelocities.TryGetValue(child, out var vel))
            return vel;

        return Vector3.zero;
    }

    public Dictionary<Rigidbody, Vector3> GetAllRelativeVelocities()
    {
        return new Dictionary<Rigidbody, Vector3>(childRelativeVelocities);
    }
}
