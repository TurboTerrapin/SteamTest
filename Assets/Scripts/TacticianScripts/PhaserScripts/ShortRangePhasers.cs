/*
    ShortRangePhasers.cs
    Last Updated: 5/1/2026
*/

using UnityEngine;

public class ShortRangePhasers : MonoBehaviour
{
    public GameObject shortRangePhaserLeftOrigin;
    public GameObject shortRangePhaserRightOrigin;

    private float SRBeamDiameter = 0.25f;
    private float SRBurstDuration = 0.6f;
    private float SRBurstGapMax = 1.5f;
    private float SRBurstGapMin = 0.5f;

    private float SRTrackingRange = 350f;
    private float SRMaxTrackingAngle = 15f;
    private float SRTrackingSpeed = 5f;
    private float SRTargetScanInterval = 2f;

    private float SRDamagePerSecond = 20f;

    private LineRenderer shortRangePhaserLeft;
    private LineRenderer shortRangePhaserRight;

    private Material shortRangePhaserMaterialLeft;
    private Material shortRangePhaserMaterialRight;
    private Color shortRangeEmissionColorLeft;
    private Color shortRangeEmissionColorRight;

    private const float SHORT_RANGE_BEAM_LENGTH = 350f;
    private const string EMISSION_COLOR = "_EmissionColor";

    private float burstCycleTime;
    private bool wasFiringLastFrame;

    // beam states (index 0 = left, 1 = right)
    private Vector3[] currentDirs = new Vector3[2];
    private Transform[] cachedTargets = new Transform[2];
    private IDamageable[] cachedDamageables = new IDamageable[2];
    private Transform[] burstTargets = new Transform[2];
    private IDamageable[] burstDamageables = new IDamageable[2];

    private float nextScanTime;

    private bool[] beamActive = new bool[2];
    private float srTemp;

    private void Start()
    {
        shortRangePhaserLeft = shortRangePhaserLeftOrigin.GetComponentInChildren<LineRenderer>(true);
        if (shortRangePhaserLeft != null)
        {
            shortRangePhaserLeft.useWorldSpace = true;
            shortRangePhaserMaterialLeft = new Material(shortRangePhaserLeft.material);
            shortRangeEmissionColorLeft = shortRangePhaserMaterialLeft.GetColor(EMISSION_COLOR);
            shortRangePhaserLeft.startWidth = SRBeamDiameter;
            shortRangePhaserLeft.endWidth = SRBeamDiameter;
            shortRangePhaserLeft.enabled = false;
        }

        shortRangePhaserRight = shortRangePhaserRightOrigin.GetComponentInChildren<LineRenderer>(true);
        if (shortRangePhaserRight != null)
        {
            shortRangePhaserRight.useWorldSpace = true;
            shortRangePhaserMaterialRight = new Material(shortRangePhaserRight.material);
            shortRangeEmissionColorRight = shortRangePhaserMaterialRight.GetColor(EMISSION_COLOR);
            shortRangePhaserRight.startWidth = SRBeamDiameter;
            shortRangePhaserRight.endWidth = SRBeamDiameter;
            shortRangePhaserRight.enabled = false;
        }

        currentDirs[0] = shortRangePhaserLeftOrigin.transform.forward;
        currentDirs[1] = shortRangePhaserRightOrigin.transform.forward;

        enabled = false;
    }

    public void setBeamActive(int beamIndex, bool active)
    {
        if (beamIndex < 0 || beamIndex > 1) return;

        bool wasActive = beamActive[beamIndex];
        beamActive[beamIndex] = active;

        if (active && !wasActive)
        {
            nextScanTime = 0f;
        }
        else if (!active)
        {
            cachedTargets[beamIndex] = null;
            cachedDamageables[beamIndex] = null;
            burstTargets[beamIndex] = null;
            burstDamageables[beamIndex] = null;
        }

        if (beamActive[0] || beamActive[1])
        {
            enabled = true;
        }
    }

    public void setIntensity(float intensity)
    {
        srTemp = intensity;
    }

    private float currentBurstCycleLength()
    {
        float gap = Mathf.Lerp(SRBurstGapMax, SRBurstGapMin, srTemp);
        return SRBurstDuration + gap;
    }

    private bool isFiring()
    {
        return burstCycleTime < SRBurstDuration;
    }

    private void Update()
    {
        updateShortRangePhasers(Time.deltaTime);
    }

    private void updateShortRangePhasers(float dt)
    {
        if (!beamActive[0] && !beamActive[1])
        {
            burstCycleTime = 0f;
            wasFiringLastFrame = false;
            currentDirs[0] = shortRangePhaserLeftOrigin.transform.forward;
            currentDirs[1] = shortRangePhaserRightOrigin.transform.forward;
            burstTargets[0] = burstTargets[1] = null;
            burstDamageables[0] = burstDamageables[1] = null;
            nextScanTime = 0f;

            updateShortRangePhaser(shortRangePhaserLeft, shortRangePhaserLeftOrigin, 0, false);
            updateShortRangePhaser(shortRangePhaserRight, shortRangePhaserRightOrigin, 1, false);

            enabled = false;
            return;
        }

        // Advance burst cycle
        burstCycleTime += dt;
        float cycleLength = currentBurstCycleLength();
        if (burstCycleTime >= cycleLength)
        {
            burstCycleTime -= cycleLength;
        }

        bool firing = isFiring();
        bool burstJustStarted = firing && !wasFiringLastFrame;

        // Shared scan
        if (Time.time >= nextScanTime)
        {
            performSharedScan();
            nextScanTime = Time.time + SRTargetScanInterval;
        }

        if (burstJustStarted)
        {
            for (int i = 0; i < 2; i++)
            {
                if (!beamActive[i])
                {
                    burstTargets[i] = null;
                    burstDamageables[i] = null;
                    continue;
                }

                GameObject origin = (i == 0) ? shortRangePhaserLeftOrigin : shortRangePhaserRightOrigin;

                if (cachedTargets[i] != null)
                {
                    burstTargets[i] = cachedTargets[i];
                    burstDamageables[i] = cachedDamageables[i];
                    currentDirs[i] = computeAimDir(origin, burstTargets[i]);
                }
                else
                {
                    // No target 
                    burstTargets[i] = null;
                    burstDamageables[i] = null;
                    currentDirs[i] = origin.transform.forward;
                }
            }
        }

        // Aim updates
        updateBeamAim(0, shortRangePhaserLeftOrigin, dt, firing);
        updateBeamAim(1, shortRangePhaserRightOrigin, dt, firing);

        updateShortRangePhaser(shortRangePhaserLeft, shortRangePhaserLeftOrigin, 0, beamActive[0] && firing);
        updateShortRangePhaser(shortRangePhaserRight, shortRangePhaserRightOrigin, 1, beamActive[1] && firing);

        // Damage during firing 
        if (firing)
        {
            for (int i = 0; i < 2; i++)
            {
                if (beamActive[i] && burstDamageables[i] != null && burstTargets[i] != null)
                {
                    burstDamageables[i].damage(SRDamagePerSecond * dt);
                }
            }
        }

        wasFiringLastFrame = firing;
    }

    private void updateBeamAim(int index, GameObject origin, float dt, bool firing)
    {
        if (!beamActive[index])
        {
            currentDirs[index] = origin.transform.forward;
            return;
        }

        if (!firing)
        {

            Transform target = cachedTargets[index];
            currentDirs[index] = (target != null)
                ? computeAimDir(origin, target)
                : origin.transform.forward;
            return;
        }

        if (burstTargets[index] != null)
        {
            Vector3 targetDir = computeAimDir(origin, burstTargets[index]);
            currentDirs[index] = Vector3.Slerp(currentDirs[index], targetDir, SRTrackingSpeed * dt);
        }

    }

    private Vector3 computeAimDir(GameObject origin, Transform target)
    {
        Vector3 originPos = origin.transform.position;
        Vector3 defaultForward = origin.transform.forward;
        Vector3 dirToTarget = (target.position - originPos).normalized;

        return Vector3.RotateTowards(defaultForward, dirToTarget, SRMaxTrackingAngle * Mathf.Deg2Rad, 0f);
    }

    private void updateShortRangePhaser(LineRenderer phaser, GameObject origin, int index, bool visible)
    {
        if (phaser == null) return;

        if (!visible)
        {
            phaser.enabled = false;
            return;
        }

        phaser.enabled = true;

        Vector3 beamStart = origin.transform.position;
        Vector3 beamDirection = currentDirs[index];
        Vector3 beamEnd = beamStart + beamDirection * SHORT_RANGE_BEAM_LENGTH;

        Transform target = burstTargets[index];
        if (target != null)
        {
            Collider targetCol = target.GetComponent<Collider>();
            if (targetCol != null && targetCol.Raycast(new Ray(beamStart, beamDirection), out RaycastHit hit, SHORT_RANGE_BEAM_LENGTH))
            {
                beamEnd = hit.point;
            }
        }

        phaser.SetPosition(0, beamStart);
        phaser.SetPosition(1, beamEnd);
    }

    private void performSharedScan()
    {
        Vector3 leftPos = shortRangePhaserLeftOrigin.transform.position;
        Vector3 rightPos = shortRangePhaserRightOrigin.transform.position;
        Vector3 midpoint = (leftPos + rightPos) * 0.5f;
        float halfSeparation = Vector3.Distance(leftPos, rightPos) * 0.5f;
        float sharedRadius = SRTrackingRange + halfSeparation;

        Collider[] overlaps = Physics.OverlapSphere(midpoint, sharedRadius);

        Transform[] bestTarget = new Transform[2];
        IDamageable[] bestDamageable = new IDamageable[2];
        float[] bestDistance = new float[2] { float.MaxValue, float.MaxValue };

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider col = overlaps[i];
            if (col == null) continue;

            IDamageable dmg = col.GetComponent<IDamageable>();
            if (dmg == null) continue;

            Transform candidate = col.transform;

            if (beamActive[0])
            {
                considerForBeam(candidate, dmg, shortRangePhaserLeftOrigin.transform,
                                ref bestTarget[0], ref bestDamageable[0], ref bestDistance[0]);
            }
            if (beamActive[1])
            {
                considerForBeam(candidate, dmg, shortRangePhaserRightOrigin.transform,
                                ref bestTarget[1], ref bestDamageable[1], ref bestDistance[1]);
            }
        }

        for (int i = 0; i < 2; i++)
        {
            if (beamActive[i])
            {
                cachedTargets[i] = bestTarget[i];
                cachedDamageables[i] = bestDamageable[i];
            }
        }
    }

    private void considerForBeam(Transform candidate, IDamageable dmg, Transform originTransform,
                                 ref Transform bestTarget, ref IDamageable bestDamageable, ref float bestDistance)
    {
        Vector3 originPos = originTransform.position;
        Vector3 toTarget = candidate.position - originPos;
        float distSqr = toTarget.sqrMagnitude;

        if (distSqr >= bestDistance) return;
        if (distSqr > SRTrackingRange * SRTrackingRange) return;

        Vector3 localToTarget = originTransform.InverseTransformDirection(toTarget);

        if (localToTarget.z <= 0f) return;

        float yaw = Mathf.Atan2(localToTarget.x, localToTarget.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(localToTarget.y, localToTarget.z) * Mathf.Rad2Deg;

        float hRatio = yaw / SRMaxTrackingAngle;
        float vRatio = pitch / (SRMaxTrackingAngle * 0.75f);

        if ((hRatio * hRatio) + (vRatio * vRatio) > 1f) return;

        bestDistance = distSqr;
        bestTarget = candidate;
        bestDamageable = dmg;
    }
}