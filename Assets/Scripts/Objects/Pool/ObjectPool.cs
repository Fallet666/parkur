using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InPoolObject<T>
{
    public T poolObject;
    public int placeInQueue;

    public InPoolObject(T _obj, int _queuePlace)
    {
        poolObject = _obj;
        placeInQueue = _queuePlace;
    }
}


public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance;



    [Header("Bullet tracers")]
    [SerializeField] private BulletTracer bulletTracerPrefab;
    [SerializeField] private int bulletTracersCount = 10;
    private List<InPoolObject<BulletTracer>> bulletTracers = new List<InPoolObject<BulletTracer>>();

    private void Awake()
    {
        if (instance == null)
            instance = this;

        if(bulletTracerPrefab != null && bulletTracersCount > 0)
        {
            for (int i = 0; i < bulletTracersCount; i++)
            {
                BulletTracer _tracer = Instantiate(bulletTracerPrefab, transform);
                _tracer.gameObject.SetActive(false);

                InPoolObject<BulletTracer> _obj = new InPoolObject<BulletTracer>(_tracer, i);
                bulletTracers.Add(_obj);
            }
        }


        MoveList(bulletTracers);
    }
    private void MoveList<T>(List<InPoolObject<T>> _inPoolObjects)
    {
        for (int i = 0; i < _inPoolObjects.Count; i++)
        {
            if (_inPoolObjects[i].placeInQueue == 0)
                _inPoolObjects[i].placeInQueue = _inPoolObjects.Count - 1;
            else
                _inPoolObjects[i].placeInQueue -= 1;
        }
    }
    private T GetFirstInList<T>(List<InPoolObject<T>> _inPoolObjects)
    {
        foreach (var j in _inPoolObjects)
        {
            if (j.placeInQueue == 0)
                return j.poolObject;
        }
        return default(T);
    }

    #region Public functions
    public static BulletTracer GetBulletTracer()
    {
        if (instance == null)
            return null;

        if (instance.bulletTracers.Count > 0)
        {
            BulletTracer _out = instance.GetFirstInList(instance.bulletTracers);
            instance.MoveList(instance.bulletTracers);
            return _out;
        }
        else
            return null;
    }
    #region Decals
    public static void SpawnHitEffect(RaycastHit hitData)
    {
        if (instance == null)
            return;
        if(hitData.collider.TryGetComponent(out Surface surface))
        {
            instance.HitEffect(surface, hitData.point, hitData.normal);
        }
    }
    private void HitEffect(Surface surface, Vector3 point, Vector3 normal)
    {
        if (surface == null || surface.Resources.hitEffect == null)
            return;

        Instantiate(surface.Resources.hitEffect, point, Quaternion.LookRotation(normal));
    }
    #endregion
    #endregion
}
