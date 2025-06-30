using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCameraRotationDelay : MonoBehaviour
{
    [SerializeField] private float returningSpeed = 10f;
    [SerializeField] private float rotationSensivity = 0.1f;
    [Range(0f, 90f)][SerializeField] private float maxVerticalAngle = 30f;
    [Range(0f, 90f)][SerializeField] private float minVerticalAngle = 30f;
    [Range(0f, 90f)][SerializeField] private float rightMaxHorizontalAngle = 30f;
    [Range(0f, 90f)][SerializeField] private float leftMaxHoriontalAngle = 30f;
   
    private Vector3 rotation = Vector2.zero;
    private Transform tr;

    private void OnEnable()
    {

        tr = transform;
    }
    private void Update()
    {
        if (CameraLooking.instance == null)
            enabled = false;

        Vector2 mouseDelta = CameraLooking.instance.MouseDelta;

        rotation += new Vector3(-mouseDelta.y, mouseDelta.x, 0f) * rotationSensivity;

        float _distance = Vector3.Distance(rotation, Vector3.zero);
        if(_distance > 0.1f)
        {
            rotation = Vector3.MoveTowards(rotation, Vector3.zero, returningSpeed * Time.deltaTime * _distance);
        }
        rotation = ClampEulers(rotation);

        tr.localEulerAngles = rotation;
    }
    private Vector3 ClampEulers(Vector3 eulers)
    {
        return new Vector3
        {
            x = Mathf.Clamp(eulers.x, -maxVerticalAngle, minVerticalAngle),
            y = Mathf.Clamp(eulers.y, -leftMaxHoriontalAngle, rightMaxHorizontalAngle),
            z = eulers.z
        };
    }
}
