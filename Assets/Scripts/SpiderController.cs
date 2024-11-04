using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

public class SpiderController : MonoBehaviour
{
    [SerializeField] LegStepper frontLeftLegStepper;
    [SerializeField] LegStepper frontRightLegStepper;
    [SerializeField] LegStepper backLeftLegStepper;
    [SerializeField] LegStepper backRightLegStepper;

    // The target we are going to track
    [SerializeField] Transform target;
    // A reference to the spider's head/body
    [SerializeField] Transform body; 

    [SerializeField] float headTrackingSpeed;

    // How fast we can turn and move full throttle
    [SerializeField] float turnSpeed;
    [SerializeField] float moveSpeed;
    // How fast we will reach the above speeds
    [SerializeField] float turnAcceleration;
    [SerializeField] float moveAcceleration;
    // Try to stay in this range from the target
    [SerializeField] float minDistToTarget;
    [SerializeField] float maxDistToTarget;
    // If we are above this angle from the target, start turning
    [SerializeField] float maxAngToTarget;

    [SerializeField] bool isFocusOnTarget;

    [SerializeField] private float bodyDistanceFromTheSurface = 0.1f;

    // World space velocity
    Vector3 currentVelocity;
    // We are only doing a rotation around the up axis, so we only use a float here
    float currentAngularVelocity;

    private Quaternion groundAlignmentRotation = Quaternion.identity;
    private Quaternion headTrackingRotation = Quaternion.identity;

    Vector3 normalBody = Vector3.zero;
    RaycastHit hit;

    void Awake()
    {
        StartCoroutine(LegUpdateCoroutine());
    }

    void LateUpdate()
    {
        AlignWithGround();

        if (isFocusOnTarget)
        {
            RootMotionUpdate();
            HeadTrackingUpdate();
        }

        ApplyRotations();
    }

    // Align the body with the ground
    void AlignWithGround()
    {
        Vector3 leg1Position = frontLeftLegStepper.homeTransform.position;
        Vector3 leg2Position = frontRightLegStepper.homeTransform.position;
        Vector3 leg3Position = backLeftLegStepper.homeTransform.position;
        Vector3 leg4Position = backRightLegStepper.homeTransform.position;

        Vector3 averageLegPosition = (leg1Position + leg2Position + leg3Position + leg4Position) / 4;

        Vector3 normalBody = LegPlaneNormalAverage();

        Vector3 targetBodyPosition = body.position;
        targetBodyPosition.y = averageLegPosition.y + bodyDistanceFromTheSurface;
        //var initialBodyPosition = body.position;
        body.position = Vector3.Lerp(body.position, targetBodyPosition, 1 - Mathf.Exp(-5 * Time.deltaTime));

        // Orientation du body
        groundAlignmentRotation = Quaternion.FromToRotation(Vector3.up, normalBody);
    }

    // Only allow diagonal leg pairs to step together
    IEnumerator LegUpdateCoroutine()
    {
        // Run continuously
        while (true)
        {
            // Try moving one diagonal pair of legs
            do
            {
                frontLeftLegStepper.TryMove();
                backRightLegStepper.TryMove();
                // Wait a frame
                yield return null;

                // Stay in this loop while either leg is moving.
                // If only one leg in the pair is moving, the calls to TryMove() will let
                // the other leg move if it wants to.
            } while (backRightLegStepper.Moving || frontLeftLegStepper.Moving);

            // Do the same thing for the other diagonal pair
            do
            {
                frontRightLegStepper.TryMove();
                backLeftLegStepper.TryMove();
                yield return null;
            } while (backLeftLegStepper.Moving || frontRightLegStepper.Moving);
        }
    }



    /// <summary>
    /// Applies head/body tracking
    /// </summary>
    void HeadTrackingUpdate()
    {
        // Store the current head rotation since we will be resetting it
        Quaternion currentLocalRotation = body.localRotation;
        body.localRotation = Quaternion.identity;

        Vector3 targetWorldLookDir = target.position - body.position;
        Vector3 targetLocalLookDir = body.InverseTransformDirection(targetWorldLookDir);

        // Get the local rotation by using LookRotation on a local directional vector
        Quaternion targetLocalRotation = Quaternion.LookRotation(targetLocalLookDir, Vector3.up);

        // Limiting rotation to Y axis
        Vector3 targetEulerAngles = targetLocalRotation.eulerAngles;
        targetEulerAngles.x = 0; // Set x to 0
        targetEulerAngles.z = 0; // Set z to 0
        headTrackingRotation = Quaternion.Euler(targetEulerAngles);

        body.localRotation = currentLocalRotation;
    }

    void RootMotionUpdate()
    {
        // Get the direction toward our target
        Vector3 towardTarget = target.position - body.transform.position;
        // Vector toward target on the local XZ plane
        Vector3 towardTargetProjected = Vector3.ProjectOnPlane(towardTarget, transform.up);
        // Get the angle from the gecko's forward direction to the direction toward toward our target
        // Here we get the signed angle around the up vector so we know which direction to turn in
        float angToTarget = Vector3.SignedAngle(body.transform.forward, towardTargetProjected, transform.up);

        float targetAngularVelocity = 0;

        // If we are within the max angle (i.e. approximately facing the target)
        // leave the target angular velocity at zero
        if (Mathf.Abs(angToTarget) > maxAngToTarget)
        {
            // Angles in Unity are clockwise, so a positive angle here means to our right
            if (angToTarget > 0)
            {
                targetAngularVelocity = turnSpeed;
            }
            // Invert angular speed if target is to our left
            else
            {
                targetAngularVelocity = -turnSpeed;
            }
        }

        // Use our smoothing function to gradually change the velocity
        currentAngularVelocity = Mathf.Lerp(
          currentAngularVelocity,
          targetAngularVelocity,
          1 - Mathf.Exp(-turnAcceleration * Time.deltaTime)
        );

        // Rotate the transform around the Y axis in world space, 
        // making sure to multiply by delta time to get a consistent angular velocity
        transform.Rotate(0, Time.deltaTime * currentAngularVelocity, 0, Space.World);

        Vector3 targetVelocity = Vector3.zero;

        // Don't move if we're facing away from the target, just rotate in place
        if (Mathf.Abs(angToTarget) < 90)
        {
            float distToTarget = Vector3.Distance(transform.position, target.position);

            // If we're too far away, approach the target
            if (distToTarget > maxDistToTarget)
            {
                targetVelocity = moveSpeed * towardTargetProjected.normalized;
            }
            // If we're too close, reverse the direction and move away
            else if (distToTarget < minDistToTarget)
            {
                targetVelocity = moveSpeed * -towardTargetProjected.normalized;
            }
        }

        currentVelocity = Vector3.Lerp(
          currentVelocity,
          targetVelocity,
          1 - Mathf.Exp(-moveAcceleration * Time.deltaTime)
        );

        // Apply the velocity
        transform.position += currentVelocity * Time.deltaTime;
    }

    public void ApplyRotations()
    {
        body.rotation = Quaternion.Slerp(
            body.rotation,
            groundAlignmentRotation * headTrackingRotation,
            Time.deltaTime * 5f
        );
    }

    public void CalculateAverageNormalFrom4Points(Vector3 A, Vector3 B, Vector3 C, Vector3 D, out Vector3 normalAverage)
    {
        Vector3 normalABC = Vector3.Cross(B - A, C - A);
        Vector3 normalABD = Vector3.Cross(B - A, D - A);
        Vector3 normalACD = Vector3.Cross(C - A, D - A);
        Vector3 normalBCD = Vector3.Cross(C - B, D - B);

        normalAverage = normalABC + normalABD + normalACD + normalBCD;

        if (normalAverage != Vector3.zero)
        {
            normalAverage = Vector3.Normalize(normalAverage);
        }
    }

    public Vector3 LegPlaneNormalAverage()
    {
        var normalAverage = Vector3.zero;

        var leg1Position = frontLeftLegStepper.homeTransform.position;
        var leg2Position = frontRightLegStepper.homeTransform.position;
        var leg3Position = backLeftLegStepper.homeTransform.position;
        var leg4Position = backRightLegStepper.homeTransform.position;

        // On se met dans le sens horaire pour avoir une normale positive (leg3 et leg4 inversé)
        CalculateAverageNormalFrom4Points(leg1Position, leg2Position, leg4Position, leg3Position, out normalAverage);
        return normalAverage;
    }

    private void OnDrawGizmos()
    {
        normalBody = LegPlaneNormalAverage();
        Gizmos.color = Color.red;
        Gizmos.DrawRay(body.position, normalBody * 5f);
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawLine(headBone.position, target.position);

    //    Gizmos.color = Color.red;
    //    Gizmos.DrawRay(headBone.position, headBone.transform.rotation * Vector3.forward);

    //    //cone
    //    if (target == null) return;

    //    Gizmos.color = Color.cyan;
    //    Vector3 position = headBone.transform.position;

    //    Vector3 directionToTarget = (target.position - position).normalized;

    //    float angleInRadians = maxAngToTarget * Mathf.Deg2Rad;

    //    Vector3 leftBoundary = Quaternion.Euler(0, -maxAngToTarget / 2, 0) * directionToTarget;
    //    Vector3 rightBoundary = Quaternion.Euler(0, maxAngToTarget / 2, 0) * directionToTarget;

    //    Gizmos.DrawLine(position, position + directionToTarget * 5);

    //    Gizmos.DrawLine(position, position + leftBoundary * 5);
    //    Gizmos.DrawLine(position, position + rightBoundary * 5);

    //    Handles.color = Color.cyan;
    //    Handles.DrawWireArc(position, Vector3.up, leftBoundary, maxAngToTarget, 5);
    //}
}
