using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 개별 룸 관리. 룸 GameObject의 루트에 부착.
/// 자식 오브젝트 중 EnemyStats를 가진 적들을 자동으로 추적한다.
/// </summary>
public class Room : MonoBehaviour
{
    [Header("포탈")]
    public Portal portal;

    [Header("플레이어 스폰 위치")]
    public Transform playerSpawnPoint;

    [HideInInspector] public bool isCleared = false;

    private List<EnemyStats> enemies = new List<EnemyStats>();

    public event Action<Room> OnRoomCleared;

    void Awake()
    {
        EnemyStats[] foundEnemies = GetComponentsInChildren<EnemyStats>(true);
        foreach (var enemy in foundEnemies)
        {
            enemies.Add(enemy);
            enemy.OnEnemyDeath += OnEnemyDeath;
        }

        if (portal != null)
            portal.gameObject.SetActive(false);
    }

    public void ActivateRoom()
    {
        gameObject.SetActive(true);
    }

    public void DeactivateRoom()
    {
        gameObject.SetActive(false);
    }

    void OnEnemyDeath(EnemyStats enemy)
    {
        enemies.Remove(enemy);

        if (enemies.Count <= 0 && !isCleared)
        {
            isCleared = true;

            if (portal != null)
                portal.gameObject.SetActive(true);

            OnRoomCleared?.Invoke(this);
        }
    }
}
