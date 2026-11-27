using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health Bar")]
    public float max = 100f;

    public float Current { get; private set; }
    public bool IsAlive => Current > 0f;

    // Vrai pendant une invulnérabilité (joueur juste touché…) : TakeDamage n'a aucun effet
    public bool Invulnerable { get; set; }

    public event Action<DamageInfo> Damaged;
    public event Action Died;

    void Awake()
    {
        Current = max;
    }

    public void TakeDamage(DamageInfo info)
    {
        if (!IsAlive || Invulnerable) return;
        Current = Mathf.Max(0f, Current - info.amount);
        Damaged?.Invoke(info);
        if (Current <= 0f) Died?.Invoke();
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        Current = Mathf.Min(max, Current + amount);
    }
}