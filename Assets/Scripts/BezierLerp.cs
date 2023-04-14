using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BezierLerp : MonoBehaviour
{
    [SerializeField] [Range(0f, 1f)] float t;
    [SerializeField] [Range(10, 100)] int lineResolution = 10;
    [SerializeField] List<Transform> transforms = new();

    private void OnDrawGizmos()
    {
        if (transforms.Count < 2) return;

        LerpLayer layer = new(transforms.Select(l => l.position).ToList());

        Gizmos.color = Color.magenta;
        for (int i = 0; i < lineResolution; i++)
            Gizmos.DrawLine(layer.SampleAt(i / (float)lineResolution), layer.SampleAt((i + 1) / (float)lineResolution));

        do
        {
            DrawLayer(layer);
            layer = layer.GetNext(t);
        } while (layer.points.Count > 1);
    }

    private void DrawLayer(LerpLayer layer)
    {
        Gizmos.color = Color.green;
        for (int i = 0; i < layer.points.Count - 1; i++)
            Gizmos.DrawLine(layer.points[i], layer.points[i + 1]);

        Gizmos.color = layer.points.Count > 2 ? Color.red : Color.yellow;
        layer.GetNext(t).points.ForEach(p => Gizmos.DrawSphere(p, layer.points.Count > 2 ? 0.05f : 0.075f));
    }
}
