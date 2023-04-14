using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateCar : MonoBehaviour
{
    [SerializeField] BezierCurveRoad curveRoad;
    void Update()
    {
        curveRoad.t.Value = 0.1f * Time.time - Mathf.Floor(0.1f * Time.time);
    }
}
