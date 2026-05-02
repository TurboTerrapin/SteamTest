/*
    LongRangePhaser.cs
    Last Updated: 5/2/2026
*/

using UnityEngine;

public class LongRangePhaser : MonoBehaviour
{
    public GameObject longRangePhaserOrigin;
    public LongRangeDirection longRangeDirection;

    private float minLRBeamDiameter = 0.25f;
    private float maxLRBeamDiameter = 1.5f;
    private float minLRIntensity = 2.0f;
    private float maxLRIntensity = 18.0f;

    private float LRTrackingRange = 500f;
    private float LRMaxTrackingAngle = 3.5f;
    private float LRTrackingSpeed = 5f;
    private float LRTargetScanInterval = 2f;
    private float LRDamagePerSecond = 60f;

    // Start/end animation
    private float LRStartupDuration = 0.35f;
    private float LRShutdownDuration = 0.25f;
    private float LRReachEaseExponent = 3.5f;

    private LineRenderer longRangePhaser;
    private Material longRangePhaserMaterial;
    private Color longRangeEmissionColor;

    private const float LONG_RANGE_BEAM_LENGTH = 500f;
    private const string EMISSION_COLOR = "_EmissionColor";
    private const string EMISSION_ = "_EMISSION";

    private float currentTrackingOffset;
    private Transform cachedTarget;
    private IDamageable cachedDamageable;
    private float nextScanTime;

    private enum Phase { Inactive, Starting, Active, Ending }
    private Phase phase = Phase.Inactive;
    private float phaseProgress;

    private float beamTemp;
    private float longRangePhaserAngle;

    private void Start()
    {
        longRangePhaser = longRangePhaserOrigin.GetComponentInChildren<LineRenderer>(true);
        if (longRangePhaser != null)
        {
            longRangePhaser.useWorldSpace = true;
            longRangePhaser.positionCount = 2;
            longRangePhaser.startWidth = minLRBeamDiameter;
            longRangePhaser.endWidth = minLRBeamDiameter;
            longRangePhaser.enabled = false;

            longRangePhaserMaterial = new Material(longRangePhaser.material);
            longRangeEmissionColor = longRangePhaserMaterial.GetColor(EMISSION_COLOR);
        }

        enabled = false;
    }

    public void setActive(bool isActive)
    {
        if (isActive)
        {
            switch (phase)
            {
                case Phase.Inactive:
                    phase = Phase.Starting;
                    phaseProgress = 0f;
                    nextScanTime = 0f;
                    enabled = true;
                    break;

                case Phase.Ending:
                    phase = Phase.Starting;
                    phaseProgress = 1f - phaseProgress;
                    enabled = true;
                    break;
            }
        }
        else
        {
            switch (phase)
            {
                case Phase.Active:
                    phase = Phase.Ending;
                    phaseProgress = 0f;
                    enabled = true;
                    break;

                case Phase.Starting:
                    phase = Phase.Ending;
                    phaseProgress = 1f - phaseProgress;
                    enabled = true;
                    break;
            }
        }
    }

    public void setIntensity(float intensity)
    {
        beamTemp = intensity;
    }

    private void Update()
    {
        if (phase == Phase.Inactive)
        {
            if (longRangePhaser != null) longRangePhaser.enabled = false;
            cachedTarget = null;
            cachedDamageable = null;
            currentTrackingOffset = 0f;
            nextScanTime = 0f;

            enabled = false;
            return;
        }

        float dt = Time.deltaTime;
        advancePhase(dt);
        updateLongRangePhaser(dt);
    }

    private void advancePhase(float dt)
    {
        switch (phase)
        {
            case Phase.Starting:
                if (LRStartupDuration <= 0f)
                {
                    phaseProgress = 1f;
                }
                else
                {
                    phaseProgress += dt / LRStartupDuration;
                }
                if (phaseProgress >= 1f)
                {
                    phaseProgress = 1f;
                    phase = Phase.Active;
                }
                break;

            case Phase.Ending:
                if (LRShutdownDuration <= 0f)
                {
                    phaseProgress = 1f;
                }
                else
                {
                    phaseProgress += dt / LRShutdownDuration;
                }
                if (phaseProgress >= 1f)
                {
                    phaseProgress = 1f;
                    phase = Phase.Inactive;
                }
                break;
        }
    }

    private float computeReachFactor()
    {
        // Reach goes 0 -> 1 during Starting, stays 1 during Active, goes 1 -> 0 during Ending.
        switch (phase)
        {
            case Phase.Starting:
                return easeOutPow(phaseProgress, LRReachEaseExponent);
            case Phase.Active:
                return 1f;
            case Phase.Ending:
                return easeOutPow(1f - phaseProgress, LRReachEaseExponent);
            default:
                return 0f;
        }
    }

    private static float easeOutPow(float t, float exponent)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, exponent);
    }

    private void updateLongRangePhaser(float dt)
    {
        longRangePhaserAngle = longRangeDirection.getPhaserDirectionAngle();

        bool atFullReach = (phase == Phase.Active);

        if (atFullReach && Time.time >= nextScanTime)
        {
            performScan();
            nextScanTime = Time.time + LRTargetScanInterval;
        }

        updateTracking(dt);

        longRangePhaserOrigin.transform.localRotation =
            Quaternion.Euler(90.0f, longRangePhaserAngle + currentTrackingOffset, 0.0f);

        longRangePhaser.enabled = true;

        Vector3 beamStart = longRangePhaserOrigin.transform.position;
        Vector3 beamDirection = longRangePhaserOrigin.transform.forward;

        float reachFactor = computeReachFactor();
        float animatedLength = LONG_RANGE_BEAM_LENGTH * reachFactor;

        float effectiveLength = animatedLength;
        bool hitTarget = false;

        if (atFullReach && cachedTarget != null)
        {
            Collider targetCol = cachedTarget.GetComponent<Collider>();
            if (targetCol != null &&
                targetCol.Raycast(new Ray(beamStart, beamDirection), out RaycastHit hit, LONG_RANGE_BEAM_LENGTH))
            {
                if (hit.distance <= animatedLength)
                {
                    effectiveLength = hit.distance;
                    hitTarget = true;
                }
            }
        }

        Vector3 beamEnd = beamStart + beamDirection * effectiveLength;

        longRangePhaser.SetPosition(0, beamStart);
        longRangePhaser.SetPosition(1, beamEnd);

        float clampedTemp = Mathf.Clamp01(Mathf.Max(0.1f, beamTemp));

        float currentWidth = Mathf.Lerp(minLRBeamDiameter, maxLRBeamDiameter, clampedTemp);
        longRangePhaser.startWidth = currentWidth;
        longRangePhaser.endWidth = currentWidth;

        updateBeamIntensity(clampedTemp);

        if (hitTarget && cachedDamageable != null)
        {
            cachedDamageable.damage(LRDamagePerSecond * clampedTemp * dt);
        }
    }

    private void updateTracking(float dt)
    {
        float desiredOffset = 0f;

        if (cachedTarget != null)
        {
            Vector3 originPos = longRangePhaserOrigin.transform.position;
            Vector3 manualForward = Quaternion.Euler(0f, longRangePhaserAngle, 0f) * Vector3.forward;
            if (longRangePhaserOrigin.transform.parent != null)
            {
                manualForward = longRangePhaserOrigin.transform.parent.rotation * manualForward;
            }

            Vector3 toTarget = cachedTarget.position - originPos;
            toTarget.y = 0f;

            Vector3 manualForwardFlat = manualForward;
            manualForwardFlat.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f && manualForwardFlat.sqrMagnitude > 0.0001f)
            {
                float signedAngle = Vector3.SignedAngle(manualForwardFlat.normalized, toTarget.normalized, Vector3.up);
                desiredOffset = Mathf.Clamp(signedAngle, -LRMaxTrackingAngle, LRMaxTrackingAngle);
            }
        }

        float t = 1f - Mathf.Exp(-LRTrackingSpeed * dt);
        currentTrackingOffset = Mathf.Lerp(currentTrackingOffset, desiredOffset, t);
    }

    private void updateBeamIntensity(float temperature)
    {
        if (longRangePhaserMaterial == null) return;

        float intensity = Mathf.Lerp(minLRIntensity, maxLRIntensity, temperature);

        longRangePhaserMaterial.EnableKeyword(EMISSION_);
        longRangePhaserMaterial.SetColor(EMISSION_COLOR, longRangeEmissionColor * intensity);
    }

    private void performScan()
    {
        Vector3 originPos = longRangePhaserOrigin.transform.position;
        Transform originTransform = longRangePhaserOrigin.transform;

        Transform bestTarget = null;
        IDamageable bestDamageable = null;
        float bestDistanceSqr = float.MaxValue;

        Collider[] overlaps = Physics.OverlapSphere(originPos, LRTrackingRange);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider col = overlaps[i];
            if (col == null) continue;

            IDamageable dmg = col.GetComponent<IDamageable>();
            if (dmg == null) continue;

            considerForBeam(col.transform, dmg, originTransform,
                            ref bestTarget, ref bestDamageable, ref bestDistanceSqr);
        }

        cachedTarget = bestTarget;
        cachedDamageable = bestDamageable;
    }

    private void considerForBeam(Transform candidate, IDamageable dmg, Transform originTransform,
                                 ref Transform bestTarget, ref IDamageable bestDamageable, ref float bestDistance)
    {
        Vector3 originPos = originTransform.position;
        Vector3 toTarget = candidate.position - originPos;
        float distSqr = toTarget.sqrMagnitude;

        if (distSqr >= bestDistance) return;
        if (distSqr > LRTrackingRange * LRTrackingRange) return;

        Vector3 localToTarget = originTransform.InverseTransformDirection(toTarget);

        if (localToTarget.z <= 0f) return;

        float yaw = Mathf.Atan2(localToTarget.x, localToTarget.z) * Mathf.Rad2Deg;

        if (Mathf.Abs(yaw) > LRMaxTrackingAngle) return;

        bestDistance = distSqr;
        bestTarget = candidate;
        bestDamageable = dmg;
    }
}


// WITHPOUT EASE IN/OUT ANIMATION

/*
using UnityEngine;

public class LongRangePhaser : MonoBehaviour
{
    public GameObject longRangePhaserOrigin;
    public LongRangeDirection longRangeDirection;

    private float minLRBeamDiameter = 0.25f;
    private float maxLRBeamDiameter = 1.5f;
    private float minLRIntensity = 2.0f;
    private float maxLRIntensity = 18.0f;

    private float LRTrackingRange = 350f;
    private float LRMaxTrackingAngle = 3.5f;
    private float LRTrackingSpeed = 5f;
    private float LRTargetScanInterval = 1f;
    private float LRDamagePerSecond = 60f;

    private LineRenderer longRangePhaser;
    private Material longRangePhaserMaterial;
    private Color longRangeEmissionColor;

    private const float LONG_RANGE_BEAM_LENGTH = 350f;
    private const string EMISSION_COLOR = "_EmissionColor";
    private const string EMISSION_ = "_EMISSION";

    private float currentTrackingOffset;
    private Transform cachedTarget;
    private IDamageable cachedDamageable;
    private float nextScanTime;

    private bool active;
    private float beamTemp;
    private float longRangePhaserAngle;

    private void Start()
    {
        longRangePhaser = longRangePhaserOrigin.GetComponentInChildren<LineRenderer>(true);
        if (longRangePhaser != null)
        {
            longRangePhaser.useWorldSpace = true;
            longRangePhaser.positionCount = 2;
            longRangePhaser.startWidth = minLRBeamDiameter;
            longRangePhaser.endWidth = minLRBeamDiameter;
            longRangePhaser.enabled = false;

            longRangePhaserMaterial = new Material(longRangePhaser.material);
            longRangeEmissionColor = longRangePhaserMaterial.GetColor(EMISSION_COLOR);
        }

        enabled = false;
    }

    public void setActive(bool isActive)
    {
        bool wasActive = active;
        active = isActive;

        if (active && !wasActive)
        {
            nextScanTime = 0f;
            enabled = true;
        }
        else if (!active)
        {
            cachedTarget = null;
            cachedDamageable = null;

            if (longRangePhaser != null) longRangePhaser.enabled = false;

            enabled = true;
        }
    }

    public void setIntensity(float intensity)
    {
        beamTemp = intensity;
    }

    private void Update()
    {
        if (!active)
        {
            longRangePhaser.enabled = false;
            cachedTarget = null;
            cachedDamageable = null;
            currentTrackingOffset = 0f;
            nextScanTime = 0f;

            enabled = false;
            return;
        }
        else
        {
            float dt = Time.deltaTime;
            updateLongRangePhaser(dt);
        }
    }

    private void updateLongRangePhaser(float dt)
    {
        longRangePhaserAngle = longRangeDirection.getPhaserDirectionAngle();

        if (Time.time >= nextScanTime)
        {
            performScan();
            nextScanTime = Time.time + LRTargetScanInterval;
        }

        updateTracking(dt);

        longRangePhaserOrigin.transform.localRotation =
            Quaternion.Euler(90.0f, longRangePhaserAngle + currentTrackingOffset, 0.0f);

        longRangePhaser.enabled = true;

        Vector3 beamStart = longRangePhaserOrigin.transform.position;
        Vector3 beamDirection = longRangePhaserOrigin.transform.forward;
        Vector3 beamEnd = beamStart + beamDirection * LONG_RANGE_BEAM_LENGTH;

        if (cachedTarget != null)
        {
            Collider targetCol = cachedTarget.GetComponent<Collider>();
            if (targetCol != null &&
                targetCol.Raycast(new Ray(beamStart, beamDirection), out RaycastHit hit, LONG_RANGE_BEAM_LENGTH))
            {
                beamEnd = hit.point;
            }
        }

        longRangePhaser.SetPosition(0, beamStart);
        longRangePhaser.SetPosition(1, beamEnd);

        float clampedTemp = Mathf.Clamp01(Mathf.Max(0.1f, beamTemp));

        float currentWidth = Mathf.Lerp(minLRBeamDiameter, maxLRBeamDiameter, clampedTemp);
        longRangePhaser.startWidth = currentWidth;
        longRangePhaser.endWidth = currentWidth;

        updateBeamIntensity(clampedTemp);


        if (cachedDamageable != null && cachedTarget != null)
        {
            cachedDamageable.damage(LRDamagePerSecond * clampedTemp * dt);
        }
    }

    private void updateTracking(float dt)
    {
        float desiredOffset = 0f;

        if (cachedTarget != null)
        {
            Vector3 originPos = longRangePhaserOrigin.transform.position;
            Vector3 manualForward = Quaternion.Euler(0f, longRangePhaserAngle, 0f) * Vector3.forward;
            if (longRangePhaserOrigin.transform.parent != null)
            {
                manualForward = longRangePhaserOrigin.transform.parent.rotation * manualForward;
            }

            Vector3 toTarget = cachedTarget.position - originPos;
            toTarget.y = 0f;

            Vector3 manualForwardFlat = manualForward;
            manualForwardFlat.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f && manualForwardFlat.sqrMagnitude > 0.0001f)
            {
                float signedAngle = Vector3.SignedAngle(manualForwardFlat.normalized, toTarget.normalized, Vector3.up);
                desiredOffset = Mathf.Clamp(signedAngle, -LRMaxTrackingAngle, LRMaxTrackingAngle);
            }
        }

        float t = 1f - Mathf.Exp(-LRTrackingSpeed * dt);
        currentTrackingOffset = Mathf.Lerp(currentTrackingOffset, desiredOffset, t);
    }

    private void updateBeamIntensity(float temperature)
    {
        if (longRangePhaserMaterial == null) return;

        float intensity = Mathf.Lerp(minLRIntensity, maxLRIntensity, temperature);

        longRangePhaserMaterial.EnableKeyword(EMISSION_);
        longRangePhaserMaterial.SetColor(EMISSION_COLOR, longRangeEmissionColor * intensity);
    }

    private void performScan()
    {
        Vector3 originPos = longRangePhaserOrigin.transform.position;
        Transform originTransform = longRangePhaserOrigin.transform;

        Transform bestTarget = null;
        IDamageable bestDamageable = null;
        float bestDistanceSqr = float.MaxValue;

        Collider[] overlaps = Physics.OverlapSphere(originPos, LRTrackingRange);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider col = overlaps[i];
            if (col == null) continue;

            IDamageable dmg = col.GetComponent<IDamageable>();
            if (dmg == null) continue;

            considerForBeam(col.transform, dmg, originTransform,
                            ref bestTarget, ref bestDamageable, ref bestDistanceSqr);
        }

        cachedTarget = bestTarget;
        cachedDamageable = bestDamageable;
    }

    private void considerForBeam(Transform candidate, IDamageable dmg, Transform originTransform,
                                 ref Transform bestTarget, ref IDamageable bestDamageable, ref float bestDistance)
    {
        Vector3 originPos = originTransform.position;
        Vector3 toTarget = candidate.position - originPos;
        float distSqr = toTarget.sqrMagnitude;

        if (distSqr >= bestDistance) return;
        if (distSqr > LRTrackingRange * LRTrackingRange) return;

        Vector3 localToTarget = originTransform.InverseTransformDirection(toTarget);

        if (localToTarget.z <= 0f) return;

        float yaw = Mathf.Atan2(localToTarget.x, localToTarget.z) * Mathf.Rad2Deg;

        if (Mathf.Abs(yaw) > LRMaxTrackingAngle) return;

        bestDistance = distSqr;
        bestTarget = candidate;
        bestDamageable = dmg;
    }
}
*/