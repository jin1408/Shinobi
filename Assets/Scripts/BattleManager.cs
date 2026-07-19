using UnityEngine;
using System;

/// <summary>
/// 전투 흐름 총괄 매니저.
/// 씬에 빈 오브젝트로 배치한다.
/// DungeonManager와 PlayerStats의 이벤트를 구독하여
/// 전투 상태(준비/진행/승리/패배)를 관리한다.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public enum BattleState
    {
        Preparing,
        InBattle,
        Victory,
        Defeat
    }

    [Header("현재 상태")]
    public BattleState currentState = BattleState.Preparing;

    [Header("전투 시작 딜레이")]
    public float battleStartDelay = 1f;

    [Header("통계")]
    public int totalKills = 0;
    public float totalDamageDealt = 0f;
    public float battleTime = 0f;

    public event Action<BattleState> OnBattleStateChanged;
    public event Action<int> OnRoomChanged;

    private DungeonManager dungeonManager;
    private PlayerStats playerStats;

    void Start()
    {
        // 자동으로 매니저들을 찾아 연결
        dungeonManager = FindObjectOfType<DungeonManager>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerStats = playerObj.GetComponent<PlayerStats>();

        // 이벤트 구독
        if (dungeonManager != null)
        {
            dungeonManager.OnDungeonCleared += OnDungeonCleared;
            dungeonManager.OnRoomChanged += OnRoomChangedHandler;
        }

        if (playerStats != null)
        {
            playerStats.OnGameOver += OnPlayerGameOver;
            playerStats.OnPlayerDeath += OnPlayerDeath;
            playerStats.OnPlayerRevive += OnPlayerRevive;
        }

        // 전투 시작
        Invoke(nameof(StartBattle), battleStartDelay);
    }

    void Update()
    {
        if (currentState == BattleState.InBattle)
        {
            battleTime += Time.deltaTime;
        }
    }

    // ===== 전투 상태 전환 =====

    void StartBattle()
    {
        SetState(BattleState.InBattle);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[BattleManager] 전투 시작!");
    }

    void SetState(BattleState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        OnBattleStateChanged?.Invoke(newState);
        Debug.Log($"[BattleManager] 상태 변경: {newState}");
    }

    // ===== 이벤트 핸들러 =====

    void OnDungeonCleared()
    {
        SetState(BattleState.Victory);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log($"[BattleManager] 던전 클리어! 처치 수: {totalKills}, 전투 시간: {battleTime:F1}초");
    }

    void OnPlayerGameOver()
    {
        SetState(BattleState.Defeat);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[BattleManager] 게임 오버!");
    }

    void OnPlayerDeath()
    {
        Debug.Log($"[BattleManager] 플레이어 사망. 남은 부활: {playerStats.remainingRevives}");
    }

    void OnPlayerRevive()
    {
        Debug.Log("[BattleManager] 플레이어 부활!");
    }

    void OnRoomChangedHandler(int roomIndex)
    {
        OnRoomChanged?.Invoke(roomIndex);
        Debug.Log($"[BattleManager] 룸 {roomIndex}로 이동");
    }

    // ===== 외부 호출용 =====

    public void RegisterKill()
    {
        totalKills++;
    }

    public void RegisterDamage(float amount)
    {
        totalDamageDealt += amount;
    }

    public bool IsInBattle()
    {
        return currentState == BattleState.InBattle;
    }

    void OnDestroy()
    {
        if (dungeonManager != null)
        {
            dungeonManager.OnDungeonCleared -= OnDungeonCleared;
            dungeonManager.OnRoomChanged -= OnRoomChangedHandler;
        }

        if (playerStats != null)
        {
            playerStats.OnGameOver -= OnPlayerGameOver;
            playerStats.OnPlayerDeath -= OnPlayerDeath;
            playerStats.OnPlayerRevive -= OnPlayerRevive;
        }
    }
}
