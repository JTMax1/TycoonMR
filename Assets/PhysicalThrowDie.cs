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
        rb.isKinematic = false;

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
        // Automatically attach Meta XR Interaction SDK Grabbable + HandGrabInteractable + DistanceHandGrabInteractable
        // so your Interactions Rig & Distance Grab Building Blocks recognize this die natively!
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
            rb.AddForce(Physics.gravity * (extraGravityMultiplier - 1f), ForceMode.Acceleration);

            // Bounded physics containment within courtyard (-0.30m to +0.30m relative to board)
            Vector3 localPos = transform.localPosition;
            
            // Soft inward physics restoring force if drifting outside courtyard boundary
            if (Mathf.Abs(localPos.x) > 0.26f)
            {
                float sign = -Mathf.Sign(localPos.x);
                Vector3 inwardDir = transform.parent != null ? transform.parent.TransformDirection(Vector3.right * sign) : Vector3.right * sign;
                rb.AddForce(inwardDir * 6.5f, ForceMode.Acceleration);
            }
            if (Mathf.Abs(localPos.z) > 0.26f)
            {
                float sign = -Mathf.Sign(localPos.z);
                Vector3 inwardDir = transform.parent != null ? transform.parent.TransformDirection(Vector3.forward * sign) : Vector3.forward * sign;
                rb.AddForce(inwardDir * 6.5f, ForceMode.Acceleration);
            }

            // Fall-through recovery: if die somehow falls below board plane
            if (localPos.y < -0.15f || Mathf.Abs(localPos.x) > 0.70f || Mathf.Abs(localPos.z) > 0.70f)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                transform.localPosition = spawnLocalPos + Vector3.up * 0.08f;
            }

            rollTimer += Time.fixedDeltaTime;
            if (rollTimer > 0.40f && rb.linearVelocity.sqrMagnitude < 0.003f && rb.angularVelocity.sqrMagnitude < 0.008f)
            {
                SettleDieFlat();
            }
            else if (rollTimer > 2.4f)
            {
                SettleDieFlat();
            }
        }
    }

    void Update()
    {
        // 1. Native Meta XR Interaction SDK Grab / Distance Grab detection
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

        // 2. Magnet-Pull to Hand & Shake while held
        if (isHeldByPlayer && trackedHandGrabber != null)
        {
            Vector3 targetPos = trackedHandGrabber.position + handHoldOffset;
            Vector3 frameVel = (trackedHandGrabber.position - lastHandPos) / Mathf.Max(0.001f, Time.deltaTime);
            smoothedHandVelocity = Vector3.Lerp(smoothedHandVelocity, frameVel, Time.deltaTime * 18f);
            lastHandPos = trackedHandGrabber.position;

            // Smooth magnet pull from across the board into your palm!
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 22f);
            transform.Rotate((smoothedHandVelocity.magnitude * 180f + 140f) * Time.deltaTime, 180f * Time.deltaTime, 110f * Time.deltaTime);
            return;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isRolling && collision.relativeVelocity.magnitude > 0.15f)
        {
            OnDieBounceImpact?.Invoke(Mathf.Clamp01(collision.relativeVelocity.magnitude / 2.5f));
        }
    }

    void SettleDieFlat()
    {
        isRolling = false;
        rb.isKinematic = true;
        currentFaceValue = ReadTopFaceFromOrientation();
        SnapOrientationToFace(currentFaceValue);
        OnDieSettled?.Invoke(currentFaceValue);
    }

    private void SnapOrientationToFace(int face)
    {
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

        Quaternion rot = Quaternion.FromToRotation(localTargetUp, Vector3.up);
        transform.localRotation = rot;
    }

    public void TossWithPhysicsImpulse()
    {
        isHeldByPlayer = false;
        trackedHandGrabber = null;
        transform.localPosition = spawnLocalPos + new Vector3(UnityEngine.Random.Range(-0.02f, 0.02f), 0.13f, UnityEngine.Random.Range(-0.02f, 0.02f));
        transform.localRotation = UnityEngine.Random.rotation;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 throwForce = new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), 1.35f, UnityEngine.Random.Range(-0.35f, 0.35f));
        rb.AddForce(throwForce * 0.16f, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.onUnitSphere * 4.2f, ForceMode.Impulse);

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