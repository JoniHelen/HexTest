using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[System.Serializable]
public class CubicBezierSpline
{
    [SerializeField, HideInInspector]
    private List<CubicBezierNode> _nodes;
    public List<CubicBezierNode> Nodes => _nodes;

    [HideInInspector]
    public bool IsClosed = false;

    [SerializeField, HideInInspector]
    private Vector3 _position;
    public Vector3 Position { get => _position; 
        set { 
            _nodes.ForEach(x => x.Position += value -  _position);
            _position = value;
        }
    }

    [HideInInspector]
    public int uniformResolution;

    [SerializeField, HideInInspector]
    float curveLength;

    [SerializeField, HideInInspector]
    float[] lengths;

    public int NumAnchors => Nodes.Count;
    public int NumSegments => Nodes.Count - (IsClosed ? 0 : 1);

    public CubicBezierSpline()
    {
        _nodes = new List<CubicBezierNode>() {
               new CubicBezierNode( -Vector3.forward, Vector3.forward * 0.3f),
               new CubicBezierNode(Vector3.forward, Vector3.forward * 0.3f)
        };
    }

    public void AddNode() {
        if (IsClosed)
        {
            Vector3 pos = EvaluateSegment(0.5f, _nodes[^1], _nodes[0]);
            Vector3 dir = (pos - EvaluateSegment(0.49999f, _nodes[^1], _nodes[0])).normalized;
            _nodes.Add(new CubicBezierNode(pos, dir * 0.3f));
        }
        else
        {
            Vector3 dir = (Evaluate(1) - Evaluate(0.999f)).normalized;
            _nodes.Add(new CubicBezierNode(Evaluate(1) + dir, dir * 0.3f));
        }
    }
    public void RemoveNode() => _nodes.RemoveAt(_nodes.Count - 1);

    public void UpdateNodeData(float3x3[] positionData)
    {
        for (int i = 0; i < positionData.Length; i++)
        {
            if (_nodes[i].BackwardHandlePosition != (Vector3)positionData[i].c2)
                _nodes[i].BackwardHandlePosition = positionData[i].c2;
            else
                _nodes[i].ForwardHandlePosition = positionData[i].c1;

            _nodes[i].Position = positionData[i].c0;
        }
    }

    private Vector3 EvaluateSegment(float t, CubicBezierNode node1, CubicBezierNode node2)
        => EvaluateBezier(t, node1.Position, node1.ForwardHandlePosition, node2.BackwardHandlePosition, node2.Position);
    private Vector3 EvaluateBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t1 = 1 - t;
        return t1 * t1 * t1 * p0 + 3 * t1 * t1 * t * p1 + 3 * t1 * t * t * p2 + t * t * t * p3;
    }

    public Vector3 Evaluate(float t) => EvaluateSegment(
        Remap(t, GetNodeIndex(t) / (float)NumSegments, Mathf.CeilToInt(Mathf.Max(t, 0.0001f) * NumSegments) / (float)NumSegments, 0, 1),
        GetNode(GetNodeIndex(t)), GetNode(GetNodeIndex(t) + 1)
    );

    public Vector3 EvaluateUniform(float t) => Evaluate(UniformTtoT(t));

    private CubicBezierNode GetNode(int i)
    {
        if (i == Nodes.Count) return Nodes[0];
        return Nodes[i];
    }

    public void UpdateLUT()
    {
        float tlength = 0;
        lengths = new float[uniformResolution + 1];
        lengths[0] = 0;
        Vector3 prevPoint = Evaluate(0);
        for (int i = 1; i <= uniformResolution; i++)
        {
            Vector3 point = Evaluate(i * (1f / uniformResolution));
            tlength += Vector3.Distance(prevPoint, point);
            lengths[i] = tlength;
            prevPoint = point;
        }

        curveLength = tlength;
    }

    private float Remap(float value, float low1, float high1, float low2, float high2)
        => low2 + (value - low1) * (high2 - low2) / (high1 - low1);
    private int GetNodeIndex(float value) => Mathf.Max(Mathf.CeilToInt(value * NumSegments) - 1, 0);
    private float UniformTtoT(float uniformT) => DistanceToT(uniformT * curveLength);
    private float DistanceToT(float distance)
    {
        int low = 0, high = uniformResolution, index = 0;
        while (low < high)
        {
            index = low + (high - low) / 2;

            if (lengths[index] < distance) low = index + 1;
            else high = index;
        }

        if (lengths[index] > distance) index--;

        if (lengths[index] == distance) return index / (float)uniformResolution;
        else return (index + (distance - lengths[index]) / (lengths[index + 1] - lengths[index])) / uniformResolution;
    }
}
