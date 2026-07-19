using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 통합 적 AI. AISimple 패턴 기반, EnemyStats와 연동.
/// Enemy 오브젝트에 부착. NavMeshAgent 필요.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum State
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Stagger,
        Knockdown,
        Lift,
        Down,
        Dead
    }

    [Header("현재 상태")]
    public State currentState = State.Idle;

    [Header("감지")]
    public float detectRange = 10f;
    public float detectAngle = 120f;
    public float chaseStopRange = 15f;

    [Header("공격")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    [Header("경직")]
    public float staggerDuration = 1.25f;

    [Header("넉다운")]
    public float knockdownDuration = 2.0f;

    [Header("리프트 (공중 띄우기)")]
    public float liftHeight = 2.0f;
    public float liftGravity = 15f; // 중력 가속도 (높을수록 빠르게 올라갔다 떨어짐)

    [Header("다운 (에어본 착지 후 누운 상태)")]
    public float downDuration = 1.5f;

    [Header("순찰")]
    public List<Transform> patrolPoints = new List<Transform>();
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3.5f;

    [Header("애니메이션 속도 매칭")]
    public float walkAnimBaseSpeed = 1.5f;
    public float runAnimBaseSpeed = 3.5f;

    [Header("데미지 텍스트")]
    public GameObject damageTextPrefab;
    public Transform damageTextPos;

    private NavMeshAgent agent;
    private Animator anim;
    private AnimatorEventsEn animEv;
    private WeaponEnemy weaponEnemy;
    private SoundManager soundMan;
    private EnemyStats enemyStats;
    private Transform player;

    private float waitTimer;
    private float attackCooldownTimer; // 공격 쿨다운 (2회 공격 방지)
    private int currentPatrolIndex = -1;
    private Quaternion downOriginalRotation;
    private bool isInvincible = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        animEv = GetComponentInChildren<AnimatorEventsEn>();
        weaponEnemy = GetComponentInChildren<WeaponEnemy>();
        soundMan = GetComponent<SoundManager>();
        enemyStats = GetComponent<EnemyStats>();

        // NavMeshAgent 설정 (끊김 방지)
        agent.updateRotation = true;
        agent.angularSpeed = 360f;
        agent.acceleration = 20f;
        agent.stoppingDistance = 0.3f;
        agent.autoBraking = false; // 순찰 지점 근처 감속 방지

        // 루트 모션 비활성화 (NavMeshAgent와 충돌 → 걷기 끊김 해소)
        if (anim != null)
            anim.applyRootMotion = false;

        // AnimatorEventsEn에 weapon 참조 자동 연결
        if (animEv != null && animEv.weapon == null && weaponEnemy != null)
            animEv.weapon = weaponEnemy;

        // 태그 자동 설정
        if (!gameObject.CompareTag("Enemy"))
        {
            try { gameObject.tag = "Enemy"; }
            catch { Debug.LogWarning("[EnemyAI] 'Enemy' 태그가 프로젝트에 없습니다."); }
        }

        // AISimple 충돌 방지
        AISimple oldAI = GetComponent<AISimple>();
        if (oldAI != null)
            oldAI.enabled = false;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        if (enemyStats != null)
            enemyStats.OnEnemyDeath += OnDeath;

        // currentState가 Inspector에서 Patrol로 설정되어 있으면
        // ChangeState 내부의 "같은 상태 스킵" 때문에 SetTrigger가 실행되지 않음
        // → 강제로 Idle로 초기화한 뒤 호출해 Animator 동기화 보장
        currentState = State.Idle;
        if (patrolPoints.Count > 0)
            ChangeState(State.Patrol);
    }

    void Update()
    {
        if (currentState == State.Dead) return;
        if (enemyStats != null && enemyStats.isDead)
        {
            ChangeState(State.Dead);
            return;
        }

        // 공격 쿨다운 감소
        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        switch (currentState)
        {
            case State.Idle:
                if (CanSeePlayer())
                    ChangeState(State.Chase);
                else if (patrolPoints.Count > 0 && Random.Range(0, 100) < 10)
                    ChangeState(State.Patrol);
                break;

            case State.Patrol:
                if (!agent.pathPending && agent.remainingDistance < 1f)
                {
                    if (currentPatrolIndex >= patrolPoints.Count - 1)
                        currentPatrolIndex = 0;
                    else
                        currentPatrolIndex++;
                    if (patrolPoints[currentPatrolIndex] != null)
                        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                if (CanSeePlayer())
                    ChangeState(State.Chase);
                break;

            case State.Chase:
                if (player == null) return;
                agent.SetDestination(player.position);
                if (agent.hasPath)
                {
                    // 쿨다운이 끝났을 때만 공격 진입 (2회 공격 방지)
                    if (CanAttackPlayer() && attackCooldownTimer <= 0f)
                        ChangeState(State.Attack);
                    else if (CanStopChase())
                        ChangeState(patrolPoints.Count > 0 ? State.Patrol : State.Idle);
                }
                break;

            case State.Attack:
                LookPlayer(2.0f);
                waitTimer += Time.deltaTime;
                if (waitTimer >= attackCooldown)
                {
                    // 공격 후 쿨다운 설정 (즉시 재공격 방지)
                    attackCooldownTimer = 0.5f;
                    if (CanAttackPlayer())
                        ChangeState(State.Chase);
                    else
                        ChangeState(patrolPoints.Count > 0 ? State.Patrol : State.Idle);
                }
                break;

            case State.Stagger:
                waitTimer += Time.deltaTime;
                if (waitTimer < 0.5f)
                    LookPlayer(5.0f);
                else if (waitTimer >= 0.5f && isInvincible)
                    isInvincible = false;
                else if (waitTimer >= staggerDuration)
                {
                    if (CanSeePlayer())
                        ChangeState(State.Chase);
                    else
                        ChangeState(patrolPoints.Count > 0 ? State.Patrol : State.Idle);
                }
                break;

            case State.Knockdown:
                waitTimer += Time.deltaTime;
                if (waitTimer >= knockdownDuration)
                {
                    isInvincible = false;
                    if (CanSeePlayer())
                        ChangeState(State.Chase);
                    else
                        ChangeState(patrolPoints.Count > 0 ? State.Patrol : State.Idle);
                }
                break;

            case State.Lift:
                break; // LiftRoutine 코루틴에서 처리

            case State.Down:
                transform.rotation = Quaternion.Euler(-80f, transform.eulerAngles.y, 0f);
                waitTimer += Time.deltaTime;
                if (waitTimer >= downDuration)
                {
                    isInvincible = false;
                    if (CanSeePlayer())
                        ChangeState(State.Chase);
                    else
                        ChangeState(patrolPoints.Count > 0 ? State.Patrol : State.Idle);
                }
                break;
        }

        // 이동 중 애니메이션 속도를 실제 이동 속도에 동기화 (발 미끄러짐 방지)
        SyncAnimSpeed();
    }

    void SyncAnimSpeed()
    {
        if (anim == null) return;
        if (currentState == State.Patrol)
        {
            // AISimple과 동일하게 anim.speed를 건드리지 않음
            // velocity 기반 동기화를 하면 순찰 시작/도착 시 속도=0이라 애니메이션이 멈춰 보임
            anim.speed = 1f;
        }
        else if (currentState == State.Chase)
        {
            float baseSpeed = Mathf.Max(runAnimBaseSpeed, 0.01f);
            float speed = Mathf.Max(agent.velocity.magnitude, agent.desiredVelocity.magnitude);
            anim.speed = Mathf.Clamp(speed / baseSpeed, 0.5f, 2f);
        }
        else
        {
            anim.speed = 1f;
        }
    }

    // ===== 상태 전환 =====

    public void ChangeState(State newState)
    {
        // 같은 상태로 재진입 방지
        if (currentState == newState) return;

        // 이전 상태 트리거 리셋 + 무기 정리
        if (anim != null)
        {
            switch (currentState)
            {
                case State.Idle:
                    anim.ResetTrigger("isIdle");
                    break;
                case State.Patrol:
                    anim.ResetTrigger("isPatrolling");
                    break;
                case State.Chase:
                    anim.ResetTrigger("isChasing");
                    break;
                case State.Attack:
                    anim.ResetTrigger("isMeleeAttacking");
                    if (animEv != null)
                    {
                        animEv.isAttacking = false;
                        animEv.DisableWeaponColl();
                    }
                    else if (weaponEnemy != null)
                    {
                        weaponEnemy.DisableColliders();
                    }
                    break;
                case State.Stagger:
                    anim.ResetTrigger("isHited");
                    break;
                case State.Knockdown:
                    anim.ResetTrigger("isKnockedDown");
                    break;
                case State.Lift:
                    if (agent != null && !agent.enabled) agent.enabled = true;
                    isInvincible = false;
                    break;
                case State.Down:
                    agent.updateRotation = true;
                    isInvincible = false;
                    transform.rotation = downOriginalRotation;
                    break;
            }
        }

        currentState = newState;

        switch (newState)
        {
            case State.Idle:
                agent.isStopped = true;
                if (anim != null) anim.SetTrigger("isIdle");
                break;

            case State.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                FindNearestPatrolPoint();
                if (anim != null) anim.SetTrigger("isPatrolling");
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                if (anim != null) anim.SetTrigger("isChasing");
                break;

            case State.Attack:
                agent.isStopped = true;
                waitTimer = 0;
                if (animEv != null)
                    animEv.isAttacking = true;
                else if (weaponEnemy != null)
                    weaponEnemy.EnableColliders();
                if (anim != null) anim.SetTrigger("isMeleeAttacking");
                break;

            case State.Stagger:
                agent.isStopped = true;
                waitTimer = 0;
                if (soundMan != null) soundMan.PlaySound("Hit");
                if (anim != null) anim.SetTrigger("isHited");
                break;

            case State.Knockdown:
                agent.isStopped = true;
                waitTimer = 0;
                isInvincible = true; // 넉다운 중 추가 피격 방지
                if (soundMan != null) soundMan.PlaySound("Hit");
                if (anim != null) anim.SetTrigger("isKnockedDown");
                break;

            case State.Lift:
                agent.isStopped = true;
                agent.enabled = false;
                waitTimer = 0;
                isInvincible = true;
                if (anim != null) anim.SetTrigger("isHited");
                StartCoroutine(LiftRoutine());
                break;

            case State.Down:
                agent.isStopped = true;
                agent.updateRotation = false;
                waitTimer = 0;
                isInvincible = true;
                downOriginalRotation = transform.rotation;
                transform.rotation = Quaternion.Euler(-80f, transform.eulerAngles.y, 0f);
                Debug.Log("다운 상태");
                break;

            case State.Dead:
                agent.isStopped = true;
                agent.enabled = false;
                break;
        }
    }

    // ===== 피격 =====

    public void ApplyDmg(DmgInfo dmgInfo)
    {
        if (currentState == State.Dead) return;

        if (damageTextPrefab != null && damageTextPos != null)
        {
            GameObject dmgText = Instantiate(damageTextPrefab, damageTextPos.position, Quaternion.identity);
            DamagePopup popup = dmgText.GetComponent<DamagePopup>();
            if (popup != null)
                popup.SetUp(dmgInfo.dmgValue + Random.Range(-10, 10), dmgInfo.textColor);
        }

        if (!isInvincible)
        {
            isInvincible = true;
            ChangeState(State.Stagger);
        }
    }

    void OnDeath(EnemyStats stats)
    {
        ChangeState(State.Dead);
    }

    // 3타 콤보 피니셔에 의한 넉다운 (isInvincible 무시하고 강제 진입)
    public void ApplyKnockdown()
    {
        if (currentState == State.Dead) return;
        isInvincible = false;
        ChangeState(State.Knockdown);
    }

    // 공중 띄우기 (isInvincible 무시하고 강제 진입)
    public void ApplyLift()
    {
        if (currentState == State.Dead) return;
        isInvincible = false;
        ChangeState(State.Lift);
    }

    IEnumerator LiftRoutine()
    {
        Vector3 groundPos = transform.position;

        // v² = 2gh → 지정 높이에 딱 도달하는 초기 상승 속도
        float velocity = Mathf.Sqrt(2f * liftGravity * liftHeight);
        float currentHeight = 0f;

        while (true)
        {
            velocity -= liftGravity * Time.deltaTime;
            currentHeight += velocity * Time.deltaTime;

            // 지면에 착지 (하강 중 높이가 0 이하)
            if (currentHeight <= 0f && velocity < 0f)
            {
                currentHeight = 0f;
                break;
            }

            transform.position = groundPos + Vector3.up * Mathf.Max(0f, currentHeight);
            yield return null;
        }

        transform.position = groundPos;
        if (currentState != State.Dead)
        {
            agent.enabled = true;
            agent.Warp(groundPos);
            ChangeState(State.Down); // 착지 후 다운 상태
        }
    }

    // ===== 감지 =====

    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 direction = player.position - transform.position;
        float angle = Vector3.Angle(direction, transform.forward);
        if (direction.magnitude < detectRange * 0.5f) return true;
        return direction.magnitude < detectRange && angle < detectAngle * 0.5f;
    }

    bool CanAttackPlayer()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) < attackRange;
    }

    bool CanStopChase()
    {
        if (player == null) return true;
        return Vector3.Distance(transform.position, player.position) > chaseStopRange;
    }

    // ===== 회전 =====

    void LookPlayer(float speedRot)
    {
        if (player == null) return;
        Vector3 direction = player.position - transform.position;
        direction.y = 0;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * speedRot);
    }

    void FindNearestPatrolPoint()
    {
        if (patrolPoints.Count == 0) return;
        float lastDist = Mathf.Infinity;
        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (patrolPoints[i] == null) continue;
            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < lastDist)
            {
                currentPatrolIndex = i - 1;
                lastDist = distance;
            }
        }
    }

    void OnDestroy()
    {
        if (enemyStats != null)
            enemyStats.OnEnemyDeath -= OnDeath;
    }
}
