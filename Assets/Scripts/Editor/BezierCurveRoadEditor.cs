using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.Mathematics;

#if UNITY_EDITOR
[CustomEditor(typeof(BezierCurveRoad))]
public class BezierCurveRoadEditor : Editor
{
    [SerializeField] private VisualTreeAsset m_InspectorXML;

    SerializedProperty knotColor;
    SerializedProperty controlColor;
    SerializedProperty controlLineColor;
    SerializedProperty curveColor;

    public override VisualElement CreateInspectorGUI()
    {
        BezierCurveRoad bezierRoad = (BezierCurveRoad)target;

        VisualElement myInspector = new VisualElement();
        m_InspectorXML.CloneTree(myInspector);

        (myInspector.Q("addButton") as Button).clicked += bezierRoad.AddPoint;
        (myInspector.Q("removeButton") as Button).clicked += bezierRoad.RemovePoint;

        return myInspector;
    }

    public void OnSceneGUI()
    {
        knotColor = serializedObject.FindProperty("knotColor");
        controlColor = serializedObject.FindProperty("controlColor");
        controlLineColor = serializedObject.FindProperty("controlLineColor");
        curveColor = serializedObject.FindProperty("curveColor");

        BezierCurveRoad bezierRoad = (BezierCurveRoad)target;

        float3x3[] posChanges = new float3x3[bezierRoad.BezierSpline.Nodes.Count];

        for (int i = 0; i < bezierRoad.BezierSpline.NumSegments - (bezierRoad.BezierSpline.IsClosed ? 1 : 0); i++)
        {
            Handles.DrawBezier(bezierRoad.BezierSpline.Nodes[i].Position, bezierRoad.BezierSpline.Nodes[i + 1].Position,
                bezierRoad.BezierSpline.Nodes[i].ForwardHandlePosition, bezierRoad.BezierSpline.Nodes[i + 1].BackwardHandlePosition, curveColor.colorValue, Texture2D.whiteTexture, 1);
        }

        if (bezierRoad.BezierSpline.IsClosed)
        {
            Handles.DrawBezier(bezierRoad.BezierSpline.Nodes[^1].Position, bezierRoad.BezierSpline.Nodes[0].Position,
                bezierRoad.BezierSpline.Nodes[^1].ForwardHandlePosition, bezierRoad.BezierSpline.Nodes[0].BackwardHandlePosition, curveColor.colorValue, Texture2D.whiteTexture, 1);
        }

        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < bezierRoad.BezierSpline.Nodes.Count; i++)
        {
            Handles.color = controlLineColor.colorValue;

            Handles.DrawLine(bezierRoad.BezierSpline.Nodes[i].Position, bezierRoad.BezierSpline.Nodes[i].ForwardHandlePosition);
            Handles.DrawLine(bezierRoad.BezierSpline.Nodes[i].Position, bezierRoad.BezierSpline.Nodes[i].BackwardHandlePosition);

            Handles.color = knotColor.colorValue;

            posChanges[i].c0 = Handles.FreeMoveHandle(
                bezierRoad.BezierSpline.Nodes[i].Position, Quaternion.identity,
                HandleUtility.GetHandleSize(bezierRoad.BezierSpline.Nodes[i].Position) * 0.25f,
                Vector3.zero, Handles.SphereHandleCap
                );

            Handles.color = controlColor.colorValue;

            posChanges[i].c1 = Handles.FreeMoveHandle(
                bezierRoad.BezierSpline.Nodes[i].ForwardHandlePosition, Quaternion.identity,
                HandleUtility.GetHandleSize(bezierRoad.BezierSpline.Nodes[i].ForwardHandlePosition) * 0.15f,
                Vector3.zero, Handles.SphereHandleCap
                );

            posChanges[i].c2 = Handles.FreeMoveHandle(
                bezierRoad.BezierSpline.Nodes[i].BackwardHandlePosition, Quaternion.identity,
                HandleUtility.GetHandleSize(bezierRoad.BezierSpline.Nodes[i].BackwardHandlePosition) * 0.15f,
                Vector3.zero, Handles.SphereHandleCap
                );
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Move point");
            bezierRoad.BezierSpline.UpdateNodeData(posChanges);
            bezierRoad.UpdateMesh();
        }
    }
}
#endif
