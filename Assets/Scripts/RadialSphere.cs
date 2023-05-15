using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialSphere : MonoBehaviour
{
    [SerializeField] float radius;
    [SerializeField] List<Transform> objects = new();

    MaterialPropertyBlock block;
    Renderer rend;

    float timer = 2f;
    float elapsed = 0;

    bool canTrigger = true;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        block = new();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void Update()
    {
        if (canTrigger)
        {
            objects.ForEach(o =>
            {
                if (Vector3.Distance(transform.position, o.position) <= radius)
                {
                    block.SetColor("_Color", Color.HSVToRGB(Random.value, 1, 1));
                    rend.SetPropertyBlock(block);
                    canTrigger = false;
                }
            });
        }
        else if (!canTrigger && elapsed < timer)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= timer)
            {
                canTrigger = true;
                elapsed = 0;
            }
        }
    }
}
