using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class BodySurfaceFollower : MonoBehaviour
{
    [SerializeField] private Transform body;

    [SerializeField] private float bodyHeight = 1f;
    [SerializeField] private float raycastMaxDistance = 1f; // Distance maximum que parcours le rayon

    private RaycastHit hit;

    public Quaternion AlignWithGround()
    {
        Quaternion groundAlignmentRotation = Quaternion.identity;
        Vector3 bodyPosition = body.position;

        if (Physics.Raycast(bodyPosition, Vector3.down, out hit, raycastMaxDistance))
        {
            // Le corps doit être à la hauteur indiqué par bodyheight par rapport au point d'impact
            body.position = hit.point + (bodyHeight* Vector3.up);

            Vector3 normal = hit.normal;
            groundAlignmentRotation = Quaternion.FromToRotation(Vector3.up, normal);
        }

        return groundAlignmentRotation;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(body.position, body.position + Vector3.down * raycastMaxDistance);
        

        if (hit.collider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(hit.point, hit.point + hit.normal);
        }
    }
}
