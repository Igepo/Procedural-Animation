using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegStepper : MonoBehaviour
{
    // The position and rotation we want to stay in range of
    [SerializeField] Transform homeTransform;
    
    [SerializeField] Transform targetTransform;
    // Stay within this distance of home
    [SerializeField] float wantStepAtDistance;
    // How long a step takes to complete
    [SerializeField] float moveDuration = 1f;

    // Fraction of the max distance from home we want to overshoot by
    [SerializeField] float stepOvershootFraction;

    // Is the leg moving?
    public bool Moving;

    void Start()
    {
        
    }

    public void TryMove()
    {
        // If we are already moving, don't start another move
        if (Moving) return;

        float distFromHome = Vector3.Distance(targetTransform.position, homeTransform.position);

        // If we are too far off in position or rotation
        if (distFromHome > wantStepAtDistance)
        {
            // Start the step coroutine
            StartCoroutine(Move());
        }
    }


    // Coroutines must return an IEnumerator
    IEnumerator Move()
    {
        // Indicate we're moving (used later)
        Moving = true;

        // Store the initial conditions
        Quaternion startRot = targetTransform.rotation;
        Vector3 startPoint = targetTransform.position;

        Quaternion endRot = homeTransform.rotation;

        // Directional vector from the foot to the home position
        Vector3 towardHome = (homeTransform.position - targetTransform.position);
        // Total distance to overshoot by   
        float overshootDistance = wantStepAtDistance * stepOvershootFraction;
        Vector3 overshootVector = towardHome * overshootDistance;
        overshootVector = Vector3.ProjectOnPlane(overshootVector, Vector3.up);

        // Apply the overshoot
        Vector3 endPoint = homeTransform.position + overshootVector;

        Vector3 centerPoint = (startPoint + endPoint) / 2;
        centerPoint += homeTransform.up * Vector3.Distance(startPoint, endPoint) / 2f;

        // Time since step started
        float timeElapsed = 0;

        // Here we use a do-while loop so the normalized time goes past 1.0 on the last iteration,
        // placing us at the end position before ending.
        do
        {
            // Add time since last frame to the time elapsed
            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / moveDuration;
            normalizedTime = Easing.EaseInOutCubic(normalizedTime); // Begin fast, end fast

            // Interpolate position and rotation
            targetTransform.position =
              Vector3.Lerp(
                Vector3.Lerp(startPoint, centerPoint, normalizedTime),
                Vector3.Lerp(centerPoint, endPoint, normalizedTime),
                normalizedTime
              );

            targetTransform.rotation = Quaternion.Slerp(startRot, endRot, normalizedTime);

            // Wait for one frame
            yield return null;
        }
        while (timeElapsed < moveDuration);

        // Done moving
        Moving = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(homeTransform.position, 0.01f);
    }
}
