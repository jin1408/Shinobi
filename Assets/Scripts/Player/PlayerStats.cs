using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 플레이어 HP/차크라 관리 및 사망/부활 처리.
/// Player 오브젝트에 부착한다.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("체력")]
    public float maxHP = 1000f;
    public float currentHP;

    [Header("차크라")]
    public float maxChakra = 500f;
    public float currentChakra;
    public float chakraRegenRate = 10f;

    [Header("부활")]
    public int maxRevives = 2;
    public int remainingRevives;
    public float reviveDelay = 3f;

    [HideInInspector] public bool isDead = false;

    public event Action<float, float> OnHPChanged;
    public event Action<float, float> OnChakraChanged;
    public event Action OnPlayerDeath;
    public event Action OnPlayerRevive;
    public event Action OnGameOver;

    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        currentHP = maxHP;
        currentChakra = maxChakra;
        remainingRevives = maxRevives;
    }

    void Update()
    {
        if (isDead) return;

        if (currentChakra < maxChakra)
        {
            currentChakra = Mathf.Min(currentChakra + chakraRegenRate * Time.deltaTime, maxChakra);
            OnChakraChanged?.Invoke(currentChakra, maxChakra);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHP = Mathf.Max(currentHP - amount, 0f);
        OnHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    public bool UseChakra(float amount)
    {
        if (currentChakra < amount) return false;

        currentChakra -= amount;
        OnChakraChanged?.Invoke(currentChakra, maxChakra);
        return true;
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHP = Mathf.Min(currentHP + amount, maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void RecoverChakra(float amount)
    {
        currentChakra = Mathf.Min(currentChakra + amount, maxChakra);
        OnChakraChanged?.Invoke(currentChakra, maxChakra);
    }

    void Die()
    {
        isDead = true;
        if (playerController != null)
            playerController.canMove = false;

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
            anim.Play("KO_big");

        OnPlayerDeath?.Invoke();

        if (remainingRevives > 0)
        {
            StartCoroutine(ReviveAfterDelay());
        }
        else
        {
            OnGameOver?.Invoke();
        }
    }

    IEnumerator ReviveAfterDelay()
    {
        yield return new WaitForSeconds(reviveDelay);
        Revive();
    }

    void Revive()
    {
        remainingRevives--;
        currentHP = maxHP * 0.5f;
        currentChakra = maxChakra * 0.5f;
        isDead = false;

        if (playerController != null)
            playerController.canMove = true;

        OnHPChanged?.Invoke(currentHP, maxHP);
        OnChakraChanged?.Invoke(currentChakra, maxChakra);
        OnPlayerRevive?.Invoke();
    }
}
