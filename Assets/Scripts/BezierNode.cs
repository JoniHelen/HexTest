using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class  BezierNode : MonoBehaviour
{
    [HideInInspector] public float ScaleNext = 1f;
    [HideInInspector] public float ScalePrevious = 1f;
    public Vector3 HandleNext { get => transform.position + transform.forward * ScaleNext; }
    public Vector3 HandlePrevious { get => transform.position - transform.forward * ScalePrevious; }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        float size = HandleUtility.GetHandleSize(transform.position);

        Gizmos.DrawLine(transform.position, HandleNext);
        Gizmos.DrawLine(transform.position, HandlePrevious);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(HandleNext, size * 0.1f);
        Gizmos.DrawSphere(HandlePrevious, size * 0.1f);
    }
}


[CustomEditor(typeof(BezierNode))]
public class BezierNodeEditor : Editor
{
    private void OnSceneGUI()
    {
        var node = (BezierNode)target;
        float size = HandleUtility.GetHandleSize(node.transform.position);

        EditorGUI.BeginChangeCheck();
        float newScaleNext = Handles.ScaleSlider(node.ScaleNext, node.transform.position, node.transform.forward * 1.5f, Quaternion.identity, size, 0.00001f);
        float newScalePrevious = Handles.ScaleSlider(node.ScalePrevious, node.transform.position, -node.transform.forward * 1.5f, Quaternion.identity, size, 0.00001f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(node, "Move Control Point");
            node.ScaleNext = newScaleNext;
            node.ScalePrevious = newScalePrevious;
        }
    }
}
