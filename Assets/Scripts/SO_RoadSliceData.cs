using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Road Slice", fileName = "New Road Slice")]
public class SO_RoadSliceData : ScriptableObject
{
    public List<Vector3> points = new();
}
