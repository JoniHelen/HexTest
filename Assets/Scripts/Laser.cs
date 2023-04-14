using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    [SerializeField] int maxBounces;
    private void OnDrawGizmos()
    {
        Vector3 dir = transform.right;
        Vector3 pos = transform.position;

        for (int i = 0; i < maxBounces; i++)
        {
            if (Physics.Raycast(pos, dir, out RaycastHit hit))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pos, hit.point);
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(hit.point, 0.075f);

                dir = Vector3.Reflect(dir, hit.normal);
                pos = hit.point;
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(pos, dir * 10000);
                break;
            }
        }
    }
    // Reflektio
    // Vector3 reflection = dir - 2 * (dir.x * hit.normal.x + dir.y * hit.normal.y + dir.z * hit.normal.z) * hit.normal;
}
