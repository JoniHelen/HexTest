using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotLookTrigger : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] new Light light;

    private void Update()
    {
        Vector3 dir = (target.position - transform.position).normalized;

        light.color = (Mathf.Acos(Vector3.Dot(dir, transform.forward)) * Mathf.Rad2Deg) > light.spotAngle / 2 ? Color.white : Color.red;
    }
}
