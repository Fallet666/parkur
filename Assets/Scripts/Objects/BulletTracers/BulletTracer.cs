using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class BulletTracer : MonoBehaviour
{
    [SerializeField] private float distanceToDestroy = 1f;
    private TrailRenderer trail;
    private Transform tr;
    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        tr = transform;
    }
    public void StartMoving(Vector3 startPosition, Vector3 endPosition, float speed)
    {
        trail.enabled = false;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(MovingEnumerator(startPosition, endPosition, speed));
    }
    private IEnumerator MovingEnumerator(Vector3 startPosition, Vector3 endPosition, float speed)
    {
        Vector3 _direction = endPosition - startPosition;
        _direction.Normalize();
        tr.position = startPosition;

        trail.enabled = true;
        trail.Clear();

        while (Vector3.Distance(endPosition, tr.position) > distanceToDestroy)
        {
            tr.Translate(_direction * speed * Time.deltaTime);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
