using UnityEngine;

public class InverseKinematics : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Transform pole;

    [SerializeField] Transform firstBone;
    [SerializeField] Vector3 firstBoneEulerAngleOffset;
    [SerializeField] Transform secondBone;
    [SerializeField] Vector3 secondBoneEulerAngleOffset;
    [SerializeField] Transform thirdBone;
    [SerializeField] Vector3 thirdBoneEulerAngleOffset;
    [SerializeField] Transform endEffector; // Le point final de la patte

    [SerializeField] bool alignThirdBoneWithTargetRotation = true;

    void OnEnable()
    {
        if (
            firstBone == null ||
            secondBone == null ||
            thirdBone == null ||
            pole == null ||
            target == null ||
            endEffector == null
        )
        {
            Debug.LogError("IK bones not initialized", this);
            enabled = false;
            return;
        }
    }

    void LateUpdate()
    {
        Vector3 towardPole = pole.position - firstBone.position;
        Vector3 towardTarget = target.position - firstBone.position;

        float firstBoneLength = Vector3.Distance(firstBone.position, secondBone.position);
        float secondBoneLength = Vector3.Distance(secondBone.position, thirdBone.position);
        float thirdBoneLength = Vector3.Distance(thirdBone.position, endEffector.position);
        float totalChainLength = firstBoneLength + secondBoneLength + thirdBoneLength;

        // Align the first bone towards the target
        firstBone.rotation = Quaternion.LookRotation(towardTarget, towardPole);
        firstBone.localRotation *= Quaternion.Euler(firstBoneEulerAngleOffset);

        Vector3 towardSecondBone = secondBone.position - firstBone.position;

        // Calculate the distance to the target
        float targetDistance = Vector3.Distance(firstBone.position, target.position);
        targetDistance = Mathf.Min(targetDistance, totalChainLength * 0.9999f);

        // Calculate the angle for the first bone
        float adjacent1 = ((firstBoneLength * firstBoneLength) + (targetDistance * targetDistance) - (secondBoneLength * secondBoneLength)) / (2 * targetDistance * firstBoneLength);
        float angle1 = Mathf.Acos(adjacent1) * Mathf.Rad2Deg;
        Vector3 cross1 = Vector3.Cross(towardPole, towardSecondBone);

        if (!float.IsNaN(angle1))
        {
            firstBone.RotateAround(firstBone.position, cross1, -angle1);
        }

        // Align the second bone towards the target
        Vector3 towardThirdBone = thirdBone.position - secondBone.position;
        Quaternion secondBoneTargetRotation = Quaternion.LookRotation(target.position - secondBone.position, cross1);
        secondBoneTargetRotation *= Quaternion.Euler(secondBoneEulerAngleOffset);
        secondBone.rotation = secondBoneTargetRotation;

        // Align the third bone to point towards the end effector (target)
        if (alignThirdBoneWithTargetRotation)
        {
            // Instead of using a look rotation, we calculate directly towards the target
            Vector3 directionToTarget = target.position - thirdBone.position;
            if (directionToTarget.magnitude > 0.01f) // Eviter le problème de division par zéro
            {
                thirdBone.rotation = Quaternion.LookRotation(directionToTarget);
            }
            thirdBone.localRotation *= Quaternion.Euler(thirdBoneEulerAngleOffset);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(target.position, 0.01f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(pole.position, 0.01f);

        Gizmos.DrawLine(thirdBone.position, target.position);
    }

}
