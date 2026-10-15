using System;
using UnityEngine;
using Oculus.Interaction;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PhysicalThrowDie : MonoBehaviour
{
    public int currentFaceValue = 6;
    public bool isRolling = false;
    public Action<int> OnDieSettled;

    private Rigidbody rb;
    private Grabbable grabbable;
    private bool wasGrabbed = false;
    private float rollTimer = 0f;
    private Vector3 spawnLocalPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 0.08f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        grabbable = GetComponent<Grabbable>();
        spawnLocalPos = transform.localPosition;
    }

    void Update()
    {
        // Detect when the player grabs and releases the die using Meta XR Interaction SDK!
        if (grabbable != null)
        {
            bool isCurrentlyGrabbed = grabbable.SelectingPointsCount > 0;
            if (isCurrentlyGrabbed)
            {
                wasGrabbed = true;
                isRolling = false;
            }
            else if (wasGrabbed && !isCurrentlyGrabbed)
            {
                // Player just released/threw the die from their hand!
                wasGrabbed = false;
                isRolling = true;
                rollTimer = 0f;
                // Give a subtle extra spin boost so hand throws feel crisp
                rb.AddTorque(UnityEngine.Random.onUnitSphere * 1.5f, ForceMode.Impulse);
            }
        }

        if (isRolling)
        {
            rollTimer += Time.deltaTime;

            // Safety catch if the player threw the die off the edge of the table
            if (transform.localPosition.y < -0.15f || Vector3.Distance(transform.localPosition, Vector3.zero) > 0.65f)
            {
                ResetAndTossWithPhysics();
                return;
            }

            // Wait at least 0.35s, then check if the Rigidbody has come to rest on the table
            if (rollTimer > 0.35f && rb.linearVelocity.sqrMagnitude < 0.002f && rb.angularVelocity.sqrMagnitude < 0.005f)
            {
                isRolling = false;
                currentFaceValue = ReadTopFaceFromOrientation();
                OnDieSettled?.Invoke(currentFaceValue);
            }
            else if (rollTimer > 3.5f)
            {
                // Timeout safety if die gets stuck leaning against a building
                isRolling = false;
                currentFaceValue = ReadTopFaceFromOrientation();
                OnDieSettled?.Invoke(currentFaceValue);
            }
        }
    }

    public void ResetAndTossWithPhysics()
    {
        transform.localPosition = spawnLocalPos + new Vector3(0f, 0.08f, 0f);
        transform.localRotation = UnityEngine.Random.rotation;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Launch upward and inward with real physics impulse + torque
        Vector3 throwDir = new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 1.4f, UnityEngine.Random.Range(-0.3f, 0.3f));
        rb.AddForce(throwDir * 0.18f, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.onUnitSphere * 2.5f, ForceMode.Impulse);

        isRolling = true;
        rollTimer = 0f;
    }

    public int ReadTopFaceFromOrientation()
    {
        // Compare the 6 local axes of our 3D Pips Die against the board's Up vector (transform.parent.up)
        // Face mapping from CreateExact3DPipsDie:
        // +Y = 1, -Y = 6, +Z = 2, -Z = 5, +X = 3, -X = 4
        Vector3 boardUp = transform.parent != null ? transform.parent.up : Vector3.up;

        float bestDot = -2f;
        int bestFace = 1;

        Vector3[] axes = new Vector3[]
        {
            transform.up,       // Face 1 (+Y)
            transform.forward,  // Face 2 (+Z)
            transform.right,    // Face 3 (+X)
            -transform.right,   // Face 4 (-X)
            -transform.forward, // Face 5 (-Z)
            -transform.up       // Face 6 (-Y)
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