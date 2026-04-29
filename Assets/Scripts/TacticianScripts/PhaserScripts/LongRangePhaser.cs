/*
    LongRangePhaser.cs
    - Renders the long range phaser beam 
    - Handles beam pulse, intensity, and collider sizing
    - Semi-tracks toward nearest enemy within a forward cone (augments manual aim)
    - Pulls aim/active/temperature data from control scripts
    Last Updated: 4/27/2026
*/

using UnityEngine;

public class LongRangePhaser : MonoBehaviour
{
    private LongRangeDirection longRangeDirection;
    private PhaserActivators phaserActivators;
    private PhaserIntensities phaserIntensities;

    public GameObject longRangePhaserOrigin;

    private bool controlsAssigned = false;

    private void resolveControls()
    {
        if (controlsAssigned) return;
        if (ReferenceAssistor.Instance == null) return;
        if (ReferenceAssistor.Instance.module_handlers == null) return;
        if (ReferenceAssistor.Instance.module_handlers.Count <= 1) return;

        GameObject controlHandler = ReferenceAssistor.Instance.module_handlers[1].gameObject;
        longRangeDirection = controlHandler.GetComponent<LongRangeDirection>();
        phaserActivators = controlHandler.GetComponent<PhaserActivators>();
        phaserIntensities = controlHandler.GetComponent<PhaserIntensities>();

        controlsAssigned = longRangeDirection && phaserActivators && phaserIntensities;
    }

    private float maxLRBeamWidth = 3.5f;
    private float LRBeamEndDiameterRatio = 0.2f;

    private float baseLRPulseSpeed = 6f;
    private float maxLRPulseSpeed = 12f;
    private float LRpulseSmoothing = 0.01f;
    private float minLRPulsePercentage = 0.1f;
    private float maxLRPulsePercentage = 0.5f;
    private float maxLRIntensity = 8.5f;

    private int LRWaveResolution = 30;
    private float LRWaveAmplitude = 1.75f;
    private float LRWaveSpeed = 15f;
    private float LRWaveCount = 4f;

    private float LRTrackingRange = 800f;
    private float LRMaxTrackingAngle = 15f;
    private float LRTrackingSpeed = 3f;
    private float LRTargetScanInterval = 1f;
    private string enemyTag = "Enemy";

    private LineRenderer longRangePhaser;
    private BoxCollider longRangePhaserCollider;

    private Material longRangePhaserMaterial;
    private Color longRangeEmissionColor;

    private const float LONG_RANGE_BEAM_LENGTH = 800f;
    private const string EMISSION_COLOR = "_EmissionColor";
    private const string EMISSION_ = "_EMISSION";

    private float pulseTimer;
    private float smoothedPulse;
    private float velocity;

    private float currentTrackingOffset;
    private Transform cachedTarget;
    private float nextTargetScanTime;

    private static readonly Collider[] overlapSphereBuffer = new Collider[128];

    private bool active;
    private float beamTemp;
    private float longRangePhaserAngle;

    private void Start()
    {
        longRangePhaser = longRangePhaserOrigin.GetComponentInChildren<LineRenderer>(true);
        if (longRangePhaser == null) return;

        longRangePhaser.useWorldSpace = true;

        longRangePhaserCollider = longRangePhaser.GetComponent<BoxCollider>();
        if (longRangePhaserCollider != null)
        {
            longRangePhaserCollider.isTrigger = true;
        }

        longRangePhaserMaterial = new Material(longRangePhaser.material);
        longRangeEmissionColor = longRangePhaserMaterial.GetColor(EMISSION_COLOR);

        longRangePhaser.positionCount = LRWaveResolution;
    }

    private void readControls()
    {
        if (phaserActivators != null)
        {
            bool[] activePhasers = phaserActivators.getActivePhasers();
            active = activePhasers != null && activePhasers.Length > 0 && activePhasers[0];
        }
        if (phaserIntensities != null)
        {
            float[] phaserTemps = phaserIntensities.getPhaserTemperatures();
            beamTemp = phaserTemps != null && phaserTemps.Length > 0 ? phaserTemps[0] : 0f;
        }
        if (longRangeDirection != null)
        {
            longRangePhaserAngle = longRangeDirection.getPhaserDirectionAngle();
        }
    }

    private void Update()
    {
        if (longRangePhaser == null) return;

        if (!controlsAssigned)
        {
            resolveControls();
            if (!controlsAssigned) return;
        }

        readControls();
        updateTracking(Time.deltaTime);
        updateLongRangePhaser(Time.deltaTime);
    }

    private void updateTracking(float dt)
    {
        float desiredOffset = 0f;

        if (active)
        {
            Vector3 originPos = longRangePhaserOrigin.transform.position;

            Vector3 manualForward = Quaternion.Euler(0f, longRangePhaserAngle, 0f) * Vector3.forward;
            if (longRangePhaserOrigin.transform.parent != null)
            {
                manualForward = longRangePhaserOrigin.transform.parent.rotation * manualForward;
            }

            if (Time.time >= nextTargetScanTime)
            {
                cachedTarget = getNearestEnemy(originPos, manualForward);
                nextTargetScanTime = Time.time + LRTargetScanInterval;
            }

            if (cachedTarget != null)
            {
                Vector3 toTarget = cachedTarget.position - originPos;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Vector3 manualForwardFlat = manualForward;
                    manualForwardFlat.y = 0f;
                    if (manualForwardFlat.sqrMagnitude > 0.0001f)
                    {
                        float signedAngle = Vector3.SignedAngle(manualForwardFlat.normalized, toTarget.normalized, Vector3.up);
                        desiredOffset = Mathf.Clamp(signedAngle, -LRMaxTrackingAngle, LRMaxTrackingAngle);
                    }
                }
            }
        }
        else
        {
            cachedTarget = null;
            nextTargetScanTime = 0f;
        }

        float t = 1f - Mathf.Exp(-LRTrackingSpeed * dt);
        currentTrackingOffset = Mathf.Lerp(currentTrackingOffset, desiredOffset, t);
    }

    private void updateLongRangePhaser(float dt)
    {
        if (longRangePhaser.enabled != active)
        {
            longRangePhaser.enabled = active;
            if (longRangePhaserCollider != null) longRangePhaserCollider.enabled = active;
            if (!active) pulseTimer = 0f;
            return;
        }

        if (!active) return;

        float clampedTemp = Mathf.Clamp01(Mathf.Max(0.1f, beamTemp));
        float temperatureScaledSpeed = Mathf.Lerp(baseLRPulseSpeed, maxLRPulseSpeed, clampedTemp);

        pulseTimer += dt * temperatureScaledSpeed;
        float currentPulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
        smoothedPulse = Mathf.SmoothDamp(smoothedPulse, currentPulse, ref velocity, LRpulseSmoothing);

        float currentBaseWidth = maxLRBeamWidth * clampedTemp;
        float pulseWidth = currentBaseWidth * calculatePulseWidth(clampedTemp) * (smoothedPulse * 0.75f);
        float finalWidth = currentBaseWidth + pulseWidth;

        longRangePhaser.startWidth = finalWidth;
        longRangePhaser.endWidth = finalWidth * LRBeamEndDiameterRatio;

        longRangePhaserOrigin.transform.localRotation = Quaternion.Euler(0f, longRangePhaserAngle + currentTrackingOffset, 0f);

        Vector3 startPos = longRangePhaserOrigin.transform.position;
        Vector3 forwardDir = longRangePhaserOrigin.transform.forward;
        Vector3 rightDir = longRangePhaserOrigin.transform.right;

        for (int i = 0; i < LRWaveResolution; i++)
        {
            float t = (float)i / (LRWaveResolution - 1);

            Vector3 basePoint = startPos + forwardDir * (LONG_RANGE_BEAM_LENGTH * t);

            float waveOffset = Mathf.Sin((t * LRWaveCount * Mathf.PI * 2f) - (Time.time * LRWaveSpeed));

            float originAttachmentScale = Mathf.Clamp01(t * 10f);

            float currentAmplitude = LRWaveAmplitude * clampedTemp * originAttachmentScale;

            Vector3 finalPoint = basePoint + (rightDir * waveOffset * currentAmplitude);

            longRangePhaser.SetPosition(i, finalPoint);
        }

        updateBeamIntensity(clampedTemp, smoothedPulse);
        resizeCollider(currentBaseWidth, LRWaveAmplitude * clampedTemp);
    }

    private float calculatePulseWidth(float temp)
    {
        return Mathf.Lerp(minLRPulsePercentage, maxLRPulsePercentage, (1f - Mathf.Max(temp, 0.001f)));
    }

    private void updateBeamIntensity(float temperature, float pulseIntensity)
    {
        if (longRangePhaserMaterial == null) return;

        float intensity = Mathf.Lerp(longRangeEmissionColor.maxColorComponent, maxLRIntensity, temperature)
                        + pulseIntensity;

        longRangePhaserMaterial.EnableKeyword(EMISSION_);
        longRangePhaserMaterial.SetColor(EMISSION_COLOR, longRangeEmissionColor * intensity);
    }

    private void resizeCollider(float beamWidth, float currentAmplitude)
    {
        if (longRangePhaserCollider == null) return;

        float finalWidth = beamWidth + (currentAmplitude * 2f);

        longRangePhaserCollider.size = new Vector3(
            finalWidth,
            finalWidth,
            LONG_RANGE_BEAM_LENGTH
        );
        longRangePhaserCollider.center = new Vector3(0f, 0f, LONG_RANGE_BEAM_LENGTH * 0.5f);
    }

    private Transform getNearestEnemy(Vector3 originPos, Vector3 forwardDir)
    {
        Transform nearestEnemy = null;
        float minDistance = float.MaxValue;

        int overlapCount = Physics.OverlapSphereNonAlloc(originPos, LRTrackingRange, overlapSphereBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider col = overlapSphereBuffer[i];
            if (col == null) continue;
            considerCandidate(col.transform, originPos, forwardDir, ref nearestEnemy, ref minDistance);
        }

        return nearestEnemy;
    }

    private void considerCandidate(Transform candidate, Vector3 originPos, Vector3 forwardDir,
                                   ref Transform nearestEnemy, ref float minDistance)
    {
        if (!candidate.CompareTag(enemyTag)) return;

        Vector3 dirToTarget = candidate.position - originPos;
        dirToTarget.y = 0f;
        if (dirToTarget.sqrMagnitude < 0.0001f) return;
        dirToTarget.Normalize();

        Vector3 forwardFlat = forwardDir;
        forwardFlat.y = 0f;
        if (forwardFlat.sqrMagnitude < 0.0001f) return;
        forwardFlat.Normalize();

        float angle = Vector3.Angle(forwardFlat, dirToTarget);
        if (angle > LRMaxTrackingAngle) return;

        float dist = Vector3.Distance(originPos, candidate.position);
        if (dist < minDistance)
        {
            minDistance = dist;
            nearestEnemy = candidate;
        }
    }
}

// For Debugging
/*

#if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    if (longRangePhaserOrigin == null) return;

    Vector3 originPos = longRangePhaserOrigin.transform.position;

    // Use the origin's current forward as the cone axis for visualization
    Vector3 forwardDir = longRangePhaserOrigin.transform.forward;
    Vector3 upDir = Vector3.up;

    // Outer detection radius (faint red)
    Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
    Gizmos.DrawWireSphere(originPos, LRTrackingRange);

    // Cone edges in the horizontal plane (LR only tracks on Y axis)
    Vector3 leftEdge = Quaternion.AngleAxis(-LRMaxTrackingAngle, upDir) * forwardDir;
    Vector3 rightEdge = Quaternion.AngleAxis(LRMaxTrackingAngle, upDir) * forwardDir;

    Gizmos.color = Color.red;
    Gizmos.DrawRay(originPos, leftEdge * LRTrackingRange);
    Gizmos.DrawRay(originPos, rightEdge * LRTrackingRange);
    Gizmos.DrawRay(originPos, forwardDir * LRTrackingRange); // Center-line

    UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.05f);
    UnityEditor.Handles.DrawSolidArc(originPos, upDir, leftEdge, LRMaxTrackingAngle * 2f, LRTrackingRange);
}
#endif
}
*/