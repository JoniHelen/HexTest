using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CubicBezierNode
{
    [SerializeField, HideInInspector]
    private Vector3 _position;
    public Vector3 Position { get => _position; 
        set {
            _forwardHandlePosition += value - _position;
            _backwardHandlePosition += value - _position;
            _position = value;
        }
    }

    [SerializeField, HideInInspector]
    private Vector3 _forwardHandlePosition;
    public Vector3 ForwardHandlePosition { get => _forwardHandlePosition; 
        set {
            _backwardHandlePosition = Align(value, _backwardHandlePosition);
            _forwardHandlePosition = value;
        }
    }

    [SerializeField, HideInInspector]
    private Vector3 _backwardHandlePosition;
    public Vector3 BackwardHandlePosition { get => _backwardHandlePosition;
        set {
            _forwardHandlePosition = Align(value, _forwardHandlePosition);
            _backwardHandlePosition = value;
        }
    }

    private Vector3 Align(Vector3 lhs, Vector3 rhs) => _position + (_position - lhs).normalized * (_position - rhs).magnitude;

    public CubicBezierNode(Vector3 pos, Vector3 fwdHandle, Vector3 bwdHandle)
    {
        _position = pos;
        _forwardHandlePosition = fwdHandle;
        _backwardHandlePosition = bwdHandle;
    }

    public CubicBezierNode(Vector3 pos, Vector3 dir) 
    { 
        _position = pos;
        _forwardHandlePosition = pos + dir;
        _backwardHandlePosition = pos - dir;
    }
}
