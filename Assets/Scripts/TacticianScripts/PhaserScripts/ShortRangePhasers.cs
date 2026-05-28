/*
    ShortRangePhasers.cs
    - Handles short range phaser targeting, firing, and rendering
    Contributor(s): Henryk Musial, Jake Schott
    Last Updated: 5/28/2026
*/

using System.Collections;
using UnityEngine;

public class ShortRangePhasers : MonoBehaviour
{
    private const float SHORT_RANGE_BEAM_RANGE = 500f;
    private float SHORT_RANGE_BEAM_DIAMETER = 1.5f;
    private const string EMISSION_COLOR = "_EmissionColor";
    private float SRMaxTrackingAngle = 15f;
    private float SRTargetScanInterval = 2f;
    private float SRDamagePerSecond = 20f;

    public GameObject[] shortRangePhaserOrigins;
    private LineRenderer[] shortRangePhasers = new LineRenderer[2];
    private GameObject[] shortRangePhaserTargets = new GameObject[2];
    private Material[] shortRangePhaserMaterials = new Material[2];
    private Color[] shortRangeEmissionColors = new Color[2];
    private Coroutine shortRangePhasersCoroutine = null;

    private void Start()
    {
        for (int p = 0; p < 2; p++)
        {
            shortRangePhasers[p] = shortRangePhaserOrigins[p].transform.GetChild(0).GetComponent<LineRenderer>();
            shortRangePhasers[p].useWorldSpace = true;
            shortRangePhaserMaterials[p] = new Material(shortRangePhasers[p].material);
            shortRangeEmissionColors[p] = shortRangePhaserMaterials[p].GetColor(EMISSION_COLOR);
            shortRangePhasers[p].enabled = false;
        }
    }

    public void updateShortRangePhasers()
    {
        if (shortRangePhasersCoroutine != null)
        {
            return;
        }

        bool[] activePhasers = GetComponent<PhaserActivators>().getActivePhasers();
        if (activePhasers[1] == true || activePhasers[2] == true)
        {
            shortRangePhasersCoroutine = StartCoroutine(shortRangePhaserFirer());
        }
    }

    IEnumerator shortRangePhaserFirer()
    {
        bool[] activePhasers = GetComponent<PhaserActivators>().getActivePhasers();
        while (activePhasers[1] == true || activePhasers[2] == true)
        {
            findShortRangeTargets();
            for (int p = 0; p < 2; p++)
            {
                shortRangePhasers[p].enabled = false;
                if (activePhasers[p + 1] == true)
                {
                    fireShortRangePhaser(p);
                }
            }

            float activeTime = 0.2f + GetComponent<PhaserIntensities>().getPhaserTemperatures()[1];
            float activeHalftime = activeTime * 0.5f;
            float timeRemaining = activeTime;
            while (timeRemaining > 0.0f)
            {
                timeRemaining = Mathf.Max(0.0f, timeRemaining - Time.deltaTime);

                float beamWidth = Mathf.Lerp(0.0f, SHORT_RANGE_BEAM_DIAMETER, Mathf.Lerp(0.0f, 1.0f, Mathf.PingPong(timeRemaining, activeHalftime) / activeHalftime));
                for (int p = 0; p < 2; p++)
                {
                    shortRangePhasers[p].startWidth = beamWidth;
                    shortRangePhasers[p].endWidth = beamWidth;
                    shortRangePhasers[p].SetPosition(0, shortRangePhaserOrigins[p].transform.position);
                }

                yield return null;
            }
            
            for (int p = 0; p < 2; p++)
            {
                shortRangePhasers[p].enabled = false;
                if (shortRangePhaserTargets[p] != null)
                {
                    shortRangePhaserTargets[p].GetComponent<IDamageable>().damage(SRDamagePerSecond);
                }
            }

            yield return new WaitForSeconds(0.2f + SRTargetScanInterval - (SRTargetScanInterval * GetComponent<PhaserIntensities>().getPhaserTemperatures()[1]));
            activePhasers = GetComponent<PhaserActivators>().getActivePhasers();
        }

        shortRangePhasersCoroutine = null;
    }

    private Vector3 computeAimDirection(GameObject origin, GameObject target)
    {
        Vector3 defaultForward = origin.transform.forward;
        if (target == null)
        {
            return defaultForward;
        }
        Vector3 originPos = origin.transform.position;
        Vector3 dirToTarget = (target.transform.position - originPos).normalized;
        return Vector3.RotateTowards(defaultForward, dirToTarget, SRMaxTrackingAngle * Mathf.Deg2Rad, 0f);
    }

    private void fireShortRangePhaser(int phaserIndex)
    {
        shortRangePhasers[phaserIndex].enabled = true;

        Vector3 beamStart = shortRangePhaserOrigins[phaserIndex].transform.position;
        Vector3 beamDirection = computeAimDirection(shortRangePhaserOrigins[phaserIndex], shortRangePhaserTargets[phaserIndex]);
        Vector3 beamEnd = beamStart + beamDirection * SHORT_RANGE_BEAM_RANGE;

        if (shortRangePhaserTargets[phaserIndex] != null)
        {
            if (Physics.Raycast(new Ray(beamStart, beamDirection), out RaycastHit hit, SHORT_RANGE_BEAM_RANGE, 8))
            {
                beamEnd = hit.point;
                if (hit.collider.gameObject != shortRangePhaserTargets[phaserIndex])
                {
                    shortRangePhaserTargets[phaserIndex] = null;
                }
            }
        }

        shortRangePhasers[phaserIndex].SetPosition(0, beamStart);
        shortRangePhasers[phaserIndex].SetPosition(1, beamEnd);
    }

    private void findShortRangeTargets()
    {
        Vector3 leftPos = shortRangePhaserOrigins[0].transform.position;
        Vector3 rightPos = shortRangePhaserOrigins[1].transform.position;
        Vector3 midpoint = (leftPos + rightPos) * 0.5f;
        float halfSeparation = Vector3.Distance(leftPos, rightPos) * 0.5f;
        float sharedRadius = SHORT_RANGE_BEAM_RANGE + halfSeparation;

        Collider[] possibleTargets = Physics.OverlapSphere(midpoint, sharedRadius);
        float[] bestDistance = new float[2] { float.MaxValue, float.MaxValue };

        for (int i = 0; i < possibleTargets.Length; i++)
        {
            Collider currentTarget = possibleTargets[i];
            if (currentTarget.GetComponent<IDamageable>() != null && (currentTarget.GetComponent<CollectibleItem>() == null || currentTarget.GetComponent<CollectibleItem>().getItemCategory() > 1))
            {
                for (int p = 0; p < 2; p++)
                {
                    float distToTarget = getDistanceToTarget(p, currentTarget.transform.position);

                    // check for collider to reduce the distance since you're firing at the collider, not the point
                    if (currentTarget.GetComponent<SphereCollider>() != null)
                    {
                        distToTarget -= currentTarget.GetComponent<SphereCollider>().radius;
                    }

                    if (distToTarget < bestDistance[p])
                    {
                        bestDistance[p] = distToTarget;
                        shortRangePhaserTargets[p] = currentTarget.gameObject;
                    }
                }
            }
        }
    }

    private float getDistanceToTarget(int phaserIndex, Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - shortRangePhaserOrigins[phaserIndex].transform.position;
        float distSqr = toTarget.sqrMagnitude;

        if (distSqr > SHORT_RANGE_BEAM_RANGE * SHORT_RANGE_BEAM_RANGE) return float.MaxValue;

        Vector3 localToTarget = shortRangePhaserOrigins[phaserIndex].transform.InverseTransformDirection(toTarget);

        if (localToTarget.z <= 0.0f) return float.MaxValue;

        float yaw = Mathf.Atan2(localToTarget.x, localToTarget.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(localToTarget.y, localToTarget.z) * Mathf.Rad2Deg;

        float hRatio = yaw / SRMaxTrackingAngle;
        float vRatio = pitch / (SRMaxTrackingAngle * 0.75f);

        if ((hRatio * hRatio) + (vRatio * vRatio) > 1.0f) return float.MaxValue;

        return distSqr;
    }
}