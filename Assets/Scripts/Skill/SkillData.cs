using UnityEngine;

/// <summary>
/// 스킬 데이터 정의. ScriptableObject로 생성.
/// Assets 폴더에서 우클릭 -> Create -> Shinobi/SkillData 로 생성 가능.
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "Shinobi/SkillData")]
public class SkillData : ScriptableObject
{
    [Header("기본 정보")]
    public string skillName;
    public SkillType skillType;

    [Header("수치")]
    public float damage = 100f;
    public float chakraCost = 50f;
    public float cooldown = 5f;
    public float range = 5f;
    public float attackRadius = 2f;

    [Header("애니메이션")]
    public string animationTrigger = "Attack";

    [Header("이펙트")]
    public GameObject effectPrefab;
    public float effectDuration = 2f;
}

public enum SkillType
{
    Melee,
    Ranged
}
