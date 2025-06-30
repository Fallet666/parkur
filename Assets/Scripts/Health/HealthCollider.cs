using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthCollider : MonoBehaviour
{
    [SerializeField] private Health health;
    public Health MainHealth { get => health; set { health = value; } }

    [SerializeField] private float damageMultiplier = 1f;


    public virtual void Damage(float _damage)
    {
        if (health == null)
            return;

        health.DamageServerRPC(_damage * damageMultiplier);
    }
}
