using UnityEngine;
using System;

/// <summary>
/// 적 HP 관리 및 사망 처리.
/// 적 오브젝트에 부착한다. EnemyAI와 함께 사용.
/// </summary>
public class EnemyStats : MonoBehaviour
{
    [Header("체력")]
    public float maxHP = 500f;
    public float currentHP;

    [Header("사망")]
    public float deathDelay = 0f; // 0이면 즉시 사라짐

    [HideInInspector] public bool isDead = false;

    public event Action<EnemyStats> OnEnemyDeath;
    public event Action<float, float> OnHPChanged;

    void Awake()
    {
        currentHP = maxHP;
    }

    public void ApplyDmg(DmgInfo dmgInfo)
    {
        if (isDead) return;

        currentHP = Mathf.Max(currentHP - dmgInfo.dmgValue, 0f);
        OnHPChanged?.Invoke(currentHP, maxHP);

        // 애니메이션은 EnemyAI.ApplyDmg의 Stagger 상태가 처리함
        // EnemyStats는 HP 수치만 관리

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        OnEnemyDeath?.Invoke(this);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        Destroy(gameObject, deathDelay);
    }
}
