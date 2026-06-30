/*
    Phasers.cs
    - Handles short-and-long-range phaser targeting, firing, and rendering
    Contributor(s): Henryk Musial, Jake Schott
    Last Updated: 6/27/2026
*/

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Phasers : NetworkBehaviour
{
    private static float[] BEAM_RANGES = new float[] { 1000.0f, 500.0f };
    private static float[] BEAM_DIAMETERS = new float[] { 2.0f, 1.5f };
    private static float[] MAX_TRACKING_ANGLES = new float[] { 10.0f, 15.0f };
    private static Vector2[] FIRE_TIMES = new Vector2[] { new Vector2(0.4f, 1.4f), new Vector2(0.2f, 1.2f) }; // fire length
    private static Vector2[] DELAY_TIMES = new Vector2[] { new Vector2(2.0f, 0.5f), new Vector2(1.5f, 0.2f) }; // after fire
    private static Vector2[] DAMAGES = new Vector2[] { new Vector2(3.0f, 12.0f), new Vector2(5.0f, 20.0f) }; // per hit, min (intensity zero) to max (intensity one)

    public List<GameObject> phaserOrigins;
    public List<AudioSource> phaserSounds;
    private PhaserHeat phaserHeat;
    private LineRenderer[] phaserRenderers = new LineRenderer[3]; // long range, short-range left, short-range right
    private GameObject[] phaserTargetObjects = new GameObject[3]; // long range, short-range left, short-range right
    private Vector3[] phaserTargetLocations = new Vector3[3]; // long range, short-range left, short-range right
    private Coroutine[] phaserManagerCoroutines = new Coroutine[2] { null, null }; // long range, short range
    private Coroutine[] phaserFireCoroutines = new Coroutine[2] { null, null }; // long range, short range

    private void Start()
    {
        for (int p = 0; p < 3; p++)
        {
            phaserRenderers[p] = phaserOrigins[p].transform.GetChild(0).GetComponent<LineRenderer>();
            phaserRenderers[p].useWorldSpace = true;
            phaserRenderers[p].enabled = false;
        }
        phaserHeat = ReferenceAssistor.Instance.module_handlers[2].GetComponent<PhaserHeat>();
    }
    
    // only run by the host
    public void updatePhasers()
    {
        if (NetworkManager.Singleton.IsHost == false)
        {
            return;
        }

        bool[] activePhasers = GetComponent<PhaserActivators>().getActivePhasers();
        phaserHeat.onPhaserActivationChange();

        // check long-range phasers
        if (phaserManagerCoroutines[0] == null)
        {
            if (activePhasers[0] == true)
            {
                phaserManagerCoroutines[0] = StartCoroutine(longRangePhaserManager());
            }
        }

        // check short-range phasers
        if (phaserManagerCoroutines[1] == null)
        {
            if (activePhasers[1] == true || activePhasers[2] == true)
            {
                phaserManagerCoroutines[1] = StartCoroutine(shortRangePhaserManager());
            }
        }
    }

    IEnumerator longRangePhaserFire(float intensity)
    {
        phaserRenderers[0].enabled = true;
        phaserRenderers[0].SetPosition(1, phaserTargetLocations[0]);
        phaserSounds[0].pitch = 1.8f - (1.0f * intensity);
        phaserSounds[0].Play();

        // play animation
        float activeTime = Mathf.Lerp(FIRE_TIMES[0].x, FIRE_TIMES[0].y, intensity);
        float activeHalftime = activeTime * 0.5f;
        float timeRemaining = activeTime;
        while (timeRemaining > 0.0f)
        {
            timeRemaining = Mathf.Max(0.0f, timeRemaining - Time.deltaTime);

            float beamWidth = Mathf.Lerp(0.0f, BEAM_DIAMETERS[0], Mathf.Lerp(0.0f, 1.0f, Mathf.PingPong(timeRemaining, activeHalftime) / activeHalftime));
            phaserRenderers[0].startWidth = beamWidth;
            phaserRenderers[0].endWidth = beamWidth;
            phaserRenderers[0].SetPosition(0, phaserOrigins[0].transform.position);
            
            yield return null;
        }

        // disable phaser
        phaserRenderers[0].enabled = false;

        // apply damage
        if (NetworkManager.Singleton.IsHost == true)
        {
            if (phaserTargetObjects[0] != null)
            {
                phaserTargetObjects[0].GetComponent<IDamageable>().damage(Mathf.Lerp(DAMAGES[0].x, DAMAGES[0].y, intensity), IDamageable.DamageType.LongRangePhaser);
            }
        }
    }

    IEnumerator shortRangePhaserFire(bool[] activePhasers, float intensity)
    {
        // enable/disable the phasers and fire sounds
        for (int p = 0; p < 2; p++)
        {
            phaserRenderers[p + 1].enabled = activePhasers[p];
            phaserSounds[p + 1].pitch = 2.0f - (1.0f * intensity);
            if (activePhasers[p] == true)
            {
                phaserSounds[p + 1].Play();
                phaserRenderers[p + 1].SetPosition(1, phaserTargetLocations[p + 1]);
            }
        }

        // play animation
        float activeTime = Mathf.Lerp(FIRE_TIMES[1].x, FIRE_TIMES[1].y, intensity);
        float activeHalftime = activeTime * 0.5f;
        float timeRemaining = activeTime;
        while (timeRemaining > 0.0f)
        {
            timeRemaining = Mathf.Max(0.0f, timeRemaining - Time.deltaTime);

            float beamWidth = Mathf.Lerp(0.0f, BEAM_DIAMETERS[1], Mathf.Lerp(0.0f, 1.0f, Mathf.PingPong(timeRemaining, activeHalftime) / activeHalftime));
            for (int p = 0; p < 2; p++)
            {
                phaserRenderers[p + 1].startWidth = beamWidth;
                phaserRenderers[p + 1].endWidth = beamWidth;
                phaserRenderers[p + 1].SetPosition(0, phaserOrigins[p + 1].transform.position);
            }

            yield return null;
        }

        // disable phasers
        for (int p = 0; p < 2; p++)
        {
            phaserRenderers[p + 1].enabled = false;
        }

        // apply damage
        if (NetworkManager.Singleton.IsHost == true)
        {
            for (int p = 0; p < 2; p++)
            {
                if (phaserTargetObjects[p + 1] != null)
                {
                    phaserTargetObjects[p + 1].GetComponent<IDamageable>().damage(Mathf.Lerp(DAMAGES[1].x, DAMAGES[1].y, intensity), IDamageable.DamageType.ShortRangePhaser);
                }
            }
        }
    }

    IEnumerator longRangePhaserManager()
    {
        bool[] activePhasers = GetComponent<PhaserActivators>().getActivePhasers();
        while (activePhasers[0] == true)
        {
            // get intensity
            float currentIntensity = GetComponent<PhaserIntensities>().getPhaserIntensities()[0];

            // only fire if not overheated
            if (phaserHeat.isOverheated(0) == false)
            {
                // determine intensity and targets
                findLongRangeTargetAndPoint();

                // send to clients
                longRangePhaserFireRPC(phaserTargetLocations[0], currentIntensity);

                // run locally as host
                yield return StartCoroutine(longRangePhaserFire(0));
            }

            // delay before next fire
            yield return new WaitForSeconds(Mathf.Lerp(DELAY_TIMES[0].x, DELAY_TIMES[0].y, currentIntensity));
            activePhasers = GetComponent<PhaserActivators>().getActivePhasers();
        }

        phaserManagerCoroutines[0] = null;
    }

    IEnumerator shortRangePhaserManager()
    {
        bool[] activePhasers = GetComponent<PhaserActivators>().getActivePhasers();
        while (activePhasers[1] == true || activePhasers[2] == true)
        {
            // get intensity
            float currentIntensity = GetComponent<PhaserIntensities>().getPhaserIntensities()[1];
            
            // only fire if not overheated
            if (phaserHeat.isOverheated(1) == false)
            {
                // determine targets
                findShortRangeTargetsAndPoints(new bool[] { activePhasers[1], activePhasers[2] });

                // send to clients
                shortRangePhaserFireRPC(activePhasers[1], activePhasers[2], phaserTargetLocations[1], phaserTargetLocations[2], currentIntensity);

                // run locally as host
                yield return StartCoroutine(shortRangePhaserFire(new bool[] { activePhasers[1], activePhasers[2] }, currentIntensity));
            }

            // delay before next fire
            yield return new WaitForSeconds(Mathf.Lerp(DELAY_TIMES[1].x, DELAY_TIMES[1].y, currentIntensity));
            activePhasers = GetComponent<PhaserActivators>().getActivePhasers();
        }

        phaserManagerCoroutines[1] = null;
    }

    private Vector3 getPhaserTargetCoordinate(int phaserCategory, int phaserIndex)
    {
        // if no target, just return forward * beam range
        if (phaserTargetObjects[phaserCategory + phaserIndex] == null)
        {
            return phaserOrigins[phaserCategory + phaserIndex].transform.position + (phaserOrigins[phaserCategory + phaserIndex].transform.forward * BEAM_RANGES[phaserCategory]);
        }

        Vector3 dirToTarget = (phaserTargetObjects[phaserCategory + phaserIndex].transform.position - phaserOrigins[phaserCategory + phaserIndex].transform.position).normalized;
        Vector3 beamDirection = Vector3.RotateTowards(phaserOrigins[phaserCategory + phaserIndex].transform.forward, dirToTarget, MAX_TRACKING_ANGLES[phaserCategory] * Mathf.Deg2Rad, 0.0f);
        Vector3 beamEnd = phaserOrigins[phaserCategory + phaserIndex].transform.position + (beamDirection * BEAM_RANGES[phaserCategory]);

        // check for collision point (and if hit something that isn't our target on the way there, then set the target to null to stop damage)
        if (Physics.Raycast(new Ray(phaserOrigins[phaserCategory + phaserIndex].transform.position, beamDirection), out RaycastHit hit, BEAM_RANGES[phaserCategory], LayerMask.GetMask("CollisionObjects")))
        {
            beamEnd = hit.point;
            if (hit.collider.gameObject != phaserTargetObjects[phaserCategory + phaserIndex])
            {
                phaserTargetObjects[phaserCategory + phaserIndex] = null;
            }
        }

        return beamEnd;
    }

    // returns true if GameObject is a valid phaser target
    private bool isValidTarget(GameObject testTarget)
    {
        return (testTarget.GetComponent<IDamageable>() != null && (testTarget.GetComponent<CollectibleItem>() == null || testTarget.GetComponent<CollectibleItem>().getItemCategory() > 1));
    }

    // returns null if no target found or a reference to a target within range and angle of phaserCategory
    private GameObject findTargetOutOfList(int phaserCategory, int phaserIndex, Collider[] possibleTargets)
    {
        float bestDistance = float.MaxValue;
        GameObject bestTarget = null;

        for (int i = 0; i < possibleTargets.Length; i++)
        {
            Collider currentTarget = possibleTargets[i];
            if (isValidTarget(currentTarget.gameObject) == true)
            {
                float distToTarget = getDistanceToTarget(phaserCategory, phaserIndex, currentTarget.transform.position);

                // check for collider to reduce the distance since you're firing at the collider, not the point
                if (currentTarget.GetComponent<SphereCollider>() != null)
                {
                    distToTarget -= currentTarget.GetComponent<SphereCollider>().radius;
                }

                if (distToTarget < bestDistance)
                {
                    bestDistance = distToTarget;
                    bestTarget = currentTarget.gameObject;
                }
            }
        }

        // check to make sure the phaser isn't clipping the target's collider
        if (bestTarget == null)
        {
            if (Physics.Raycast(new Ray(phaserOrigins[phaserCategory + phaserIndex].transform.position, phaserOrigins[phaserCategory + phaserIndex].transform.forward), out RaycastHit hit, BEAM_RANGES[phaserCategory], LayerMask.GetMask("CollisionObjects")))
            {
                if (isValidTarget(hit.collider.gameObject) == true)
                {
                    bestTarget = hit.collider.gameObject;
                }
            }
        }

        return bestTarget;
    }

    // sets phaserTargetObject and phaserTargetLocation
    private void findLongRangeTargetAndPoint()
    {
        Collider[] possibleTargets = Physics.OverlapSphere(phaserOrigins[0].transform.position, BEAM_RANGES[0]);
        phaserTargetObjects[0] = findTargetOutOfList(0, 0, possibleTargets);
        phaserTargetLocations[0] = getPhaserTargetCoordinate(0, 0);
    }

    // sets phaserTargetObjects and phaserTargetLocations if the phaser is active
    private void findShortRangeTargetsAndPoints(bool[] activePhasers)
    {
        Vector3 leftPos = phaserOrigins[1].transform.position;
        Vector3 rightPos = phaserOrigins[2].transform.position;
        Vector3 midpoint = (leftPos + rightPos) * 0.5f;
        float halfSeparation = Vector3.Distance(leftPos, rightPos) * 0.5f;
        float sharedRadius = BEAM_RANGES[1] + halfSeparation;

        Collider[] possibleTargets = Physics.OverlapSphere(midpoint, sharedRadius);
        for (int p = 0; p < 2; p++)
        {
            if (activePhasers[p] == true)
            {
                phaserTargetObjects[p + 1] = findTargetOutOfList(1, p, possibleTargets);
                phaserTargetLocations[p + 1] = getPhaserTargetCoordinate(1, p);
            }
        }
    }

    // returns distance from phaserOrigin to targetPosition or float.MaxValue if outside of angle/range
    private float getDistanceToTarget(int phaserCategory, int phaserIndex, Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - phaserOrigins[phaserCategory + phaserIndex].transform.position;
        float distSqr = toTarget.sqrMagnitude;

        if (distSqr > BEAM_RANGES[phaserCategory] * BEAM_RANGES[phaserCategory]) return float.MaxValue;

        Vector3 localToTarget = phaserOrigins[phaserCategory + phaserIndex].transform.InverseTransformDirection(toTarget);

        if (localToTarget.z <= 0.0f) return float.MaxValue;

        float yaw = Mathf.Atan2(localToTarget.x, localToTarget.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(localToTarget.y, localToTarget.z) * Mathf.Rad2Deg;

        float hRatio = yaw / MAX_TRACKING_ANGLES[phaserCategory];
        float vRatio = pitch / (MAX_TRACKING_ANGLES[phaserCategory] * 0.75f);

        if ((hRatio * hRatio) + (vRatio * vRatio) > 1.0f) return float.MaxValue;

        return distSqr;
    }

    // communicated to clients to ensure that they are on the same page for short-range phasers
    [Rpc(SendTo.NotServer)]
    private void shortRangePhaserFireRPC(bool leftActive, bool rightActive, Vector3 targetLeft, Vector3 targetRight, float intensity)
    {
        bool[] activePhasers = new bool[2] { leftActive, rightActive };
        phaserTargetLocations[1] = targetLeft;
        phaserTargetLocations[2] = targetRight;
        if (phaserFireCoroutines[1] != null)
        {
            StopCoroutine(phaserFireCoroutines[1]);
        }
        phaserFireCoroutines[1] = StartCoroutine(shortRangePhaserFire(activePhasers, intensity));
    }

    // communicated to clients to ensure that they are on the same page for long-range phaser
    [Rpc(SendTo.NotServer)]
    private void longRangePhaserFireRPC(Vector3 target, float intensity)
    {
        phaserTargetLocations[0] = target;
        if (phaserFireCoroutines[0] != null)
        {
            StopCoroutine(phaserFireCoroutines[0]);
        }
        phaserFireCoroutines[0] = StartCoroutine(longRangePhaserFire(intensity));
    }
}