using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class HomePositionPlacement : MonoBehaviour
{
    [SerializeField] private float height = 10f;

    private Vector3 raycastOrigin;
    RaycastHit hit;

    void Start()
    {
        
    }

    void Update()
    {
        Vector3 homePosition = transform.position;

        raycastOrigin = homePosition + Vector3.up * height;
        if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, Mathf.Infinity))
        {
            transform.position = hit.point;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(raycastOrigin, hit.point);
    }
}
