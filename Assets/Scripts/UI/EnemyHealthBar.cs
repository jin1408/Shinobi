using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 머리 위 체력바 - 자동 생성.
/// 적 오브젝트(EnemyStats가 있는)에 직접 부착하면 된다.
/// 수동 Canvas 설정 불필요.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("체력바 설정")]
    public float barWidth = 1.2f;
    public float barHeight = 0.15f;
    public float heightOffset = 2.2f;

    private EnemyStats enemyStats;
    private Camera mainCam;
    private Transform barTransform;
    private RectTransform hpFillRect;
    private GameObject canvasObj;

    void Start()
    {
        enemyStats = GetComponent<EnemyStats>();
        mainCam = Camera.main;

        if (enemyStats == null)
        {
            Debug.LogWarning("EnemyHealthBar: EnemyStats가 없습니다.", gameObject);
            return;
        }

        CreateHealthBar();
    }

    void CreateHealthBar()
    {
        // World Space Canvas
        canvasObj = new GameObject("HP_Canvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0, heightOffset, 0);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth * 100f, barHeight * 100f);
        canvasRect.localScale = Vector3.one * 0.01f;

        barTransform = canvasObj.transform;

        // 배경 (회색)
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

        // HP Fill (빨간색)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        hpFillRect = fillObj.AddComponent<RectTransform>();
        hpFillRect.anchorMin = Vector2.zero;
        hpFillRect.anchorMax = Vector2.one;
        hpFillRect.offsetMin = Vector2.zero;
        hpFillRect.offsetMax = Vector2.zero;
        hpFillRect.pivot = new Vector2(0, 0.5f);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.2f, 0.2f, 1f);
    }

    void Update()
    {
        if (enemyStats == null || hpFillRect == null) return;

        // 사망 시 체력바 숨기기
        if (enemyStats.isDead)
        {
            if (canvasObj != null) canvasObj.SetActive(false);
            return;
        }

        // HP 비율 반영 (0~1 클램프)
        float ratio = Mathf.Clamp01(enemyStats.currentHP / enemyStats.maxHP);
        hpFillRect.localScale = new Vector3(ratio, 1, 1);

        // 카메라를 향해 빌보드
        if (mainCam != null && barTransform != null)
            barTransform.rotation = Quaternion.LookRotation(barTransform.position - mainCam.transform.position);
    }
}
