using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialTrigger : MonoBehaviour
{
    [SerializeField] float radius;
    [SerializeField] List<Transform> targets = new();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);

        targets.ForEach(t => {
            Gizmos.color = Vector3.Distance(transform.position, t.position) < radius ? Color.red : Color.green;
            Gizmos.DrawSphere(t.position, 0.25f);
        });
    }

    // Miten Distance toimii (kirjotin tähän ku en haluu käyttää sitä oikeessa koodissa):
    // Vector3 between = transform.position - t.position;
    // float distance = Mathf.Sqrt(between.x * between.x + between.y * between.y + between.z * between.z);
}
