using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StringHelpers
{
    public static string SerializeStringArray(string[] _array)
    {
        string _out = "";
        foreach(string _str in _array)
        {
            _out += _str + ";";
        }
        return _out;
    }
    public static string[] DeserializeStringArray(string _serializedArray)
    {
        List<string> _out = new List<string>();
        foreach (string _str in _serializedArray.Split(";"))
        {
            if(_str != "")
            {
                _out.Add(_str);
            }
        }
        return _out.ToArray();
    }
    public static List<string> DeserializeStringList(string _serializedArray)
    {
        List<string> _out = new List<string>();
        foreach (string _str in _serializedArray.Split(";"))
        {
            if (_str != "")
            {
                _out.Add(_str);
            }
        }
        return _out;
    }
}
