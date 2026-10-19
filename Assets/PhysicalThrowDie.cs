using System;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PhysicalThrowDie : MonoBehaviour
{
    [Header("Tunable Dice Throw Physics")]
    public float throwForceMultiplier = 1.65f;
    public float throwUpwardArc = 0.55f;
    public float maxThrowSpeed = 3.8f;
    public float extraGravityMultiplier = 1.8f;
    [Range(0.1f, 0.9f)]
    public float diceBounciness = 0.52f;

    [Header("Boundary Box (Local Space)")]
    public float maxLocalX = 0.30f;
    public float maxLocalZ = 0.30f;
    public float minLocalY = 0.035f;

    public int currentFaceValue = 6;
    public bool isRolling = false;
    public bool isHeldByPlayer = false;
    public Action<int> OnDieSettled;
    public Action<float> OnDieBounceImpact;

    private Rigidbody rb;
    private BoxCollider boxCol;
    private Grabbable metaGrabbable;
    private Transform trackedHandGrabber;
    private Vector3 lastHandPos;
    private Vector3 smoothedHandVelocity;
    private float rollTimer = 0f;
    private Vector3 spawnLocalPos;
    private Vector3 handHoldOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 0.12f;
        rb.linearDamping = 0.35f;
        rb.angularDamping = 0.32f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = true;

        boxCol = GetComponent<BoxCollider>();
        PhysicsMaterial bounceMat = new PhysicsMaterial("TunedDiceBounce");
        bounceMat.bounciness = diceBounciness;
        bounceMat.dynamicFriction = 0.28f;
        bounceMat.staticFriction = 0.28f;
        bounceMat.bounceCombine = PhysicsMaterialCombine.Maximum;
        boxCol.material = bounceMat;

        spawnLocalPos = transform.localPosition;
        SetupMetaISDKGrabComponents();
    }

    public void InjectMetaGrabbable(Grabbable g)
    {
        metaGrabbable = g;
    }

    public void SetupMetaISDKGrabComponents()
    {
        try
        {
            if (metaGrabbable == null)
            {
                metaGrabbable = GetComponent<Grabbable>();
                if (metaGrabbable == null)
                {
                    metaGrabbable = gameObject.AddComponent<Grabbable>();
                    metaGrabbable.InjectOptionalRigidbody(rb);
                }
            }

            if (GetComponent<HandGrabInteractable>() == null && GetComponentInChildren<HandGrabInteractable>() == null)
            {
                HandGrabInteractable hgi = gameObject.AddComponent<HandGrabInteractable>();
                hgi.InjectRigidbody(rb);
                hgi.InjectOptionalPointableElement(metaGrabbable);
            }

            if (GetComponent<DistanceHandGrabInteractable>() == null && GetComponentInChildren<DistanceHandGrabInteractable>() == null)
            {
                DistanceHandGrabInteractable dhgi = gameObject.AddComponent<DistanceHandGrabInteractable>();
                dhgi.InjectRigidbody(rb);
                dhgi.InjectOptionalPointableElement(metaGrabbable);
            }
        }
        catch { }
    }

    public void BeginHandGrab(Transform handTransform, Vector3 sideOffset)
    {
        isHeldByPlayer = true;
        isRolling = false;
        trackedHandGrabber = handTransform;
        handHoldOffset = sideOffset;
        lastHandPos = handTransform.position;
        smoothedHandVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    public void EndHandGrabAndThrow(Vector3 boardCenterWorld)
    {
        if (!isHeldByPlayer) return;
        isHeldByPlayer = false;
        trackedHandGrabber = null;

        rb.isKinematic = false;

        Vector3 toCenter = (boardCenterWorld - transform.position);
        toCenter.y = 0f;
        Vector3 rawThrow = smoothedHandVelocity * throwForceMultiplier;

        if (rawThrow.magnitude < 0.35f)
        {
            rawThrow = toCenter.normalized * 0.85f + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0f, UnityEngine.Random.Range(-0.2f, 0.2f));
        }

        Vector3 finalVel = Vector3.ClampMagnitude(rawThrow, maxThrowSpeed);
        finalVel.y = Mathf.Clamp(finalVel.y + throwUpwardArc, 0.35f, 1.8f);

        rb.linearVelocity = finalVel;
        rb.angularVelocity = UnityEngine.Random.onUnitSphere * 24f;

        isRolling = true;
        rollTimer = 0f;
    }

    public void LaunchWithDirectVelocity(Vector3 velocity, Vector3 boardCenterWorld)
    {
        isHeldByPlayer = false;
        trackedHandGrabber = null;
        rb.isKinematic = false;

        Vector3 toCenter = (boardCenterWorld - transform.position);
        toCenter.y = 0f;
        Vector3 rawThrow = velocity * throwForceMultiplier;

        if (rawThrow.magnitude < 0.35f)
        {
            rawThrow = toCenter.normalized * 0.95f + new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0f, UnityEngine.Random.Range(-0.2f, 0.2f));
        }

        Vector3 finalVel = Vector3.ClampMagnitude(rawThrow, maxThrowSpeed);
        finalVel.y = Mathf.Clamp(finalVel.y + throwUpwardArc, 0.35f, 1.8f);

        rb.linearVelocity = finalVel;
        rb.angularVelocity = UnityEngine.Random.onUnitSphere * 26f;

        isRolling = true;
        rollTimer = 0f;
    }

    void FixedUpdate()
    {
        if (isRolling && !rb.isKinematic)
        {
            // Extra gravity helps dice feel weighted and prevents floatiness in VR
            rb.AddForce(Physics.gravity * (extraGravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    void Update()
    {
        // 1. Meta XR SDK Grabbable tracking
        if (metaGrabbable != null && metaGrabbable.SelectingPointsCount > 0 && !isHeldByPlayer)
        {
            isHeldByPlayer = true;
            isRolling = false;
            rb.isKinematic = false;
        }
        else if (metaGrabbable != null && metaGrabbable.SelectingPointsCount == 0 && isHeldByPlayer && trackedHandGrabber == null)
        {
            EndHandGrabAndThrow(transform.parent != null ? transform.parent.position : Vector3.zero);
        }

        // 2. Custom manual magnet-pull / shake handling
        if (isHeldByPlayer && trackedHandGrabber != null)
        {
            Vector3 targetPos = trackedHandGrabber.position + handHoldOffset;
            Vector3 frameVel = (trackedHandGrabber.position - lastHandPos) / Mathf.Max(0.001f, Time.deltaTime);
            smoothedHandVelocity = Vector3.Lerp(smoothedHandVelocity, frameVel, Time.deltaTime * 18f);
            lastHandPos = trackedHandGrabber.position;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 22f);
            transform.Rotate((smoothedHandVelocity.magnitude * 180f + 140f) * Time.deltaTime, 180f * Time.deltaTime, 110f * Time.deltaTime);
            return;
        }

        // 3. Rolling & Boundary Simulation
        if (isRolling)
        {
            rollTimer += Time.deltaTime;
            Vector3 localPos = transform.localPosition;

            // Failsafe: if knocked clean off the table, recover and force settle
            if (localPos.y < -0.4f || transform.position.y < -2.0f)
            {
                transform.localPosition = spawnLocalPos;
                SettleDieFlat();
                return;
            }

            // Courtyard boundary bumper reflection
            if (localPos.y < minLocalY || Mathf.Abs(localPos.x) > maxLocalX || Mathf.Abs(localPos.z) > maxLocalZ)
            {
                Vector3 clamped = new Vector3(
                    Mathf.Clamp(localPos.x, -maxLocalX, maxLocalX),
                    Mathf.Max(minLocalY, localPos.y),
                    Mathf.Clamp(localPos.z, -maxLocalZ, maxLocalZ)
                );
                transform.localPosition = clamped;

                // Reflect and dampen velocity inward
                Vector3 v = rb.linearVelocity;
                if (Mathf.Abs(clamped.x) >= maxLocalX) v.x = -v.x * 0.6f;
                if (Mathf.Abs(clamped.z) >= maxLocalZ) v.z = -v.z * 0.6f;
                if (v.y < 0f) v.y = -v.y * diceBounciness;
                rb.linearVelocity = v;
            }

            // Check if die has come to rest or timed out
            if (rollTimer > 0.40f && rb.linearVelocity.sqrMagnitude < 0.004f && rb.angularVelocity.sqrMagnitude < 0.01f)
            {
                SettleDieFlat();
            }
            else if (rollTimer > 2.5f)
            {
                SettleDieFlat();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isRolling && collision.relativeVelocity.magnitude > 0.15f)
        {
            OnDieBounceImpact?.Invoke(Mathf.Clamp01(collision.relativeVelocity.magnitude / 2.5f));
        }
    }

    public void SettleDieFlat()
    {
        isRolling = false;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        currentFaceValue = ReadTopFaceFromOrientation();
        SnapOrientationToFace(currentFaceValue);

        OnDieSettled?.Invoke(currentFaceValue);
    }

    public void SnapOrientationToFace(int face)
    {
        // Vector pointing out of the top face in the die's local model coordinates
        Vector3 localTargetUp;
        switch (face)
        {
            case 1: localTargetUp = Vector3.forward; break;
            case 2: localTargetUp = Vector3.up; break;
            case 3: localTargetUp = Vector3.right; break;
            case 4: localTargetUp = Vector3.left; break;
            case 5: localTargetUp = Vector3.down; break;
            case 6: default: localTargetUp = Vector3.back; break;
        }

        // Align local target axis with the parent's up axis (preserving horizontal yaw)
        Vector3 parentUp = transform.parent != null ? transform.parent.up : Vector3.up;
        Vector3 currentWorldUp = transform.TransformDirection(localTargetUp);
        Quaternion correctiveRotation = Quaternion.FromToRotation(currentWorldUp, parentUp);

        transform.rotation = correctiveRotation * transform.rotation;
    }

    public void TossWithPhysicsImpulse()
    {
        isHeldByPlayer = false;
        trackedHandGrabber = null;

        transform.localPosition = spawnLocalPos + new Vector3(UnityEngine.Random.Range(-0.03f, 0.03f), 0.15f, UnityEngine.Random.Range(-0.03f, 0.03f));
        transform.localRotation = UnityEngine.Random.rotation;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 throwForce = new Vector3(
            UnityEngine.Random.Range(-0.5f, 0.5f),
            UnityEngine.Random.Range(1.8f, 2.5f),
            UnityEngine.Random.Range(-0.5f, 0.5f)
        );

        rb.AddForce(throwForce * 0.25f, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(6f, 12f), ForceMode.Impulse);

        isRolling = true;
        rollTimer = 0f;
    }

    public int ReadTopFaceFromOrientation()
    {
        Vector3 boardUp = transform.parent != null ? transform.parent.up : Vector3.up;
        float bestDot = -2f;
        int bestFace = 1;

        Vector3[] axes = new Vector3[]
        {
            transform.forward,  // Face 1 (+Z)
            transform.up,       // Face 2 (+Y)
            transform.right,    // Face 3 (+X)
            -transform.right,   // Face 4 (-X)
            -transform.up,      // Face 5 (-Y)
            -transform.forward  // Face 6 (-Z)
        };

        for (int i = 0; i < 6; i++)
        {
            float d = Vector3.Dot(axes[i], boardUp);
            if (d > bestDot)
            {
                bestDot = d;
                bestFace = i + 1;
            }
        }
        return bestFace;
    }
}