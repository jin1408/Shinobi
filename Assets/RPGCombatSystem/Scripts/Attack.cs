using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    private Animator anim;
    private PlayerController playerCont;
    private Coroutine attackRoutine;

    [Header("공격 범위")]
    public float attackRadius = 1.5f;
    public float attackForwardOffset = 1.0f;

    [Header("1타 (ATK_k_1 → hit01)")]
    public float hitDelay1 = 0.15f;
    public float attackDuration1 = 0.45f;
    public int attackDamage1 = 100;

    [Header("2타 (hit04)")]
    public float hitDelay2 = 0.15f;
    public float attackDuration2 = 0.45f;
    public int attackDamage2 = 120;

    [Header("3타 (atk_k_3 → hit03) - 넉다운")]
    public float hitDelay3 = 0.20f;
    public float attackDuration3 = 0.55f;
    public int attackDamage3 = 180;

    [Header("콤보 설정")]
    public Color dmgColor = Color.white;

    [Header("사운드")]
    public AudioClip attackSound;
    [Range(0f, 1f)]
    public float attackSoundVolume = 0.5f;

    private bool comboInputBuffered = false;

    void Awake()
    {
        playerCont = GetComponent<PlayerController>();
        anim = GetComponentInChildren<Animator>();

        if (attackSound == null)
            attackSound = Resources.Load<AudioClip>("Woman_Attack");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!playerCont.IsGrounded() || playerCont.IsDashing()) return;
            if (Input.GetMouseButton(1)) return; // 우클릭 홀드 중 = 스킬 입력, 콤보 차단

            if (attackRoutine == null)
            {
                // 새 콤보 시작
                comboInputBuffered = false;
                attackRoutine = StartCoroutine(ComboSequence());
            }
            else
            {
                // 진행 중인 공격에 다음 타 입력 버퍼
                comboInputBuffered = true;
            }
        }
    }

    IEnumerator ComboSequence()
    {
        // 1타
        yield return StartCoroutine(SingleAttack(1));
        if (!comboInputBuffered) { EndCombo(); yield break; }

        // 2타
        comboInputBuffered = false;
        yield return StartCoroutine(SingleAttack(2));
        if (!comboInputBuffered) { EndCombo(); yield break; }

        // 3타 (마무리 - 넉다운)
        comboInputBuffered = false;
        yield return StartCoroutine(SingleAttack(3));

        EndCombo();
    }

    IEnumerator SingleAttack(int step)
    {
        float hitDelay  = step == 1 ? hitDelay1  : (step == 2 ? hitDelay2  : hitDelay3);
        float duration  = step == 1 ? attackDuration1 : (step == 2 ? attackDuration2 : attackDuration3);
        int   damage    = step == 1 ? attackDamage1   : (step == 2 ? attackDamage2   : attackDamage3);
        bool  isFinisher = (step == 3);

        playerCont.canMove = false;
        playerCont.isMoving = false;

        // 스텝→state/param 매핑 (1-indexed; [0] 미사용)
        // 콤보 순서: 1타=hit01, 2타=hit04, 3타=hit01
        string[] stateNames = { null, "hit01", "hit04", "hit01" };
        string[] boolParams = { null, "param_idletohit01", "param_idletohit04", "param_idletohit01" };

        // 이전 타 파라미터 모두 리셋
        anim.SetBool("param_idletohit01", false);
        anim.SetBool("param_idletohit02", false);
        anim.SetBool("param_idletohit03", false);
        anim.SetBool("param_idletohit04", false);

        anim.SetBool(boolParams[step], true);
        anim.Play(stateNames[step], 0, 0f);

        PlayAttackSound();

        // 타격 판정 타이밍 대기
        yield return new WaitForSeconds(hitDelay);
        MeleeHitCheck(step, damage, isFinisher);

        // 애니메이션 전체 재생 대기
        yield return new WaitForSeconds(duration - hitDelay);

        anim.SetBool(boolParams[step], false);
    }

    void EndCombo()
    {
        anim.SetBool("param_idletohit01", false);
        anim.SetBool("param_idletohit02", false);
        anim.SetBool("param_idletohit03", false);
        anim.SetBool("param_idletohit04", false);
        // hit04 state에 exit transition이 없으면 idle로 강제 복귀
        anim.Play("idle", 0, 0f);
        playerCont.canMove = true;
        playerCont.isMoving = false;
        comboInputBuffered = false;
        attackRoutine = null;
    }

    void PlayAttackSound()
    {
        if (attackSound != null)
        {
            AudioSource.PlayClipAtPoint(attackSound, transform.position, attackSoundVolume);
            return;
        }
        if (playerCont.soundMan != null)
            playerCont.soundMan.PlaySound("Attack");
    }

    void MeleeHitCheck(int step, int damage, bool isFinisher)
    {
        Transform model = playerCont.childPlayer != null ? playerCont.childPlayer.transform : transform;
        Vector3 attackCenter = model.position + model.forward * attackForwardOffset + Vector3.up * 0.5f;

        Collider[] hits = Physics.OverlapSphere(attackCenter, attackRadius);
        HashSet<GameObject> damaged = new HashSet<GameObject>();

        Debug.Log($"[Attack] {step}타 MeleeHitCheck: center={attackCenter}, radius={attackRadius}, hits={hits.Length}");

        foreach (Collider hit in hits)
        {
            if (hit.transform.root.gameObject == transform.root.gameObject) continue;

            EnemyStats enemyStats = hit.GetComponentInParent<EnemyStats>();
            if (enemyStats == null) continue;

            GameObject enemyObj = enemyStats.gameObject;
            if (damaged.Contains(enemyObj)) continue;
            damaged.Add(enemyObj);

            DmgInfo dmgInfo = new DmgInfo(damage, dmgColor, transform.position);

            // HP 데미지
            enemyStats.ApplyDmg(dmgInfo);

            // AI 상태 전환
            EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                if (isFinisher)
                    enemyAI.ApplyKnockdown(); // 3타: 넉다운
                else
                    enemyAI.ApplyDmg(dmgInfo); // 1·2타: 경직
            }

            Debug.Log($"[Attack] {enemyObj.name}에게 {damage} 데미지! ({step}타{(isFinisher ? " [넉다운]" : "")})");
        }
    }
}
