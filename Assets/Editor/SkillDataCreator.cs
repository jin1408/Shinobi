#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// SkillData ScriptableObject 4종을 자동 생성하는 에디터 도구.
/// Unity 메뉴: Shinobi > Create Default Skills
/// </summary>
public class SkillDataCreator
{
    [MenuItem("Shinobi/Create Default Skills")]
    public static void CreateDefaultSkills()
    {
        string folder = "Assets/Scripts/Skill/Data";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Scripts/Skill", "Data");
        }

        // 스킬 1: 우클릭 - 빠른 참격 (hit01)
        CreateSkill(folder, "Skill_QuickSlash", new SkillConfig
        {
            skillName = "빠른 참격",
            skillType = SkillType.Melee,
            damage = 80f,
            chakraCost = 30f,
            cooldown = 2f,
            range = 3f,
            attackRadius = 2f,
            animTrigger = "hit01"
        });

        // 스킬 2: 양클릭 - 강타 (hit02)
        CreateSkill(folder, "Skill_HeavyStrike", new SkillConfig
        {
            skillName = "강타",
            skillType = SkillType.Melee,
            damage = 180f,
            chakraCost = 70f,
            cooldown = 5f,
            range = 3.5f,
            attackRadius = 2.5f,
            animTrigger = "hit02"
        });

        // 스킬 3: E키 - 광역 참격 (hit03)
        CreateSkill(folder, "Skill_WideSlash", new SkillConfig
        {
            skillName = "광역 참격",
            skillType = SkillType.Melee,
            damage = 120f,
            chakraCost = 50f,
            cooldown = 4f,
            range = 4f,
            attackRadius = 3.5f,
            animTrigger = "hit03"
        });

        // 스킬 4: R키 - 풍둔 원거리 (hit01 모션 + 원거리 판정)
        CreateSkill(folder, "Skill_WindBlast", new SkillConfig
        {
            skillName = "풍둔 - 진공파",
            skillType = SkillType.Ranged,
            damage = 150f,
            chakraCost = 80f,
            cooldown = 8f,
            range = 10f,
            attackRadius = 2f,
            animTrigger = "hit01"
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SkillDataCreator] 기본 스킬 4종 생성 완료! 경로: " + folder);
    }

    struct SkillConfig
    {
        public string skillName;
        public SkillType skillType;
        public float damage;
        public float chakraCost;
        public float cooldown;
        public float range;
        public float attackRadius;
        public string animTrigger;
    }

    static void CreateSkill(string folder, string fileName, SkillConfig config)
    {
        string path = $"{folder}/{fileName}.asset";

        // 이미 존재하면 건너뛰기
        if (AssetDatabase.LoadAssetAtPath<SkillData>(path) != null)
        {
            Debug.Log($"[SkillDataCreator] 이미 존재: {path}");
            return;
        }

        SkillData skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillName = config.skillName;
        skill.skillType = config.skillType;
        skill.damage = config.damage;
        skill.chakraCost = config.chakraCost;
        skill.cooldown = config.cooldown;
        skill.range = config.range;
        skill.attackRadius = config.attackRadius;
        skill.animationTrigger = config.animTrigger;

        AssetDatabase.CreateAsset(skill, path);
        Debug.Log($"[SkillDataCreator] 생성: {config.skillName} ({path})");
    }
}
#endif
