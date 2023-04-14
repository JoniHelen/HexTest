using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct LerpLayer
{
    public List<Vector3> points;

    public LerpLayer(List<Vector3> _points)
    {
        points = _points;
    }

    public LerpLayer GetNext(float t)
    {
        if (points.Count < 2) return new(points);

        List<Vector3> list = new();
        
        for (int i = 0; i < points.Count - 1; i++)
            list.Add(Vector3.Lerp(points[i], points[i + 1], t));

        return new LerpLayer(list);
    }

    public Vector3 SampleAt(float t)
    {
        if (points.Count < 2) return points[0];

        LerpLayer layer = GetNext(t);

        while (layer.points.Count > 1)
            layer = layer.GetNext(t);

        return layer.points[0];
    }
}
