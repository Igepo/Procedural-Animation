using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class HomePositionPlacement : MonoBehaviour
{
    //[SerializeField] private float height = 10f;
    [SerializeField] private Vector3 raycastDirectionOffset = Vector3.zero;
    [SerializeField] private Transform homeTransform;

    private Vector3 raycastOrigin;
    private Vector3 raycastDirection;
    private RaycastHit hit;
    private Vector3 initialPosition;

    void Start()
    {
    }

    void Update()
    {
        //raycastOrigin = initialPosition + Vector3.up * height;
        //raycastOrigin = transform.position;
        //raycastDirection = bodyTransform.TransformDirection(Vector3.down + raycastDirectionOffset).normalized;
        raycastDirection = -(transform.up + raycastDirectionOffset).normalized;
        if (Physics.Raycast(transform.position, raycastDirection, out hit, Mathf.Infinity))
        {
            homeTransform.position = hit.point;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + raycastDirection);
    }
}
