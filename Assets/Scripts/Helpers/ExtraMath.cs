using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TimedFloat
{
    public float value;
    public float time;

    public TimedFloat(float _value, float _time)
    {
        value = _value;
        time = _time;
    }
}
public static class ExtraMath
{
    public static float LerpFloats(TimedFloat _fromFloat, TimedFloat _toFloat, bool isFutureTime = false)
    {
        float _base = _toFloat.time - _fromFloat.time;
        float _step = Time.time - _fromFloat.time;
        if (isFutureTime)
            _step = Time.time - _toFloat.time;

        if (_base == 0 || _step > _base)
            return _toFloat.value;

        return Mathf.Lerp(_fromFloat.value, _toFloat.value, _step / _base);
    }
}
