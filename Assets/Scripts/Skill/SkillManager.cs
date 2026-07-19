using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 스킬 슬롯 관리 및 입력 처리.
/// Player 오브젝트에 부착한다.
///
/// 입력 매핑:
/// - 마우스 좌클릭: 기본 공격 (Attack.cs에서 처리)
/// - 마우스 우클릭: 스킬 슬롯 1
/// - 마우스 양클릭: 스킬 슬롯 2
/// - E 키: 스킬 슬롯 3
/// - R 키: 스킬 슬롯 4
/// </summary>
public class SkillManager : MonoBehaviour
{
    [Header("스킬 슬롯")]
    public SkillData skill1_RightClick;
    public SkillData skill2_BothClick;
    public SkillData skill3_E;
    public SkillData skill4_R;

    [Header("스킬 이펙트 스폰")]
    public Transform skillSpawnPoint;

    [Header("양클릭 슬램 이동")]
    public float slamHeight = 3f;      // 최고 점프 높이 (m)
    public float slamGravity = 20f;    // 포물선 중력 (높을수록 빠름)
    public float slamMaxRange = 8f;    // 최대 도달 거리 (m)

    private float[] cooldownTimers = new float[4];
    private PlayerStats playerStats;
    private PlayerController playerController;
    private Animator anim;
    private Coroutine skillIdleRoutine;

    private bool leftHeld = false;
    private bool rightHeld = false;
    private bool bothClickTriggered = false; // 양클릭을 누르는 동안 한 번만 발동

    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        playerController = GetComponent<PlayerController>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (cooldownTimers[i] > 0f)
                cooldownTimers[i] -= Time.deltaTime;
        }

        if (!playerController.canMove) return;
        if (!playerController.IsGrounded()) return;
        if (playerController.IsDashing()) return;

        leftHeld = Input.GetMouseButton(0);
        rightHeld = Input.GetMouseButton(1);

        if (leftHeld && rightHeld)
        {
            if (!bothClickTriggered)
            {
                bothClickTriggered = true;
                TryUseSkill(skill2_BothClick, 1);
            }
            return;
        }
        bothClickTriggered = false; // 버튼 중 하나라도 떼면 리셋

        if (Input.GetMouseButtonDown(1))
        {
            TryUseSkill(skill1_RightClick, 0);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryUseSkill(skill3_E, 2);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryUseSkill(skill4_R, 3);
        }
    }

    void TryUseSkill(SkillData skill, int slotIndex)
    {
        if (skill == null) return;
        if (cooldownTimers[slotIndex] > 0f) return;
        if (playerStats != null && !playerStats.UseChakra(skill.chakraCost)) return;

        UseSkill(skill, slotIndex);
    }

    void UseSkill(SkillData skill, int slotIndex)
    {
        cooldownTimers[slotIndex] = skill.cooldown;

        // 슬롯별 애니메이션 오버라이드: 슬롯 0(우클릭)=hit02, 슬롯 1(양클릭)=hit03
        string[] slotAnims = { "hit02", "hit03", null, null };
        string animName = (slotIndex < slotAnims.Length && slotAnims[slotIndex] != null)
            ? slotAnims[slotIndex] : skill.animationTrigger;
        if (anim != null && !string.IsNullOrEmpty(animName))
        {
            anim.SetBool("param_idletohit04", false);
            anim.Play(animName, 0, 0f);
            // 슬롯 1(양클릭 슬램)은 JumpSlamRoutine에서 idle 복귀 처리
            if (slotIndex != 1)
            {
                if (skillIdleRoutine != null) StopCoroutine(skillIdleRoutine);
                skillIdleRoutine = StartCoroutine(ReturnToIdleAfterSkillAnim(animName));
            }
        }

        if (skill.effectPrefab != null && skillSpawnPoint != null)
        {
            GameObject effect = Instantiate(skill.effectPrefab, skillSpawnPoint.position, skillSpawnPoint.rotation);
            Destroy(effect, skill.effectDuration);
        }

        if (skill.skillType == SkillType.Melee)
        {
            if (slotIndex == 1)
                StartCoroutine(JumpSlamRoutine(skill)); // 착지 후 피격
            else
                MeleeAttack(skill, slotIndex);
        }
        else
        {
            RangedAttack(skill, slotIndex);
        }
    }

    void MeleeAttack(SkillData skill, int slotIndex)
    {
        // 슬롯 1(착지 슬램): 착지점 중심 / 슬롯 0(우클릭): 플레이어 전방 중심
        Vector3 center = slotIndex == 1
            ? transform.position + Vector3.up * 0.5f
            : transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
        Collider[] hits = Physics.OverlapSphere(center, skill.attackRadius);
        HashSet<GameObject> damaged = new HashSet<GameObject>();

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            EnemyStats es = hit.GetComponentInParent<EnemyStats>();
            if (es == null || !damaged.Add(es.gameObject)) continue;

            es.ApplyDmg(new DmgInfo((int)skill.damage, Color.yellow, transform.position));

            // 슬롯 0(우클릭), 1(양클릭) → 공중 띄우기 적용
            if (slotIndex == 0 || slotIndex == 1)
            {
                EnemyAI ai = es.GetComponent<EnemyAI>();
                if (ai != null) ai.ApplyLift();
            }
        }
    }

    void RangedAttack(SkillData skill, int slotIndex)
    {
        Vector3 spawnPos = skillSpawnPoint != null ? skillSpawnPoint.position : transform.position + transform.forward * 1.5f;
        Collider[] hits = Physics.OverlapSphere(spawnPos + transform.forward * skill.range, skill.attackRadius);
        HashSet<GameObject> damaged = new HashSet<GameObject>();

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            EnemyStats es = hit.GetComponentInParent<EnemyStats>();
            if (es == null || !damaged.Add(es.gameObject)) continue;

            es.ApplyDmg(new DmgInfo((int)skill.damage, Color.cyan, transform.position));
        }
    }

    IEnumerator JumpSlamRoutine(SkillData skill)
    {
        // 마우스 커서가 가리키는 지면 위치를 착지 목표로 결정
        Vector3 startPos = transform.position;
        Vector3 landingPos = GetGroundTarget();

        // 착지 방향으로 플레이어 회전
        Vector3 dir = landingPos - startPos;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);

        // 플레이어 이동 제어 및 CC 비활성화
        playerController.canMove = false;
        playerController.isMoving = false;
        playerController.charCont.enabled = false;

        // 포물선 이동: v²=2gh → 정확히 slamHeight에 도달하는 초기 수직 속도
        float vertVel = Mathf.Sqrt(2f * slamGravity * slamHeight);
        float totalAirTime = 2f * vertVel / slamGravity;
        Vector3 horizVelPerSec = new Vector3(dir.x, 0f, dir.z) / totalAirTime;

        float elapsed = 0f;
        float curHeight = 0f;

        while (true)
        {
            float dt = Time.deltaTime;
            vertVel -= slamGravity * dt;
            curHeight += vertVel * dt;
            elapsed += dt;

            if (curHeight <= 0f && vertVel < 0f)
            {
                curHeight = 0f;
                break;
            }

            transform.position = startPos
                + horizVelPerSec * elapsed
                + Vector3.up * Mathf.Max(0f, curHeight);
            yield return null;
        }

        // 착지 위치로 스냅
        transform.position = new Vector3(landingPos.x, startPos.y, landingPos.z);

        // 착지 위치 기준 피격 판정 (slotIndex=1 고정)
        MeleeAttack(skill, 1);

        // 복구
        playerController.charCont.enabled = true;
        playerController.canMove = true;
        playerController.isMoving = false;
        if (anim != null) anim.Play("idle", 0, 0f);
    }

    // 카메라→마우스 커서 레이캐스트로 착지 목표 지면 위치 반환
    Vector3 GetGroundTarget()
    {
        Camera cam = playerController.cam;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 toTarget = hit.point - transform.position;
                toTarget.y = 0;
                // 최대 도달 거리 클램프
                if (toTarget.magnitude > slamMaxRange)
                    return transform.position + toTarget.normalized * slamMaxRange;
                return new Vector3(hit.point.x, transform.position.y, hit.point.z);
            }
        }
        // 레이 실패 시 플레이어 전방 2m
        return transform.position + transform.forward * 2f;
    }

    IEnumerator ReturnToIdleAfterSkillAnim(string stateName)
    {
        yield return null;
        yield return null;

        float startTime = Time.time;
        float prevNorm = 0f;

        // 최대 1.5초, AnyState 재시작·완료·전환 중 하나라도 감지하면 즉시 break
        while (Time.time - startTime < 1.5f)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            float curNorm = info.normalizedTime % 1f;

            // AnyState가 애니메이션을 재시작한 경우 (normalizedTime이 역방향으로 점프)
            if (prevNorm > 0.6f && curNorm < 0.1f) break;
            // 다른 state로 전환됨
            if (!info.IsName(stateName)) break;
            // 애니메이션 거의 완료
            if (info.normalizedTime >= 0.9f) break;

            prevNorm = curNorm;
            yield return null;
        }

        anim.SetBool("param_idletohit04", false);
        anim.Play("idle", 0, 0f);
        skillIdleRoutine = null;
    }

    public float GetCooldownRemaining(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= cooldownTimers.Length) return 0f;
        return cooldownTimers[slotIndex];
    }

    public float GetCooldownPercent(int slotIndex)
    {
        SkillData skill = GetSkillBySlot(slotIndex);
        if (skill == null || skill.cooldown <= 0f) return 0f;
        return Mathf.Clamp01(cooldownTimers[slotIndex] / skill.cooldown);
    }

    public SkillData GetSkillBySlot(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return skill1_RightClick;
            case 1: return skill2_BothClick;
            case 2: return skill3_E;
            case 3: return skill4_R;
            default: return null;
        }
    }
}
