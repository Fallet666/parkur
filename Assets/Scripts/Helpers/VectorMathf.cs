using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TimedVector
{
	public float time;
	public Vector3 vector;

	public TimedVector(Vector3 _vector, float _time)
    {
		time = _time;
		vector = _vector;
    }
}
public static class VectorMathf
{
	public static Vector3 RemoveDotVector(Vector3 _vector, Vector3 _direction)
	{
		if (_direction.sqrMagnitude != 1)
			_direction.Normalize();

		float _amount = Vector3.Dot(_vector, _direction);

		_vector -= _direction * _amount;

		return _vector;
	}
	public static Vector3 ExtractDotVector(Vector3 _vector, Vector3 _direction)
	{
		if (_direction.sqrMagnitude != 1)
			_direction.Normalize();

		float _amount = Vector3.Dot(_vector, _direction);

		return _direction * _amount;
	}

    public static Vector3 LerpVectors(TimedVector _fromVector, TimedVector _toVector, bool isFutureTime = false)
    {
        float _base = _toVector.time - _fromVector.time;
		float _step = Time.time - _fromVector.time;
		if(isFutureTime)
			_step = Time.time - _toVector.time;

        if (_base == 0 || _step > _base)
            return _toVector.vector;

        return Vector3.Lerp(_fromVector.vector, _toVector.vector, _step / _base);
    }
}
