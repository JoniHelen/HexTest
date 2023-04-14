using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtTrigger : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float threshold;


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 0.25f);

        Vector3 dir = (target.position - transform.position).normalized;

        Gizmos.color = Vector3.Dot(dir, transform.forward) > 1 - threshold ? Color.red : Color.green;
        Gizmos.DrawRay(transform.position, transform.forward);
    }

    // Pistetulo
    // float dot = dir.x * transform.forward.x + dir.y * transform.forward.y + dir.z * transform.forward.z;
}
