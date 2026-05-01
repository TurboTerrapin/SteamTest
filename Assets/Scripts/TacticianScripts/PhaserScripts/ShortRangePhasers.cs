/*
    ShortRangePhasers.cs
    - Renders the left & right short-range phaser beams
    - Handles shared pulse phase, semi-tracking aim toward nearest enemy
    - Pulls active/temperature data from control scripts
    Last Updated: 5/1/2026
*/

using UnityEngine;

public class ShortRangePhasers : MonoBehaviour
{
    private PhaserActivators phaserActivators;
    private PhaserIntensities phaserIntensities;

    public GameObject shortRangePhaserLeftOrigin;
    public GameObject shortRangePhaserRightOrigin;

    private bool controlsAssigned = false;

    private void resolveControls()
    {
        if (controlsAssigned) return;
        if (ReferenceAssistor.Instance == null) return;
        if (ReferenceAssistor.Instance.module_handlers == null) return;
        if (ReferenceAssistor.Instance.module_handlers.Count <= 1) return;

        GameObject controlHandler = ReferenceAssistor.Instance.module_handlers[1].gameObject;
        phaserActivators = controlHandler.GetComponent<PhaserActivators>();
        phaserIntensities = controlHandler.GetComponent<PhaserIntensities>();

        controlsAssigned = phaserActivators && phaserIntensities;
    }

    private float minSRBeamDiameter = 0.1f;
    private float maxSRBeamDiameter = 0.3f;
    private float SRBeamEndDiameterRatio = 0.4f;

    private float minSRPulseInterval = 1f;
    private float maxSRPulseInterval = 0.4f;

    private float SRTrackingRadius = 50f;
    private float SRTrackingRange = 350f;
    private float SRMaxTrackingAngle = 15f;
    private float SRTrackingSpeed = 5f;
    private float SRTargetScanInterval = 1f;
    private string enemyTag = "Enemy";

    private float SRDamagePerSecond = 20f;

    private LineRenderer shortRangePhaserLeft;
    private LineRenderer shortRangePhaserRight;

    private Material shortRangePhaserMaterialLeft;
    private Material shortRangePhaserMaterialRight;
    private Color shortRangeEmissionColorLeft;
    private Color shortRangeEmissionColorRight;

    private const float SHORT_RANGE_BEAM_LENGTH = 350f;
    private const string EMISSION_COLOR = "_EmissionColor";

    private float SRPulsePhase;
    private float[] currSRBeamWidth = new float[2];

    private Vector3 sharedSRCurrentDir;

    private Transform cachedTarget;
    private IDamageable cachedDamageable;
    private float nextTargetScanTime;

    private bool leftActive;
    private bool rightActive;
    private float srTemp;

    private void Start()
    {
        shortRangePhaserLeft = shortRangePhaserLeftOrigin.GetComponentInChildren<LineRenderer>(true);
        if (shortRangePhaserLeft != null)
        {
            shortRangePhaserLeft.useWorldSpace = true;
            shortRangePhaserMaterialLeft = new Material(shortRangePhaserLeft.material);
            shortRangeEmissionColorLeft = shortRangePhaserMaterialLeft.GetColor(EMISSION_COLOR);
        }

        shortRangePhaserRight = shortRangePhaserRightOrigin.GetComponentInChildren<LineRenderer>(true);
        if (shortRangePhaserRight != null)
        {
            shortRangePhaserRight.useWorldSpace = true;
            shortRangePhaserMaterialRight = new Material(shortRangePhaserRight.material);
            shortRangeEmissionColorRight = shortRangePhaserMaterialRight.GetColor(EMISSION_COLOR);
        }

        sharedSRCurrentDir = shortRangePhaserLeftOrigin.transform.forward;

        currSRBeamWidth[0] = minSRBeamDiameter;
        currSRBeamWidth[1] = minSRBeamDiameter;
    }

    private void readControls()
    {
        if (phaserActivators != null)
        {
            bool[] activePhasers = phaserActivators.getActivePhasers();
            leftActive = activePhasers != null && activePhasers.Length > 1 && activePhasers[1];
            rightActive = activePhasers != null && activePhasers.Length > 2 && activePhasers[2];
        }
        if (phaserIntensities != null)
        {
            float[] phaserTemps = phaserIntensities.getPhaserTemperatures();
            srTemp = phaserTemps != null && phaserTemps.Length > 1 ? phaserTemps[1] : 0f;
        }
    }

    private void Update()
    {
        if (!controlsAssigned)
        {
            resolveControls();
            if (!controlsAssigned) return;
        }

        readControls();
        updateShortRangePhasers(Time.deltaTime);
    }

    private void updateShortRangePhasers(float dt)
    {
        Vector3 defaultForward = shortRangePhaserLeftOrigin.transform.forward;

        if (!leftActive && !rightActive)
        {
            SRPulsePhase = 0f;
            sharedSRCurrentDir = defaultForward;
            cachedTarget = null;
            cachedDamageable = null;
            nextTargetScanTime = 0f;
        }
        else
        {
            float currentPulseInterval = Mathf.Lerp(maxSRPulseInterval, minSRPulseInterval, 1 - srTemp);
            SRPulsePhase += dt / currentPulseInterval;
            SRPulsePhase %= 2f;

            Vector3 trackingMidpoint = (shortRangePhaserLeftOrigin.transform.position
                                      + shortRangePhaserRightOrigin.transform.position) / 2f;

            if (Time.time >= nextTargetScanTime)
            {
                cachedTarget = getNearestEnemy(trackingMidpoint, defaultForward);
                nextTargetScanTime = Time.time + SRTargetScanInterval;
            }

            Vector3 targetDir = defaultForward;
            if (cachedTarget != null)
            {
                Vector3 dirToTarget = (cachedTarget.position - trackingMidpoint).normalized;
                targetDir = Vector3.RotateTowards(defaultForward, dirToTarget, SRMaxTrackingAngle * Mathf.Deg2Rad, 0f);
            }

            sharedSRCurrentDir = Vector3.Slerp(sharedSRCurrentDir, targetDir, SRTrackingSpeed * dt);
        }

        updateShortRangePhaser(shortRangePhaserLeft, shortRangePhaserLeftOrigin, sharedSRCurrentDir, 0, leftActive, srTemp);
        updateShortRangePhaser(shortRangePhaserRight, shortRangePhaserRightOrigin, sharedSRCurrentDir, 1, rightActive, srTemp);

        // Apply damage to the locked target while either beam is firing
        if ((leftActive || rightActive) && cachedDamageable != null && cachedTarget != null)
        {
            float pulseValue = Mathf.SmoothStep(0, 1, Mathf.PingPong(SRPulsePhase, 1));
            if (pulseValue > 0.3f)
            {
                cachedDamageable.damage(SRDamagePerSecond * dt);
            }
        }
    }

    private void updateShortRangePhaser(LineRenderer phaser, GameObject origin, Vector3 beamDirection, int index, bool active, float temperature)
    {
        if (phaser == null) return;

        if (!active)
        {
            phaser.enabled = false;
            currSRBeamWidth[index] = minSRBeamDiameter;
            return;
        }

        float pulseValue = Mathf.SmoothStep(0, 1, Mathf.PingPong(SRPulsePhase, 1));

        bool beamVisible = pulseValue > 0.3f;
        phaser.enabled = beamVisible;

        float beamWidth = Mathf.Lerp(minSRBeamDiameter, maxSRBeamDiameter, Mathf.Max(0.01f, temperature)) * pulseValue;
        phaser.startWidth = currSRBeamWidth[index];
        phaser.endWidth = beamWidth * SRBeamEndDiameterRatio;

        Vector3 beamStart = origin.transform.position;
        Vector3 beamEnd = beamStart + beamDirection * SHORT_RANGE_BEAM_LENGTH;

        // Shorten beam to hit point if locked onto a target
        if (cachedTarget != null)
        {
            Collider targetCol = cachedTarget.GetComponent<Collider>();
            if (targetCol != null && targetCol.Raycast(new Ray(beamStart, beamDirection), out RaycastHit hit, SHORT_RANGE_BEAM_LENGTH))
            {
                beamEnd = hit.point;
            }
        }

        phaser.SetPosition(0, beamStart);
        phaser.SetPosition(1, beamEnd);
    }

    private static readonly Collider[] overlapSphereBuffer = new Collider[128];

    private Transform getNearestEnemy(Vector3 originPos, Vector3 forwardDir)
    {
        Transform nearestEnemy = null;
        IDamageable nearestDamageable = null;
        float minDistance = float.MaxValue;

        int overlapCount = Physics.OverlapSphereNonAlloc(originPos, SRTrackingRange, overlapSphereBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider col = overlapSphereBuffer[i];
            if (col == null) continue;
            considerCandidate(col.transform, originPos, forwardDir, ref nearestEnemy, ref nearestDamageable, ref minDistance);
        }

        cachedDamageable = nearestDamageable;
        return nearestEnemy;
    }

    private void considerCandidate(Transform candidate, Vector3 originPos, Vector3 forwardDir,
                                   ref Transform nearestEnemy, ref IDamageable nearestDamageable, ref float minDistance)
    {
        IDamageable dmg = candidate.GetComponent<IDamageable>();
        if (dmg == null) return;

        Vector3 dirToTarget = (candidate.position - originPos).normalized;
        float angle = Vector3.Angle(forwardDir, dirToTarget);
        if (angle > SRMaxTrackingAngle) return;

        float dist = Vector3.Distance(originPos, candidate.position);
        if (dist < minDistance)
        {
            minDistance = dist;
            nearestEnemy = candidate;
            nearestDamageable = dmg;
        }
    }
}


// For Debugging
/*
#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    if (shortRangePhaserLeftOrigin == null || shortRangePhaserRightOrigin == null) return;

    Vector3 trackingMidpoint = (shortRangePhaserLeftOrigin.transform.position
                              + shortRangePhaserRightOrigin.transform.position) / 2f;
    Vector3 defaultForward = shortRangePhaserLeftOrigin.transform.forward;

    // Draw the inner proximity radius (Faint Cyan)
    Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
    Gizmos.DrawWireSphere(trackingMidpoint, SRTrackingRadius);

    // Draw the outer limits of the radar sweep (Faint Red)
    Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
    Gizmos.DrawWireSphere(trackingMidpoint, SRTrackingRange);

    // Calculate the physical edges of the tracking cone
    Vector3 rightDir = shortRangePhaserLeftOrigin.transform.right;
    Vector3 upDir = shortRangePhaserLeftOrigin.transform.up;

    Vector3 upperEdge = Quaternion.AngleAxis(-SRMaxTrackingAngle, rightDir) * defaultForward;
    Vector3 lowerEdge = Quaternion.AngleAxis(SRMaxTrackingAngle, rightDir) * defaultForward;
    Vector3 leftEdge = Quaternion.AngleAxis(-SRMaxTrackingAngle, upDir) * defaultForward;
    Vector3 rightEdge = Quaternion.AngleAxis(SRMaxTrackingAngle, upDir) * defaultForward;

    // Draw the Cone frame (Solid Red)
    Gizmos.color = Color.red;
    Gizmos.DrawRay(trackingMidpoint, upperEdge * SRTrackingRange);
    Gizmos.DrawRay(trackingMidpoint, lowerEdge * SRTrackingRange);
    Gizmos.DrawRay(trackingMidpoint, leftEdge * SRTrackingRange);
    Gizmos.DrawRay(trackingMidpoint, rightEdge * SRTrackingRange);
    Gizmos.DrawRay(trackingMidpoint, defaultForward * SRTrackingRange); // Center-line

    // Draw the transparent arc faces using UnityEditor Handles
    UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.05f);
    UnityEditor.Handles.DrawSolidArc(trackingMidpoint, upDir, leftEdge, SRMaxTrackingAngle * 2, SRTrackingRange);
    UnityEditor.Handles.DrawSolidArc(trackingMidpoint, rightDir, upperEdge, SRMaxTrackingAngle * 2, SRTrackingRange);
}
#endif
}
*/