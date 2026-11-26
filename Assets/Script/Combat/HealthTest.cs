using UnityEngine;

public class HealthTest : MonoBehaviour
{
    Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        health.Damaged += info => Debug.Log("Hit : -" + info.amount + ", remain " + health.Current);
        health.Died += () => Debug.Log("Died Trigered");
    }

    [ContextMenu("Deal 40 damages")]
    void Hit()
    {
        health.TakeDamage(new DamageInfo
        {
            amount = 40f,
            point = transform.position,
            direction = Vector3.forward,
            source = gameObject
        });
    }
}